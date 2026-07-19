using Kista.Caching;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
[Trait("Feature", "Caching")]
public class DependencyInjectionTests : EntityCacheDependencyInjectionTestBase {
	[Fact]
	public void Should_ResolveEntityDistributedCache_When_Registered() {
		var services = new ServiceCollection();
		services.AddDistributedMemoryCache();
		services.AddEntityDistributedCacheFor<Person>(options => {
			options.Expiration = TimeSpan.FromMinutes(15);
		});

		var provider = services.BuildServiceProvider();

		Assert.NotNull(provider.GetService<IEntityCache<Person>>());
		Assert.NotNull(provider.GetService<EntityDistributedCache<Person>>());
	}
}