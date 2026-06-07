using Kista.Caching;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using ZiggyCreatures.Caching.Fusion;

namespace Kista;

[Trait("Category", "Integration")]
[Trait("Layer", "Manager")]
[Trait("Feature", "FusionCaching")]
public class FusionCacheExtensionTests {
    [Fact]
    public void WithFusionCaching_RegistersCacheForTrackedEntityTypes() {
        var services = new ServiceCollection();
        services.AddRepositoryContext()
            .AddRepository<CachingTestRepository>(_ => { })
            .WithFusionCaching();

        var cacheDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IEntityCache<CachingTestEntity>));
        Assert.NotNull(cacheDescriptor);
    }

    [Fact]
    public void WithFusionCaching_DoesNotRegisterForUntrackedEntityTypes() {
        var services = new ServiceCollection();
        services.AddRepositoryContext()
            .WithFusionCaching();

        var cacheDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IEntityCache<CachingTestEntity>));
        Assert.Null(cacheDescriptor);
    }

    [Fact]
    public void WithFusionCaching_WithDefaultDuration_RegistersOptions() {
        var services = new ServiceCollection();
        services.AddRepositoryContext()
            .AddRepository<CachingTestRepository>(_ => { })
            .WithFusionCaching(opts => opts.DefaultEntryDuration = TimeSpan.FromMinutes(30));

        var optionsDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IConfigureOptions<EntityCacheOptions<CachingTestEntity>>));
        Assert.NotNull(optionsDescriptor);
    }

    [Fact]
    public void WithFusionCaching_WithOptionsAppliesToAllEntityTypes() {
        var services = new ServiceCollection();
        services.AddRepositoryContext()
            .AddRepository<CachingTestRepository>(_ => { })
            .AddRepository<SecondCachingRepository>(_ => { })
            .WithFusionCaching(opts => opts.DefaultEntryDuration = TimeSpan.FromHours(1));

        var optionsForFirst = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IConfigureOptions<EntityCacheOptions<CachingTestEntity>>));
        var optionsForSecond = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IConfigureOptions<EntityCacheOptions<SecondCachingEntity>>));

        Assert.NotNull(optionsForFirst);
        Assert.NotNull(optionsForSecond);
    }

    [Fact]
    public void WithFusionCaching_EntitySpecificOptionsOverrideGlobal() {
        var services = new ServiceCollection();
        services.AddRepositoryContext()
            .AddRepository<CachingTestRepository>(_ => { })
            .WithFusionCaching(opts => opts.DefaultEntryDuration = TimeSpan.FromHours(1));

        services.AddEntityCacheOptions<CachingTestEntity>(opts => opts.Expiration = TimeSpan.FromMinutes(5));

        var provider = services.BuildServiceProvider();
        var entityOptions = provider.GetRequiredService<IOptions<EntityCacheOptions<CachingTestEntity>>>();

        Assert.NotNull(entityOptions);
        Assert.Equal(TimeSpan.FromMinutes(5), entityOptions.Value.Expiration);
    }

    [Fact]
    public void WithFusionCaching_GlobalDurationAppliedAsFallback() {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddRepositoryContext()
            .AddRepository<CachingTestRepository>(_ => { })
            .WithFusionCaching(opts => opts.DefaultEntryDuration = TimeSpan.FromHours(1));

        var provider = services.BuildServiceProvider();
        var entityOptions = provider.GetRequiredService<IOptions<EntityCacheOptions<CachingTestEntity>>>();

        Assert.Equal(TimeSpan.FromHours(1), entityOptions.Value.Expiration);
    }

    [Fact]
    public void WithFusionCaching_NoDuration_DoesNotRegisterOptions() {
        var services = new ServiceCollection();
        services.AddRepositoryContext()
            .AddRepository<CachingTestRepository>(_ => { })
            .WithFusionCaching();

        var optionsDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IConfigureOptions<EntityCacheOptions<CachingTestEntity>>));
        Assert.Null(optionsDescriptor);
    }

    [Fact]
    public void WithFusionCaching_FailSafeOptions_AppliedWhenProvided() {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddRepositoryContext()
            .AddRepository<CachingTestRepository>(_ => { })
            .WithFusionCaching(opts => {
                opts.DefaultEntryDuration = TimeSpan.FromMinutes(10);
                opts.FailSafeEnabled = true;
                opts.FailSafeMaxDuration = TimeSpan.FromHours(1);
            });

        var fusionOptions = services.BuildServiceProvider()
            .GetRequiredService<IOptions<FusionCachingOptions>>();

        Assert.True(fusionOptions.Value.FailSafeEnabled);
        Assert.Equal(TimeSpan.FromHours(1), fusionOptions.Value.FailSafeMaxDuration);
    }

    [Fact]
    public void WithFusionCaching_PriorityAndThreshold_AppliedWhenProvided() {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddRepositoryContext()
            .AddRepository<CachingTestRepository>(_ => { })
            .WithFusionCaching(opts => {
                opts.DefaultEntryDuration = TimeSpan.FromMinutes(10);
                opts.Priority = CacheItemPriority.High;
                opts.EagerRefreshThreshold = 0.5f;
            });

        var fusionOptions = services.BuildServiceProvider()
            .GetRequiredService<IOptions<FusionCachingOptions>>();

        Assert.Equal(CacheItemPriority.High, fusionOptions.Value.Priority);
        Assert.Equal(0.5f, fusionOptions.Value.EagerRefreshThreshold);
    }

    [Fact]
    public void AddEntityFusionCacheFor_ResolvesCache_WhenFusionCacheRegistered() {
        var services = new ServiceCollection();
        services.AddFusionCache();
        services.AddEntityFusionCacheFor<CachingTestEntity>(opts => {
            opts.DefaultEntryDuration = TimeSpan.FromMinutes(10);
        });

        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IEntityCache<CachingTestEntity>>());
    }

    [Fact]
    public void WithFusionCaching_ResolvesEntityCache_WhenProviderBuilt() {
        var services = new ServiceCollection();
        services.AddFusionCache();
        services.AddRepositoryContext()
            .AddRepository<CachingTestRepository>(_ => { })
            .WithFusionCaching();

        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IEntityCache<CachingTestEntity>>());
    }

}
