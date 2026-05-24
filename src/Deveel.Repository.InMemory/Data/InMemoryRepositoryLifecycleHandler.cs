using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Deveel.Data {
	/// <summary>
	/// A lifecycle handler for in-memory repositories that performs no-op
	/// create/drop operations (since in-memory storage is transient) and
	/// resolves seed data via <see cref="IRepository{TEntity}"/> from DI.
	/// </summary>
	/// <typeparam name="TEntity">The type of the entity.</typeparam>
	public class InMemoryRepositoryLifecycleHandler<TEntity> : IRepositoryLifecycleHandler<TEntity>
		where TEntity : class {
        /// <summary>
		/// Creates a new instance of the handler.
		/// </summary>
		/// <param name="serviceProvider">The service provider to resolve the repository.</param>
		/// <param name="logger">An optional typed logger instance.</param>
		public InMemoryRepositoryLifecycleHandler(IServiceProvider serviceProvider, ILogger<InMemoryRepositoryLifecycleHandler<TEntity>>? logger = null) {
			ServiceProvider = serviceProvider;
			Logger = logger ?? NullLogger<InMemoryRepositoryLifecycleHandler<TEntity>>.Instance;
		}

		/// <summary>
		/// Gets the service provider used to resolve the repository instance.
		/// </summary>
		protected IServiceProvider ServiceProvider { get; }

        /// <summary>
		/// Gets the logger used for diagnostic output.
		/// </summary>
		protected ILogger Logger { get; }

        /// <inheritdoc/>
		public ValueTask<bool> ExistsAsync(CancellationToken cancellationToken = default) {
			return ValueTask.FromResult(false);
		}

		/// <inheritdoc/>
		public ValueTask CreateAsync(CancellationToken cancellationToken = default) {
			return ValueTask.CompletedTask;
		}

		/// <inheritdoc/>
		public ValueTask DropAsync(CancellationToken cancellationToken = default) {
			return ValueTask.CompletedTask;
		}

		/// <inheritdoc/>
		public async ValueTask SeedAsync(object? seedData = null, CancellationToken cancellationToken = default) {
			if (seedData == null)
				return;

			var repository = ServiceProvider.GetRequiredService<IRepository<TEntity>>();

			if (seedData is IEnumerable<TEntity> entities) {
				await repository.AddRangeAsync(entities, cancellationToken);
				return;
			}

			if (seedData is IEnumerable<object> objects) {
				var typedEntities = objects.OfType<TEntity>().ToList();
				if (typedEntities.Any()) {
					await repository.AddRangeAsync(typedEntities, cancellationToken);
				}
				return;
			}

			if (seedData is TEntity single) {
				await repository.AddAsync(single, cancellationToken);
				return;
			}
		}
	}
}
