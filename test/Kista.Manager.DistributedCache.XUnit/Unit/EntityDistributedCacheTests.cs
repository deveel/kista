using System.Text.Json;

using Kista.Caching;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
[Trait("Feature", "Caching")]
public sealed class EntityDistributedCacheTests : IDisposable
{
    private readonly IDistributedCache _distributedCache;
    private readonly PersonFaker _faker = new();
    private bool _disposed;

    public EntityDistributedCacheTests()
    {
        _distributedCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_distributedCache is IDisposable disposable)
                disposable.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Should_ReturnCachedValue_When_KeyExists()
    {
        var cache = new EntityDistributedCache<Person>(_distributedCache);
        var person = _faker.Generate();
        var cacheKey = $"person:{person.Id}";
        var bytes = JsonSerializer.SerializeToUtf8Bytes(person, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
        });
        await _distributedCache.SetAsync(cacheKey, bytes, TestContext.Current.CancellationToken);

        var result = await cache.GetOrSetAsync(cacheKey, () => ValueTask.FromResult<Person?>(null), TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(person.Id, result.Id);
    }

    [Fact]
    public async Task Should_InvokeFactory_When_KeyNotInCache()
    {
        var cache = new EntityDistributedCache<Person>(_distributedCache);
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
        var cache = new EntityDistributedCache<Person>(_distributedCache);
        var cacheKey = "person:missing";

        var result = await cache.GetOrSetAsync(cacheKey, () => ValueTask.FromResult<Person?>(null), TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task Should_CacheValue_After_FactoryInvocation()
    {
        var cache = new EntityDistributedCache<Person>(_distributedCache);
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
        var cache = new EntityDistributedCache<Person>(_distributedCache);
        var person = _faker.Generate();
        var keys = new[] { $"person:{person.Id}", $"email:{person.Email}" };

        await cache.SetAsync(keys, person, TestContext.Current.CancellationToken);

        foreach (var key in keys)
        {
            var bytes = await _distributedCache.GetAsync(key, TestContext.Current.CancellationToken);
            Assert.NotNull(bytes);
        }
    }

    [Fact]
    public async Task Should_RemoveKey_When_RemoveAsyncCalled()
    {
        var cache = new EntityDistributedCache<Person>(_distributedCache);
        var person = _faker.Generate();
        var cacheKey = $"person:{person.Id}";
        var bytes = JsonSerializer.SerializeToUtf8Bytes(person);
        await _distributedCache.SetAsync(cacheKey, bytes, TestContext.Current.CancellationToken);

        await cache.RemoveAsync(new[] { cacheKey }, TestContext.Current.CancellationToken);

        var afterRemoval = await _distributedCache.GetAsync(cacheKey, TestContext.Current.CancellationToken);
        Assert.Null(afterRemoval);
    }

    [Fact]
    public async Task Should_RemoveMultipleKeys_When_RemoveAsyncCalled()
    {
        var cache = new EntityDistributedCache<Person>(_distributedCache);
        var person = _faker.Generate();
        var keys = new[] { $"person:{person.Id}", $"email:{person.Email}" };
        foreach (var key in keys)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(person);
            await _distributedCache.SetAsync(key, bytes, TestContext.Current.CancellationToken);
        }

        await cache.RemoveAsync(keys, TestContext.Current.CancellationToken);

        foreach (var key in keys)
        {
            var afterRemoval = await _distributedCache.GetAsync(key, TestContext.Current.CancellationToken);
            Assert.Null(afterRemoval);
        }
    }

    [Fact]
    public async Task Should_NotStoreNull_When_FactoryReturnsNull()
    {
        var cache = new EntityDistributedCache<Person>(_distributedCache);
        var cacheKey = "person:missing";

        await cache.GetOrSetAsync(cacheKey, () => ValueTask.FromResult<Person?>(null), TestContext.Current.CancellationToken);

        var stored = await _distributedCache.GetAsync(cacheKey, TestContext.Current.CancellationToken);
        Assert.Null(stored);
    }

    [Fact]
    public async Task Should_UseCustomExpiration_When_Configured()
    {
        var options = Options.Create(new EntityCacheOptions<Person>
        {
            Expiration = TimeSpan.FromMilliseconds(30)
        });
        var cache = new EntityDistributedCache<Person>(_distributedCache, options);
        var person = _faker.Generate();
        var cacheKey = $"person:{person.Id}";

        await cache.GetOrSetAsync(cacheKey, () => ValueTask.FromResult<Person?>(person), TestContext.Current.CancellationToken);

        var stored = await _distributedCache.GetAsync(cacheKey, TestContext.Current.CancellationToken);
        Assert.NotNull(stored);
    }
}
