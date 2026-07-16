using Bogus;

using Microsoft.Extensions.DependencyInjection;

namespace Kista;

[Trait("Category", "Integration")]
[Trait("Layer", "Application")]
[Trait("Feature", "EntityManager")]
public class EntityManagerTests : EntityManagerTestSuite<EntityManager<Person, string>, Person, string> {
	public EntityManagerTests(ITestOutputHelper testOutput) : base(testOutput) {
	}

	protected override Faker<Person> PersonFaker { get; } = new PersonFaker();

	protected override string GenerateKey() => Guid.NewGuid().ToString();

	protected override void SetKey(Person person, string key) {
		person.Id = key;
	}

	protected override void ConfigureServices(IServiceCollection services) {
		services.AddRepositoryContext()
			.AddRepository<InMemoryRepository<Person, string>>(_ => { });
		base.ConfigureServices(services);
	}
}
