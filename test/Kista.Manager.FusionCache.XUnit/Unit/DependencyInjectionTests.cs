using Kista.Caching;

using Microsoft.Extensions.Caching.Memory;

using ZiggyCreatures.Caching.Fusion;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
[Trait("Feature", "Caching")]
public class DependencyInjectionTests : EntityCacheDependencyInjectionTestBase {
	[Fact]
	public void Should_ResolveEntityFusionCache_When_Registered() {
		var services = new ServiceCollection();
		services.AddFusionCache();
		services.AddEntityFusionCacheFor<Person>(options => {
			options.DefaultEntryDuration = TimeSpan.FromMinutes(15);
		});

		var provider = services.BuildServiceProvider();

		Assert.NotNull(provider.GetService<IEntityCache<Person>>());
		Assert.NotNull(provider.GetService<EntityFusionCache<Person>>());
	}
}