#pragma warning disable CS8618

using Hermodr;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
[Trait("Feature", "DomainEvents")]
[Trait("Feature", "Hermodr")]
public class HermodrEventPublisherTests {
	private readonly PersonFaker _faker = new();

	private static readonly Uri ExpectedSource = new("kista://person");
	private static readonly Uri SchemaBaseUri = new("https://schemas.example.com/");

	private Person CreatePerson(string id = "1") {
		var person = _faker.Generate();
		person.Id = id;
		return person;
	}

	private static (EntityManager<Person, string> manager, List<CloudEvent> publishedEvents) BuildManagerWithTestChannel() {
		var repo = Substitute.For<IRepository<Person, string>>();
		repo.GetEntityKey(Arg.Any<Person>()).Returns(c => c.Arg<Person>().Id);
		repo.UpdateAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(true);
		repo.RemoveAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(true);
		repo.HardDeleteAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(true);

		Person? stored = null;
		repo.AddAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>())
			.Returns(c => { stored = c.Arg<Person>(); return ValueTask.CompletedTask; });
		repo.FindAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
			.Returns(c => stored);

		var publishedEvents = new List<CloudEvent>();

		var services = new ServiceCollection();
		services.AddLogging();
		var builder = services.AddEventPublisher();
		builder.AddTestChannel(e => publishedEvents.Add(e));

		services.AddSingleton<IEntityEventPublisher<Person>, HermodrEventPublisher<Person>>();
		services.AddSingleton<IEntityManagerInterceptor<Person, string>, EntityEventInterceptor<Person, string>>();

		var provider = services.BuildServiceProvider();
		var manager = new EntityManager<Person, string>(repo, services: provider);
		return (manager, publishedEvents);
	}

	[Fact]
	public async Task Should_PublishCanonicalCloudEvent_OnCreate() {
		var (manager, publishedEvents) = BuildManagerWithTestChannel();
		var person = CreatePerson("1");

		await manager.AddAsync(person, TestContext.Current.CancellationToken);

		var evt = Assert.Single(publishedEvents);
		Assert.Equal("kista.entity.created", evt.Type);
		Assert.Equal(ExpectedSource, evt.Source);
		Assert.Equal("1", evt.Subject);
		Assert.Equal("application/json", evt.DataContentType);
		Assert.IsType<EntityCreatedData<Person>>(evt.Data);
	}

	[Fact]
	public async Task Should_PublishCanonicalCloudEvent_OnUpdate() {
		var (manager, publishedEvents) = BuildManagerWithTestChannel();
		var person = CreatePerson("1");
		await manager.AddAsync(person, TestContext.Current.CancellationToken);
		publishedEvents.Clear();

		var updated = CreatePerson("1");
		updated.FirstName = "Updated-" + Guid.NewGuid();
		await manager.UpdateAsync(updated, TestContext.Current.CancellationToken);

		var evt = Assert.Single(publishedEvents);
		Assert.Equal("kista.entity.updated", evt.Type);
		Assert.Equal("1", evt.Subject);
		var data = Assert.IsType<EntityUpdatedData<Person>>(evt.Data);
		Assert.NotNull(data.Original);
	}

	[Fact]
	public async Task Should_PublishCanonicalCloudEvent_WithDeleteKindExtension_OnHardDelete() {
		var (manager, publishedEvents) = BuildManagerWithTestChannel();
		var person = CreatePerson("1");
		await manager.AddAsync(person, TestContext.Current.CancellationToken);
		publishedEvents.Clear();

		await manager.HardDeleteAsync(person, TestContext.Current.CancellationToken);

		var evt = Assert.Single(publishedEvents);
		Assert.Equal("kista.entity.deleted", evt.Type);
		Assert.Equal("1", evt.Subject);
		Assert.Equal("Hard", (string?)evt["kistadeletekind"]);
	}

	[Fact]
	public async Task Should_PublishCanonicalCloudEvent_OnRemoveNonSoftDeletable() {
		var (manager, publishedEvents) = BuildManagerWithTestChannel();
		var person = CreatePerson("1");
		await manager.AddAsync(person, TestContext.Current.CancellationToken);
		publishedEvents.Clear();

		await manager.RemoveAsync(person, TestContext.Current.CancellationToken);

		var evt = Assert.Single(publishedEvents);
		Assert.Equal("kista.entity.deleted", evt.Type);
		Assert.Equal("Hard", (string?)evt["kistadeletekind"]);
	}

	[Fact]
	public async Task Should_PublishCanonicalCloudEvent_WithSoftDeleteKind_OnRemoveSoftDeletable() {
		var repo = Substitute.For<IRepository<SoftDeletablePerson, string>>();
		repo.GetEntityKey(Arg.Any<SoftDeletablePerson>()).Returns(c => c.Arg<SoftDeletablePerson>().Id);
		repo.UpdateAsync(Arg.Any<SoftDeletablePerson>(), Arg.Any<CancellationToken>()).Returns(true);
		repo.HardDeleteAsync(Arg.Any<SoftDeletablePerson>(), Arg.Any<CancellationToken>()).Returns(true);

		SoftDeletablePerson? stored = null;
		repo.AddAsync(Arg.Any<SoftDeletablePerson>(), Arg.Any<CancellationToken>())
			.Returns(c => { stored = c.Arg<SoftDeletablePerson>(); return ValueTask.CompletedTask; });
		repo.FindAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
			.Returns(c => stored);

		var publishedEvents = new List<CloudEvent>();

		var services = new ServiceCollection();
		services.AddLogging();
		var builder = services.AddEventPublisher();
		builder.AddTestChannel(e => publishedEvents.Add(e));

		services.AddSingleton<IEntityEventPublisher<SoftDeletablePerson>, HermodrEventPublisher<SoftDeletablePerson>>();
		services.AddSingleton<IEntityManagerInterceptor<SoftDeletablePerson, string>, EntityEventInterceptor<SoftDeletablePerson, string>>();

		var provider = services.BuildServiceProvider();
		var manager = new EntityManager<SoftDeletablePerson, string>(repo, services: provider);

		var faker = new SoftDeletablePersonFaker();
		var person = faker.Generate();
		person.Id = "1";
		await manager.AddAsync(person, TestContext.Current.CancellationToken);
		publishedEvents.Clear(); // SONAR: S4158 — false positive: AddAsync populates the list via the AddTestChannel callback

		await manager.RemoveAsync(person, TestContext.Current.CancellationToken);

		var evt = Assert.Single(publishedEvents);
		Assert.Equal("kista.entity.deleted", evt.Type);
		Assert.Equal("Soft", (string?)evt["kistadeletekind"]);
	}

	[Fact]
	public async Task Should_PublishCanonicalCloudEvent_OnRestore() {
		var repo = Substitute.For<IRepository<SoftDeletablePerson, string>>();
		repo.GetEntityKey(Arg.Any<SoftDeletablePerson>()).Returns(c => c.Arg<SoftDeletablePerson>().Id);
		repo.UpdateAsync(Arg.Any<SoftDeletablePerson>(), Arg.Any<CancellationToken>()).Returns(true);
		repo.HardDeleteAsync(Arg.Any<SoftDeletablePerson>(), Arg.Any<CancellationToken>()).Returns(true);

		SoftDeletablePerson? stored = null;
		repo.AddAsync(Arg.Any<SoftDeletablePerson>(), Arg.Any<CancellationToken>())
			.Returns(c => { stored = c.Arg<SoftDeletablePerson>(); return ValueTask.CompletedTask; });
		repo.FindAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
			.Returns(c => stored);

		var publishedEvents = new List<CloudEvent>();

		var services = new ServiceCollection();
		services.AddLogging();
		var builder = services.AddEventPublisher();
		builder.AddTestChannel(e => publishedEvents.Add(e));

		services.AddSingleton<IEntityEventPublisher<SoftDeletablePerson>, HermodrEventPublisher<SoftDeletablePerson>>();
		services.AddSingleton<IEntityManagerInterceptor<SoftDeletablePerson, string>, EntityEventInterceptor<SoftDeletablePerson, string>>();

		var provider = services.BuildServiceProvider();
		var manager = new EntityManager<SoftDeletablePerson, string>(repo, services: provider);

		var faker = new SoftDeletablePersonFaker();
		var person = faker.Generate();
		person.Id = "1";
		person.IsDeleted = true;
		await manager.AddAsync(person, TestContext.Current.CancellationToken);
		publishedEvents.Clear(); // SONAR: S4158 — false positive: AddAsync populates the list via the AddTestChannel callback

		await manager.RestoreAsync(person, TestContext.Current.CancellationToken);

		var evt = Assert.Single(publishedEvents);
		Assert.Equal("kista.entity.restored", evt.Type);
		Assert.Equal("1", evt.Subject);
		Assert.IsType<EntityRestoredData<SoftDeletablePerson>>(evt.Data);
	}

	[Fact]
	public async Task Should_SetDataSchema_When_DataSchemaBaseUriIsConfigured() {
		var publishedEvents = new List<CloudEvent>();

		var services = new ServiceCollection();
		services.AddLogging();
		var builder = services.AddEventPublisher();
		builder.AddTestChannel(e => publishedEvents.Add(e));

		var options = new HermodrEventsOptions {
			DataSchemaBaseUri = SchemaBaseUri
		};
		services.AddSingleton(options);

		var provider = services.BuildServiceProvider();
		var hermodrPublisher = provider.GetRequiredService<Hermodr.IEventPublisher>();
		var publisher = new HermodrEventPublisher<Person>(hermodrPublisher,
			Microsoft.Extensions.Options.Options.Create(options));

		var person = _faker.Generate();
		person.Id = "1";
		var data = new EntityCreatedData<Person>(person, person.Id, null, DateTimeOffset.UtcNow);

		await publisher.PublishAsync(data, TestContext.Current.CancellationToken);

		var evt = Assert.Single(publishedEvents);
		Assert.Equal("https://schemas.example.com/kista.entity.created", evt.DataSchema?.ToString());
	}

	[Fact]
	public async Task Should_ThrowArgumentOutOfRange_When_PayloadTypeIsUnknown() {
		var services = new ServiceCollection();
		services.AddLogging();
		var builder = services.AddEventPublisher();
		builder.AddTestChannel(_ => { });

		var provider = services.BuildServiceProvider();
		var hermodrPublisher = provider.GetRequiredService<Hermodr.IEventPublisher>();
		var publisher = new HermodrEventPublisher<Person>(hermodrPublisher);

		var person = _faker.Generate();
		person.Id = "1";
		var data = new CustomEventData<Person>(person, person.Id, "actor", DateTimeOffset.UtcNow);

		await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
			publisher.PublishAsync(data, TestContext.Current.CancellationToken).AsTask());
	}

	[Fact]
	public void Should_UseConfiguredSourceUriScheme_When_SetInOptions() {
		var options = new HermodrEventsOptions {
			SourceUriScheme = "myapp"
		};

		Assert.Equal("myapp", options.SourceUriScheme);
		Assert.Null(options.DataSchemaBaseUri);
	}

	private sealed class CustomEventData<TEntity> : EntityEventData<TEntity>
		where TEntity : class {
		public CustomEventData(TEntity entity, object? key, string? actor, DateTimeOffset timestamp)
			: base(entity, EntityOperationKind.Create, key, actor, timestamp) {
		}
	}
}