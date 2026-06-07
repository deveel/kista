using System;
using System.Diagnostics.CodeAnalysis;

namespace Kista {
	/// <summary>
	/// An obsolete adapter that bridges the legacy <see cref="IRepositoryController"/>
	/// interface to the modern <see cref="IRepositoryLifecycleService"/>.
	/// Use <see cref="IRepositoryLifecycleService"/> directly instead.
	/// </summary>
	[Obsolete("Use IRepositoryLifecycleService instead")]
	[ExcludeFromCodeCoverage]
	public class RepositoryControllerAdapter : IRepositoryController {
		private readonly IRepositoryLifecycleService service;

		/// <summary>
		/// Creates a new adapter wrapping the given lifecycle service.
		/// </summary>
		/// <param name="service">The lifecycle service to delegate to.</param>
		public RepositoryControllerAdapter(IRepositoryLifecycleService service) {
			this.service = service;
		}

		/// <inheritdoc/>
		public ValueTask CreateRepositoryAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class
			=> service.CreateRepositoryAsync<TEntity>(cancellationToken);

		/// <inheritdoc/>
		public ValueTask CreateRepositoryAsync<TEntity, TKey>(CancellationToken cancellationToken = default) where TEntity : class
			=> service.CreateRepositoryAsync<TEntity, TKey>(cancellationToken);

		/// <inheritdoc/>
		public ValueTask DropRepositoryAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class
			=> service.DropRepositoryAsync<TEntity>(cancellationToken);

		/// <inheritdoc/>
		public ValueTask DropRepositoryAsync<TEntity, TKey>(CancellationToken cancellationToken = default) where TEntity : class
			=> service.DropRepositoryAsync<TEntity, TKey>(cancellationToken);
	}
}
