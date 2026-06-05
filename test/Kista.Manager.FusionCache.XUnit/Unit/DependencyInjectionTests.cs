using Kista.Caching;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

using ZiggyCreatures.Caching.Fusion;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
[Trait("Feature", "Caching")]
public class DependencyInjectionTests {
    [Fact]
    public void Should_ResolveConfiguredOptions_When_OptionsConfiguredByAction() {
        var services = new ServiceCollection();
        services.AddEntityCacheOptions<Person>(options => {
            options.Expiration = TimeSpan.FromMinutes(15);
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetService<IOptions<EntityCacheOptions<Person>>>();

        Assert.NotNull(options);
        Assert.NotNull(options.Value);
        Assert.Equal(TimeSpan.FromMinutes(15), options.Value.Expiration);
    }

    [Fact]
    public void Should_ResolveConfiguredOptions_When_OptionsBindFromConfiguration() {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                { "EntityCacheOptions:Person:Expiration", "00:15:00" }
            });
        services.AddSingleton<IConfiguration>(config.Build());
        services.AddEntityCacheOptions<Person>("EntityCacheOptions:Person");

        var provider = services.BuildServiceProvider();
        var options = provider.GetService<IOptions<EntityCacheOptions<Person>>>();

        Assert.NotNull(options);
        Assert.NotNull(options.Value);
        Assert.Equal(TimeSpan.FromMinutes(15), options.Value.Expiration);
    }

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

    [Fact]
    public void Should_ResolveKeyGenerator_When_EntityCacheKeyGeneratorRegistered() {
        var services = new ServiceCollection();
        services.AddEntityCacheKeyGenerator<PersonCacheKeyGenerator>();

        var provider = services.BuildServiceProvider();
        var generator = provider.GetService<IEntityCacheKeyGenerator<Person>>();

        Assert.NotNull(generator);
        Assert.IsType<PersonCacheKeyGenerator>(generator);
    }


}
