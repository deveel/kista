using Kista.Caching;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
[Trait("Feature", "Caching")]
public class DependencyInjectionTests {
    [Fact]
    public void Should_ResolveConfiguredOptions_When_OptionsConfiguredByAction() {
        var services = new ServiceCollection();
        services.AddOptions<EntityCacheOptions<Person>>().Configure(options => {
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
        services.AddOptions<EntityCacheOptions<Person>>().BindConfiguration("EntityCacheOptions:Person");

        var provider = services.BuildServiceProvider();
        var options = provider.GetService<IOptions<EntityCacheOptions<Person>>>();

        Assert.NotNull(options);
        Assert.NotNull(options.Value);
        Assert.Equal(TimeSpan.FromMinutes(15), options.Value.Expiration);
    }

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

    [Fact]
    public void Should_ResolveKeyGenerator_When_EntityCacheKeyGeneratorRegistered() {
        var services = new ServiceCollection();
        services.AddRepositoryContext()
            .AddRepository<InMemoryRepository<Person, string>>(repo => repo
                .WithManagement(mgmt => mgmt.WithCacheKeyGenerator<PersonCacheKeyGenerator>()));

        var provider = services.BuildServiceProvider();
        var generator = provider.GetService<IEntityCacheKeyGenerator<Person>>();

        Assert.NotNull(generator);
        Assert.IsType<PersonCacheKeyGenerator>(generator);
    }


}
