// Copyright 2023-2026 Antonello Provenzano
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Kista {
	/// <summary>
	/// A repository that uses the memory of the process to store
	/// the entities.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of entity managed by the repository.
	/// </typeparam>
	/// <typeparam name="TKey">
	/// The type of the key of the entity managed by the repository.
	/// </typeparam>
	/// <remarks>
	/// <para>
	/// <strong>Thread safety:</strong> This class is safe for concurrent use by
	/// multiple threads.  All read operations (
	/// <see cref="FindAsync"/>, <see cref="FindFirstAsync"/>,
	/// <see cref="FindAllAsync"/>, <see cref="ExistsAsync"/>,
	/// <see cref="CountAsync"/>, <see cref="GetPageAsync"/>,
	/// <see cref="Entities"/>) acquire a shared read lock so that they can
	/// execute in parallel.  All write operations (
	/// <see cref="AddAsync"/>, <see cref="AddRangeAsync"/>,
	/// <see cref="UpdateAsync"/>, <see cref="RemoveAsync"/>,
	/// <see cref="RemoveRangeAsync"/>) acquire an exclusive write lock.
	/// </para>
	/// <para>
	/// The reflection-based key-member discovery is performed exactly once per
	/// generic instantiation via static <see cref="Lazy{T}"/> guards, so it is
	/// safe regardless of how many threads call it simultaneously and the
	/// compiled delegates require no further reflection at runtime.
	/// </para>
	/// </remarks>
	public class InMemoryRepository<TEntity, TKey> :
		Repository<TEntity, TKey>,
		ITrackingRepository<TEntity, TKey>,
		IDisposable
		where TEntity : class
		where TKey : notnull {

		private SortedList<TKey, Entry> entities;
		private bool disposedValue;

		private const string TheEntityDoesNotHaveAnId = "The entity does not have an ID";

		/// <summary>
		/// Guards <see cref="entities"/> for concurrent access.
		/// Multiple readers are allowed to run simultaneously;
		/// any writer holds exclusive access.
		/// </summary>
		private readonly ReaderWriterLockSlim _lock =
			new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

		// ------------------------------------------------------------------
		// Generation-based snapshot cache for the Entities property.
		//
		// _version is incremented inside the write lock after every successful
		// mutation.  The Entities getter checks the cached version under the
		// read lock; when it matches it returns the cached list without any
		// additional allocation.  Because object-reference writes are atomic
		// on .NET, multiple concurrent readers may each build their own
		// SnapshotCache and race to publish it — the last writer wins, but
		// all candidates are semantically identical (same version, same data).
		// ------------------------------------------------------------------

		/// <summary>
		/// Incremented inside the write lock after each successful mutation.
		/// Read inside the read lock from <see cref="Entities"/>.
		/// </summary>
		private int _version = 0;

		/// <summary>
		/// Latest materialised snapshot.  Written and read as a single atomic
		/// object reference (volatile) so readers never observe a partially
		/// written value.
		/// </summary>
		private volatile SnapshotCache? _snapshotCache;

		// ------------------------------------------------------------------
		// Static, per-generic-instantiation reflection cache.
		// Each field is initialised exactly once, on first access, regardless
		// of the number of concurrent callers.
		// ------------------------------------------------------------------

		/// <summary>
		/// Thread-safe, lazily-initialised cache of the <see cref="MemberInfo"/>
		/// decorated with <see cref="KeyAttribute"/> on <typeparamref name="TEntity"/>.
		/// Static so the reflection walk happens at most once per closed generic type.
		/// </summary>
		private static readonly Lazy<MemberInfo?> _idMember =
			new Lazy<MemberInfo?>(() =>
                    typeof(TEntity)
                        .GetMembers()
                        .FirstOrDefault(x => Attribute.IsDefined(x, typeof(KeyAttribute))),
				LazyThreadSafetyMode.ExecutionAndPublication);

		/// <summary>
		/// Compiled delegate that reads the key property/field without reflection
		/// overhead at call time.
		/// </summary>
		private static readonly Lazy<Func<TEntity, TKey?>?> _keyGetter =
			new Lazy<Func<TEntity, TKey?>?>(() => BuildKeyGetter(_idMember.Value),
				LazyThreadSafetyMode.ExecutionAndPublication);

		/// <summary>
		/// Compiled delegate that writes the key property/field without reflection
		/// overhead at call time.
		/// </summary>
		private static readonly Lazy<Action<TEntity, TKey>?> _keySetter =
			new Lazy<Action<TEntity, TKey>?>(() => BuildKeySetter(_idMember.Value),
				LazyThreadSafetyMode.ExecutionAndPublication);

		private static Func<TEntity, TKey?>? BuildKeyGetter(MemberInfo? member) {
			if (member == null) return null;
			var param = Expression.Parameter(typeof(TEntity), "e");
			Expression access = member is PropertyInfo pi
				? Expression.Property(param, pi)
				: Expression.Field(param, (FieldInfo)member);
			var convert = Expression.Convert(access, typeof(TKey?));
			return Expression.Lambda<Func<TEntity, TKey?>>(convert, param).Compile();
		}

		private static Action<TEntity, TKey>? BuildKeySetter(MemberInfo? member) {
			if (member == null) return null;
			var entityParam = Expression.Parameter(typeof(TEntity), "e");
			var valueParam  = Expression.Parameter(typeof(TKey), "v");
			Expression access = member is PropertyInfo pi
				? Expression.Property(entityParam, pi)
				: Expression.Field(entityParam, (FieldInfo)member);
			var assign = Expression.Assign(access, valueParam);
			return Expression.Lambda<Action<TEntity, TKey>>(assign, entityParam, valueParam).Compile();
		}

		private readonly IFieldMapper<TEntity>? fieldMapper;

		/// <summary>
		/// Constructs the repository with the given list of
		/// initial entities.
		/// </summary>
		/// <param name="list">
		/// The list of entities to initialize the repository with.
		/// </param>
		/// <param name="fieldMapper">
		/// A service that maps a field by name to an expression that
		/// can select the field from an entity.
		/// </param>
		/// <param name="services">
		/// An optional service provider used to resolve infrastructure services
		/// such as expression caches for filter optimization.
		/// </param>
		public InMemoryRepository(
			IEnumerable<TEntity>? list = null,
			IFieldMapper<TEntity>? fieldMapper = null,
			IServiceProvider? services = null) {
			entities = CopyList(list ?? Enumerable.Empty<TEntity>());
			this.fieldMapper = fieldMapper;
			Services = services;
		}

		/// <summary>
		/// Destroys the instance of the repository.
		/// </summary>
		~InMemoryRepository() {
			Dispose(disposing: false);
		}

		bool ITrackingRepository<TEntity, TKey>.IsTrackingChanges => true;

	/// <inheritdoc />
	protected override IServiceProvider? Services { get; }

	/// <summary>
	/// Returns the <see cref="IQueryable{T}"/> that backs the entity set
	/// exposed by this repository. The implementation materialises a snapshot under the read
	/// lock so that the returned queryable is safe to iterate after the
	/// lock is released.
	/// </summary>
	/// <returns>
	/// Returns a snapshot <see cref="IQueryable{T}"/> of the entities
	/// currently in the repository.
	/// </returns>
	protected override IQueryable<TEntity> Queryable() => Entities.AsQueryable();

	/// <inheritdoc />
	protected override bool IsQueryable => true;

		/// <summary>
		/// Gets a value indicating whether the entity type managed by this
		/// repository implements <see cref="ISoftDeletable"/>.
		/// </summary>
		protected static bool IsSoftDeletable => typeof(ISoftDeletable).IsAssignableFrom(typeof(TEntity));

		/// <summary>
		/// Applies the soft-delete mode to the given queryable, according
		/// to the provided <see cref="IQueryOptions"/>.
		/// </summary>
		/// <param name="queryable">
		/// The queryable to filter.
		/// </param>
		/// <param name="options">
		/// The query options carrying the soft-delete mode, or <c>null</c>
		/// for the default mode (exclude soft-deleted records).
		/// </param>
		/// <returns>
		/// Returns the queryable filtered according to the soft-delete mode.
		/// When the entity is not <see cref="ISoftDeletable"/>, the queryable
		/// is returned unchanged.
		/// </returns>
		protected virtual IQueryable<TEntity> ApplySoftDeleteMode(IQueryable<TEntity> queryable, IQueryOptions? options) {
			ArgumentNullException.ThrowIfNull(queryable);

			if (!IsSoftDeletable)
				return queryable;

			var mode = options?.SoftDeleteMode ?? SoftDeleteMode.Default;

			return mode switch {
				SoftDeleteMode.Default => queryable.Where(e => !((ISoftDeletable)e).IsDeleted),
				SoftDeleteMode.IncludeDeleted => queryable,
				SoftDeleteMode.OnlyDeleted => queryable.Where(e => ((ISoftDeletable)e).IsDeleted),
				_ => queryable
			};
		}

		/// <summary>
		/// Gets a point-in-time snapshot of all entities in the repository.
		/// </summary>
		/// <remarks>
		/// <para>
		/// The snapshot is taken under a shared read lock; it is safe to call
		/// concurrently with any number of readers or with ongoing writes
		/// (readers will not see a partially-written state).
		/// </para>
		/// <para>
		/// The result is <em>cached</em> per write generation: as long as no
		/// mutation has occurred since the last call the same list instance is
		/// returned without any additional heap allocation.
		/// </para>
		/// </remarks>
		public virtual IReadOnlyList<TEntity> Entities {
			get {
				_lock.EnterReadLock();
				try {
					// Fast path: if the snapshot was built for the current version,
					// return it directly — zero additional allocation.
					var cached = _snapshotCache;
					if (cached != null && cached.Version == _version)
						return cached.Snapshot;

					// Slow path: materialise a fresh snapshot and publish it.
					// List<T> already implements IReadOnlyList<T>; avoid the extra
					// ReadOnlyCollection<T> wrapper allocation from AsReadOnly().
					var values = entities.Values;
					var list = new List<TEntity>(values.Count);
					foreach (var entry in values)
						list.Add(entry.Entity);

					// The volatile write is atomic on all .NET platforms, so
					// concurrent readers racing here are safe — last writer wins
					// but all candidates are identical for the same _version.
					_snapshotCache = new SnapshotCache(_version, list);
					return list;
				} finally {
					_lock.ExitReadLock();
				}
			}
		}

		// Returns a queryable view of the entity collection.
		// Must be called while the read (or write) lock is held.
		private IQueryable<TEntity> GetEntityQueryable() =>
			entities.Values.Select(x => x.Entity).AsQueryable();

		private SortedList<TKey, Entry> CopyList(IEnumerable<TEntity> source) {
			var result = new SortedList<TKey, Entry>();
			foreach (var item in source) {
				var id = GetEntityId(item);
				if (EqualityComparer<TKey>.Default.Equals(id, default))
					throw new RepositoryException(TheEntityDoesNotHaveAnId);

				result.Add(id, new Entry(item));
			}

			return result;
		}

		/// <inheritdoc/>
		protected override TKey? GetEntityKey(TEntity entity) {
			ArgumentNullException.ThrowIfNull(entity);

			return GetEntityId(entity);
		}

		private void SetEntityId(TEntity entity, TKey value) {
			var setter = _keySetter.Value;
			if (setter == null)
				throw new RepositoryException(TheEntityDoesNotHaveAnId);
			setter(entity, value);
		}

		private TKey? GetEntityId(TEntity entity) {
			var getter = _keyGetter.Value;
			return getter == null ? default(TKey?) : getter(entity);
		}

		private TKey GenerateNewKey() {
			// make this generator configurable ...
			if (typeof(TKey) == typeof(Guid))
				return (TKey)(object)(Guid.NewGuid());
			if (typeof(TKey) == typeof(string))
				return (TKey)(object)Guid.NewGuid().ToString();

			throw new NotSupportedException($"The key type {typeof(TKey)} is not supported");
		}

		/// <inheritdoc/>
		protected override ValueTask<long> CountAsync(IQueryFilter? filter, IQueryOptions? options, CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();

			try {
				InitializeFilter(filter);
				_lock.EnterReadLock();
				try {
					var queryable = ApplySoftDeleteMode(GetEntityQueryable(), options);
					var result = queryable.LongCount(filter);
					return new ValueTask<long>(result);
				} finally {
					_lock.ExitReadLock();
				}
			} catch (Exception ex) when (ex is not RepositoryException) {
				throw new RepositoryException("Could not count the entities", ex);
			}
		}

		/// <summary>
		/// Adds a single entity to the repository.
		/// </summary>
		/// <remarks>
		/// This method acquires an exclusive write lock before mutating the
		/// internal store, so it is safe to call concurrently from multiple threads.
		/// </remarks>
		/// <inheritdoc/>
		public override ValueTask AddAsync(TEntity entity, CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(entity);

			cancellationToken.ThrowIfCancellationRequested();

			try {
				var key = GetEntityId(entity);
				if (key == null)
				{
					key = GenerateNewKey();
					SetEntityId(entity, key);
				}

				_lock.EnterWriteLock();
				try {
					if (!entities.TryAdd(key, new Entry(entity)))
						throw new RepositoryException("An entity with the same ID already exists in the repository");
					_version++;
				} finally {
					_lock.ExitWriteLock();
				}

				return ValueTask.CompletedTask;
			} catch (RepositoryException) {
				throw;
			} catch (Exception ex) {
				throw new RepositoryException("Could not create the entity", ex);
			}
		}

		/// <summary>
		/// Adds a range of entities to the repository.
		/// </summary>
		/// <remarks>
		/// This method acquires an exclusive write lock before mutating the
		/// internal store, so it is safe to call concurrently from multiple threads.
		/// </remarks>
		/// <inheritdoc/>
		public override ValueTask AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(entities);

			cancellationToken.ThrowIfCancellationRequested();

			try {
				// Assign keys outside the lock so key generation doesn't block readers.
				// Consistent with AddAsync: only generate a key when the entity has none.
				var items = entities.Select(item => {
					var key = GetEntityId(item);
					if (key == null) {
						key = GenerateNewKey();
						SetEntityId(item, key);
					}
					return (key, item);
				}).ToList();

				_lock.EnterWriteLock();
				try {
					foreach (var (key, item) in items) {
						this.entities.Add(key, new Entry(item));
					}
					_version++;
				} finally {
					_lock.ExitWriteLock();
				}

				return ValueTask.CompletedTask;
			} catch (RepositoryException) {
				throw;
			} catch (Exception ex) {
				throw new RepositoryException("Could not add the entities to the repository", ex);
			}
		}

		/// <summary>
		/// Removes a single entity from the repository, or marks it as
		/// soft-deleted when the entity implements <see cref="ISoftDeletable"/>.
		/// </summary>
		/// <remarks>
		/// This method acquires an exclusive write lock before mutating the
		/// internal store, so it is safe to call concurrently from multiple threads.
		/// </remarks>
		/// <inheritdoc/>
		public override ValueTask<bool> RemoveAsync(TEntity entity, CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(entity);

			cancellationToken.ThrowIfCancellationRequested();

			if (entity is ISoftDeletable softDeletable)
				return SoftDeleteAsync(entity, softDeletable, cancellationToken);

			return HardDeleteAsync(entity, cancellationToken);
		}

		/// <summary>
		/// Marks the given entity as soft-deleted by setting its
		/// <see cref="ISoftDeletable.IsDeleted"/> flag and
		/// <see cref="ISoftDeletable.DeletedAtUtc"/> timestamp, then
		/// persists the change through the in-memory store.
		/// </summary>
		/// <remarks>
		/// The caller may pre-set <see cref="ISoftDeletable.DeletedBy"/>
		/// on the entity before calling <see cref="RemoveAsync"/> to
		/// attribute the deletion to an actor for audit purposes: the
		/// driver preserves and persists any value already set on the
		/// entity. When soft-deleting through <c>EntityManager</c>,
		/// the <c>DeletedBy</c> stamp is resolved from the registered
		/// <see cref="IUserAccessor{TKey}"/> and set before this method
		/// is reached.
		/// </remarks>
		/// <param name="entity">
		/// The entity instance to soft-delete.
		/// </param>
		/// <param name="softDeletable">
		/// The <see cref="ISoftDeletable"/> view of the same entity.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns <c>true</c> if the entity was successfully soft-deleted,
		/// otherwise <c>false</c>.
		/// </returns>
		protected virtual async ValueTask<bool> SoftDeleteAsync(TEntity entity, ISoftDeletable softDeletable, CancellationToken cancellationToken) {
			cancellationToken.ThrowIfCancellationRequested();

			try {
				var entityId = GetEntityId(entity);
				if (EqualityComparer<TKey>.Default.Equals(entityId, default))
					return false;

				_lock.EnterWriteLock();
				try {
					if (!entities.TryGetValue(entityId, out var entry))
						return false;

					if (((ISoftDeletable)entry.Entity).IsDeleted)
						return false;

					softDeletable.IsDeleted = true;
					softDeletable.DeletedAtUtc = ResolveSystemTime().UtcNow;

					entry.Update(entity);
					_version++;
					return true;
				} finally {
					_lock.ExitWriteLock();
				}
			} catch (RepositoryException) {
				throw;
			} catch (Exception ex) {
				throw new RepositoryException("Could not soft-delete the entity", ex);
			}
		}

		/// <inheritdoc/>
		public override ValueTask<bool> HardDeleteAsync(TEntity entity, CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(entity);

			cancellationToken.ThrowIfCancellationRequested();

			try {
				var entityId = GetEntityId(entity);
				if (entityId == null)
					return new ValueTask<bool>(false);

				_lock.EnterWriteLock();
				try {
					var removed = this.entities.Remove(entityId);
					if (removed) _version++;
					return new ValueTask<bool>(removed);
				} finally {
					_lock.ExitWriteLock();
				}
			} catch (RepositoryException) {
				throw;
			} catch (Exception ex) {
				throw new RepositoryException("Could not delete the entity", ex);
			}
		}

		/// <summary>
		/// Removes a range of entities from the repository, or marks them
		/// as soft-deleted when the entities implement
		/// <see cref="ISoftDeletable"/>.
		/// </summary>
		/// <remarks>
		/// This method acquires an exclusive write lock before mutating the
		/// internal store, so it is safe to call concurrently from multiple threads.
		/// </remarks>
		/// <inheritdoc/>
		public override ValueTask RemoveRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();

			ArgumentNullException.ThrowIfNull(entities);

			if (IsSoftDeletable)
				return SoftDeleteRangeAsync(entities, cancellationToken);

			return HardDeleteRangeAsync(entities, cancellationToken);
		}

		/// <summary>
		/// Marks the given entities as soft-deleted and persists the
		/// changes through the in-memory store.
		/// </summary>
		/// <param name="entities">
		/// The entities to soft-delete.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		protected virtual async ValueTask SoftDeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken) {
			cancellationToken.ThrowIfCancellationRequested();

			try {
				var now = ResolveSystemTime().UtcNow;
				var toUpdate = entities.ToList();

				var ids = toUpdate.Select(entity => {
					var id = GetEntityId(entity);
					if (EqualityComparer<TKey>.Default.Equals(id, default))
						throw new RepositoryException(TheEntityDoesNotHaveAnId);
					return id;
				}).ToList();

				_lock.EnterWriteLock();
				try {
					if (ids.Any(id => !this.entities.ContainsKey(id)))
						throw new RepositoryException("The entity is not in the repository");

					foreach (var entity in toUpdate) {
						if (entity is ISoftDeletable softDeletable) {
							softDeletable.IsDeleted = true;
							softDeletable.DeletedAtUtc = now;
						}

						var id = GetEntityId(entity)!;
						this.entities[id].Update(entity);
					}

					_version++;
				} finally {
					_lock.ExitWriteLock();
				}

				await Task.CompletedTask;
			} catch (RepositoryException) {
				throw;
			} catch (Exception ex) {
				throw new RepositoryException("Could not soft-delete the entities", ex);
			}
		}

		/// <inheritdoc/>
		public override ValueTask HardDeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();

			ArgumentNullException.ThrowIfNull(entities);

			try {
				var toRemove = entities.ToList();

				// Resolve all IDs before acquiring the lock
				var ids = toRemove.Select(entity => {
					var id = GetEntityId(entity);
					if (EqualityComparer<TKey>.Default.Equals(id, default))
						throw new RepositoryException(TheEntityDoesNotHaveAnId);
					return id;
				}).ToList();

			_lock.EnterWriteLock();
			try {
				// verify all entities exist before removing any
				foreach (var id in ids) {
						if (!this.entities.ContainsKey(id))
							throw new RepositoryException("The entity is not in the repository");
					}

					foreach (var id in ids) {
						if (!this.entities.Remove(id))
							throw new RepositoryException("The entity was not removed from the repository");
					}
					_version++;
				} finally {
					_lock.ExitWriteLock();
				}

				return ValueTask.CompletedTask;
			} catch (RepositoryException) {
				throw;
			} catch (Exception ex) {
				throw new RepositoryException("Could not delete the entities", ex);
			}
		}

		/// <summary>
		/// Resolves the <see cref="ISystemTime"/> service used to stamp
		/// soft-deletion timestamps.
		/// </summary>
		/// <returns>
		/// Returns an <see cref="ISystemTime"/> instance.
		/// </returns>
		protected virtual ISystemTime ResolveSystemTime() {
			return Services?.GetService(typeof(ISystemTime)) as ISystemTime ?? SystemTime.Default;
		}

		/// <inheritdoc/>
		protected override ValueTask<bool> ExistsAsync(IQueryFilter? filter, IQueryOptions? options, CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();

			try {
				InitializeFilter(filter);
				_lock.EnterReadLock();
				try {
					var queryable = ApplySoftDeleteMode(GetEntityQueryable(), options);
					var result = queryable.Any(filter);
					return new ValueTask<bool>(result);
				} finally {
					_lock.ExitReadLock();
				}
			} catch (Exception ex) when (ex is not RepositoryException) {
				throw new RepositoryException("Could not check if any entities exist in the repository", ex);
			}
		}


		/// <inheritdoc/>
		protected override ValueTask<IReadOnlyList<TEntity>> FindAllAsync(IQuery query, CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();

			try {
				InitializeFilter(query.Filter);
				_lock.EnterReadLock();
				try {
					var queryable = ApplySoftDeleteMode(GetEntityQueryable(), query.Options);
					var result = query.Apply(queryable).ToList();
					return new ValueTask<IReadOnlyList<TEntity>>(result);
				} finally {
					_lock.ExitReadLock();
				}
			} catch (Exception ex) when (ex is not RepositoryException) {
				throw new RepositoryException("Error while trying to find all the entities in the repository matching the filter", ex);
			}
		}

		/// <inheritdoc/>
		protected override ValueTask<TEntity?> FindFirstAsync(IQuery query, CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();

			try {
				InitializeFilter(query.Filter);
				_lock.EnterReadLock();
				try {
					var queryable = ApplySoftDeleteMode(GetEntityQueryable(), query.Options);
					var result = query.Apply(queryable).FirstOrDefault();
					return new ValueTask<TEntity?>(result);
				} finally {
					_lock.ExitReadLock();
				}
			} catch (Exception ex) when (ex is not RepositoryException) {
				throw new RepositoryException("Error while searching for any entities in the repository matching the filter", ex);
			}
		}

		/// <inheritdoc/>
		public ValueTask<TEntity?> FindOriginalAsync(TKey key, CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(key);
			cancellationToken.ThrowIfCancellationRequested();

			try {
				_lock.EnterReadLock();
				try {
					if (!entities.TryGetValue(key, out var entry))
						return new ValueTask<TEntity?>((TEntity?)null);

					return new ValueTask<TEntity?>(entry.Original);
				} finally {
					_lock.ExitReadLock();
				}
			} catch (Exception ex) when (ex is not RepositoryException) {
				throw new RepositoryException("Error while searching any entities with the given ID", ex);
			}
		}

		/// <inheritdoc/>
		public override ValueTask<TEntity?> FindAsync(TKey key, CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(key);

			cancellationToken.ThrowIfCancellationRequested();

			try {
				_lock.EnterReadLock();
				try {
					if (!entities.TryGetValue(key, out var entity))
						return new ValueTask<TEntity?>((TEntity?)null);

					if (entity.Entity is ISoftDeletable softDeletable && softDeletable.IsDeleted)
						return new ValueTask<TEntity?>((TEntity?)null);

					return new ValueTask<TEntity?>(entity.Entity);
				} finally {
					_lock.ExitReadLock();
				}
			} catch (Exception ex) when (ex is not RepositoryException) {
				throw new RepositoryException("Error while searching any entities with the given ID", ex);
			}
		}

		/// <summary>
		/// Maps the given field name to an expression that can select
		/// a field from an entity.
		/// </summary>
		/// <param name="fieldName">
		/// The name of the field to map.
		/// </param>
		/// <returns>
		/// Returns an expression that can select the field from an entity.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown if the mapping is not supported by the repository.
		/// </exception>
		protected virtual Expression<Func<TEntity, object?>> MapField(string fieldName) {
			if (fieldMapper == null)
				throw new NotSupportedException("No field mapper was provided");

			return fieldMapper.MapField(fieldName);
		}

		/// <inheritdoc/>
		public override async ValueTask<PageResult<TEntity>> GetPageAsync(PageRequest request, CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(request);
			cancellationToken.ThrowIfCancellationRequested();

			if (request is PageQuery<TEntity> pageQuery) {
				cancellationToken.ThrowIfCancellationRequested();

			try {
				InitializeFilter(((IQuery)pageQuery).Filter);
				_lock.EnterReadLock();
				try {
					var queryable = ApplySoftDeleteMode(GetEntityQueryable(), pageQuery.Options);
					var entitySet = pageQuery.ApplyQuery(queryable);
					var itemCount = entitySet.Count();
					var items = entitySet
						.Skip(request.Offset)
						.Take(request.Size)
						.ToList();

					return new PageQueryResult<TEntity>(pageQuery, itemCount, items);
				} finally {
					_lock.ExitReadLock();
				}
			} catch (Exception ex) when (ex is not RepositoryException) {
				throw new RepositoryException("Unable to retrieve the pages", ex);
			}
		}

			try {
				_lock.EnterReadLock();
				try {
					var entitySet = ApplySoftDeleteMode(GetEntityQueryable(), null);
					var itemCount = entitySet.Count();
					var items = entitySet
						.Skip(request.Offset)
						.Take(request.Size)
						.ToList();

					return new PageResult<TEntity>(request, itemCount, items);
				} finally {
					_lock.ExitReadLock();
				}
			} catch (Exception ex) when (ex is not RepositoryException) {
				throw new RepositoryException("Unable to retrieve the page", ex);
			}
		}

		/// <summary>
		/// Updates an existing entity in the repository.
		/// </summary>
		/// <remarks>
		/// This method acquires an exclusive write lock before mutating the
		/// internal store, so it is safe to call concurrently from multiple threads.
		/// </remarks>
		/// <inheritdoc/>
		public override ValueTask<bool> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();

			ArgumentNullException.ThrowIfNull(entity);

			try {
				var entityId = GetEntityId(entity);
				if (entityId == null)
					return new ValueTask<bool>(false);

				_lock.EnterWriteLock();
				try {
					if (!entities.TryGetValue(entityId, out var entry))
						return new ValueTask<bool>(false);

					entry.Update(entity);
					_version++;
					return new ValueTask<bool>(true);
				} finally {
					_lock.ExitWriteLock();
				}
			} catch (Exception ex) when (ex is not RepositoryException) {
				throw new RepositoryException("Unable to update the entity", ex);
			}
		}

		/// <summary>
		/// Disposes the repository and releases all the resources.
		/// </summary>
		/// <param name="disposing">
		/// The flag indicating if the repository is disposing.
		/// </param>
		protected virtual void Dispose(bool disposing) {
			if (!disposedValue) {
				if (disposing) {
					_lock.EnterWriteLock();
					try {
						entities.Clear();
						_version++;
					} finally {
						_lock.ExitWriteLock();
					}
					_snapshotCache = null;
					_lock.Dispose();
				}

				entities = null!;
				disposedValue = true;
			}
		}

		/// <inheritdoc/>
		public void Dispose() {
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Immutable snapshot holder published atomically via a volatile reference.
		/// Multiple concurrent readers may each build a candidate with the same
		/// <see cref="Version"/> and <see cref="Snapshot"/>; the last volatile
		/// store wins, but all candidates are semantically equivalent.
		/// </summary>
		private sealed class SnapshotCache {
			/// <summary>
			/// Gets the version number of the snapshot.
			/// </summary>
			public readonly int Version;
			/// <summary>
			/// Gets the immutable snapshot of entities.
			/// </summary>
			public readonly IReadOnlyList<TEntity> Snapshot;
			public SnapshotCache(int version, IReadOnlyList<TEntity> snapshot) {
				Version  = version;
				Snapshot = snapshot;
			}
		}

		class Entry {
			/// <summary>
			/// Compiled shallow-clone delegate built once per closed generic type.
			/// Avoids the per-call overhead of <c>GetMethod</c> + <c>Invoke</c> +
			/// <c>new object[0]</c> that the pure-reflection version incurred.
			/// </summary>
			private static readonly Func<TEntity, TEntity> _cloner = BuildCloner();

		private static Func<TEntity, TEntity> BuildCloner() {
#pragma warning disable S3011 // Reflection is used intentionally to create a shallow clone via MemberwiseClone
			var method = typeof(TEntity).GetMethod(
				"MemberwiseClone",
				BindingFlags.Instance | BindingFlags.NonPublic)!;
#pragma warning restore S3011
			var param = Expression.Parameter(typeof(TEntity), "e");
			var call  = Expression.Convert(Expression.Call(param, method), typeof(TEntity));
			return Expression.Lambda<Func<TEntity, TEntity>>(call, param).Compile();
		}

			public Entry(TEntity entity) {
				Entity   = entity;
				Original = _cloner(entity);
			}

			/// <summary>
			/// Gets the original entity state at the time of entry creation or last update.
			/// </summary>
			public TEntity Original { get; private set; }

			/// <summary>
			/// Gets the current entity.
			/// </summary>
			public TEntity Entity { get; private set; }

			/// <summary>
			/// Updates the entry with a new entity, preserving a clone as the original.
			/// </summary>
			/// <param name="entity">The new entity.</param>
			public void Update(TEntity entity) {
				Original = _cloner(entity);
				Entity   = entity;
			}
		}
	}
}
