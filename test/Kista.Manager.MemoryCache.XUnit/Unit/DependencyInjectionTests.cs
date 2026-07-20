using Kista.Caching;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
[Trait("Feature", "Caching")]
public class DependencyInjectionTests : EntityCacheDependencyInjectionTestBase {
	[Fact]
	public void Should_ResolveEntityMemoryCache_When_Registered() {
		var services = new ServiceCollection();
		services.AddMemoryCache();
		services.AddEntityMemoryCacheFor<Person>(options => {
			options.Expiration = TimeSpan.FromMinutes(15);
		});

		var provider = services.BuildServiceProvider();

		Assert.NotNull(provider.GetService<IEntityCache<Person>>());
		Assert.NotNull(provider.GetService<EntityMemoryCache<Person>>());
	}
}