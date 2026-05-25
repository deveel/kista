using System;

namespace Kista {
	/// <summary>
	/// Handles lifecycle operations (existence check, create, drop, seed)
	/// for a specific entity type.
	/// </summary>
	/// <typeparam name="TEntity">The type of the entity managed by the handler.</typeparam>
	public interface IRepositoryLifecycleHandler<TEntity> where TEntity : class {
		/// <summary>
		/// Checks whether the repository for the entity type exists.
		/// </summary>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns><c>true</c> if the repository exists; otherwise <c>false</c>.</returns>
		ValueTask<bool> ExistsAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Creates the repository for the entity type.
		/// </summary>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		ValueTask CreateAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Drops the repository for the entity type.
		/// </summary>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		ValueTask DropAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Seeds the repository with the given data.
		/// </summary>
		/// <param name="seedData">Optional seed data to insert.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		ValueTask SeedAsync(object? seedData = null, CancellationToken cancellationToken = default);
	}
}
