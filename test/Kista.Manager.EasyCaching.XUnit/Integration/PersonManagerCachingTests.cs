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
		services.AddRepository<InMemoryRepository<Person>>();
		services.AddEasyCaching(options => options.UseInMemory("default"));
		services.AddEntityEasyCacheFor<Person>(options => {
			options.Expiration = TimeSpan.FromMinutes(15);
		});
		services.AddEntityCacheKeyGenerator<PersonCacheKeyGenerator>();
		base.ConfigureServices(services);
	}

	[Fact]
	public async Task Should_FindPersonByEmail_When_EntityExistsInCache() {
		// Arrange
		var cancellationToken = TestContext.Current.CancellationToken;
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
