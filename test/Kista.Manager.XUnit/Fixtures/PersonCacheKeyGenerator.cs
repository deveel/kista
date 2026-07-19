using Kista.Caching;

namespace Kista {
	/// <summary>
	/// A sample implementation of <see cref="IEntityCacheKeyGenerator{TEntity}"/>
	/// used by the caching integration test suites to verify the registration
	/// of cache key generators in the dependency injection container.
	/// </summary>
	public class PersonCacheKeyGenerator : IEntityCacheKeyGenerator<Person> {
		/// <inheritdoc/>
		public string[] GenerateAllKeys(Person entity) => new[] { GenerateKey(entity) };

		/// <inheritdoc/>
		public string GenerateKey(object key) => $"person({key})";
	}
}