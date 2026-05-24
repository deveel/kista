using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Deveel.Data {
	/// <summary>
	/// A lifecycle handler for Entity Framework Core repositories that uses
	/// <see cref="DbContext.Database"/> operations (<c>EnsureCreated</c>,
	/// <c>EnsureDeleted</c>, <c>CanConnect</c>) for create/drop/existence checks
	/// and <see cref="DbSet{TEntity}"/> operations for seeding.
	/// </summary>
	/// <typeparam name="TEntity">The type of the entity.</typeparam>
	public class EntityFrameworkRepositoryLifecycleHandler<TEntity> : IRepositoryLifecycleHandler<TEntity>
		where TEntity : class {
        /// <summary>
		/// Creates a new instance of the handler for the given <see cref="DbContext"/>.
		/// </summary>
		/// <param name="context">The EF Core database context.</param>
		/// <param name="logger">An optional typed logger instance.</param>
		public EntityFrameworkRepositoryLifecycleHandler(DbContext context, ILogger<EntityFrameworkRepositoryLifecycleHandler<TEntity>>? logger = null) {
			this.Context = context;
			this.Logger = logger ?? NullLogger<EntityFrameworkRepositoryLifecycleHandler<TEntity>>.Instance;
		}

		/// <summary>
		/// Gets the underlying <see cref="DbContext"/> used for database operations.
		/// </summary>
		protected DbContext Context { get; }

        /// <summary>
		/// Gets the logger used for diagnostic output.
		/// </summary>
		protected ILogger Logger { get; }

        /// <inheritdoc/>
		public ValueTask<bool> ExistsAsync(CancellationToken cancellationToken = default) {
			try {
				return ValueTask.FromResult(Context.Database.CanConnect());
			} catch (Exception ex) {
				throw new RepositoryException("Unable to determine the existence of the repository", ex);
			}
		}

		/// <inheritdoc/>
		public async ValueTask CreateAsync(CancellationToken cancellationToken = default) {
			try {
				await Context.Database.EnsureCreatedAsync(cancellationToken);
			} catch (Exception ex) {
				throw new RepositoryException("Unable to create the repository", ex);
			}
		}

		/// <inheritdoc/>
		public async ValueTask DropAsync(CancellationToken cancellationToken = default) {
			try {
				await Context.Database.EnsureDeletedAsync(cancellationToken);
			} catch (Exception ex) {
				throw new RepositoryException("Unable to drop the repository", ex);
			}
		}

		/// <inheritdoc/>
		public async ValueTask SeedAsync(object? seedData = null, CancellationToken cancellationToken = default) {
			if (seedData == null)
				return;

			if (seedData is IEnumerable<TEntity> entities) {
				Context.Set<TEntity>().AddRange(entities);
				await Context.SaveChangesAsync(cancellationToken);
				return;
			}

			if (seedData is IEnumerable<object> objects) {
				var typedEntities = objects.OfType<TEntity>().ToList();
				if (typedEntities.Any()) {
					Context.Set<TEntity>().AddRange(typedEntities);
					await Context.SaveChangesAsync(cancellationToken);
				}
				return;
			}

			if (seedData is TEntity single) {
				Context.Set<TEntity>().Add(single);
				await Context.SaveChangesAsync(cancellationToken);
				return;
			}
		}
	}
}
