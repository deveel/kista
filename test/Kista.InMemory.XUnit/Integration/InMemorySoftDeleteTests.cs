using Bogus;

using Deveel;

using Microsoft.Extensions.DependencyInjection;

namespace Kista;

[Trait("Category", "Integration")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "SoftDelete")]
public class InMemorySoftDeleteTests : SoftDeleteRepositoryTestSuite<SoftDeletablePerson, string, PersonRelationship> {
    public InMemorySoftDeleteTests(ITestOutputHelper outputHelper) : base(outputHelper) {
    }

    protected override Faker<SoftDeletablePerson> PersonFaker { get; } = new SoftDeletablePersonFaker();

    protected override Faker<PersonRelationship> RelationshipFaker => new PersonRelationshipFaker();

    protected override IEnumerable<SoftDeletablePerson> NaturalOrder(IEnumerable<SoftDeletablePerson> source) => source.OrderBy(x => x.Id);

    protected override string GeneratePersonId() => Guid.NewGuid().ToString("N");

    protected override Task AddRelationshipAsync(SoftDeletablePerson person, PersonRelationship relationship) {
        person.Relationships ??= new List<PersonRelationship>();
        person.Relationships.Add(relationship);
        return Task.CompletedTask;
    }

    protected override Task RemoveRelationshipAsync(SoftDeletablePerson person, PersonRelationship relationship) {
        person.Relationships?.Remove(relationship);
        return Task.CompletedTask;
    }

    protected override void ConfigureServices(IServiceCollection services) {
        services.AddRepository<SoftDeletablePersonRepository>();
        services.AddRepositoryController();
        base.ConfigureServices(services);
    }
}

[Trait("Category", "Integration")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "SoftDelete")]
public class InMemorySoftDeleteRestoreTests : SoftDeleteRepositoryTestSuite<SoftDeletablePerson, string, PersonRelationship> {
    public InMemorySoftDeleteRestoreTests(ITestOutputHelper outputHelper) : base(outputHelper) {
    }

    protected override Faker<SoftDeletablePerson> PersonFaker { get; } = new SoftDeletablePersonFaker();

    protected override Faker<PersonRelationship> RelationshipFaker => new PersonRelationshipFaker();

    protected override IEnumerable<SoftDeletablePerson> NaturalOrder(IEnumerable<SoftDeletablePerson> source) => source.OrderBy(x => x.Id);

    protected override string GeneratePersonId() => Guid.NewGuid().ToString("N");

    protected override Task AddRelationshipAsync(SoftDeletablePerson person, PersonRelationship relationship) {
        person.Relationships ??= new List<PersonRelationship>();
        person.Relationships.Add(relationship);
        return Task.CompletedTask;
    }

    protected override Task RemoveRelationshipAsync(SoftDeletablePerson person, PersonRelationship relationship) {
        person.Relationships?.Remove(relationship);
        return Task.CompletedTask;
    }

    protected override void ConfigureServices(IServiceCollection services) {
        services.AddRepository<SoftDeletablePersonRepository>();
        services.AddRepositoryController();
        base.ConfigureServices(services);
    }

    [Fact]
    public async Task Should_RestoreEntity_ThroughEntityManager_BringsEntityBackToQueries() {
        var person = await RandomPersonAsync();
        var personId = Repository.GetEntityKey(person)!;

        var services = new ServiceCollection();
        services.AddSingleton<IUserAccessor<string>>(new StaticUserAccessor("user-42"));
        services.AddLogging();
        var provider = services.BuildServiceProvider();

        var manager = new EntityManager<SoftDeletablePerson, string>(Repository, services: provider);

        var removed = await manager.RemoveAsync(person, TestContext.Current.CancellationToken);
        Assert.True(removed.IsSuccess());

        var foundDefault = await Repository.FindAsync(personId, TestContext.Current.CancellationToken);
        Assert.Null(foundDefault);

			var restored = await manager.RestoreAsync(person, TestContext.Current.CancellationToken);
			Assert.True(restored.IsSuccess());

			var foundRestored = await Repository.FindAsync(personId, TestContext.Current.CancellationToken);
			Assert.NotNull(foundRestored);
			Assert.False(foundRestored!.IsDeleted);
			Assert.Null(foundRestored.DeletedAtUtc);
			Assert.Null(foundRestored.DeletedBy);
		}

    private sealed class StaticUserAccessor : IUserAccessor<string> {
        private readonly string _userId;

        public StaticUserAccessor(string userId) {
            _userId = userId;
        }

        public string? GetUserId() => _userId;
    }
}