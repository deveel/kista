using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kista {
	/// <summary>
	/// Base class for repository lifecycle handlers that provides common
	/// functionality for logging and seed data insertion.
	/// </summary>
	/// <typeparam name="TEntity">The type of the entity.</typeparam>
	/// <remarks>
	/// <para>
	/// This class handles the common pattern of accepting seed data in multiple
	/// forms (<see cref="IEnumerable{TEntity}"/>, <see cref="IEnumerable{Object}"/>, or a single <typeparamref name="TEntity"/>)
	/// and dispatching to the abstract <see cref="SeedEntitiesAsync"/> method.
	/// </para>
	/// <para>
	/// Subclasses must implement <see cref="ExistsAsync"/>, <see cref="CreateAsync"/>,
	/// <see cref="DropAsync"/>, and <see cref="SeedEntitiesAsync"/> to provide
	/// driver-specific behavior.
	/// </para>
	/// </remarks>
	public abstract class RepositoryLifecycleHandler<TEntity> : IRepositoryLifecycleHandler<TEntity>
		where TEntity : class {

		/// <summary>
		/// Creates a new instance with the given logger.
		/// </summary>
		/// <param name="logger">The logger instance, or <c>null</c> to use a null logger.</param>
		protected RepositoryLifecycleHandler(ILogger? logger = null) {
			Logger = logger ?? NullLogger<RepositoryLifecycleHandler<TEntity>>.Instance;
		}

		/// <summary>
		/// Gets the logger used for diagnostic output.
		/// </summary>
		protected ILogger Logger { get; }

		/// <inheritdoc/>
		public abstract ValueTask<bool> ExistsAsync(CancellationToken cancellationToken = default);

		/// <inheritdoc/>
		public abstract ValueTask CreateAsync(CancellationToken cancellationToken = default);

		/// <inheritdoc/>
		public abstract ValueTask DropAsync(CancellationToken cancellationToken = default);

		/// <inheritdoc/>
		public ValueTask SeedAsync(object? seedData = null, CancellationToken cancellationToken = default) {
			if (seedData == null)
				return ValueTask.CompletedTask;

			if (seedData is IEnumerable<TEntity> entities) {
				return SeedEntitiesAsync(entities, cancellationToken);
			}

			if (seedData is IEnumerable<object> objects) {
				var typedEntities = objects.OfType<TEntity>().ToList();
				return typedEntities.Any()
					? SeedEntitiesAsync(typedEntities, cancellationToken)
					: ValueTask.CompletedTask;
			}

			if (seedData is TEntity single) {
				return SeedEntitiesAsync(new[] { single }, cancellationToken);
			}

			return ValueTask.CompletedTask;
		}

		/// <summary>
		/// Seeds the repository with the given entities.
		/// </summary>
		/// <param name="entities">The entities to insert.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		protected abstract ValueTask SeedEntitiesAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
	}
}
