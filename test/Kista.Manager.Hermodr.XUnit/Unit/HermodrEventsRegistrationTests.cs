#pragma warning disable CS8618

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
[Trait("Feature", "DomainEvents")]
[Trait("Feature", "Hermodr")]
public class HermodrEventsRegistrationTests {
	private readonly PersonFaker _faker = new();

	private static readonly Uri ExpectedSource = new("kista://person");

	[Fact]
	public async Task WithHermodrEvents_RegistersHermodrPublisherAndInterceptor() {
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddRepositoryContext()
			.AddRepository<InMemoryRepository<Person, string>>(repo => repo
				.WithManagement(mgmt => mgmt
					.WithHermodrEvents()))
			.UseInMemory();

		var provider = services.BuildServiceProvider();
		using var scope = provider.CreateAsyncScope();
		var scopedProvider = scope.ServiceProvider;

		var publisher = scopedProvider.GetRequiredService<IEntityEventPublisher<Person>>();
		Assert.IsType<HermodrEventPublisher<Person>>(publisher);

		var interceptors = scopedProvider.GetRequiredService<IEnumerable<IEntityManagerInterceptor<Person, string>>>();
		Assert.Contains(interceptors, i => i is EntityEventInterceptor<Person, string>);

		var hermodrPublisher = scopedProvider.GetService<Hermodr.IEventPublisher>();
		Assert.NotNull(hermodrPublisher);
	}

	[Fact]
	public async Task WithHermodrEvents_PublishesCloudEventThroughHermodrPipeline() {
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddRepositoryContext()
			.AddRepository<InMemoryRepository<Person, string>>(repo => repo
				.WithManagement(mgmt => mgmt
					.WithHermodrEvents()))
			.UseInMemory();

		var publishedEvents = new List<CloudNative.CloudEvents.CloudEvent>();
		var hermodrBuilder = services.AddEventPublisher();
		hermodrBuilder.AddTestChannel(e => publishedEvents.Add(e));

		var provider = services.BuildServiceProvider();
		using var scope = provider.CreateAsyncScope();
		var scopedProvider = scope.ServiceProvider;

		var manager = scopedProvider.GetRequiredService<EntityManager<Person, string>>();
		var person = _faker.Generate();
		person.Id = "1";

		await manager.AddAsync(person, TestContext.Current.CancellationToken);

		var evt = Assert.Single(publishedEvents);
		Assert.Equal("kista.entity.created", evt.Type);
		Assert.Equal(ExpectedSource, evt.Source);
	}

	[Fact]
	public void WithHermodrEvents_OnContextBuilder_RegistersForAllEntityTypes() {
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddRepositoryContext()
			.AddRepository<InMemoryRepository<Person, string>>(repo => repo
				.WithManagement())
			.AddRepository<InMemoryRepository<SoftDeletablePerson, string>>(repo => repo
				.WithManagement())
			.WithHermodrEvents()
			.UseInMemory();

		var provider = services.BuildServiceProvider();
		using var scope = provider.CreateAsyncScope();
		var scopedProvider = scope.ServiceProvider;

		var personPublisher = scopedProvider.GetService<IEntityEventPublisher<Person>>();
		Assert.NotNull(personPublisher);
		Assert.IsType<HermodrEventPublisher<Person>>(personPublisher);

		var softDeletablePublisher = scopedProvider.GetService<IEntityEventPublisher<SoftDeletablePerson>>();
		Assert.NotNull(softDeletablePublisher);
		Assert.IsType<HermodrEventPublisher<SoftDeletablePerson>>(softDeletablePublisher);

		var hermodrPublisher = scopedProvider.GetService<Hermodr.IEventPublisher>();
		Assert.NotNull(hermodrPublisher);
	}
}