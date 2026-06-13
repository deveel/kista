using Kista.Caching;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
[Trait("Feature", "Caching")]
public class EntityManagerBuilderExtensionsTests
{
    [Fact]
    public void Should_RegisterCache_When_WithEasyCachingOnBuilder()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddEasyCaching(options => options.UseInMemory("default"));
        var ctx = services.AddRepositoryContext();
        ctx.AddRepository<InMemoryRepo>(repo => repo
            .WithManagement(mgmt => mgmt.WithEasyCaching()));

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IEntityCache<Person>>());
        Assert.NotNull(provider.GetService<EntityEasyCache<Person>>());
    }

    [Fact]
    public void Should_ConfigureOptions_When_WithEasyCachingWithExpiration()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddEasyCaching(options => options.UseInMemory("default"));
        var ctx = services.AddRepositoryContext();
        ctx.AddRepository<InMemoryRepo>(repo => repo
            .WithManagement(mgmt => mgmt.WithEasyCaching(opts =>
            {
                opts.DefaultExpiration = TimeSpan.FromMinutes(30);
                opts.CacheKeyPrefix = "test";
            })));

        var provider = services.BuildServiceProvider();
        var options = provider.GetService<IOptions<EntityCacheOptions<Person>>>();

        Assert.NotNull(options);
        Assert.Equal(TimeSpan.FromMinutes(30), options.Value.Expiration);
        Assert.Equal("test", options.Value.CacheKeyPrefix);
    }

    [Fact]
    public void Should_NotConfigureOptions_When_WithEasyCachingWithoutConfig()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddEasyCaching(options => options.UseInMemory("default"));
        var ctx = services.AddRepositoryContext();
        ctx.AddRepository<InMemoryRepo>(repo => repo
            .WithManagement(mgmt => mgmt.WithEasyCaching()));

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IEntityCache<Person>>());
    }

    [Fact]
    public void Should_RegisterCacheForEachEntity_When_WithEasyCachingOnContextBuilder()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddEasyCaching(options => options.UseInMemory("default"));
        var ctx = services.AddRepositoryContext();
        ctx.AddRepository<InMemoryRepo>(ServiceLifetime.Singleton);
        ctx.WithEasyCaching(opts =>
        {
            opts.DefaultExpiration = TimeSpan.FromMinutes(15);
        });

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IEntityCache<Person>>());
        Assert.NotNull(provider.GetService<EntityEasyCache<Person>>());
        var options = provider.GetService<IOptions<EntityCacheOptions<Person>>>();
        Assert.NotNull(options);
        Assert.Equal(TimeSpan.FromMinutes(15), options.Value.Expiration);
    }

    [Fact]
    public void Should_RegisterCacheOnContextBuilder_WithoutOptions()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddEasyCaching(options => options.UseInMemory("default"));
        var ctx = services.AddRepositoryContext();
        ctx.AddRepository<InMemoryRepo>(ServiceLifetime.Singleton);
        ctx.WithEasyCaching();

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IEntityCache<Person>>());
        Assert.NotNull(provider.GetService<EntityEasyCache<Person>>());
    }

    private sealed class InMemoryRepo : InMemoryRepository<Person>
    {
        public InMemoryRepo() : base(null, null, null) { }
    }
}
