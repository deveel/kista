using System;

namespace Deveel.Data {
	/// <summary>
	/// Defines a lifecycle profile that provides environment-specific
	/// seed strategies and seed data for repository initialization.
	/// </summary>
	public interface IRepositoryLifecycleProfile {
		/// <summary>
		/// Gets the <see cref="SeedStrategy"/> to use for the given environment.
		/// </summary>
		/// <param name="environmentName">
		/// The name of the hosting environment, or <c>null</c> if unknown.
		/// </param>
		/// <returns>
		/// The <see cref="SeedStrategy"/> appropriate for the environment.
		/// </returns>
		SeedStrategy GetSeedStrategy(string? environmentName = null);

		/// <summary>
		/// Gets the seed data for the given entity type.
		/// </summary>
		/// <typeparam name="TEntity">The type of the entity to seed.</typeparam>
		/// <returns>
		/// Seed data for the entity type, or <c>null</c> if none is available.
		/// </returns>
		object? GetSeedData<TEntity>() where TEntity : class;

		/// <summary>
		/// Gets the seed data for the given entity type by <see cref="Type"/>.
		/// </summary>
		/// <param name="entityType">The type of the entity to seed.</param>
		/// <returns>
		/// Seed data for the entity type, or <c>null</c> if none is available.
		/// </returns>
		object? GetSeedData(Type entityType);
	}
}
