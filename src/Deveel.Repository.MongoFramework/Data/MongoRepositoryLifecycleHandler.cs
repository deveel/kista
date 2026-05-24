using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using MongoDB.Bson;
using MongoDB.Driver;

using MongoFramework;
using MongoFramework.Infrastructure.Mapping;

namespace Deveel.Data {
	/// <summary>
	/// A lifecycle handler for MongoDB repositories that uses MongoFramework's
	/// entity mapping to create, drop, and seed MongoDB collections, including
	/// index creation defined in the entity mapping.
	/// </summary>
	/// <typeparam name="TEntity">The type of the entity.</typeparam>
	public class MongoRepositoryLifecycleHandler<TEntity> : IRepositoryLifecycleHandler<TEntity>
		where TEntity : class {
        /// <summary>
		/// Creates a new instance of the handler for the given MongoFramework context.
		/// </summary>
		/// <param name="context">The MongoFramework database context.</param>
		/// <param name="logger">An optional typed logger instance.</param>
		public MongoRepositoryLifecycleHandler(IMongoDbContext context, ILogger<MongoRepositoryLifecycleHandler<TEntity>>? logger = null) {
			this.Context = context;
			this.Logger = logger ?? NullLogger<MongoRepositoryLifecycleHandler<TEntity>>.Instance;
		}

		/// <summary>
		/// Gets the underlying <see cref="IMongoDbContext"/> used for database operations.
		/// </summary>
		protected IMongoDbContext Context { get; }

        /// <summary>
		/// Gets the logger used for diagnostic output.
		/// </summary>
		protected ILogger Logger { get; }

        /// <summary>
		/// Resolves the MongoDB collection name for the entity type from the
		/// MongoFramework entity mapping.
		/// </summary>
		/// <returns>The collection name as defined in the entity mapping.</returns>
		protected virtual string ResolveCollectionName() {
			var entityDef = EntityMapping.GetOrCreateDefinition(typeof(TEntity));
			return entityDef.CollectionName;
		}

		/// <inheritdoc/>
		public async ValueTask<bool> ExistsAsync(CancellationToken cancellationToken = default) {
			try {
				var collectionName = ResolveCollectionName();
				var options = new ListCollectionNamesOptions {
					Filter = new BsonDocument("name", collectionName)
				};
				var collectionNames = await Context.Connection.GetDatabase()
					.ListCollectionNamesAsync(options, cancellationToken);
				var list = await collectionNames.ToListAsync<string>(cancellationToken);
				return list.Any();
			} catch (Exception ex) {
				throw new RepositoryException("Unable to determine the existence of the repository", ex);
			}
		}

		/// <inheritdoc/>
		public async ValueTask CreateAsync(CancellationToken cancellationToken = default) {
			try {
				var collectionName = ResolveCollectionName();
				await Context.Connection.GetDatabase()
					.CreateCollectionAsync(collectionName, null, cancellationToken);

				await CreateIndicesAsync(cancellationToken);
			} catch (Exception ex) {
				throw new RepositoryException("Unable to create the repository", ex);
			}
		}

		/// <summary>
		/// Creates all indexes defined in the MongoFramework entity mapping for the entity type.
		/// </summary>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		protected virtual async ValueTask CreateIndicesAsync(CancellationToken cancellationToken) {
			try {
				var entityDef = EntityMapping.GetOrCreateDefinition(typeof(TEntity));
				var collection = Context.Connection.GetDatabase()
					.GetCollection<TEntity>(entityDef.CollectionName);

				foreach (var indexDef in entityDef.Indexes) {
					var keysBuilder = new IndexKeysDefinitionBuilder<TEntity>();
					var indices = new List<CreateIndexModel<TEntity>>();
					foreach (var path in indexDef.IndexPaths) {
						var fieldDef = new StringFieldDefinition<TEntity>(path.Path);
						IndexKeysDefinition<TEntity>? keysDef = null;
						if (path.IndexType == IndexType.Standard) {
							keysDef = path.SortOrder == IndexSortOrder.Descending
								? keysBuilder.Descending(fieldDef)
								: keysBuilder.Ascending(fieldDef);
						} else if (path.IndexType == IndexType.Geo2dSphere) {
							keysDef = keysBuilder.Geo2DSphere(fieldDef);
						} else if (path.IndexType == IndexType.Text) {
							keysDef = keysBuilder.Text(fieldDef);
						}

						if (keysDef != null) {
							var options = new CreateIndexOptions {
								Unique = indexDef.IsUnique,
								Name = indexDef.IndexName
							};
							var indexModel = new CreateIndexModel<TEntity>(keysDef, options);
							indices.Add(indexModel);
						}
					}

					if (indices.Any())
						await collection.Indexes.CreateManyAsync(indices, cancellationToken);
				}
			} catch (Exception ex) {
				throw new RepositoryException("Unable to create the indices for the repository", ex);
			}
		}

		/// <inheritdoc/>
		public async ValueTask DropAsync(CancellationToken cancellationToken = default) {
			try {
				var collectionName = ResolveCollectionName();
				var collection = Context.Connection.GetDatabase()
					.GetCollection<TEntity>(collectionName);

				await collection.Indexes.DropAllAsync(cancellationToken);
				await Context.Connection.GetDatabase()
					.DropCollectionAsync(collectionName, cancellationToken);
			} catch (Exception ex) {
				throw new RepositoryException("Unable to drop the repository", ex);
			}
		}

		/// <inheritdoc/>
		public async ValueTask SeedAsync(object? seedData = null, CancellationToken cancellationToken = default) {
			if (seedData == null)
				return;

			if (seedData is IEnumerable<TEntity> entities) {
				var collectionName = ResolveCollectionName();
				var collection = Context.Connection.GetDatabase()
					.GetCollection<TEntity>(collectionName);
				await collection.InsertManyAsync(entities, cancellationToken: cancellationToken);
				return;
			}

			if (seedData is IEnumerable<object> objects) {
				var typedEntities = objects.OfType<TEntity>().ToList();
				if (typedEntities.Any()) {
					var collectionName = ResolveCollectionName();
					var collection = Context.Connection.GetDatabase()
						.GetCollection<TEntity>(collectionName);
					await collection.InsertManyAsync(typedEntities, cancellationToken: cancellationToken);
				}
				return;
			}

			if (seedData is TEntity single) {
				var collectionName = ResolveCollectionName();
				var collection = Context.Connection.GetDatabase()
					.GetCollection<TEntity>(collectionName);
				await collection.InsertOneAsync(single, cancellationToken: cancellationToken);
			}
		}
	}
}
