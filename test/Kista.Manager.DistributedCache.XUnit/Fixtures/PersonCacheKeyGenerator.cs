using Kista.Caching;

namespace Kista;

public class PersonCacheKeyGenerator : IEntityCacheKeyGenerator<Person> {
	public string[] GenerateAllKeys(Person entity) => new[] { GenerateKey(entity) };

	public string GenerateKey(object key) => $"person({key})";
}
