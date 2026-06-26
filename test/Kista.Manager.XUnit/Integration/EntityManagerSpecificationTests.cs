using Bogus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kista;

[Trait("Category", "Integration")]
[Trait("Layer", "Application")]
[Trait("Feature", "Specification")]
#pragma warning disable S1939 // IAsyncLifetime does not implement IAsyncDisposable in this xUnit version
public class EntityManagerSpecificationTests : IAsyncLifetime, IAsyncDisposable {
#pragma warning restore S1939
    private readonly ITestOutputHelper testOutput;
    private AsyncServiceScope scope;
    private readonly Faker<Person> personFaker;

    public EntityManagerSpecificationTests(ITestOutputHelper testOutput) {
        this.testOutput = testOutput;
        personFaker = new Faker<Person>("en")
            .RuleFor(p => p.Id, f => f.Random.Guid().ToString())
            .RuleFor(p => p.FirstName, f => f.Name.FirstName())
            .RuleFor(p => p.LastName, f => f.Name.LastName())
            .RuleFor(p => p.Email, f => f.Internet.Email())
            .RuleFor(p => p.PhoneNumber, f => f.Phone.PhoneNumber())
            .RuleFor(p => p.DateOfBirth, f => f.Date.Past(30).OrNull(f));

        CreateServices();
    }

    private void CreateServices() {
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.AddXUnit(testOutput));
        services.AddRepository<InMemoryRepository<Person, string>>();
        services.AddEntityManager<EntityManager<Person, string>>();
        scope = services.BuildServiceProvider().CreateAsyncScope();
    }

    private IServiceProvider Services => scope.ServiceProvider;
    private IRepository<Person, string> Repository => Services.GetRequiredService<IRepository<Person, string>>();
    private EntityManager<Person, string> Manager => Services.GetRequiredService<EntityManager<Person, string>>();

    public async ValueTask InitializeAsync() {
        var people = personFaker.Generate(50);
        await Repository.AddRangeAsync(people);
    }

    public async Task DisposeAsync() {
        var all = await Repository.FindAllAsync(Query.Empty);
        await Repository.RemoveRangeAsync(all);
    }

    async ValueTask IAsyncDisposable.DisposeAsync() {
        await scope.DisposeAsync();
    }

    private sealed class ActivePersonSpec : Specification<Person> {
        public override IQuery ToQuery() {
            return new Query(QueryFilter.Where<Person>(p => p.Email != null));
        }
    }

    private sealed class FirstNameSpec : Specification<Person> {
        private readonly string firstName;
        public FirstNameSpec(string firstName) => this.firstName = firstName;
        public override IQuery ToQuery() {
            return new Query(QueryFilter.Where<Person>(p => p.FirstName == firstName));
        }
    }

    [Fact]
    public async Task Should_FindFirst_When_SpecMatches() {
        var all = await Repository.FindAllAsync(Query.Empty);
        var targetFirstName = all[0].FirstName;

        var spec = new FirstNameSpec(targetFirstName);
        var result = await Manager.FindFirstAsync(spec);

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Value);
        Assert.Equal(targetFirstName, result.Value.FirstName);
    }

    [Fact]
    public async Task Should_FindAll_When_SpecMatches() {
        var all = await Repository.FindAllAsync(Query.Empty);
        var targetFirstName = all[0].FirstName;

        var spec = new FirstNameSpec(targetFirstName);
        var result = await Manager.FindAllAsync(spec);

        Assert.NotEmpty(result);
        Assert.All(result, p => Assert.Equal(targetFirstName, p.FirstName));
    }

    [Fact]
    public async Task Should_Count_When_SpecMatches() {
        var all = await Repository.FindAllAsync(Query.Empty);
        var targetFirstName = all[0].FirstName;
        var expectedCount = all.Count(p => p.FirstName == targetFirstName);

        var spec = new FirstNameSpec(targetFirstName);
        var count = await Manager.CountAsync(spec);

        Assert.Equal(expectedCount, count);
    }

    [Fact]
    public async Task Should_FindAll_When_AndSpecMatches() {
        var all = await Repository.FindAllAsync(Query.Empty);
        var target = all[0];

        var spec = new FirstNameSpec(target.FirstName) & new ActivePersonSpec();
        var result = await Manager.FindAllAsync(spec);

        Assert.NotEmpty(result);
        Assert.All(result, p => Assert.Equal(target.FirstName, p.FirstName));
    }

    [Fact]
    public async Task Should_FindAll_When_OrSpecMatches() {
        var all = await Repository.FindAllAsync(Query.Empty);
        var target1 = all[0];
        var target2 = all[1];

        var spec = new FirstNameSpec(target1.FirstName) | new FirstNameSpec(target2.FirstName);
        var result = await Manager.FindAllAsync(spec);

        Assert.NotEmpty(result);
        Assert.Contains(result, p => p.FirstName == target1.FirstName);
        Assert.Contains(result, p => p.FirstName == target2.FirstName);
    }

    [Fact]
    public async Task Should_FindAll_When_NotSpecMatches() {
        var all = await Repository.FindAllAsync(Query.Empty);
        var targetFirstName = all[0].FirstName;
        var expectedCount = all.Count(p => p.FirstName != targetFirstName);

        var spec = !new FirstNameSpec(targetFirstName);
        var result = await Manager.FindAllAsync(spec);

        Assert.Equal(expectedCount, result.Count);
        Assert.DoesNotContain(result, p => p.FirstName == targetFirstName);
    }

    [Fact]
    public async Task Should_FindFirst_OnRepository_When_SpecMatches() {
        var all = await Repository.FindAllAsync(Query.Empty);
        var targetFirstName = all[0].FirstName;

        var spec = new FirstNameSpec(targetFirstName);
        var result = await Repository.FindFirstAsync<Person, string>(spec);

        Assert.NotNull(result);
        Assert.Equal(targetFirstName, result.FirstName);
    }

    [Fact]
    public async Task Should_FindAll_OnRepository_When_SpecMatches() {
        var all = await Repository.FindAllAsync(Query.Empty);
        var targetFirstName = all[0].FirstName;

        var spec = new FirstNameSpec(targetFirstName);
        var result = await Repository.FindAllAsync<Person, string>(spec);

        Assert.NotEmpty(result);
        Assert.All(result, p => Assert.Equal(targetFirstName, p.FirstName));
    }

    [Fact]
    public async Task Should_Count_OnRepository_When_SpecMatches() {
        var all = await Repository.FindAllAsync(Query.Empty);
        var targetFirstName = all[0].FirstName;
        var expectedCount = all.Count(p => p.FirstName == targetFirstName);

        var spec = new FirstNameSpec(targetFirstName);
        var count = await Repository.CountAsync<Person, string>(spec);

        Assert.Equal(expectedCount, count);
    }

    [Fact]
    public async Task Should_Exists_OnRepository_When_SpecMatches() {
        var all = await Repository.FindAllAsync(Query.Empty);
        var targetFirstName = all[0].FirstName;

        var spec = new FirstNameSpec(targetFirstName);
        var exists = await Repository.ExistsAsync<Person, string>(spec);

        Assert.True(exists);
    }

    [Fact]
    public async Task Should_NotExist_OnRepository_When_SpecDoesNotMatch() {
        var spec = new FirstNameSpec("__NONEXISTENT__");
        var exists = await Repository.ExistsAsync<Person, string>(spec);

        Assert.False(exists);
    }
}
