using Kista.Caching;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
[Trait("Feature", "Caching")]
public sealed class EntityMemoryCacheTests : IDisposable
{
    private readonly IMemoryCache _memoryCache;
    private readonly PersonFaker _faker = new();
    private bool _disposed;

    public EntityMemoryCacheTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _memoryCache.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Should_ReturnCachedValue_When_KeyExists()
    {
        var cache = new EntityMemoryCache<Person>(_memoryCache);
        var person = _faker.Generate();
        var cacheKey = $"person:{person.Id}";

        _memoryCache.Set(cacheKey, person);

        var result = await cache.GetOrSetAsync(cacheKey, () => ValueTask.FromResult<Person?>(null), TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(person.Id, result.Id);
    }

    [Fact]
    public async Task Should_InvokeFactory_When_KeyNotInCache()
    {
        var cache = new EntityMemoryCache<Person>(_memoryCache);
        var person = _faker.Generate();
        var cacheKey = $"person:{person.Id}";
        var factoryInvoked = false;

        var result = await cache.GetOrSetAsync(cacheKey, () =>
        {
            factoryInvoked = true;
            return ValueTask.FromResult<Person?>(person);
        }, TestContext.Current.CancellationToken);

        Assert.True(factoryInvoked);
        Assert.NotNull(result);
        Assert.Equal(person.Id, result.Id);
    }

    [Fact]
    public async Task Should_ReturnNull_When_FactoryReturnsNull()
    {
        var cache = new EntityMemoryCache<Person>(_memoryCache);
        var cacheKey = "person:missing";

        var result = await cache.GetOrSetAsync(cacheKey, () => ValueTask.FromResult<Person?>(null), TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task Should_CacheValue_After_FactoryInvocation()
    {
        var cache = new EntityMemoryCache<Person>(_memoryCache);
        var person = _faker.Generate();
        var cacheKey = $"person:{person.Id}";
        var invocationCount = 0;

        await cache.GetOrSetAsync(cacheKey, () =>
        {
            invocationCount++;
            return ValueTask.FromResult<Person?>(person);
        }, TestContext.Current.CancellationToken);

        var second = await cache.GetOrSetAsync(cacheKey, () =>
        {
            invocationCount++;
            return ValueTask.FromResult<Person?>(null);
        }, TestContext.Current.CancellationToken);

        Assert.Equal(1, invocationCount);
        Assert.NotNull(second);
        Assert.Equal(person.Id, second.Id);
    }

    [Fact]
    public async Task Should_StoreMultipleKeys_When_SetAsyncCalled()
    {
        var cache = new EntityMemoryCache<Person>(_memoryCache);
        var person = _faker.Generate();
        var keys = new[] { $"person:{person.Id}", $"email:{person.Email}" };

        await cache.SetAsync(keys, person, TestContext.Current.CancellationToken);

        foreach (var key in keys)
        {
            Assert.True(_memoryCache.TryGetValue(key, out Person? cached));
            Assert.Equal(person.Id, cached!.Id);
        }
    }

    [Fact]
    public async Task Should_NotFail_When_SetAsyncCalledWithEmptyKeys()
    {
        var cache = new EntityMemoryCache<Person>(_memoryCache);
        var person = _faker.Generate();

        await cache.SetAsync(Array.Empty<string>(), person, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Should_RemoveKey_When_RemoveAsyncCalled()
    {
        var cache = new EntityMemoryCache<Person>(_memoryCache);
        var person = _faker.Generate();
        var cacheKey = $"person:{person.Id}";
        _memoryCache.Set(cacheKey, person);

        await cache.RemoveAsync(new[] { cacheKey }, TestContext.Current.CancellationToken);

        Assert.False(_memoryCache.TryGetValue(cacheKey, out _));
    }

    [Fact]
    public async Task Should_RemoveMultipleKeys_When_RemoveAsyncCalled()
    {
        var cache = new EntityMemoryCache<Person>(_memoryCache);
        var person = _faker.Generate();
        var keys = new[] { $"person:{person.Id}", $"email:{person.Email}" };
        foreach (var key in keys)
            _memoryCache.Set(key, person);

        await cache.RemoveAsync(keys, TestContext.Current.CancellationToken);

        foreach (var key in keys)
            Assert.False(_memoryCache.TryGetValue(key, out _));
    }

    [Fact]
    public async Task Should_UseExpiration_When_Configured()
    {
        var options = Options.Create(new EntityCacheOptions<Person>
        {
            Expiration = TimeSpan.FromMilliseconds(50)
        });
        var cache = new EntityMemoryCache<Person>(_memoryCache, options);
        var person = _faker.Generate();
        var cacheKey = $"person:{person.Id}";

        await cache.GetOrSetAsync(cacheKey, () => ValueTask.FromResult<Person?>(person), TestContext.Current.CancellationToken);
        Assert.True(_memoryCache.TryGetValue(cacheKey, out _));

        await Task.Delay(100);

        Assert.False(_memoryCache.TryGetValue(cacheKey, out _));
    }
}
