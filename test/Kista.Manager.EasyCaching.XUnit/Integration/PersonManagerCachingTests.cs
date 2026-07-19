using Bogus;

using Kista.Caching;

using Microsoft.Extensions.DependencyInjection;

namespace Kista;

[Trait("Category", "Integration")]
[Trait("Layer", "Application")]
[Trait("Feature", "Caching")]
public class PersonManagerCachingTests : EntityManagerTestSuite<PersonManager, Person, string> {
	public PersonManagerCachingTests(ITestOutputHelper testOutput) : base(testOutput) {
	}

	protected override Faker<Person> PersonFaker { get; } = new PersonFaker();

	protected override string GenerateKey() => Guid.NewGuid().ToString();

	protected override void SetKey(Person person, string key) {
		person.Id = key;
	}

	protected override void ConfigureServices(IServiceCollection services) {
		services.AddEasyCaching(options => options.UseInMemory("default"));
		services.AddRepositoryContext()
			.AddRepository<InMemoryRepository<Person, string>>(repo => repo
				.WithManagement(mgmt => {
					mgmt.WithEasyCaching(options => {
						options.DefaultExpiration = TimeSpan.FromMinutes(15);
					});
					mgmt.WithCacheKeyGenerator<PersonCacheKeyGenerator>();
				}));
		base.ConfigureServices(services);
	}

	[Fact]
	public async Task Should_FindPersonByEmail_When_EntityExistsInCache() {
		// Arrange
		var person = People.Random(x => x.Email != null);

		Assert.NotNull(person);
		Assert.NotNull(person.Email);

		// Act
		var found = await Manager.FindByEmailAsync(person.Email);

		// Assert
		Assert.NotNull(found);
		Assert.Equal(person.Id, found.Id);
		Assert.Equal(person.Email, found.Email);
	}
}
