#pragma warning disable CS8618

using NSubstitute;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
[Trait("Feature", "DomainEvents")]
public class EntityEventSoftDeleteTests {
	private readonly SoftDeletablePersonFaker _faker = new();

	private SoftDeletablePerson CreatePerson(string id = "1") {
		var person = _faker.Generate();
		person.Id = id;
		return person;
	}

	private static (EntityManager<SoftDeletablePerson, string> manager, CapturingEventPublisher<SoftDeletablePerson> publisher) BuildManager() {
		var repo = Substitute.For<IRepository<SoftDeletablePerson, string>>();
		repo.GetEntityKey(Arg.Any<SoftDeletablePerson>()).Returns(c => c.Arg<SoftDeletablePerson>().Id);
		repo.UpdateAsync(Arg.Any<SoftDeletablePerson>(), Arg.Any<CancellationToken>()).Returns(true);
		repo.RemoveAsync(Arg.Any<SoftDeletablePerson>(), Arg.Any<CancellationToken>()).Returns(true);
		repo.HardDeleteAsync(Arg.Any<SoftDeletablePerson>(), Arg.Any<CancellationToken>()).Returns(true);

		SoftDeletablePerson? stored = null;
		repo.AddAsync(Arg.Any<SoftDeletablePerson>(), Arg.Any<CancellationToken>())
			.Returns(c => { stored = c.Arg<SoftDeletablePerson>(); return ValueTask.CompletedTask; });
		repo.FindAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
			.Returns(c => stored);

		var publisher = new CapturingEventPublisher<SoftDeletablePerson>();
		var interceptor = new EntityEventInterceptor<SoftDeletablePerson, string>(publisher);

		var services = new ServiceCollection();
		services.AddSingleton<IEntityManagerInterceptor<SoftDeletablePerson, string>>(interceptor);
		var provider = services.BuildServiceProvider();
		var manager = new EntityManager<SoftDeletablePerson, string>(repo, services: provider);
		return (manager, publisher);
	}

	[Fact]
	public async Task Should_PublishEntityDeletedData_WithSoftKind_OnRemoveSoftDeletable() {
		var (manager, publisher) = BuildManager();
		var person = CreatePerson("1");
		await manager.AddAsync(person, TestContext.Current.CancellationToken);
		publisher.PublishedEvents.Clear();

		await manager.RemoveAsync(person, TestContext.Current.CancellationToken);

		var evt = Assert.Single(publisher.PublishedEvents);
		var deleted = Assert.IsType<EntityDeletedData<SoftDeletablePerson>>(evt);
		Assert.Equal(EntityDeleteKind.Soft, deleted.DeleteKind);
		Assert.Equal(EntityOperationKind.Remove, deleted.OperationKind);
		Assert.Same(person, deleted.Entity);
		Assert.Equal("1", deleted.Key);
	}

	[Fact]
	public async Task Should_PublishEntityDeletedData_WithHardKind_OnHardDeleteSoftDeletable() {
		var (manager, publisher) = BuildManager();
		var person = CreatePerson("1");
		await manager.AddAsync(person, TestContext.Current.CancellationToken);
		publisher.PublishedEvents.Clear();

		await manager.HardDeleteAsync(person, TestContext.Current.CancellationToken);

		var evt = Assert.Single(publisher.PublishedEvents);
		var deleted = Assert.IsType<EntityDeletedData<SoftDeletablePerson>>(evt);
		Assert.Equal(EntityDeleteKind.Hard, deleted.DeleteKind);
		Assert.Equal(EntityOperationKind.HardDelete, deleted.OperationKind);
	}

	[Fact]
	public async Task Should_PublishEntityRestoredData_OnRestore() {
		var (manager, publisher) = BuildManager();
		var person = CreatePerson("1");
		person.IsDeleted = true;
		await manager.AddAsync(person, TestContext.Current.CancellationToken);
		publisher.PublishedEvents.Clear();

		await manager.RestoreAsync(person, TestContext.Current.CancellationToken);

		var evt = Assert.Single(publisher.PublishedEvents);
		var restored = Assert.IsType<EntityRestoredData<SoftDeletablePerson>>(evt);
		Assert.Equal(EntityOperationKind.Restore, restored.OperationKind);
		Assert.Same(person, restored.Entity);
		Assert.Equal("1", restored.Key);
	}
}