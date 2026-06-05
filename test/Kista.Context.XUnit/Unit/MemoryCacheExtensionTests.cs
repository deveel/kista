using System.ComponentModel.DataAnnotations;

using Kista.Caching;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Kista;

[Trait("Category", "Integration")]
[Trait("Layer", "Manager")]
[Trait("Feature", "MemoryCaching")]
public class MemoryCacheExtensionTests {
    [Fact]
    public void WithMemoryCaching_RegistersCacheForTrackedEntityTypes() {
        var services = new ServiceCollection();
        services.AddRepositoryContext()
            .AddRepository<CachingTestRepository>(_ => { })
            .WithMemoryCaching();

        var cacheDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IEntityCache<CachingTestEntity>));
        Assert.NotNull(cacheDescriptor);
    }

    [Fact]
    public void WithMemoryCaching_DoesNotRegisterForUntrackedEntityTypes() {
        var services = new ServiceCollection();
        services.AddRepositoryContext()
            .WithMemoryCaching();

        var cacheDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IEntityCache<CachingTestEntity>));
        Assert.Null(cacheDescriptor);
    }

    [Fact]
    public void WithMemoryCaching_WithDefaultExpiration_RegistersOptions() {
        var services = new ServiceCollection();
        services.AddRepositoryContext()
            .AddRepository<CachingTestRepository>(_ => { })
            .WithMemoryCaching(opts => opts.DefaultExpiration = TimeSpan.FromMinutes(30));

        var optionsDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IConfigureOptions<EntityCacheOptions<CachingTestEntity>>));
        Assert.NotNull(optionsDescriptor);
    }

    [Fact]
    public void WithMemoryCaching_WithCacheKeyPrefix_RegistersOptions() {
        var services = new ServiceCollection();
        services.AddRepositoryContext()
            .AddRepository<CachingTestRepository>(_ => { })
            .WithMemoryCaching(opts => opts.CacheKeyPrefix = "myapp:");

        var optionsDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IConfigureOptions<EntityCacheOptions<CachingTestEntity>>));
        Assert.NotNull(optionsDescriptor);
    }

    [Fact]
    public void WithMemoryCaching_WithOptionsAppliesToAllEntityTypes() {
        var services = new ServiceCollection();
        services.AddRepositoryContext()
            .AddRepository<CachingTestRepository>(_ => { })
            .AddRepository<SecondCachingRepository>(_ => { })
            .WithMemoryCaching(opts => opts.DefaultExpiration = TimeSpan.FromHours(1));

        var optionsForFirst = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IConfigureOptions<EntityCacheOptions<CachingTestEntity>>));
        var optionsForSecond = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IConfigureOptions<EntityCacheOptions<SecondCachingEntity>>));

        Assert.NotNull(optionsForFirst);
        Assert.NotNull(optionsForSecond);
    }

    [Fact]
    public void WithMemoryCaching_EntitySpecificExpirationOverridesGlobal() {
        var services = new ServiceCollection();
        services.AddRepositoryContext()
            .AddRepository<CachingTestRepository>(_ => { })
            .WithMemoryCaching(opts => opts.DefaultExpiration = TimeSpan.FromHours(1));

        services.AddEntityCacheOptions<CachingTestEntity>(opts => opts.Expiration = TimeSpan.FromMinutes(5));

        var provider = services.BuildServiceProvider();
        var entityOptions = provider.GetRequiredService<IOptions<EntityCacheOptions<CachingTestEntity>>>();

        Assert.NotNull(entityOptions);
        Assert.Equal(TimeSpan.FromMinutes(5), entityOptions.Value.Expiration);
    }

    [Fact]
    public void WithMemoryCaching_EntityOptionsRegisteredAfterBuilder_AreApplied() {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddRepositoryContext()
            .AddRepository<CachingTestRepository>(_ => { })
            .WithMemoryCaching(opts => opts.DefaultExpiration = TimeSpan.FromHours(1));

        var provider = services.BuildServiceProvider();
        var entityOptions = provider.GetRequiredService<IOptions<EntityCacheOptions<CachingTestEntity>>>();

        Assert.Equal(TimeSpan.FromHours(1), entityOptions.Value.Expiration);
    }

    [Fact]
    public void AddEntityMemoryCacheFor_WithEntitySpecificOptions_ResolvesCorrectly() {
        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddEntityMemoryCacheFor<CachingTestEntity>(opts => opts.Expiration = TimeSpan.FromMinutes(10));

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<EntityCacheOptions<CachingTestEntity>>>();

        Assert.Equal(TimeSpan.FromMinutes(10), options.Value.Expiration);
        Assert.NotNull(provider.GetService<IEntityCache<CachingTestEntity>>());
    }

    [Fact]
    public void WithMemoryCaching_WithNoExpiration_DoesNotRegisterOptions() {
        var services = new ServiceCollection();
        services.AddRepositoryContext()
            .AddRepository<CachingTestRepository>(_ => { })
            .WithMemoryCaching();

        var optionsDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IConfigureOptions<EntityCacheOptions<CachingTestEntity>>));
        Assert.Null(optionsDescriptor);
    }

    [Fact]
    public void WithMemoryCaching_ResolvesEntityCache_WhenProviderBuilt() {
        var services = new ServiceCollection();
        services.AddRepositoryContext()
            .AddRepository<CachingTestRepository>(_ => { })
            .WithMemoryCaching();

        services.AddMemoryCache();
        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IEntityCache<CachingTestEntity>>());
    }

    public class SecondCachingEntity {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
    }

    public class SecondCachingRepository : IRepository<SecondCachingEntity> {
        public ValueTask AddAsync(SecondCachingEntity entity, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask AddRangeAsync(IEnumerable<SecondCachingEntity> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask<bool> UpdateAsync(SecondCachingEntity entity, CancellationToken cancellationToken = default) => new(false);
        public ValueTask<bool> RemoveAsync(SecondCachingEntity entity, CancellationToken cancellationToken = default) => new(false);
        public ValueTask RemoveRangeAsync(IEnumerable<SecondCachingEntity> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask<SecondCachingEntity?> FindAsync(object key, CancellationToken cancellationToken = default) => new((SecondCachingEntity?)null);
        public object? GetEntityKey(SecondCachingEntity entity) => (object?)entity.Id;
        public ValueTask<PageResult<SecondCachingEntity>> GetPageAsync(PageRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    public class CachingTestEntity {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
    }

    public class CachingTestRepository : IRepository<CachingTestEntity> {
        public ValueTask AddAsync(CachingTestEntity entity, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask AddRangeAsync(IEnumerable<CachingTestEntity> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask<bool> UpdateAsync(CachingTestEntity entity, CancellationToken cancellationToken = default) => new(false);
        public ValueTask<bool> RemoveAsync(CachingTestEntity entity, CancellationToken cancellationToken = default) => new(false);
        public ValueTask RemoveRangeAsync(IEnumerable<CachingTestEntity> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask<CachingTestEntity?> FindAsync(object key, CancellationToken cancellationToken = default) => new((CachingTestEntity?)null);
        public object? GetEntityKey(CachingTestEntity entity) => (object?)entity.Id;
        public ValueTask<PageResult<CachingTestEntity>> GetPageAsync(PageRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
