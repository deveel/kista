using Bogus;

using Microsoft.Extensions.DependencyInjection;

namespace Kista;

[Trait("Category", "Integration")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "InMemoryRepository")]
public class InMemoryRepositoryNoKeyTests : RepositoryTestSuite<Person, PersonRelationship> {
	public InMemoryRepositoryNoKeyTests(ITestOutputHelper outputHelper) : base(outputHelper) {
	}

	protected override Faker<Person> PersonFaker { get; } = new PersonFaker();

	protected override Faker<PersonRelationship> RelationshipFaker => new PersonRelationshipFaker();

	protected override IEnumerable<Person> NaturalOrder(IEnumerable<Person> source) => source.OrderBy(x => x.Id);

	protected override string GeneratePersonId() => Guid.NewGuid().ToString("N");

	protected override Task AddRelationshipAsync(Person person, PersonRelationship relationship) {
		person.Relationships ??= new List<PersonRelationship>();
		person.Relationships.Add(relationship);
		return Task.CompletedTask;
	}

	protected override Task RemoveRelationshipAsync(Person person, PersonRelationship relationship) {
		person.Relationships?.Remove(relationship);
		return Task.CompletedTask;
	}

	protected override void ConfigureServices(IServiceCollection services) {
		AddRepository(services);
		base.ConfigureServices(services);
	}

	protected virtual void AddRepository(IServiceCollection services) {
		services.AddRepository<InMemoryRepository<Person>>();
		services.AddRepositoryController();
	}
}
