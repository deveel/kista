namespace Kista
{
    /// <summary>
    /// Provides strongly-typed seed data for a specific entity type.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity to seed.</typeparam>
    public interface IRepositorySeedDataProvider<out TEntity> : IRepositorySeedDataProvider
        where TEntity : class {
        /// <summary>
        /// Retrieves the seed data as a collection of <typeparamref name="TEntity"/> instances.
        /// </summary>
        /// <returns>An enumerable of seed data entities.</returns>
        new IEnumerable<TEntity> GetSeedData();
    }
}