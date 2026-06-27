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

using System.Globalization;
using System.Linq.Expressions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using MongoDB.Bson;
using MongoDB.Driver;

using MongoFramework;
using MongoFramework.Infrastructure;
using MongoFramework.Infrastructure.Mapping;
using MongoFramework.Linq;

namespace Kista {
	/// <summary>
	/// An implementation of <see cref="IRepository{TEntity}"/> contract
	/// that uses the MongoDB system to store and retrieve data.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of the entity that is stored in the repository.
	/// </typeparam>
	/// <typeparam name="TKey">
	/// The type of the key of the entity that is stored in the repository.
	/// </typeparam>
	public class MongoRepository<TEntity, TKey> : Repository<TEntity, TKey>,
		IControllableRepository,
		IAsyncDisposable,
		IDisposable
		where TEntity : class {
		private IMongoDbSet<TEntity>? _dbSet;
		private bool disposed;

		/// <summary>
		/// Constructs the repository with the given context and logger.
		/// </summary>
		/// <param name="context">
		/// The context that is used to handle the connection to the MongoDB server.
		/// </param>
		/// <param name="logger">
		/// A logger instance that is used to log messages from the repository.
		/// </param>
		/// <param name="services">
		/// An optional service provider used to resolve infrastructure services
		/// such as expression caches for filter optimization.
		/// </param>
		protected internal MongoRepository(IMongoDbContext context, ILogger? logger = null, IServiceProvider? services = null) {
			Context = context;
			Logger = logger ?? NullLogger.Instance;
			Services = services;
		}

		/// <summary>
		/// Constructs the repository with the given context and logger.
		/// </summary>
		/// <param name="context">
		/// The context that is used to handle the connection to the MongoDB server.
		/// </param>
		/// <param name="logger">
		/// A logger instance that is used to log messages from the repository.
		/// </param>
		/// <param name="services">
		/// An optional service provider used to resolve infrastructure services
		/// such as expression caches for filter optimization.
		/// </param>
		public MongoRepository(IMongoDbContext context, ILogger<MongoRepository<TEntity, TKey>>? logger = null, IServiceProvider? services = null)
			: this(context, (ILogger?)logger, services) {
		}

		/// <summary>
		/// Gets the context that is used to handle the connection to the MongoDB server.
		/// </summary>
		protected IMongoDbContext Context { get; }

		/// <summary>
		/// Gets the <see cref="IMongoDbSet{TEntity}"/> that is used to handle the
		/// repository operations.
		/// </summary>
		protected IMongoDbSet<TEntity> DbSet => GetEntitySet();

		/// <summary>
		/// Gets the <see cref="ILogger"/> instance that is used to log messages
		/// </summary>
		protected ILogger Logger { get; }

		/// <inheritdoc />
		protected override IServiceProvider? Services { get; }

		/// <summary>
		/// Returns the <see cref="IQueryable{T}"/> that backs the entity set
		/// exposed by this repository. The hatch is intentionally
		/// <c>protected</c>: the LINQ provider must not leak to consumer
		/// code.
		/// </summary>
		/// <returns>
		/// Returns the <see cref="IQueryable{T}"/> produced by
		/// <see cref="DbSet"/>.AsQueryable().
		/// </returns>
		public override IQueryable<TEntity> Queryable() => DbSet.AsQueryable();

		/// <inheritdoc />
		protected override bool IsQueryable => true;

		/// <summary>
		/// Gets the <see cref="IMongoCollection{TEntity}"/> instance that is used
		/// to handle the data in the repository.
		/// </summary>
		protected IMongoCollection<TEntity> Collection {
			get {
				var entityDef = EntityMapping.GetOrCreateDefinition(typeof(TEntity));
				return Context.Connection.GetDatabase().GetCollection<TEntity>(entityDef.CollectionName);
			}
		}

		/// <summary>
		/// Throws an exception if the repository has been disposed.
		/// </summary>
		/// <exception cref="ObjectDisposedException">
		/// Thrown when the repository has been disposed.
		/// </exception>
		protected void ThrowIfDisposed() {
			DisposableHelper.ThrowIfDisposed(disposed, GetType().Name);
		}

		/// <summary>
		/// Constructs a new <see cref="IMongoDbSet{TEntity}"/> that is
		/// coherent with the context and the entity type.
		/// </summary>
		/// <returns>
		/// Returns an instance of <see cref="IMongoDbSet{TEntity}"/> that is
		/// coherent with the context and the entity type.
		/// </returns>
		/// <exception cref="RepositoryException">
		/// Thrown when the entity set cannot be created.
		/// </exception>
		protected virtual IMongoDbSet<TEntity> MakeEntitySet() {
			if (Context is IMongoDbTenantContext tenantContext &&
				typeof(IHaveTenantId).IsAssignableFrom(typeof(TEntity))) {
				var dbSetType = typeof(MongoDbTenantSet<>).MakeGenericType(typeof(TEntity));
				var result = (IMongoDbSet<TEntity>?)Activator.CreateInstance(dbSetType, new object[] { Context });
				if (result == null)
					throw new RepositoryException("Unable to create any tenant set");

				return result;
			}

			return Context.Set<TEntity>();
		}

		private IMongoDbSet<TEntity> GetEntitySet() {
			if (_dbSet == null) {
				_dbSet = MakeEntitySet();
			}

			return _dbSet;
		}

		/// <summary>
		/// Gets the value of the ID property of the given entity.
		/// </summary>
		/// <param name="entity">
		/// The entity whose ID property value is to be retrieved.
		/// </param>
		/// <returns>
		/// Returns the value of the ID property of the entity.
		/// </returns>
		protected override TKey? GetEntityKey(TEntity entity) {
			ArgumentNullException.ThrowIfNull(entity);

			var entityDef = EntityMapping.GetOrCreateDefinition(typeof(TEntity));

			var idProperty = entityDef.GetIdProperty();

			if (idProperty == null)
				throw new RepositoryException($"The type '{typeof(TEntity)}' has no ID property specified");

			return (TKey?) entityDef.GetIdValue(entity);
		}

		/// <summary>
		/// Converts the given value to the type of the ID property of the
		/// entity managed by this repository.
		/// </summary>
		/// <param name="key">
		/// The value representing the key of the entity.
		/// </param>
		/// <returns>
		/// Returns the value converted accordingly to the type of the ID property
		/// of the entity managed by this repository, or <c>null</c> if the given
		/// key is <c>null</c> or empty.
		/// </returns>
		/// <exception cref="RepositoryException">
		/// Thrown if the entity managed by this repository has no ID property
		/// </exception>
		/// <exception cref="NotSupportedException">
		/// Thrown when the value cannot be converted to the type of the ID
		/// property of the entity managed by this repository.
		/// </exception>
		protected virtual TKey? ConvertKeyValue(TKey? key) {
			if (key == null)
				return default(TKey?);

			var idType = key.GetType();
			var entityDef = EntityMapping.GetOrCreateDefinition(typeof(TEntity));

			var idProperty = entityDef.GetIdProperty();

			if (idProperty == null)
				throw new RepositoryException($"The type '{typeof(TEntity)}' has no ID property specified");

			var valueType = idProperty.PropertyInfo.PropertyType;

			if (valueType == idType)
				return key;

			if (idType == typeof(string)) {
				if (valueType == typeof(ObjectId))
					return (TKey)(object) ObjectId.Parse(key.ToString());

				if (typeof(IConvertible).IsAssignableFrom(valueType))
					return (TKey) Convert.ChangeType(key, valueType, CultureInfo.InvariantCulture);
			}

			throw new NotSupportedException($"It is not possible to convert the ID to '{valueType}'");
		}

		/// <summary>
		/// Resolves a given field name to an expression that can be used
		/// to access the field in the entity.
		/// </summary>
		/// <param name="fieldName">
		/// The name of the field to be resolved.
		/// </param>
		/// <returns>
		/// Returns an instance of <see cref="Expression{TDelegate}"/> that
		/// is used to access the field in the entity.
		/// </returns>
		protected virtual Expression<Func<TEntity, object>> Field(string fieldName) {
			var param = Expression.Parameter(typeof(TEntity), "x");
			var body = Expression.PropertyOrField(param, fieldName);

			return Expression.Lambda<Func<TEntity, object>>(body, param);
		}

		#region Controllable

		ValueTask<bool> IControllableRepository.ExistsAsync(CancellationToken cancellationToken)
			=> CollectionExistsAsync(cancellationToken);

		/// <summary>
		/// Verifies if the repository exists in the underlying database.
		/// </summary>
		/// <param name="cancellationToken">
		/// A cancellation token that can be used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns <c>true</c> if the repository exists in the underlying
		/// database, otherwise <c>false</c>.
		/// </returns>
		/// <exception cref="RepositoryException">
		/// Thrown when an error occurs while verifying the existence of the
		/// collection in the underlying database.
		/// </exception>
		public async ValueTask<bool> CollectionExistsAsync(CancellationToken cancellationToken = default) {
			try {
				var entityDef = EntityMapping.GetOrCreateDefinition(typeof(TEntity));

				var options = new ListCollectionNamesOptions {
					Filter = new BsonDocument(new Dictionary<string, object> { { "name", entityDef.CollectionName } })
				};

				var collectionNames = await Context.Connection.GetDatabase().ListCollectionNamesAsync(options, cancellationToken);
				var list = await collectionNames.ToListAsync(cancellationToken);

				return list.Any();
			} catch (Exception ex) {
				Logger.LogUnknownError(ex);
				throw new RepositoryException("Unable to determine the existence of the repository", ex);
			}
		}

		async ValueTask IControllableRepository.CreateAsync(CancellationToken cancellationToken) {
			await CreateCollectionAsync(cancellationToken);
			await CreateIndicesAsync(cancellationToken);
		}

		/// <summary>
		/// Creates the collection that is used to store the entities 
		/// of this repository in the underlying database.
		/// </summary>
		/// <param name="cancellationToken">
		/// A cancellation token that can be used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns a task that, when completed, has created the collection.
		/// </returns>
		/// <exception cref="RepositoryException">
		/// Thrown when an error occurs while creating the collection in the
		/// database or if the collection already exists.
		/// </exception>
		public async ValueTask CreateCollectionAsync(CancellationToken cancellationToken = default) {
			try {
				var entityDef = EntityMapping.GetOrCreateDefinition(typeof(TEntity));

				// TODO: should we also create the indices here?
				await Context.Connection.GetDatabase()
					.CreateCollectionAsync(entityDef.CollectionName, null, cancellationToken);
			} catch (Exception ex) {
				Logger.LogUnknownError(ex);
				throw new RepositoryException("Unable to create the repository", ex);
			}
		}

		/// <summary>
		/// Creates all the indices that are defined for the entity
		/// that is managed by this repository.
		/// </summary>
		/// <param name="cancellationToken">
		/// A cancellation token that can be used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns a task that, when completed, has created all the indices
		/// of the repository.
		/// </returns>
		/// <exception cref="RepositoryException">
		/// Thrown when an error occurs while creating the indices for the repository.
		/// </exception>
		public async ValueTask CreateIndicesAsync(CancellationToken cancellationToken = default) {
			try {
				var entityDef = EntityMapping.GetOrCreateDefinition(typeof(TEntity));

				foreach (var indexDef in entityDef.Indexes) {
					var indices = MongoIndexBuilder<TEntity>.BuildIndexModels(indexDef);
					await Collection.Indexes.CreateManyAsync(indices, cancellationToken);
				}
			} catch (Exception ex) {
				throw new RepositoryException("Unable to create the indices for the repository", ex);
			}
		}

		async ValueTask IControllableRepository.DropAsync(CancellationToken cancellationToken) {
			await DropIndicesAsync(cancellationToken);
			await DropCollectionAsync(cancellationToken);
		}

		/// <summary>
		/// Drops all the indices that are defined for the entity
		/// that is managed by this repository.
		/// </summary>
		/// <param name="cancellationToken">
		/// A cancellation token that can be used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns a task that, when completed, has dropped all the indices
		/// from the repository.
		/// </returns>
		/// <exception cref="RepositoryException">
		/// Thrown when an error occurs while dropping the indices.
		/// </exception>
		public async ValueTask DropIndicesAsync(CancellationToken cancellationToken = default) {
			try {
				await Collection.Indexes.DropAllAsync(cancellationToken);
			} catch (Exception ex) {

				throw new RepositoryException("Unable to drop the indices", ex);
			}
		}

		/// <summary>
		/// Drops the collection that is used to store the entities
		/// that are managed by this repository in the underlying database.
		/// </summary>
		/// <param name="cancellationToken">
		/// A cancellation token that can be used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns a task that, when completed, has dropped the collection.
		/// </returns>
		/// <exception cref="RepositoryException">
		/// Thrown when an error occurs while dropping the collection in the database.
		/// </exception>
		public async ValueTask DropCollectionAsync(CancellationToken cancellationToken = default) {
			try {
				var entityDef = EntityMapping.GetOrCreateDefinition(typeof(TEntity));

				await Context.Connection.GetDatabase().DropCollectionAsync(entityDef.CollectionName, cancellationToken);
			} catch (Exception ex) {
				Logger.LogUnknownError(ex);
				throw new RepositoryException("Unable to drop the repository", ex);
			}
		}

		#endregion

		/// <summary>
		/// A callback method that is invoked before the entity is created.
		/// </summary>
		/// <param name="entity">
		/// The entity that is about to be created.
		/// </param>
		/// <returns>
		/// Returns the entity that is about to be created.
		/// </returns>
		protected virtual TEntity OnAddEntity(TEntity entity) {
			return entity;
		}

		/// <summary>
		/// Creates a new entity in the repository.
		/// </summary>
		/// <param name="entity">
		/// The entity to be created in the repository.
		/// </param>
		/// <param name="cancellationToken">
		/// A cancellation token that can be used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns the unique identifier of the created entity.
		/// </returns>
		/// <exception cref="RepositoryException">
		/// Thrown when an error occurs while creating the entity in the
		/// underlying database.
		/// </exception>
		public override async ValueTask AddAsync(TEntity entity, CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(entity);

			ThrowIfDisposed();
			cancellationToken.ThrowIfCancellationRequested();

			Logger.TraceCreating();

			try {
				entity = OnAddEntity(entity);

				DbSet.Add(entity);
				await DbSet.Context.SaveChangesAsync(cancellationToken);

				var id = GetEntityKey(entity);

				Logger.TraceCreated(id!);
			} catch (Exception ex) {
				Logger.LogUnknownError(ex);
				throw new RepositoryException("Unable to create the entity", ex);
			}
		}

		/// <inheritdoc/>
		public override async ValueTask AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(entities);

			ThrowIfDisposed();
			cancellationToken.ThrowIfCancellationRequested();

			try {
				entities = entities.Select(OnAddEntity);

				DbSet.AddRange(entities);
				await DbSet.Context.SaveChangesAsync(cancellationToken);
			} catch (Exception ex) {
				Logger.LogUnknownError(ex);
				throw new RepositoryException("Could not add the list of entities", ex);
			}
		}

		#region Update

		/// <summary>
		/// A callback method that is invoked before the entity is updated.
		/// </summary>
		/// <param name="entity">
		/// The entity that is about to be updated.
		/// </param>
		/// <returns>
		/// Returns the entity that is about to be updated.
		/// </returns>
		protected virtual TEntity OnEntityUpdate(TEntity entity) {
			return entity;
		}

		/// <inheritdoc/>
		public override async ValueTask<bool> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(entity);

			ThrowIfDisposed();
			cancellationToken.ThrowIfCancellationRequested();

			var id = GetEntityKey(entity);

			if (id == null)
				throw new ArgumentException(nameof(entity), "Cannot determine the identifier of the entity");

			Logger.TraceUpdating(id.ToString()!);

			try {
				var entry = Context.ChangeTracker.GetEntryById<TEntity>(id);
				if (entry == null || entry.State == EntityEntryState.Deleted)
					return false;

				entity = OnEntityUpdate(entity);

				DbSet.Update(entity);
				var updated = entry.State == EntityEntryState.Updated;

				await Context.SaveChangesAsync(cancellationToken);

				if (updated) {
					Logger.TraceUpdated(id.ToString()!);
				}

				return updated;
			} catch (Exception ex) {
				Logger.LogUnknownEntityError(ex, id.ToString()!);
				throw new RepositoryException("Unable to update the entity", ex);
			}
		}

		#endregion

		/// <inheritdoc/>
		public override async ValueTask<bool> RemoveAsync(TEntity entity, CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(entity);

			ThrowIfDisposed();
			cancellationToken.ThrowIfCancellationRequested();

			var entityId = GetEntityKey(entity);
			if (entityId == null)
				throw new ArgumentException("The entity does not have an ID", nameof(entity));

			try {
				Logger.TraceDeleting(entityId);

				var entry = Context.ChangeTracker.GetEntry(entity);
				if (entry == null)
					return false;

				DbSet.Remove(entity);
				await Context.SaveChangesAsync(cancellationToken);
	
				Logger.TraceDeleted(entityId);

				return entry.State == EntityEntryState.Deleted;
			} catch (Exception ex) {
				Logger.LogUnknownEntityError(ex, entityId);

				throw new RepositoryException("Unable to delete the entity", ex);
			}
		}

		/// <inheritdoc/>
		public override async ValueTask RemoveRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(entities);

			ThrowIfDisposed();
			cancellationToken.ThrowIfCancellationRequested();

			try {
				if (entities.Any(x => DbSet.Context.ChangeTracker.GetEntry(x) == null))
					throw new RepositoryException("The list contains entities that are not tracked by the repository");

				DbSet.RemoveRange(entities);

				await Context.SaveChangesAsync(cancellationToken);
			} catch (Exception ex) {
				Logger.LogUnknownError(ex);
				throw new RepositoryException("Could not delete the list of entities", ex);
			}
		}

		/// <inheritdoc/>
		public override async ValueTask<TEntity?> FindAsync(TKey key, CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(key);

			ThrowIfDisposed();
			cancellationToken.ThrowIfCancellationRequested();

			Logger.TraceFindingById(key);

			try {
				var keyValue = ConvertKeyValue(key);

				var entry = Context.ChangeTracker.GetEntryById<TEntity>(keyValue);
				if (entry != null && entry.State == EntityEntryState.Deleted)
					return null;

				var result = await DbSet.FindAsync(keyValue);

				if (result == null)
					return null;

				Logger.TraceFoundById(key);

				return result;
			} catch (Exception ex) {
				Logger.LogUnknownEntityError(ex, key);
				throw new RepositoryException("Unable to find the entity", ex);
			}
		}

		/// <inheritdoc/>
		protected override async ValueTask<TEntity?> FindFirstAsync(IQuery query, CancellationToken cancellationToken = default) {
			try {
				InitializeFilter(query.Filter);
				var entities = query.Apply(Queryable());
				return await entities.FirstOrDefaultAsync(cancellationToken);
			} catch (Exception ex) {
				throw new RepositoryException("Unable to find the entity", ex);
			}
		}

		/// <inheritdoc/>
		protected override async ValueTask<IReadOnlyList<TEntity>> FindAllAsync(IQuery query, CancellationToken cancellationToken = default) {
			try {
				InitializeFilter(query.Filter);
				var entities = query.Apply(Queryable());
				return await entities.ToListAsync(cancellationToken);
			} catch (Exception ex) {

				throw new RepositoryException("Unable to find the entities", ex);
			}
		}

		/// <inheritdoc/>
		public override async ValueTask<PageResult<TEntity>> GetPageAsync(PageRequest request, CancellationToken cancellationToken = default) {
			ThrowIfDisposed();
			ArgumentNullException.ThrowIfNull(request);
			cancellationToken.ThrowIfCancellationRequested();

			try {
				if (request is PageQuery<TEntity> pageQuery) {
					InitializeFilter(((IQuery)pageQuery).Filter);
					var querySet = pageQuery.ApplyQuery(Queryable());

					var totalCount = await querySet.CountAsync(cancellationToken);

					querySet = querySet.Skip(request.Offset).Take(request.Size);

					var items = await querySet.ToListAsync(cancellationToken);
					return new PageQueryResult<TEntity>(pageQuery, totalCount, items);
				}

				var entitySet = Queryable();
				var allTotalCount = await entitySet.CountAsync(cancellationToken);

				entitySet = entitySet.Skip(request.Offset).Take(request.Size);

				var allItems = await entitySet.ToListAsync(cancellationToken);

				return new PageResult<TEntity>(request, allTotalCount, allItems);
			} catch (Exception ex) {

				throw new RepositoryException("Unable to retrieve the page", ex);
			}
		}

		/// <inheritdoc/>
		protected override ValueTask<bool> ExistsAsync(IQueryFilter filter, CancellationToken cancellationToken = default) {
			try {
				InitializeFilter(filter);
				var query = Queryable();
				if (filter != null) {
					query = filter.Apply(query);
				}

				return new ValueTask<bool>(query.AnyAsync(cancellationToken));
			} catch (Exception ex) {

				throw new RepositoryException("Unable to execute the query", ex);
			}
		}

		/// <inheritdoc/>
		protected override async ValueTask<long> CountAsync(IQueryFilter filter, CancellationToken cancellationToken = default) {
			try {
				InitializeFilter(filter);
				var query = Queryable();
				if (filter != null) {
					query = filter.Apply(query);
				}

				return await query.CountAsync(cancellationToken);
			} catch (Exception ex) {
				throw new RepositoryException("Unable to count the entities", ex);
			}
		}

		/// <inheritdoc/>
		protected override ValueTask<long> CountAsync(IQueryable<TEntity> queryable, CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(queryable);

			return new ValueTask<long>(queryable.Count());
		}

		#region Dispose

		/// <summary>
		/// Finalizes the instance of the repository.
		/// </summary>
		~MongoRepository() {
			Dispose(disposing: false);
		}

		void IDisposable.Dispose() {
			if (!disposed) {
				Dispose(disposing: true);
				GC.SuppressFinalize(this);
				disposed = true;
			}
		}

		/// <summary>
		/// Disposes of the unmanaged resources used by the repository.
		/// </summary>
		/// <param name="disposing">
		/// Indicates if the repository is being disposed of.
		/// </param>
		protected virtual void Dispose(bool disposing) {
		}

		/// <summary>
		/// Performs the async cleanup of the repository.
		/// </summary>
		/// <returns>
		/// Returns a task that, when completed, has performed the cleanup.
		/// </returns>
		protected virtual ValueTask DisposeAsyncCore() {
			return ValueTask.CompletedTask;
		}

		async ValueTask IAsyncDisposable.DisposeAsync() {
			if (!disposed) {
				// Perform async cleanup.
				await DisposeAsyncCore();

				// Dispose of unmanaged resources.
				Dispose(false);

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
				// Suppress finalization.
				GC.SuppressFinalize(this);
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize

				disposed = true;
			}
		}

		#endregion
	}
}
