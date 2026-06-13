using Microsoft.Extensions.DependencyInjection;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "Lifecycle")]
public class RepositoryLifecycleHandlerTests
{
    private readonly PersonFaker _faker = new();

    private sealed class TestLifecycleHandler : RepositoryLifecycleHandler<Person>
    {
        public List<Person> SeededEntities { get; } = new();

        public override ValueTask<bool> ExistsAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(false);

        public override ValueTask CreateAsync(CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;

        public override ValueTask DropAsync(CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;

        protected override ValueTask SeedEntitiesAsync(IEnumerable<Person> entities, CancellationToken cancellationToken = default)
        {
            SeededEntities.AddRange(entities);
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task Should_SeedEntities_When_SeedDataIsIEnumerableOfTEntity()
    {
        var handler = new TestLifecycleHandler();
        var entities = _faker.Generate(3);

        await handler.SeedAsync(entities, TestContext.Current.CancellationToken);

        Assert.Equal(3, handler.SeededEntities.Count);
    }

    [Fact]
    public async Task Should_SeedEntities_When_SeedDataIsIEnumerableOfObject()
    {
        var handler = new TestLifecycleHandler();
        var entities = _faker.Generate(3).Cast<object>();

        await handler.SeedAsync(entities, TestContext.Current.CancellationToken);

        Assert.Equal(3, handler.SeededEntities.Count);
    }

    [Fact]
    public async Task Should_SeedSingleEntity_When_SeedDataIsSingleTEntity()
    {
        var handler = new TestLifecycleHandler();
        var entity = _faker.Generate();

        await handler.SeedAsync(entity, TestContext.Current.CancellationToken);

        Assert.Single(handler.SeededEntities);
        Assert.Equal(entity.Id, handler.SeededEntities[0].Id);
    }

    [Fact]
    public async Task Should_SkipSeeding_When_SeedDataIsNull()
    {
        var handler = new TestLifecycleHandler();

        await handler.SeedAsync(null, TestContext.Current.CancellationToken);

        Assert.Empty(handler.SeededEntities);
    }

    [Fact]
    public async Task Should_SkipSeeding_When_SeedDataIsEmptyEnumerable()
    {
        var handler = new TestLifecycleHandler();

        await handler.SeedAsync(Enumerable.Empty<Person>(), TestContext.Current.CancellationToken);

        Assert.Empty(handler.SeededEntities);
    }

    [Fact]
    public async Task Should_SkipSeeding_When_SeedDataIsOfWrongType()
    {
        var handler = new TestLifecycleHandler();

        await handler.SeedAsync("wrong-type-data", TestContext.Current.CancellationToken);

        Assert.Empty(handler.SeededEntities);
    }

    [Fact]
    public async Task Should_FilterMatchingTypes_When_SeedDataIsMixedEnumerable()
    {
        var handler = new TestLifecycleHandler();
        var people = _faker.Generate(2);
        var mixed = people.Cast<object>().Concat(new object[] { "string", 42 });

        await handler.SeedAsync(mixed, TestContext.Current.CancellationToken);

        Assert.Equal(2, handler.SeededEntities.Count);
    }
}
