using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "Lifecycle")]
public class InMemoryRepositoryLifecycleHandlerTests
{
    private readonly PersonFaker _faker = new();

    [Fact]
    public async Task Should_ReturnFalse_When_CheckingExists()
    {
        var services = new ServiceCollection()
            .AddRepository<InMemoryRepository<Person>>()
            .BuildServiceProvider();
        var handler = new InMemoryRepositoryLifecycleHandler<Person>(services, NullLogger<InMemoryRepositoryLifecycleHandler<Person>>.Instance);

        var exists = await handler.ExistsAsync(TestContext.Current.CancellationToken);

        Assert.False(exists);
    }

    [Fact]
    public async Task Should_NotThrow_When_CreateAsyncCalled()
    {
        var services = new ServiceCollection()
            .AddRepository<InMemoryRepository<Person>>()
            .BuildServiceProvider();
        var handler = new InMemoryRepositoryLifecycleHandler<Person>(services, NullLogger<InMemoryRepositoryLifecycleHandler<Person>>.Instance);

        await handler.CreateAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Should_NotThrow_When_DropAsyncCalled()
    {
        var services = new ServiceCollection()
            .AddRepository<InMemoryRepository<Person>>()
            .BuildServiceProvider();
        var handler = new InMemoryRepositoryLifecycleHandler<Person>(services, NullLogger<InMemoryRepositoryLifecycleHandler<Person>>.Instance);

        await handler.DropAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Should_SeedEntities_When_SeedAsyncCalled()
    {
        var services = new ServiceCollection()
            .AddRepository<InMemoryRepository<Person>>()
            .BuildServiceProvider();
        var handler = new InMemoryRepositoryLifecycleHandler<Person>(services, NullLogger<InMemoryRepositoryLifecycleHandler<Person>>.Instance);
        var entities = _faker.Generate(3);

        await handler.SeedAsync(entities, TestContext.Current.CancellationToken);

        var repository = services.GetRequiredService<IRepository<Person>>();
        var allPeople = await repository.FindAllAsync(QueryFilter.Empty, TestContext.Current.CancellationToken);
        Assert.Equal(3, allPeople.Count());
    }

    [Fact]
    public async Task Should_SeedSingleEntity_When_SingleEntityProvided()
    {
        var services = new ServiceCollection()
            .AddRepository<InMemoryRepository<Person>>()
            .BuildServiceProvider();
        var handler = new InMemoryRepositoryLifecycleHandler<Person>(services, NullLogger<InMemoryRepositoryLifecycleHandler<Person>>.Instance);
        var entity = _faker.Generate();

        await handler.SeedAsync(entity, TestContext.Current.CancellationToken);

        var repository = services.GetRequiredService<IRepository<Person>>();
        var found = await repository.FindAsync(entity.Id!, TestContext.Current.CancellationToken);
        Assert.NotNull(found);
    }

    [Fact]
    public async Task Should_Throw_When_NoRepositoryRegistered()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var handler = new InMemoryRepositoryLifecycleHandler<Person>(services, NullLogger<InMemoryRepositoryLifecycleHandler<Person>>.Instance);

        await Assert.ThrowsAnyAsync<InvalidOperationException>(() =>
            handler.SeedAsync(_faker.Generate(), TestContext.Current.CancellationToken).AsTask());
    }
}
