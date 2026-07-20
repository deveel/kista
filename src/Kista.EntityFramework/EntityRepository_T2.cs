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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using System.Globalization;

namespace Kista
{
	/// <summary>
	/// A repository that uses an <see cref="DbContext"/> to access the data
	/// of the entities.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of the entity managed by the repository.
	/// </typeparam>
	/// <typeparam name="TKey">
	/// The type of the key of the entity managed by the repository.
	/// </typeparam>
	public class EntityRepository<TEntity, TKey> :
		Repository<TEntity, TKey>,
		ITrackingRepository<TEntity, TKey>,
		IDisposable
		where TEntity : class {
		private bool disposedValue;

	/// <summary>
	/// Constructs the repository using the given <see cref="DbContext"/>.
	/// </summary>
	/// <param name="context">
	/// The <see cref="DbContext"/> used to access the data of the entities.
	/// </param>
	/// <param name="services">
	/// An optional service provider used to resolve infrastructure services
	/// such as expression caches for filter initialization.
	/// </param>
	/// <param name="logger">
	/// A logger used to log the operations of the repository.
	/// </param>
	public EntityRepository(DbContext context, IServiceProvider? services = null, ILogger<EntityRepository<TEntity, TKey>>? logger = null) {
		Context = context ?? throw new ArgumentNullException(nameof(context));
		Services = services;
		Logger = logger ?? NullLogger<EntityRepository<TEntity, TKey>>.Instance;

			var entityKey = Context.Model.FindEntityType(typeof(TEntity))?.FindPrimaryKey();

			if (entityKey == null)
				throw new RepositoryException($"The model of the entity '{typeof(TEntity)}' has no primary key configured");
			if (entityKey.Properties.Count > 1)
				throw new NotSupportedException("The repository does not support entities with composite primary keys");

			PrimaryKey = entityKey;
		}

		/// <summary>
		/// The destructor of the repository.
		/// </summary>
		~EntityRepository() {
			Dispose(disposing: false);
		}

		/// <summary>
		/// Gets the instance of the <see cref="DbContext"/> used by the repository.
		/// </summary>
		protected DbContext Context { get; private set; }

		/// <inheritdoc />
		protected override IServiceProvider? Services { get; }

		/// <summary>
		/// Gets a reference to the primary key of the entity.
		/// </summary>
		protected IKey PrimaryKey { get; }

		/// <summary>
		/// Gets the logger used by the repository.
		/// </summary>
		protected ILogger Logger { get; }

		/// <summary>
		/// Gets the <see cref="DbSet{TEntity}"/> used by the repository to access the data.
		/// </summary>
		protected virtual DbSet<TEntity> Entities => Context.Set<TEntity>();

		/// <summary>
		/// Returns the <see cref="IQueryable{T}"/> that backs the entity set
		/// exposed by this repository. The hatch is intentionally
		/// <c>protected</c>: the LINQ provider must not leak to consumer
		/// code.
		/// </summary>
		/// <returns>
		/// Returns the <see cref="IQueryable{T}"/> produced by
		/// <see cref="Entities"/>.AsQueryable().
		/// </returns>
		protected override IQueryable<TEntity> Queryable() => Entities.AsQueryable();

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
		/// <remarks>
		/// <para>
		/// Overrides the base in-memory filter: EF Core relies on the
		/// <c>HasQueryFilter</c> convention registered through
		/// <see cref="SoftDeleteModelBuilderExtensions.HasSoftDeleteFilter{TEntity}(EntityTypeBuilder{TEntity})"/>
		/// for <see cref="SoftDeleteMode.Default"/>, so no extra filter is
		/// applied here; <see cref="SoftDeleteMode.IncludeDeleted"/> and
		/// <see cref="SoftDeleteMode.OnlyDeleted"/> call
		/// <c>IgnoreQueryFilters()</c> to surface soft-deleted records.
		/// </para>
		/// </remarks>
		protected override IQueryable<TEntity> ApplySoftDeleteMode(IQueryable<TEntity> queryable, IQueryOptions? options) {
			ArgumentNullException.ThrowIfNull(queryable);

			if (!IsSoftDeletable)
				return queryable;

			var mode = options?.SoftDeleteMode ?? SoftDeleteMode.Default;

			return mode switch {
				SoftDeleteMode.Default => queryable,
				SoftDeleteMode.IncludeDeleted => queryable.IgnoreQueryFilters(),
				SoftDeleteMode.OnlyDeleted => queryable.IgnoreQueryFilters().Where(e => ((ISoftDeletable)e).IsDeleted),
				_ => queryable
			};
		}

		/// <inheritdoc />
		protected override bool IsQueryable => true;

		/// <summary>
		/// Applies the Entity Framework-specific query normalisation
		/// (collapsing redundant sub-expressions) to the given queryable
		/// before materialisation.
		/// </summary>
		/// <param name="queryable">
		/// The queryable produced by <see cref="Repository{TEntity,TKey}.Queryable"/>.
		/// </param>
		/// <returns>
		/// Returns the normalised queryable.
		/// </returns>
		protected override IQueryable<TEntity> NormalizeQuery(IQueryable<TEntity> queryable) {
			ArgumentNullException.ThrowIfNull(queryable);
			return EfQueryNormalizer.Normalize(queryable);
		}

		/// <summary>
		/// Asynchronously counts the entities in the given queryable through
		/// the Entity Framework provider.
		/// </summary>
		/// <param name="queryable">
		/// The queryable to count.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns the number of entities matched by the queryable.
		/// </returns>
		protected override ValueTask<long> CountAsync(IQueryable<TEntity> queryable, CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(queryable);
			return new ValueTask<long>(queryable.LongCountAsync(cancellationToken));
		}

		/// <summary>
		/// Asynchronously materialises the given queryable through the
		/// Entity Framework provider.
		/// </summary>
		/// <param name="queryable">
		/// The queryable to materialise.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns the read-only list of entities matched by the queryable.
		/// </returns>
		protected override async ValueTask<IReadOnlyList<TEntity>> ToListAsync(IQueryable<TEntity> queryable, CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(queryable);
			return await queryable.ToListAsync(cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Gets a value indicating if the repository is tracking the changes
		/// to the entities returned by the queries.
		/// </summary>
		protected bool IsTrackingChanges => Entities.Local != null ||
			Context.ChangeTracker.QueryTrackingBehavior != QueryTrackingBehavior.NoTracking;

		bool ITrackingRepository<TEntity, TKey>.IsTrackingChanges => IsTrackingChanges;

		/// <summary>
		/// Assesses if the repository has been disposed.
		/// </summary>
		/// <exception cref="ObjectDisposedException">
		/// Thrown when the repository has been disposed.
		/// </exception>
		protected void ThrowIfDisposed() {
			DisposableHelper.ThrowIfDisposed(disposedValue, GetType().Name);
		}

		/// <summary>
		/// Converts the given value to the type of the 
		/// primary key of the entity.
		/// </summary>
		/// <param name="key">
		/// The key that represents the identifier of the entity.
		/// </param>
		/// <returns>
		/// Returns the identifier converted to the type of the primary key
		/// of the entity.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown when the given string is not a valid identifier for the entity.
		/// </exception>
		protected virtual TKey? ConvertEntityKey(TKey? key) {
			if (KeyHelper.IsNull(key))
				return default(TKey?);

			var keyType = PrimaryKey.GetKeyType();

			if (keyType == null)
				return default(TKey?);

			if (Nullable.GetUnderlyingType(keyType) != null)
				keyType = Nullable.GetUnderlyingType(keyType);

			var valueType = key.GetType();

			// These are the most common types of primary keys in SQL databases
			if (keyType == valueType)
				return key;

			if (key is string s) {
				if (keyType == typeof(Guid) && Guid.TryParse(s, out var guid))
					return (TKey)(object) guid;

				if (typeof(IConvertible).IsAssignableFrom(keyType))
					return (TKey) Convert.ChangeType(key, keyType, CultureInfo.InvariantCulture);
			}

			throw new ArgumentException($"The given key '{key}' is not a valid identifier for the entity '{typeof(TEntity)}'");
		}

		/// <inheritdoc/>
		protected override TKey? GetEntityKey(TEntity entity) {
			ArgumentNullException.ThrowIfNull(entity);

			var props = PrimaryKey.Properties.ToList();
			if (props.Count > 1)
				throw new RepositoryException($"The entity '{typeof(TEntity)}' has more than one property has primary key");

			var getter = props[0].GetGetter();
			return (TKey?) getter.GetClrValue(entity);
		}

		/// <summary>
		/// A method that is invoked when an entity is 
		/// being added to the repository.
		/// </summary>
		/// <param name="entity">
		/// The entity that is being added to the repository.
		/// </param>
		/// <returns>
		/// Returns the entity that will be added to the repository.
		/// </returns>
		protected virtual TEntity OnAddingEntity(TEntity entity)
		{
			return entity;
		}

		/// <inheritdoc/>
		public override async ValueTask AddAsync(TEntity entity, CancellationToken cancellationToken = default) {
			ThrowIfDisposed();

			ArgumentNullException.ThrowIfNull(entity);

			Logger.TraceCreatingEntity(typeof(TEntity));

			try {
				Entities.Add(OnAddingEntity(entity));

				var count = await Context.SaveChangesAsync(cancellationToken);

				if (count > 1) {
					// warn about this...
				}

				var key = GetEntityKey(entity)!;

				Logger.LogEntityCreated(typeof(TEntity), key);
			} catch (Exception ex) {
				Logger.LogUnknownError(ex, typeof(TEntity));
				throw new RepositoryException("Unknown error while trying to add an entity to the repository", ex);
			}
		}

		/// <inheritdoc/>
		public override async ValueTask AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) {
			ThrowIfDisposed();

			try {
				var toAdd = entities.Select(OnAddingEntity).ToList();

				await Entities.AddRangeAsync(toAdd, cancellationToken);

				await Context.SaveChangesAsync(true, cancellationToken);
			} catch (Exception ex) {
				Logger.LogUnknownError(ex, typeof(TEntity));
				throw new RepositoryException("Unknown error while trying to add a range of entities to the repository", ex);
			}
		}

		/// <inheritdoc/>
		public override async ValueTask<bool> RemoveAsync(TEntity entity, CancellationToken cancellationToken = default) {
			ThrowIfDisposed();

			ArgumentNullException.ThrowIfNull(entity);

			if (entity is ISoftDeletable softDeletable)
				return await SoftDeleteAsync(entity, softDeletable, cancellationToken);

			return await HardDeleteAsync(entity, cancellationToken);
		}

		/// <summary>
		/// Marks the given entity as soft-deleted by setting its
		/// <see cref="ISoftDeletable.IsDeleted"/> flag and
		/// <see cref="ISoftDeletable.DeletedAtUtc"/> timestamp, then
		/// persists the change through the EF Core change tracker.
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
			try {
				var entityId = GetEntityKey(entity)!;

				Logger.TraceDeletingEntity(typeof(TEntity), entityId);

				var entry = Context.Entry(entity);
				if (entry.State == EntityState.Detached) {
					entry = ResolveEntryForEntityKey(entity, entityId);
				}

				if (((ISoftDeletable)entry.Entity).IsDeleted)
					return false;

				softDeletable.IsDeleted = true;
				softDeletable.DeletedAtUtc = ResolveSystemTime().UtcNow;

				if (!ReferenceEquals(entry.Entity, entity))
					entry.CurrentValues.SetValues(entity);

				return await PersistDeletionAsync(entity, entityId, entry, EntityState.Modified, "Unable to soft-delete the entity", cancellationToken);
			} catch (DbUpdateConcurrencyException) {
				Logger.WarnEntityNotFound(typeof(TEntity), GetEntityKey(entity)!);
				return false;
			} catch (DbUpdateException ex) {
				Logger.LogUnknownError(ex, typeof(TEntity));
				throw new RepositoryException("Unable to soft-delete the entity", ex);
			}
		}

		/// <inheritdoc/>
		public override async ValueTask<bool> HardDeleteAsync(TEntity entity, CancellationToken cancellationToken = default) {
			ThrowIfDisposed();

			ArgumentNullException.ThrowIfNull(entity);

			try {
				var entityId = GetEntityKey(entity)!;

				Logger.TraceDeletingEntity(typeof(TEntity), entityId);

				var entry = Context.Entry(entity);
				if (entry.State == EntityState.Detached) {
					entry = ResolveEntryForEntityKey(entity, entityId);
				}

				return await PersistDeletionAsync(entity, entityId, entry, EntityState.Deleted, "Unable to delete the entity", cancellationToken);
			} catch (DbUpdateConcurrencyException) {
				Logger.WarnEntityNotFound(typeof(TEntity), GetEntityKey(entity)!);
				return false;
			} catch (RepositoryException) {
				throw;
			} catch (Exception ex) {
				Logger.LogUnknownError(ex, typeof(TEntity));
				throw new RepositoryException("Unable to delete the entity", ex);
			}
		}

		/// <summary>
		/// Persists a deletion (soft or hard) through the EF change tracker
		/// and reports the outcome via the logger, normalising the
		/// <see cref="DbUpdateConcurrencyException"/> and generic-exception
		/// handling shared by <see cref="SoftDeleteAsync"/> and
		/// <see cref="HardDeleteAsync"/>.
		/// </summary>
		/// <param name="entity">
		/// The entity being deleted (used to re-read its key on the
		/// concurrency-exception recovery path).
		/// </param>
		/// <param name="entityId">
		/// The previously-resolved key of <paramref name="entity"/>, used
		/// for logging.
		/// </param>
		/// <param name="entry">
		/// The already-resolved <see cref="EntityEntry{TEntity}"/> whose
		/// <see cref="EntityEntry.State"/> will be set to
		/// <paramref name="state"/>.
		/// </param>
		/// <param name="state">
		/// The target <see cref="EntityState"/> (<see cref="EntityState.Modified"/>
		/// for soft-delete, <see cref="EntityState.Deleted"/> for hard-delete).
		/// </param>
		/// <param name="errorContext">
		/// A human-readable phrase used in the <see cref="RepositoryException"/>
		/// message if persistence fails unexpectedly.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns <c>true</c> if the underlying <c>SaveChangesAsync</c>
		/// reported at least one affected row, otherwise <c>false</c>.
		/// </returns>
		private async ValueTask<bool> PersistDeletionAsync(TEntity entity, object entityId, EntityEntry<TEntity> entry, EntityState state, string errorContext, CancellationToken cancellationToken) {
			entry.State = state;

			var count = await Context.SaveChangesAsync(cancellationToken);
			var deleted = count > 0;

			if (deleted) {
				Logger.LogEntityDeleted(typeof(TEntity), entityId);
			} else {
				Logger.WarnEntityNotDeleted(typeof(TEntity), entityId);
			}

			return deleted;
		}

		/// <inheritdoc/>
		public override async ValueTask RemoveRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) {
			ThrowIfDisposed();

			if (IsSoftDeletable) {
				await SoftDeleteRangeAsync(entities, cancellationToken);
				return;
			}

			await HardDeleteRangeAsync(entities, cancellationToken);
		}

		/// <summary>
		/// Marks the given entities as soft-deleted and persists the
		/// changes through the EF Core change tracker.
		/// </summary>
		/// <param name="entities">
		/// The entities to soft-delete.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		protected virtual async ValueTask SoftDeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken) {
			try {
				var now = ResolveSystemTime().UtcNow;

				foreach (var item in entities) {
					var entityId = GetEntityKey(item);
					if (EqualityComparer<TKey>.Default.Equals(entityId, default))
						throw new RepositoryException("One of the entities has no primary key configured");

					var entry = Context.Entry(item);
					if (entry.State == EntityState.Detached) {
						entry = ResolveEntryForEntityKey(item, entityId);
					}

					if (item is ISoftDeletable softDeletable) {
						softDeletable.IsDeleted = true;
						softDeletable.DeletedAtUtc = now;
					}

					if (!ReferenceEquals(entry.Entity, item))
						entry.CurrentValues.SetValues(item);

					entry.State = EntityState.Modified;
				}

				await Context.SaveChangesAsync(true, cancellationToken);
			} catch (DbUpdateConcurrencyException ex) {
				throw new RepositoryException("One or more entities were not found in the repository", ex);
			} catch (DbUpdateException ex) {
				Logger.LogUnknownError(ex, typeof(TEntity));
				throw new RepositoryException("Unknown error while trying to soft-delete a range of entities from the repository", ex);
			}
		}

		/// <inheritdoc/>
		public override async ValueTask HardDeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) {
			ThrowIfDisposed();

			try {
				foreach (var item in entities) {
					var entityId = GetEntityKey(item);
					if (EqualityComparer<TKey>.Default.Equals(entityId, default))
						throw new RepositoryException("One of the entities has no primary key configured");

					var entry = Context.Entry(item);
					if (entry.State == EntityState.Detached) {
						entry = ResolveEntryForEntityKey(item, entityId);
					}

					entry.State = EntityState.Deleted;
				}

				await Context.SaveChangesAsync(true, cancellationToken);
			} catch (DbUpdateConcurrencyException ex) {
				throw new RepositoryException("One or more entities were not found in the repository", ex);
			} catch (Exception ex) {
				Logger.LogUnknownError(ex, typeof(TEntity));
				throw new RepositoryException("Unknown error while trying to remove a range of entities from the repository", ex);
			}
		}

		/// <summary>
		/// Resolves the <see cref="ISystemTime"/> service used to stamp
		/// soft-deletion timestamps, falling back to the default
		/// implementation when no service provider is available.
		/// </summary>
		/// <returns>
		/// Returns an <see cref="ISystemTime"/> instance.
		/// </returns>
		protected virtual ISystemTime ResolveSystemTime() {
			return Services?.GetService(typeof(ISystemTime)) as ISystemTime ?? SystemTime.Default;
		}


		/// <summary>
		/// A callback invoked when an entity is found by its key.
		/// </summary>
		/// <param name="key">
		/// The key used to find the entity.
		/// </param>
		/// <param name="entity">
		/// The entity that was found in the repository.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns the entity after any modification by the callback.
		/// </returns>
		protected virtual ValueTask<TEntity> OnEntityFoundByKeyAsync(TKey key, TEntity entity, CancellationToken cancellationToken = default) {
			return new ValueTask<TEntity>(entity);
		}

		/// <inheritdoc/>
		public override async ValueTask<bool> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default) {
			ThrowIfDisposed();

			ArgumentNullException.ThrowIfNull(entity);

			try {
				var entityId = GetEntityKey(entity)!;

				Logger.TraceUpdatingEntity(typeof(TEntity), entityId);

				var entry = Context.Entry(entity);
				if (entry.State == EntityState.Detached) {
					entry = ResolveEntryForEntityKey(entity, entityId);
				}

			if (!ReferenceEquals(entry.Entity, entity)) {
				entry.CurrentValues.SetValues(entity);
				}

				entry.State = EntityState.Modified;

				var count = await Context.SaveChangesAsync(cancellationToken);

				var updated = count > 0;

				if (updated) {
					Logger.LogEntityUpdated(typeof(TEntity), entityId);
				} else {
					Logger.WarnEntityNotUpdated(typeof(TEntity), entityId);
				}

				return updated;
			} catch (DbUpdateConcurrencyException) {
				Logger.WarnEntityNotFound(typeof(TEntity), GetEntityKey(entity)!);
				return false;
			} catch (Exception ex) {
				Logger.LogUnknownError(ex, typeof(TEntity));
				throw new RepositoryException("Unable to update the entity because of an error", ex);
			}
		}


		/// <summary>
		/// Checks if the repository contains an entity that matches 
		/// the given filter.
		/// </summary>
		/// <param name="filter">
		/// The expression that defines the filter to apply to the entities.
		/// </param>
		/// <param name="options">
		/// An optional bag of query options that influence how the query
		/// is executed by the driver, such as the soft-delete mode.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns <c>true</c> if the repository contains at least one entity
		/// that matches the given filter, otherwise <c>false</c>.
		/// </returns>
		/// <exception cref="RepositoryException"></exception>
		protected override async ValueTask<bool> ExistsAsync(IQueryFilter? filter, IQueryOptions? options, CancellationToken cancellationToken = default) {
			ThrowIfDisposed();

			try {
				InitializeFilter(filter);
				var query = ApplySoftDeleteMode(Queryable().AsNoTracking(), options);
				query = ApplyFilter(query, filter);

				return await query.AnyAsync(cancellationToken);
			} catch (Exception ex) {
				Logger.LogUnknownError(ex, typeof(TEntity));
				throw new RepositoryException("Unable to determine the existence of an entity", ex);
			}
		}

		/// <summary>
		/// Counts the number of entities in the repository that match 
		/// the given filter.
		/// </summary>
		/// <param name="filter">
		/// The expression that defines the filter to apply to the entities.
		/// </param>
		/// <param name="options">
		/// An optional bag of query options that influence how the query
		/// is executed by the driver, such as the soft-delete mode.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns the number of entities that match the given filter.
		/// </returns>
		protected override async ValueTask<long> CountAsync(IQueryFilter? filter, IQueryOptions? options, CancellationToken cancellationToken = default) {
			ThrowIfDisposed();

			try {
				InitializeFilter(filter);
				var query = ApplySoftDeleteMode(Queryable().AsNoTracking(), options);
				query = ApplyFilter(query, filter);

				return await query.LongCountAsync(cancellationToken);
			} catch (Exception ex) {

				throw new RepositoryException("Unable to count the entities", ex);
			}
		}

		/// <inheritdoc/>
		protected override async ValueTask<TEntity?> FindFirstAsync(IQuery query, CancellationToken cancellationToken = default) {
			try {
				InitializeFilter(query.Filter);
				var queryable = ApplySoftDeleteMode(Queryable(), query.Options);
				var result = EfQueryNormalizer.Normalize(query.Apply(queryable));

				return await result.FirstOrDefaultAsync(cancellationToken);
			} catch (Exception ex) {
				Logger.LogUnknownError(ex, typeof(TEntity));
				throw new RepositoryException("Unknown error while trying to find an entity", ex);
			}
		}

		/// <inheritdoc/>
		public override async ValueTask<TEntity?> FindAsync(TKey key, CancellationToken cancellationToken = default) {
			ThrowIfDisposed();

			try {
				var result = await Entities.FindAsync(new object?[] { ConvertEntityKey(key) }, cancellationToken);
				if (result == null)
					return result;

				if (result is ISoftDeletable softDeletable && softDeletable.IsDeleted)
					return null;

				result = await OnEntityFoundByKeyAsync(key, result, cancellationToken);

				return result;
			} catch (Exception ex) {
				Logger.LogUnknownError(ex, typeof(TEntity));
				throw new RepositoryException("Unable to find an entity in the repository because of an error", ex);
			}
		}

		/// <inheritdoc/>
		public virtual async ValueTask<TEntity?> FindOriginalAsync(TKey key, CancellationToken cancellationToken = default) {
			ThrowIfDisposed();

			try {
				var result = await Entities.FindAsync(new object?[] { ConvertEntityKey(key) }, cancellationToken);
				if (result == null)
					return result;

				var entry = Context.Entry(result);
				
				// find a way to get the original values
				//      of related entities...

				return (TEntity) entry.OriginalValues.ToObject();
			} catch (Exception ex) {
				Logger.LogUnknownError(ex, typeof(TEntity));
				throw new RepositoryException("Unable to find an entity int he repository because of an error", ex);
			}
		}

		/// <inheritdoc/>
		protected override async ValueTask<IReadOnlyList<TEntity>> FindAllAsync(IQuery query, CancellationToken cancellationToken = default) {
			try {
				InitializeFilter(query.Filter);
				var queryable = ApplySoftDeleteMode(Queryable(), query.Options);
				var result = EfQueryNormalizer.Normalize(query.Apply(queryable));
				return await result.ToListAsync(cancellationToken);
			} catch (Exception ex) {
				Logger.LogUnknownError(ex, typeof(TEntity));
				throw new RepositoryException("Unable to list the entities", ex);
			}
		}

		private static IQueryable<TEntity> ApplyFilter(IQueryable<TEntity> query, IQueryFilter? filter) {
			if (filter == null || filter.IsEmpty())
				return query;

			return EfQueryNormalizer.Normalize(filter.Apply(query));
		}

		private EntityEntry<TEntity> ResolveEntryForEntityKey(TEntity entity, TKey entityId) {
			var tracked = Entities.Local.FirstOrDefault(x => EqualityComparer<TKey>.Default.Equals(GetEntityKey(x)!, entityId));
			if (tracked != null)
				return Context.Entry(tracked);

			Entities.Attach(entity);
			return Context.Entry(entity);
		}

		/// <summary>
		/// Disposes the repository and frees all the resources used by it.
		/// </summary>
		/// <param name="disposing">
		/// Indicates if the repository is explicitly disposing.
		/// </param>
		protected virtual void Dispose(bool disposing) {
			if (!disposedValue) {
				Context = null;
				disposedValue = true;
			}
		}

		/// <inheritdoc/>
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <inheritdoc/>
		public override async ValueTask<PageResult<TEntity>> GetPageAsync(PageRequest request, CancellationToken cancellationToken = default) {
			ThrowIfDisposed();
			ArgumentNullException.ThrowIfNull(request);

			try {
				if (request is PageQuery<TEntity> pageQuery) {
					var queryable = ApplySoftDeleteMode(Queryable(), pageQuery.Options);
					var querySet = EfQueryNormalizer.Normalize(pageQuery.ApplyQuery(queryable));
					var totalCount = await querySet.CountAsync(cancellationToken);

					var items = await querySet
						.Skip(request.Offset)
						.Take(request.Size)
						.ToListAsync(cancellationToken);

					return new PageQueryResult<TEntity>(pageQuery, totalCount, items);
				}

				var allQueryable = ApplySoftDeleteMode(Queryable(), null);
				var allQuerySet = EfQueryNormalizer.Normalize(allQueryable);
				var allTotalCount = await allQuerySet.CountAsync(cancellationToken);

				var allItems = await allQuerySet
					.Skip(request.Offset)
					.Take(request.Size)
					.ToListAsync(cancellationToken);

				return new PageResult<TEntity>(request, allTotalCount, allItems);
			} catch (Exception ex) {
				Logger.LogUnknownError(ex, typeof(TEntity));
				throw new RepositoryException("Could not get the page of entities", ex);
			}
		}
	}
}
