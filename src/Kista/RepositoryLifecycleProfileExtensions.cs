namespace Kista
{
    /// <summary>
    /// Extension methods for <see cref="IRepositoryLifecycleProfile"/>.
    /// </summary>
    public static class RepositoryLifecycleProfileExtensions {
        /// <summary>
        /// Gets the seed data for the given entity type.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity to seed.</typeparam>
        /// <param name="profile">The lifecycle profile.</param>
        /// <returns>
        /// Seed data for the entity type, or <c>null</c> if none is available.
        /// </returns>
        public static object? GetSeedData<TEntity>(this IRepositoryLifecycleProfile profile)
            where TEntity : class {
            ArgumentNullException.ThrowIfNull(nameof(profile));

            return profile.GetSeedData(typeof(TEntity));
        }
    }
}