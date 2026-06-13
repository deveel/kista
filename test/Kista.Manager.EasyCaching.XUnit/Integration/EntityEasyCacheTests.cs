using Kista.Caching;

using EasyCaching.Core;
using EasyCaching.InMemory;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Kista;

[Trait("Category", "Integration")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "Caching")]
public class EntityEasyCacheTests
{
    private readonly PersonFaker _faker = new();

    private static (IServiceProvider, IEntityCache<Person>) CreateCache()
    {
        var services = new ServiceCollection();
        services.AddEasyCaching(options => options.UseInMemory("default"));
        services.AddEntityEasyCacheFor<Person>(options =>
        {
            options.Expiration = TimeSpan.FromMinutes(15);
        });
        var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<IEntityCache<Person>>();
        return (provider, cache);
    }

    [Fact]
    public async Task Should_ReturnCachedValue_When_KeyExists()
    {
        var (provider, cache) = CreateCache();
        var easyCache = provider.GetRequiredService<IEasyCachingProvider>();
        var person = _faker.Generate();
        var cacheKey = $"person:{person.Id}";

        await cache.SetAsync(new[] { cacheKey }, person, TestContext.Current.CancellationToken);

        var result = await cache.GetOrSetAsync(cacheKey, () => ValueTask.FromResult<Person?>(null), TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(person.Id, result.Id);
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
    public void Should_ResolveEntityEasyCache_When_Registered()
    {
        var (provider, _) = CreateCache();

        Assert.NotNull(provider.GetRequiredService<IEntityCache<Person>>());
        Assert.NotNull(provider.GetRequiredService<EntityEasyCache<Person, Person>>());
    }
}
