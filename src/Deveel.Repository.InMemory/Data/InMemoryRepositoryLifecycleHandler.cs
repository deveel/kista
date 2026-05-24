using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Deveel.Data {
	/// <summary>
	/// A lifecycle handler for in-memory repositories that performs no-op
	/// create/drop operations (since in-memory storage is transient) and
	/// resolves seed data via <see cref="IRepository{TEntity}"/> from DI.
	/// </summary>
	/// <typeparam name="TEntity">The type of the entity.</typeparam>
	public class InMemoryRepositoryLifecycleHandler<TEntity> : RepositoryLifecycleHandler<TEntity>
		where TEntity : class {

		/// <summary>
		/// Creates a new instance of the handler.
		/// </summary>
		/// <param name="serviceProvider">The service provider to resolve the repository.</param>
		/// <param name="logger">An optional typed logger instance.</param>
		public InMemoryRepositoryLifecycleHandler(IServiceProvider serviceProvider, ILogger<InMemoryRepositoryLifecycleHandler<TEntity>>? logger = null)
			: base(logger) {
			ServiceProvider = serviceProvider;
		}

		/// <summary>
		/// Gets the service provider used to resolve the repository instance.
		/// </summary>
		protected IServiceProvider ServiceProvider { get; }

		/// <inheritdoc/>
		public override ValueTask<bool> ExistsAsync(CancellationToken cancellationToken = default) {
			return ValueTask.FromResult(false);
		}

		/// <inheritdoc/>
		public override ValueTask CreateAsync(CancellationToken cancellationToken = default) {
			return ValueTask.CompletedTask;
		}

		/// <inheritdoc/>
		public override ValueTask DropAsync(CancellationToken cancellationToken = default) {
			return ValueTask.CompletedTask;
		}

		/// <inheritdoc/>
		protected override async ValueTask SeedEntitiesAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) {
			var repository = ServiceProvider.GetRequiredService<IRepository<TEntity>>();
			await repository.AddRangeAsync(entities, cancellationToken);
		}
	}
}
