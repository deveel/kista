using System;

namespace Deveel.Data {
	/// <summary>
	/// An obsolete adapter that bridges the legacy <see cref="IRepositoryController"/>
	/// interface to the modern <see cref="IRepositoryLifecycleOrchestrator"/>.
	/// Use <see cref="IRepositoryLifecycleOrchestrator"/> directly instead.
	/// </summary>
	[Obsolete("Use IRepositoryLifecycleOrchestrator instead")]
	public class RepositoryControllerAdapter : IRepositoryController {
		private readonly IRepositoryLifecycleOrchestrator orchestrator;

		/// <summary>
		/// Creates a new adapter wrapping the given orchestrator.
		/// </summary>
		/// <param name="orchestrator">The orchestrator to delegate to.</param>
		public RepositoryControllerAdapter(IRepositoryLifecycleOrchestrator orchestrator) {
			this.orchestrator = orchestrator;
		}

		/// <inheritdoc/>
		public ValueTask CreateRepositoryAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class
			=> orchestrator.CreateRepositoryAsync<TEntity>(cancellationToken);

		/// <inheritdoc/>
		public ValueTask CreateRepositoryAsync<TEntity, TKey>(CancellationToken cancellationToken = default) where TEntity : class
			=> orchestrator.CreateRepositoryAsync<TEntity, TKey>(cancellationToken);

		/// <inheritdoc/>
		public ValueTask DropRepositoryAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class
			=> orchestrator.DropRepositoryAsync<TEntity>(cancellationToken);

		/// <inheritdoc/>
		public ValueTask DropRepositoryAsync<TEntity, TKey>(CancellationToken cancellationToken = default) where TEntity : class
			=> orchestrator.DropRepositoryAsync<TEntity, TKey>(cancellationToken);
	}
}
