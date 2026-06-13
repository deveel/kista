using Kista.Caching;

using Microsoft.Extensions.Logging.Abstractions;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
[Trait("Feature", "EntityManager")]
public class EntityManagerNoKeyBehaviorTests
{
    private readonly PersonFaker _faker = new();

    [Fact]
    public void Should_ReturnExpectedTracking_When_RepositoryIsInMemory()
    {
        var repository = new InMemoryRepository<Person>();
        var manager = new EntityManager<Person>(repository);

        Assert.False(manager.IsTrackingChanges);
    }

    [Fact]
    public void Should_ThrowObjectDisposed_When_AccessingIsTrackingAfterDispose()
    {
        var repository = new InMemoryRepository<Person>();
        var manager = new EntityManager<Person>(repository);
        manager.Dispose();

        Assert.Throws<ObjectDisposedException>(() => manager.IsTrackingChanges);
    }

    [Fact]
    public void Should_ThrowObjectDisposed_When_AccessingSupportsQueriesAfterDispose()
    {
        var repository = new InMemoryRepository<Person>();
        var manager = new EntityManager<Person>(repository);
        manager.Dispose();

        Assert.Throws<ObjectDisposedException>(() => manager.SupportsQueries);
    }

    [Fact]
    public void Should_ThrowObjectDisposed_When_AccessingSupportsFiltersAfterDispose()
    {
        var repository = new InMemoryRepository<Person>();
        var manager = new EntityManager<Person>(repository);
        manager.Dispose();

        Assert.Throws<ObjectDisposedException>(() => manager.SupportsFilters);
    }

    [Fact]
    public async Task Should_ThrowObjectDisposed_When_AddingAfterDispose()
    {
        var repository = new InMemoryRepository<Person>();
        var manager = new EntityManager<Person>(repository);
        manager.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            manager.AddAsync(_faker.Generate(), TestContext.Current.CancellationToken).AsTask());
    }

    [Fact]
    public async Task Should_ThrowObjectDisposed_When_FindingAfterDispose()
    {
        var repository = new InMemoryRepository<Person>();
        var manager = new EntityManager<Person>(repository);
        manager.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            manager.FindAsync("test", TestContext.Current.CancellationToken).AsTask());
    }

    [Fact]
    public async Task Should_ThrowObjectDisposed_When_UpdatingAfterDispose()
    {
        var repository = new InMemoryRepository<Person>();
        var manager = new EntityManager<Person>(repository);
        manager.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            manager.UpdateAsync(_faker.Generate(), TestContext.Current.CancellationToken).AsTask());
    }

    [Fact]
    public async Task Should_ThrowObjectDisposed_When_RemovingAfterDispose()
    {
        var repository = new InMemoryRepository<Person>();
        var manager = new EntityManager<Person>(repository);
        manager.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            manager.RemoveAsync(_faker.Generate(), TestContext.Current.CancellationToken).AsTask());
    }

    [Fact]
    public void Should_ReturnCorrectCapabilities_ForInMemoryRepository()
    {
        var repository = new InMemoryRepository<Person>();
        var manager = new EntityManager<Person>(repository);

        Assert.True(manager.SupportsQueries);
        Assert.True(manager.SupportsFilters);
        Assert.True(manager.SupportsPaging);
    }

    [Fact]
    public async Task Should_CacheEntity_When_CacheProvided()
    {
        var repository = new InMemoryRepository<Person>();
        using var memoryCache = new Microsoft.Extensions.Caching.Memory.MemoryCache(
            new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
        var cacheOptions = Microsoft.Extensions.Options.Options.Create(
            new EntityCacheOptions<Person> { Expiration = TimeSpan.FromMinutes(5) });
        var entityCache = new EntityMemoryCache<Person>(memoryCache, cacheOptions);
        var manager = new EntityManager<Person>(repository, null, entityCache);

        var person = _faker.Generate();
        var addResult = await manager.AddAsync(person, TestContext.Current.CancellationToken);
        Assert.True(addResult.IsSuccess(), $"Add failed: {addResult.Error?.Message}");

        var findResult = await manager.FindAsync(person.Id!, TestContext.Current.CancellationToken);
        Assert.True(findResult.IsSuccess());
    }

}
