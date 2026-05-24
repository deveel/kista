// Copyright 2023-2025 Antonello Provenzano
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

namespace Deveel.Data {
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
	public class MongoRepository<TEntity, TKey> : IRepository<TEntity, TKey>,
		IQueryableRepository<TEntity, TKey>,
		IPageableRepository<TEntity, TKey>,
		IFilterableRepository<TEntity, TKey>,
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
		public IServiceProvider? Services { get; }

		private void InitializeFilter(IQueryFilter? filter) {
			if (filter != null && Services != null)
				filter.Initialize(new DefaultFilterContext(Services));
		}

		IQueryable<TEntity> IQueryableRepository<TEntity, TKey>.AsQueryable() => DbSet.AsQueryable();

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
			if (disposed)
				throw new ObjectDisposedException(GetType().Name);
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
		/// Returns the value of the ID property of the given entity.
		/// </returns>
		public virtual TKey? GetEntityKey(TEntity entity) {
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
				return default;

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
					var indices = BuildIndexModels(indexDef);
					await Collection.Indexes.CreateManyAsync(indices, cancellationToken);
				}
			} catch (Exception ex) {
				throw new RepositoryException("Unable to create the indices for the repository", ex);
			}
		}

		private static List<CreateIndexModel<TEntity>> BuildIndexModels(IndexDefinition indexDef) {
			var keysBuilder = new IndexKeysDefinitionBuilder<TEntity>();
			var indices = new List<CreateIndexModel<TEntity>>();

			foreach (var path in indexDef.IndexPaths) {
				var keysDef = BuildIndexKeys(keysBuilder, path);
				if (keysDef == null)
					continue;

				var options = new CreateIndexOptions {
					Unique = indexDef.IsUnique,
					Name = indexDef.IndexName
				};
				indices.Add(new CreateIndexModel<TEntity>(keysDef, options));
			}

			return indices;
		}

		private static IndexKeysDefinition<TEntity>? BuildIndexKeys(IndexKeysDefinitionBuilder<TEntity> builder, IndexPathDefinition path) {
			var fieldDef = new StringFieldDefinition<TEntity>(path.Path);
			return path.IndexType switch {
				IndexType.Standard => path.SortOrder == IndexSortOrder.Descending
					? builder.Descending(fieldDef)
					: builder.Ascending(fieldDef),
				IndexType.Geo2dSphere => builder.Geo2DSphere(fieldDef),
				IndexType.Text => builder.Text(fieldDef),
				_ => null
			};
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
		public virtual async ValueTask AddAsync(TEntity entity, CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(entity, nameof(entity));

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
		public async ValueTask AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(entities, nameof(entities));

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
		public virtual async ValueTask<bool> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(entity, nameof(entity));

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
		public virtual async ValueTask<bool> RemoveAsync(TEntity entity, CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(entity, nameof(entity));

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
		public async ValueTask RemoveRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(entities, nameof(entities));

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
		public async ValueTask<TEntity?> FindAsync(TKey key, CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(key, nameof(key));

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
		public async ValueTask<TEntity?> FindFirstAsync(IQuery query, CancellationToken cancellationToken = default) {
			try {
				InitializeFilter(query.Filter);
				var entities = query.Apply(DbSet.AsQueryable());
				return await entities.FirstOrDefaultAsync(cancellationToken);
			} catch (Exception ex) {
				throw new RepositoryException("Unable to find the entity", ex);
			}
		}

		/// <inheritdoc/>
		public async ValueTask<IList<TEntity>> FindAllAsync(IQuery query, CancellationToken cancellationToken = default) {
			try {
				InitializeFilter(query.Filter);
				var entities = query.Apply(DbSet.AsQueryable());
				return await entities.ToListAsync(cancellationToken);
			} catch (Exception ex) {

				throw new RepositoryException("Unable to find the entities", ex);
			}
		}

		/// <inheritdoc/>
		public async ValueTask<PageResult<TEntity>> GetPageAsync(PageQuery<TEntity> query, CancellationToken cancellationToken = default) {
			try {
				InitializeFilter(((IQuery)query).Filter);
				var entitySet = query.ApplyQuery(DbSet.AsQueryable());

				var totalCount = await entitySet.CountAsync(cancellationToken);

				entitySet = entitySet.Skip(query.Offset).Take(query.Size);

				var items = await entitySet.ToListAsync(cancellationToken);
				return new PageResult<TEntity>(query, totalCount, items);
			} catch (Exception ex) {

				throw new RepositoryException("Unable to execute the query", ex);
			}
		}

		/// <inheritdoc/>
		public ValueTask<bool> ExistsAsync(IQueryFilter filter, CancellationToken cancellationToken = default) {
			try {
				InitializeFilter(filter);
				var query = DbSet.AsQueryable();
				if (filter != null) {
					query = filter.Apply(query);
				}

				return new ValueTask<bool>(query.AnyAsync(cancellationToken));
			} catch (Exception ex) {

				throw new RepositoryException("Unable to execute the query", ex);
			}
		}

		/// <inheritdoc/>
		public async ValueTask<long> CountAsync(IQueryFilter filter, CancellationToken cancellationToken = default) {
			try {
				InitializeFilter(filter);
				var query = DbSet.AsQueryable();
				if (filter != null) {
					query = filter.Apply(query);
				}

				return await query.CountAsync(cancellationToken);
			} catch (Exception ex) {
				throw new RepositoryException("Unable to count the entities", ex);
			}
		}

		#region Dispose

		/// <summary>
		/// Finalizes the instance of the repository.
		/// </summary>
		~MongoRepository() {
			Dispose(disposing: false);
		}

		void IDisposable.Dispose() {
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
			disposed = true;
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
