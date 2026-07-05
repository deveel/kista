using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
[Trait("Feature", "SoftDelete")]
public class EntityManagerSoftDeleteTests {
	private const string TestUserId = "user-42";
	private readonly SoftDeletablePersonFaker _faker = new();

	private static IServiceProvider BuildServicesWithUserAccessor(string userId) {
		var services = new ServiceCollection();
		services.AddSingleton<IUserAccessor<string>>(new StaticUserAccessor<string>(userId));
		return services.BuildServiceProvider();
	}

	private static IServiceProvider BuildServicesWithoutUserAccessor() {
		var services = new ServiceCollection();
		return services.BuildServiceProvider();
	}

	[Fact]
	public async Task Should_SoftDeleteViaManager_When_EntityIsSoftDeletable() {
		var person = _faker.Generate();
		person.Id = "1";

		var repo = Substitute.For<IRepository<SoftDeletablePerson, string>>();
		repo.GetEntityKey(Arg.Any<SoftDeletablePerson>()).Returns(c => c.Arg<SoftDeletablePerson>().Id);
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(person);
		repo.UpdateAsync(Arg.Any<SoftDeletablePerson>(), Arg.Any<CancellationToken>()).Returns(true);

		var services = BuildServicesWithUserAccessor(TestUserId);
		var manager = new EntityManager<SoftDeletablePerson, string>(repo, services: services);

		var result = await manager.RemoveAsync(person, TestContext.Current.CancellationToken);

		Assert.True(result.IsSuccess());
		await repo.DidNotReceive().RemoveAsync(Arg.Any<SoftDeletablePerson>(), Arg.Any<CancellationToken>());
		await repo.Received().UpdateAsync(Arg.Is<SoftDeletablePerson>(p => p.IsDeleted), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Should_StampDeletedAtUtc_OnManagerRemove() {
		var person = _faker.Generate();
		person.Id = "1";

		var repo = Substitute.For<IRepository<SoftDeletablePerson, string>>();
		repo.GetEntityKey(Arg.Any<SoftDeletablePerson>()).Returns(c => c.Arg<SoftDeletablePerson>().Id);
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(person);
		repo.UpdateAsync(Arg.Any<SoftDeletablePerson>(), Arg.Any<CancellationToken>()).Returns(true);

		var services = BuildServicesWithUserAccessor(TestUserId);
		var manager = new EntityManager<SoftDeletablePerson, string>(repo, services: services);

		await manager.RemoveAsync(person, TestContext.Current.CancellationToken);

		Assert.True(person.IsDeleted);
		Assert.NotNull(person.DeletedAtUtc);
	}

	[Fact]
	public async Task Should_StampDeletedBy_When_UserAccessorRegistered() {
		var person = _faker.Generate();
		person.Id = "1";

		var repo = Substitute.For<IRepository<SoftDeletablePerson, string>>();
		repo.GetEntityKey(Arg.Any<SoftDeletablePerson>()).Returns(c => c.Arg<SoftDeletablePerson>().Id);
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(person);
		repo.UpdateAsync(Arg.Any<SoftDeletablePerson>(), Arg.Any<CancellationToken>()).Returns(true);

		var services = BuildServicesWithUserAccessor(TestUserId);
		var manager = new EntityManager<SoftDeletablePerson, string>(repo, services: services);

		await manager.RemoveAsync(person, TestContext.Current.CancellationToken);

		Assert.Equal(TestUserId, person.DeletedBy);
	}

	[Fact]
	public async Task Should_LeaveDeletedByNull_When_NoUserAccessorRegistered() {
		var person = _faker.Generate();
		person.Id = "1";

		var repo = Substitute.For<IRepository<SoftDeletablePerson, string>>();
		repo.GetEntityKey(Arg.Any<SoftDeletablePerson>()).Returns(c => c.Arg<SoftDeletablePerson>().Id);
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(person);
		repo.UpdateAsync(Arg.Any<SoftDeletablePerson>(), Arg.Any<CancellationToken>()).Returns(true);

		var services = BuildServicesWithoutUserAccessor();
		var manager = new EntityManager<SoftDeletablePerson, string>(repo, services: services);

		var result = await manager.RemoveAsync(person, TestContext.Current.CancellationToken);

		Assert.True(result.IsSuccess());
		Assert.Null(person.DeletedBy);
	}

	[Fact]
	public async Task Should_RestoreEntity_When_SoftDeleted() {
		var person = _faker.Generate();
		person.Id = "1";
		person.IsDeleted = true;
		person.DeletedAtUtc = DateTimeOffset.UtcNow;
		person.DeletedBy = TestUserId;

		var repo = Substitute.For<IRepository<SoftDeletablePerson, string>>();
		repo.GetEntityKey(Arg.Any<SoftDeletablePerson>()).Returns(c => c.Arg<SoftDeletablePerson>().Id);
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(person);
		repo.UpdateAsync(Arg.Any<SoftDeletablePerson>(), Arg.Any<CancellationToken>()).Returns(true);

		var manager = new EntityManager<SoftDeletablePerson, string>(repo);

		var result = await manager.RestoreAsync(person, TestContext.Current.CancellationToken);

		Assert.True(result.IsSuccess());
		Assert.False(person.IsDeleted);
		Assert.Null(person.DeletedAtUtc);
		Assert.Null(person.DeletedBy);
	}

	[Fact]
	public async Task Should_FailRestore_When_EntityNotSoftDeletable() {
		var person = new Person { Id = "1" };

		var repo = Substitute.For<IRepository<Person, string>>();
		repo.GetEntityKey(Arg.Any<Person>()).Returns(c => c.Arg<Person>().Id);

		var manager = new EntityManager<Person, string>(repo);

		var result = await manager.RestoreAsync(person, TestContext.Current.CancellationToken);

		Assert.False(result.IsSuccess());
	}

	[Fact]
	public async Task Should_HardDeleteViaManager_When_EntityIsSoftDeletable() {
		var person = _faker.Generate();
		person.Id = "1";

		var repo = Substitute.For<IRepository<SoftDeletablePerson, string>>();
		repo.GetEntityKey(Arg.Any<SoftDeletablePerson>()).Returns(c => c.Arg<SoftDeletablePerson>().Id);
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(person);
		repo.HardDeleteAsync(Arg.Any<SoftDeletablePerson>(), Arg.Any<CancellationToken>()).Returns(true);

		var manager = new EntityManager<SoftDeletablePerson, string>(repo);

		var result = await manager.HardDeleteAsync(person, TestContext.Current.CancellationToken);

		Assert.True(result.IsSuccess());
		Assert.False(person.IsDeleted);
		await repo.DidNotReceive().RemoveAsync(Arg.Any<SoftDeletablePerson>(), Arg.Any<CancellationToken>());
		await repo.Received().HardDeleteAsync(Arg.Any<SoftDeletablePerson>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Should_NotStamp_OnHardDelete() {
		var person = _faker.Generate();
		person.Id = "1";

		var repo = Substitute.For<IRepository<SoftDeletablePerson, string>>();
		repo.GetEntityKey(Arg.Any<SoftDeletablePerson>()).Returns(c => c.Arg<SoftDeletablePerson>().Id);
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(person);
		repo.HardDeleteAsync(Arg.Any<SoftDeletablePerson>(), Arg.Any<CancellationToken>()).Returns(true);

		var manager = new EntityManager<SoftDeletablePerson, string>(repo);

		await manager.HardDeleteAsync(person, TestContext.Current.CancellationToken);

		Assert.False(person.IsDeleted);
		Assert.Null(person.DeletedAtUtc);
	}

	[Fact]
	public async Task Should_FireOnHardRemovingEntityHook_When_HardDelete() {
		var person = _faker.Generate();
		person.Id = "1";

		var repo = Substitute.For<IRepository<SoftDeletablePerson, string>>();
		repo.GetEntityKey(Arg.Any<SoftDeletablePerson>()).Returns(c => c.Arg<SoftDeletablePerson>().Id);
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(person);
		repo.HardDeleteAsync(Arg.Any<SoftDeletablePerson>(), Arg.Any<CancellationToken>()).Returns(true);

		var manager = new TestManager<SoftDeletablePerson, string>(repo);

		await manager.HardDeleteAsync(person, TestContext.Current.CancellationToken);

		Assert.True(manager.HardRemovingHookCalled);
	}

	[Fact]
	public async Task Should_HardDeleteRangeViaManager() {
		var persons = _faker.Generate(3);
		for (int i = 0; i < persons.Count; i++)
			persons[i].Id = (i + 1).ToString();

		var repo = Substitute.For<IRepository<SoftDeletablePerson, string>>();
		repo.GetEntityKey(Arg.Any<SoftDeletablePerson>()).Returns(c => c.Arg<SoftDeletablePerson>().Id);
		repo.HardDeleteRangeAsync(Arg.Any<IEnumerable<SoftDeletablePerson>>(), Arg.Any<CancellationToken>()).Returns(ValueTask.CompletedTask);

		var manager = new EntityManager<SoftDeletablePerson, string>(repo);

		var result = await manager.HardDeleteRangeAsync(persons, TestContext.Current.CancellationToken);

		Assert.True(result.IsSuccess());
		await repo.Received().HardDeleteRangeAsync(Arg.Any<IEnumerable<SoftDeletablePerson>>(), Arg.Any<CancellationToken>());
	}

	private sealed class StaticUserAccessor<TKey> : IUserAccessor<TKey>
		where TKey : notnull {
		private readonly TKey? _userId;

		public StaticUserAccessor(TKey? userId) {
			_userId = userId;
		}

		public TKey? GetUserId() => _userId;
	}

	private sealed class TestManager<TEntity, TKey> : EntityManager<TEntity, TKey>
		where TEntity : class
		where TKey : notnull {
		public bool HardRemovingHookCalled { get; private set; }

		public TestManager(IRepository<TEntity, TKey> repository) : base(repository) {
		}

		protected override ValueTask<TEntity> OnHardRemovingEntityAsync(TEntity entity, CancellationToken cancellationToken) {
			HardRemovingHookCalled = true;
			return new ValueTask<TEntity>(entity);
		}
	}
}