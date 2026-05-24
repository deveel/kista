using System;

namespace Deveel.Data {
	/// <summary>
	/// Provides lifecycle operations for repositories, including create,
	/// drop, and seed for entity types with or without explicit keys.
	/// </summary>
	public interface IRepositoryLifecycleService {
		/// <summary>
		/// Creates the repository for the given entity type.
		/// </summary>
		/// <typeparam name="TEntity">The type of the entity.</typeparam>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		ValueTask CreateRepositoryAsync<TEntity>(CancellationToken cancellationToken = default)
			where TEntity : class;

		/// <summary>
		/// Creates the repository for the given entity type with a key type.
		/// </summary>
		/// <typeparam name="TEntity">The type of the entity.</typeparam>
		/// <typeparam name="TKey">The type of the entity key.</typeparam>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		ValueTask CreateRepositoryAsync<TEntity, TKey>(CancellationToken cancellationToken = default)
			where TEntity : class;

		/// <summary>
		/// Drops the repository for the given entity type.
		/// </summary>
		/// <typeparam name="TEntity">The type of the entity.</typeparam>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		ValueTask DropRepositoryAsync<TEntity>(CancellationToken cancellationToken = default)
			where TEntity : class;

		/// <summary>
		/// Drops the repository for the given entity type with a key type.
		/// </summary>
		/// <typeparam name="TEntity">The type of the entity.</typeparam>
		/// <typeparam name="TKey">The type of the entity key.</typeparam>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		ValueTask DropRepositoryAsync<TEntity, TKey>(CancellationToken cancellationToken = default)
			where TEntity : class;

		/// <summary>
		/// Seeds the repository for the given entity type with the provided data.
		/// </summary>
		/// <typeparam name="TEntity">The type of the entity.</typeparam>
		/// <param name="seedData">Optional seed data to insert.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		ValueTask SeedRepositoryAsync<TEntity>(object? seedData = null, CancellationToken cancellationToken = default)
			where TEntity : class;

		/// <summary>
		/// Seeds the repository for the given entity type with a key type.
		/// </summary>
		/// <typeparam name="TEntity">The type of the entity.</typeparam>
		/// <typeparam name="TKey">The type of the entity key.</typeparam>
		/// <param name="seedData">Optional seed data to insert.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		ValueTask SeedRepositoryAsync<TEntity, TKey>(object? seedData = null, CancellationToken cancellationToken = default)
			where TEntity : class;
	}
}
