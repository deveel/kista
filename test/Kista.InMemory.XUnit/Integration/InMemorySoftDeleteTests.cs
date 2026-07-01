using Bogus;

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
        services.AddRepository<InMemoryRepository<SoftDeletablePerson>>();
        services.AddRepositoryController();
        base.ConfigureServices(services);
    }
}