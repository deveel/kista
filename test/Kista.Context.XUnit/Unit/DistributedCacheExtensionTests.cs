using Kista.Caching;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Kista;

[Trait("Category", "Integration")]
[Trait("Layer", "Manager")]
[Trait("Feature", "DistributedCaching")]
public class DistributedCacheExtensionTests {
    [Fact]
    public void WithDistributedCaching_RegistersCacheForTrackedEntityTypes() {
        var services = new ServiceCollection();
        services.AddRepositoryContext()
            .AddRepository<CachingTestRepository>(_ => { })
            .WithDistributedCaching();

        var cacheDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IEntityCache<CachingTestEntity>));
        Assert.NotNull(cacheDescriptor);
    }

    [Fact]
    public void WithDistributedCaching_DoesNotRegisterForUntrackedEntityTypes() {
        var services = new ServiceCollection();
        services.AddRepositoryContext()
            .WithDistributedCaching();

        var cacheDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IEntityCache<CachingTestEntity>));
        Assert.Null(cacheDescriptor);
    }

    [Fact]
    public void WithDistributedCaching_WithDefaultExpiration_RegistersOptions() {
        var services = new ServiceCollection();
        services.AddRepositoryContext()
            .AddRepository<CachingTestRepository>(_ => { })
            .WithDistributedCaching(opts => opts.DefaultExpiration = TimeSpan.FromMinutes(30));

        var optionsDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IConfigureOptions<EntityCacheOptions<CachingTestEntity>>));
        Assert.NotNull(optionsDescriptor);
    }

    [Fact]
    public void WithDistributedCaching_WithCacheKeyPrefix_RegistersOptions() {
        var services = new ServiceCollection();
        services.AddRepositoryContext()
            .AddRepository<CachingTestRepository>(_ => { })
            .WithDistributedCaching(opts => opts.CacheKeyPrefix = "myapp:");

        var optionsDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IConfigureOptions<EntityCacheOptions<CachingTestEntity>>));
        Assert.NotNull(optionsDescriptor);
    }

    [Fact]
    public void WithDistributedCaching_WithOptionsAppliesToAllEntityTypes() {
        var services = new ServiceCollection();
        services.AddRepositoryContext()
            .AddRepository<CachingTestRepository>(_ => { })
            .AddRepository<SecondCachingRepository>(_ => { })
            .WithDistributedCaching(opts => opts.DefaultExpiration = TimeSpan.FromHours(1));

        var optionsForFirst = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IConfigureOptions<EntityCacheOptions<CachingTestEntity>>));
        var optionsForSecond = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IConfigureOptions<EntityCacheOptions<SecondCachingEntity>>));

        Assert.NotNull(optionsForFirst);
        Assert.NotNull(optionsForSecond);
    }

    [Fact]
    public void WithDistributedCaching_EntitySpecificExpirationOverridesGlobal() {
        var services = new ServiceCollection();
        services.AddRepositoryContext()
            .AddRepository<CachingTestRepository>(_ => { })
            .WithDistributedCaching(opts => opts.DefaultExpiration = TimeSpan.FromHours(1));

        services.AddOptions<EntityCacheOptions<CachingTestEntity>>().Configure(opts => opts.Expiration = TimeSpan.FromMinutes(5));

        var provider = services.BuildServiceProvider();
        var entityOptions = provider.GetRequiredService<IOptions<EntityCacheOptions<CachingTestEntity>>>();

        Assert.NotNull(entityOptions);
        Assert.Equal(TimeSpan.FromMinutes(5), entityOptions.Value.Expiration);
    }

    [Fact]
    public void WithDistributedCaching_GlobalExpirationAppliedAsFallback() {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddRepositoryContext()
            .AddRepository<CachingTestRepository>(_ => { })
            .WithDistributedCaching(opts => opts.DefaultExpiration = TimeSpan.FromHours(1));

        var provider = services.BuildServiceProvider();
        var entityOptions = provider.GetRequiredService<IOptions<EntityCacheOptions<CachingTestEntity>>>();

        Assert.Equal(TimeSpan.FromHours(1), entityOptions.Value.Expiration);
    }

    [Fact]
    public void WithDistributedCaching_WithNoExpiration_DoesNotRegisterOptions() {
        var services = new ServiceCollection();
        services.AddRepositoryContext()
            .AddRepository<CachingTestRepository>(_ => { })
            .WithDistributedCaching();

        var optionsDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IConfigureOptions<EntityCacheOptions<CachingTestEntity>>));
        Assert.Null(optionsDescriptor);
    }

    [Fact]
    public void AddEntityDistributedCacheFor_ResolvesCache_WhenDistributedCacheRegistered() {
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        services.AddEntityDistributedCacheFor<CachingTestEntity>(opts => opts.Expiration = TimeSpan.FromMinutes(10));

        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IEntityCache<CachingTestEntity>>());
    }

    [Fact]
    public void WithDistributedCaching_ResolvesEntityCache_WhenProviderBuilt() {
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        services.AddRepositoryContext()
            .AddRepository<CachingTestRepository>(_ => { })
            .WithDistributedCaching();

        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IEntityCache<CachingTestEntity>>());
    }

}
