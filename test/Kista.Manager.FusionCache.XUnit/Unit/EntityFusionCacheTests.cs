using Kista.Caching;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using ZiggyCreatures.Caching.Fusion;

namespace Kista;

[Trait("Category", "Integration")]
[Trait("Layer", "Application")]
[Trait("Feature", "Caching")]
public class EntityFusionCacheTests
{
    private readonly PersonFaker _faker = new();

    private static (IServiceProvider, IEntityCache<Person>) CreateCache(Action<FusionCachingOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddFusionCache();
        services.AddEntityFusionCacheFor<Person>(configure);
        var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<IEntityCache<Person>>();
        return (provider, cache);
    }

    [Fact]
    public async Task Should_InvokeFactory_When_KeyNotInCache()
    {
        var (_, cache) = CreateCache();
        var person = _faker.Generate();
        var factoryInvoked = false;

        var result = await cache.GetOrSetAsync($"person:{person.Id}", () =>
        {
            factoryInvoked = true;
            return ValueTask.FromResult<Person?>(person);
        }, TestContext.Current.CancellationToken);

        Assert.True(factoryInvoked);
        Assert.NotNull(result);
        Assert.Equal(person.Id, result.Id);
    }

    [Fact]
    public async Task Should_ReturnCachedValue_When_KeyExists()
    {
        var (provider, cache) = CreateCache();
        var fusionCache = provider.GetRequiredService<IFusionCache>();
        var person = _faker.Generate();
        var cacheKey = $"person:{person.Id}";

        await fusionCache.SetAsync(cacheKey, person);

        var result = await cache.GetOrSetAsync(cacheKey, () => ValueTask.FromResult<Person?>(null), TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(person.Id, result.Id);
    }

    [Fact]
    public async Task Should_ReturnNull_When_FactoryReturnsNull()
    {
        var (_, cache) = CreateCache();

        var result = await cache.GetOrSetAsync("missing", () => ValueTask.FromResult<Person?>(null), TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task Should_CacheValue_After_FactoryInvocation()
    {
        var (_, cache) = CreateCache();
        var person = _faker.Generate();
        var cacheKey = $"person:{person.Id}";
        var invocationCount = 0;

        await cache.GetOrSetAsync(cacheKey, () =>
        {
            invocationCount++;
            return ValueTask.FromResult<Person?>(person);
        }, TestContext.Current.CancellationToken);

        var secondResult = await cache.GetOrSetAsync(cacheKey, () =>
        {
            invocationCount++;
            return ValueTask.FromResult<Person?>(null);
        }, TestContext.Current.CancellationToken);

        Assert.Equal(1, invocationCount);
        Assert.NotNull(secondResult);
        Assert.Equal(person.Id, secondResult.Id);
    }

    [Fact]
    public async Task Should_StoreMultipleKeys_When_SetAsyncCalled()
    {
        var (_, cache) = CreateCache();
        var person = _faker.Generate();
        var keys = new[] { $"person:{person.Id}", $"email:{person.Email}" };

        await cache.SetAsync(keys, person, TestContext.Current.CancellationToken);

        var result = await cache.GetOrSetAsync(keys[0], () => ValueTask.FromResult<Person?>(null), TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(person.Id, result.Id);
    }

    [Fact]
    public async Task Should_RemoveKey_When_RemoveAsyncCalled()
    {
        var (_, cache) = CreateCache();
        var person = _faker.Generate();
        var cacheKey = $"person:{person.Id}";

        await cache.GetOrSetAsync(cacheKey, () => ValueTask.FromResult<Person?>(person), TestContext.Current.CancellationToken);
        await cache.RemoveAsync(new[] { cacheKey }, TestContext.Current.CancellationToken);

        var result = await cache.GetOrSetAsync(cacheKey, () => ValueTask.FromResult<Person?>(null), TestContext.Current.CancellationToken);
        Assert.Null(result);
    }

    [Fact]
    public async Task Should_RemoveMultipleKeys_When_RemoveAsyncCalled()
    {
        var (_, cache) = CreateCache();
        var person = _faker.Generate();
        var keys = new[] { $"person:{person.Id}", $"email:{person.Email}" };

        await cache.SetAsync(keys, person, TestContext.Current.CancellationToken);
        await cache.RemoveAsync(keys, TestContext.Current.CancellationToken);

        foreach (var key in keys)
        {
            var result = await cache.GetOrSetAsync(key, () => ValueTask.FromResult<Person?>(null), TestContext.Current.CancellationToken);
            Assert.Null(result);
        }
    }

    [Fact]
    public async Task Should_UseOptions_When_Configured()
    {
        var (_, cache) = CreateCache(options =>
        {
            options.DefaultEntryDuration = TimeSpan.FromMinutes(30);
            options.FailSafeEnabled = true;
        });
        var person = _faker.Generate();

        await cache.GetOrSetAsync($"person:{person.Id}", () => ValueTask.FromResult<Person?>(person), TestContext.Current.CancellationToken);
        var result = await cache.GetOrSetAsync($"person:{person.Id}", () => ValueTask.FromResult<Person?>(null), TestContext.Current.CancellationToken);

        Assert.NotNull(result);
    }

    [Fact]
    public void Should_ResolveEntityFusionCache_When_Registered()
    {
        var (provider, _) = CreateCache();

        Assert.NotNull(provider.GetRequiredService<IEntityCache<Person>>());
        Assert.NotNull(provider.GetRequiredService<EntityFusionCache<Person>>());
    }

    [Fact]
    public async Task Should_UseFailSafeMaxDuration_When_Configured()
    {
        var (_, cache) = CreateCache(options => {
            options.FailSafeEnabled = true;
            options.FailSafeMaxDuration = TimeSpan.FromHours(2);
        });
        var person = _faker.Generate();

        await cache.GetOrSetAsync($"person:{person.Id}", () => ValueTask.FromResult<Person?>(person), TestContext.Current.CancellationToken);
        var result = await cache.GetOrSetAsync($"person:{person.Id}", () => ValueTask.FromResult<Person?>(null), TestContext.Current.CancellationToken);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Should_UseFactoryTimeouts_When_Configured()
    {
        var (_, cache) = CreateCache(options => {
            options.FailSafeEnabled = true;
            options.FactorySoftTimeout = TimeSpan.FromMilliseconds(100);
            options.FactoryHardTimeout = TimeSpan.FromMilliseconds(500);
        });
        var person = _faker.Generate();

        await cache.GetOrSetAsync($"person:{person.Id}", () => ValueTask.FromResult<Person?>(person), TestContext.Current.CancellationToken);
        var result = await cache.GetOrSetAsync($"person:{person.Id}", () => ValueTask.FromResult<Person?>(null), TestContext.Current.CancellationToken);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Should_UseEagerRefreshThreshold_When_Configured()
    {
        var (_, cache) = CreateCache(options => {
            options.DefaultEntryDuration = TimeSpan.FromMinutes(30);
            options.EagerRefreshThreshold = 0.5f;
        });
        var person = _faker.Generate();

        await cache.GetOrSetAsync($"person:{person.Id}", () => ValueTask.FromResult<Person?>(person), TestContext.Current.CancellationToken);
        var result = await cache.GetOrSetAsync($"person:{person.Id}", () => ValueTask.FromResult<Person?>(null), TestContext.Current.CancellationToken);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Should_UsePriority_When_Configured()
    {
        var (_, cache) = CreateCache(options => {
            options.Priority = CacheItemPriority.High;
        });
        var person = _faker.Generate();

        await cache.GetOrSetAsync($"person:{person.Id}", () => ValueTask.FromResult<Person?>(person), TestContext.Current.CancellationToken);
        var result = await cache.GetOrSetAsync($"person:{person.Id}", () => ValueTask.FromResult<Person?>(null), TestContext.Current.CancellationToken);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Should_UseDefaults_When_NoFusionOptionsConfigured()
    {
        // CreateCache without options -> GetEntryOptions falls back to entity options
        // (null _fusionOptions path) and uses default 5-minute expiration.
        var services = new ServiceCollection();
        services.AddFusionCache();
        services.AddEntityFusionCacheFor<Person>();
        var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<IEntityCache<Person>>();
        var person = _faker.Generate();

        await cache.GetOrSetAsync($"person:{person.Id}", () => ValueTask.FromResult<Person?>(person), TestContext.Current.CancellationToken);
        var result = await cache.GetOrSetAsync($"person:{person.Id}", () => ValueTask.FromResult<Person?>(null), TestContext.Current.CancellationToken);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Should_UseEntityOptionsExpiration_When_FusionOptionsHasNoDuration()
    {
        // _fusionOptions is non-null but DefaultEntryDuration is null,
        // so GetEntryOptions uses _entityOptions.Expiration.
        var services = new ServiceCollection();
        services.AddFusionCache();
        services.AddEntityFusionCacheFor<Person>(options => {
            options.FailSafeEnabled = true;
        });
        // Configure entity options with a specific expiration.
        services.Configure<EntityCacheOptions<Person>>(o => o.Expiration = TimeSpan.FromMinutes(10));
        var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<IEntityCache<Person>>();
        var person = _faker.Generate();

        await cache.GetOrSetAsync($"person:{person.Id}", () => ValueTask.FromResult<Person?>(person), TestContext.Current.CancellationToken);
        var result = await cache.GetOrSetAsync($"person:{person.Id}", () => ValueTask.FromResult<Person?>(null), TestContext.Current.CancellationToken);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Should_NoOp_When_SetAsyncCalledWithEmptyKeys()
    {
        var (_, cache) = CreateCache();
        await cache.SetAsync(Array.Empty<string>(), _faker.Generate(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Should_NoOp_When_RemoveAsyncCalledWithEmptyKeys()
    {
        var (_, cache) = CreateCache();
        await cache.RemoveAsync(Array.Empty<string>(), TestContext.Current.CancellationToken);
    }
}
