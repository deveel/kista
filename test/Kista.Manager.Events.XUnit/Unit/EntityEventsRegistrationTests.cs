#pragma warning disable CS8618

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
[Trait("Feature", "DomainEvents")]
public class EntityEventsRegistrationTests {
	private readonly PersonFaker _faker = new();

	[Fact]
	public async Task Should_ResolveInterceptorAndPublisher_When_WithEntityEventsCalled() {
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddRepositoryContext()
			.AddRepository<InMemoryRepository<Person, string>>(repo => repo
				.WithManagement(mgmt => mgmt
					.WithEntityEvents()))
			.UseInMemory();

		var provider = services.BuildServiceProvider();
		using var scope = provider.CreateAsyncScope();
		var scopedProvider = scope.ServiceProvider;

		var interceptors = scopedProvider.GetRequiredService<IEnumerable<IEntityManagerInterceptor<Person, string>>>();
		Assert.Contains(interceptors, i => i is EntityEventInterceptor<Person, string>);

		var publisher = scopedProvider.GetRequiredService<IEntityEventPublisher<Person>>();
		Assert.IsType<InMemoryEntityEventPublisher<Person>>(publisher);

		var manager = scopedProvider.GetRequiredService<EntityManager<Person, string>>();
		var person = _faker.Generate();
		person.Id = "1";

		await manager.AddAsync(person, TestContext.Current.CancellationToken);

		var inMemoryPublisher = (InMemoryEntityEventPublisher<Person>)publisher;
		var evt = Assert.Single(inMemoryPublisher.PublishedEvents);
		Assert.IsType<EntityCreatedData<Person>>(evt);
	}

	[Fact]
	public void Should_RegisterInMemoryPublisher_AsIEntityEventPublisher() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.AddRepository<InMemoryRepository<Person, string>>(repo => repo
				.WithManagement(mgmt => mgmt
					.WithEntityEvents()))
			.UseInMemory();

		var provider = services.BuildServiceProvider();
		using var scope = provider.CreateAsyncScope();

		var publisher = scope.ServiceProvider.GetService<IEntityEventPublisher<Person>>();
		Assert.NotNull(publisher);
		Assert.IsType<InMemoryEntityEventPublisher<Person>>(publisher);
	}

	[Fact]
	public void Should_NotRegisterInterceptor_When_WithEntityEventsNotCalled() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.AddRepository<InMemoryRepository<Person, string>>(repo => repo
				.WithManagement())
			.UseInMemory();

		var provider = services.BuildServiceProvider();
		using var scope = provider.CreateAsyncScope();

		var publisher = scope.ServiceProvider.GetService<IEntityEventPublisher<Person>>();
		Assert.Null(publisher);
	}

	[Fact]
	public void Should_RegisterForAllEntityTypes_When_WithEntityEventsCalledOnContextBuilder() {
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddRepositoryContext()
			.AddRepository<InMemoryRepository<Person, string>>(repo => repo
				.WithManagement())
			.AddRepository<InMemoryRepository<SoftDeletablePerson, string>>(repo => repo
				.WithManagement())
			.WithEntityEvents()
			.UseInMemory();

		var provider = services.BuildServiceProvider();
		using var scope = provider.CreateAsyncScope();
		var scopedProvider = scope.ServiceProvider;

		var personPublisher = scopedProvider.GetService<IEntityEventPublisher<Person>>();
		Assert.NotNull(personPublisher);
		Assert.IsType<InMemoryEntityEventPublisher<Person>>(personPublisher);

		var softDeletablePublisher = scopedProvider.GetService<IEntityEventPublisher<SoftDeletablePerson>>();
		Assert.NotNull(softDeletablePublisher);
		Assert.IsType<InMemoryEntityEventPublisher<SoftDeletablePerson>>(softDeletablePublisher);

		var personInterceptors = scopedProvider.GetRequiredService<IEnumerable<IEntityManagerInterceptor<Person, string>>>();
		Assert.Contains(personInterceptors, i => i is EntityEventInterceptor<Person, string>);

		var softDeletableInterceptors = scopedProvider.GetRequiredService<IEnumerable<IEntityManagerInterceptor<SoftDeletablePerson, string>>>();
		Assert.Contains(softDeletableInterceptors, i => i is EntityEventInterceptor<SoftDeletablePerson, string>);
	}
}