#pragma warning disable CS8618

using NSubstitute;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
[Trait("Feature", "DomainEvents")]
public class EntityEventInterceptorTests {
	private readonly PersonFaker _faker = new();

	private static readonly DateTimeOffset FixedTimestamp =
		new(2026, 7, 12, 10, 0, 0, TimeSpan.Zero);

	private Person CreatePerson(string id = "1") {
		var person = _faker.Generate();
		person.Id = id;
		return person;
	}

	private sealed class FixedSystemTime : ISystemTime {
		public DateTimeOffset UtcNow => FixedTimestamp;
		public DateTimeOffset Now => UtcNow.ToLocalTime();
	}

	private static (EntityManager<Person, string> manager, CapturingEventPublisher<Person> publisher, Action<Person> store) BuildManager(
		ISystemTime? systemTime = null,
		IUserAccessor<string>? userAccessor = null) {
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

		var publisher = new CapturingEventPublisher<Person>();
		var interceptor = new EntityEventInterceptor<Person, string>(publisher);

		var services = new ServiceCollection();
		services.AddSingleton<IEntityManagerInterceptor<Person, string>>(interceptor);
		if (userAccessor != null)
			services.AddSingleton(userAccessor);
		if (systemTime != null)
			services.AddSingleton(systemTime);

		var provider = services.BuildServiceProvider();
		var manager = new EntityManager<Person, string>(repo, systemTime: systemTime, services: provider);
		return (manager, publisher, p => stored = p);
	}

	[Fact]
	public async Task Should_PublishEntityCreatedData_OnCreate() {
		var (manager, publisher, _) = BuildManager();
		var person = CreatePerson("1");

		await manager.AddAsync(person, TestContext.Current.CancellationToken);

		var evt = Assert.Single(publisher.PublishedEvents);
		var created = Assert.IsType<EntityCreatedData<Person>>(evt);
		Assert.Same(person, created.Entity);
		Assert.Equal(EntityOperationKind.Create, created.OperationKind);
		Assert.Equal("1", created.Key);
	}

	[Fact]
	public async Task Should_PublishEntityUpdatedData_WithOriginalPreImage_OnUpdate() {
		var repo = Substitute.For<IRepository<Person, string>>();
		repo.GetEntityKey(Arg.Any<Person>()).Returns(c => c.Arg<Person>().Id);
		repo.UpdateAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(true);
		var original = CreatePerson("1");
		original.FirstName = "Old";
		repo.FindAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(original);

		var publisher = new CapturingEventPublisher<Person>();
		var interceptor = new EntityEventInterceptor<Person, string>(publisher);
		var services = new ServiceCollection();
		services.AddSingleton<IEntityManagerInterceptor<Person, string>>(interceptor);
		var provider = services.BuildServiceProvider();
		var manager = new EntityManager<Person, string>(repo, services: provider);

		var updated = CreatePerson("1");
		updated.FirstName = "New";
		await manager.UpdateAsync(updated, TestContext.Current.CancellationToken);

		var evt = Assert.Single(publisher.PublishedEvents);
		var updatedEvt = Assert.IsType<EntityUpdatedData<Person>>(evt);
		Assert.Same(updated, updatedEvt.Entity);
		Assert.Equal(EntityOperationKind.Update, updatedEvt.OperationKind);
		Assert.NotNull(updatedEvt.Original);
		Assert.Equal("Old", updatedEvt.Original!.FirstName);
		Assert.Equal("1", updatedEvt.Key);
	}

	[Fact]
	public async Task Should_PublishEntityDeletedData_WithHardKind_OnRemoveNonSoftDeletable() {
		var (manager, publisher, _) = BuildManager();
		var person = CreatePerson("1");
		await manager.AddAsync(person, TestContext.Current.CancellationToken);
		publisher.PublishedEvents.Clear();

		await manager.RemoveAsync(person, TestContext.Current.CancellationToken);

		var evt = Assert.Single(publisher.PublishedEvents);
		var deleted = Assert.IsType<EntityDeletedData<Person>>(evt);
		Assert.Equal(EntityDeleteKind.Hard, deleted.DeleteKind);
		Assert.Same(person, deleted.Entity);
		Assert.Equal("1", deleted.Key);
	}

	[Fact]
	public async Task Should_PublishEntityDeletedData_WithHardKind_OnHardDelete() {
		var (manager, publisher, _) = BuildManager();
		var person = CreatePerson("1");
		await manager.AddAsync(person, TestContext.Current.CancellationToken);
		publisher.PublishedEvents.Clear();

		await manager.HardDeleteAsync(person, TestContext.Current.CancellationToken);

		var evt = Assert.Single(publisher.PublishedEvents);
		var deleted = Assert.IsType<EntityDeletedData<Person>>(evt);
		Assert.Equal(EntityDeleteKind.Hard, deleted.DeleteKind);
		Assert.Equal(EntityOperationKind.HardDelete, deleted.OperationKind);
	}

	[Fact]
	public async Task Should_NotPublishEvent_When_WriteDoesNotChange() {
		var repo = Substitute.For<IRepository<Person, string>>();
		repo.GetEntityKey(Arg.Any<Person>()).Returns(c => c.Arg<Person>().Id);
		repo.UpdateAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(false);

		var original = CreatePerson("1");
		repo.FindAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(original);

		var publisher = new CapturingEventPublisher<Person>();
		var interceptor = new EntityEventInterceptor<Person, string>(publisher);
		var services = new ServiceCollection();
		services.AddSingleton<IEntityManagerInterceptor<Person, string>>(interceptor);
		var provider = services.BuildServiceProvider();
		var manager = new EntityManager<Person, string>(repo, services: provider);

		var person = CreatePerson("1");
		await manager.UpdateAsync(person, TestContext.Current.CancellationToken);

		Assert.Empty(publisher.PublishedEvents);
	}

	[Fact]
	public async Task Should_SwallowPublisherFailure_And_LeaveWriteSucceeded() {
		var repo = Substitute.For<IRepository<Person, string>>();
		repo.GetEntityKey(Arg.Any<Person>()).Returns(c => c.Arg<Person>().Id);
		repo.AddAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(ValueTask.CompletedTask);

		var publisher = new ThrowingEventPublisher<Person>();
		var interceptor = new EntityEventInterceptor<Person, string>(publisher);
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSingleton<IEntityManagerInterceptor<Person, string>>(interceptor);
		var provider = services.BuildServiceProvider();
		var manager = new EntityManager<Person, string>(repo, services: provider);

		var person = CreatePerson("1");
		var result = await manager.AddAsync(person, TestContext.Current.CancellationToken);

		Assert.True(result.IsSuccess());
	}

	[Fact]
	public async Task Should_PublishEvent_WithActorAndTimestamp_FromContext() {
		var systemTime = new FixedSystemTime();
		var userAccessor = Substitute.For<IUserAccessor<string>>();
		userAccessor.GetUserId().Returns("user-42");

		var (manager, publisher, _) = BuildManager(systemTime: systemTime, userAccessor: userAccessor);
		var person = CreatePerson("1");

		await manager.AddAsync(person, TestContext.Current.CancellationToken);

		var evt = Assert.Single(publisher.PublishedEvents);
		Assert.Equal("user-42", evt.Actor);
		Assert.Equal(FixedTimestamp, evt.Timestamp);
	}

	[Fact]
	public async Task Should_PublishEvent_ThroughSingleKeyInterceptor_When_UsingEntityManagerNoKey() {
		var repo = new InMemoryRepository<Person>();
		var publisher = new CapturingEventPublisher<Person>();
		var interceptor = new EntityEventInterceptor<Person>(publisher);

		var services = new ServiceCollection();
		services.AddSingleton(typeof(IEntityManagerInterceptor<Person>), interceptor);
		var provider = services.BuildServiceProvider();
		var manager = new EntityManager<Person>(repo, services: provider);

		var person = CreatePerson("1");
		await manager.AddAsync(person, TestContext.Current.CancellationToken);

		var evt = Assert.Single(publisher.PublishedEvents);
		var created = Assert.IsType<EntityCreatedData<Person>>(evt);
		Assert.Same(person, created.Entity);
		Assert.Equal(EntityOperationKind.Create, created.OperationKind);
	}
}