#pragma warning disable CS8618

using NSubstitute;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
[Trait("Feature", "OperationPipeline")]
public class OnHooksInterceptorTests {
	private readonly PersonFaker _faker = new();

	[Fact]
	public async Task Should_InvokeOnAddingEntityHook_When_NoUserInterceptorRegistered() {
		var repo = Substitute.For<IRepository<Person, string>>();
		repo.GetEntityKey(Arg.Any<Person>()).Returns(c => c.Arg<Person>().Id);
		repo.AddAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(ValueTask.CompletedTask);

		var services = new ServiceCollection().BuildServiceProvider();
		var manager = new TimestampRecordingManager(repo, services);
		var person = _faker.Generate();
		person.Id = "1";

		await manager.AddAsync(person, TestContext.Current.CancellationToken);

		Assert.True(manager.OnAddingEntityCalled);
		Assert.Equal(manager.TestTime.UtcNow, person.CreatedAtUtc);
	}

	[Fact]
	public async Task Should_InvokeOnUpdatingEntityHook_When_NoUserInterceptorRegistered() {
		var repo = Substitute.For<IRepository<Person, string>>();
		repo.GetEntityKey(Arg.Any<Person>()).Returns(c => c.Arg<Person>().Id);

		var existing = _faker.Generate();
		existing.Id = "1";
		existing.FirstName = "OldName";
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(existing);
		repo.UpdateAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(true);

		var services = new ServiceCollection().BuildServiceProvider();
		var manager = new TimestampRecordingManager(repo, services);

		var updated = _faker.Generate();
		updated.Id = "1";
		updated.FirstName = "NewName";
		await manager.UpdateAsync(updated, TestContext.Current.CancellationToken);

		Assert.True(manager.OnUpdatingEntityCalled);
	}

	[Fact]
	public async Task Should_RunUserInterceptorBeforeBuiltinHook() {
		var callOrder = new List<string>();
		var userInterceptor = new RecordingInterceptor("User", callOrder);

		var repo = Substitute.For<IRepository<Person, string>>();
		repo.GetEntityKey(Arg.Any<Person>()).Returns(c => c.Arg<Person>().Id);
		repo.AddAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(ValueTask.CompletedTask);

		var services = new ServiceCollection();
		services.AddSingleton<IEntityManagerInterceptor<Person, string>>(userInterceptor);
		var provider = services.BuildServiceProvider();

		var manager = new TimestampRecordingManager(repo, provider, callOrder);
		var person = _faker.Generate();
		person.Id = "1";

		await manager.AddAsync(person, TestContext.Current.CancellationToken);

		Assert.Equal(new[] { "User.Pre", "Hook.Pre", "User.Post" }.AsEnumerable(), callOrder.AsEnumerable());
		Assert.True(manager.OnAddingEntityCalled);
	}

	[Fact]
	public async Task Should_UserInterceptorSeeEntityBeforeTimestampStamping() {
		var repo = Substitute.For<IRepository<Person, string>>();
		repo.GetEntityKey(Arg.Any<Person>()).Returns(c => c.Arg<Person>().Id);
		repo.AddAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(ValueTask.CompletedTask);

		var userInterceptor = new MutatingInterceptor(p => {
			p.FirstName = "MutatedByUser";
		});

		var services = new ServiceCollection();
		services.AddSingleton<IEntityManagerInterceptor<Person, string>>(userInterceptor);
		var provider = services.BuildServiceProvider();

		var manager = new TimestampRecordingManager(repo, provider);
		var person = _faker.Generate();
		person.Id = "1";
		person.FirstName = "Original";

		await manager.AddAsync(person, TestContext.Current.CancellationToken);

		await repo.Received().AddAsync(
			Arg.Is<Person>(p => p.FirstName == "MutatedByUser" && p.CreatedAtUtc != null),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Should_ShortCircuitSkipBuiltinHook() {
		var repo = Substitute.For<IRepository<Person, string>>();
		repo.GetEntityKey(Arg.Any<Person>()).Returns(c => c.Arg<Person>().Id);
		repo.AddAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(ValueTask.CompletedTask);

		var userInterceptor = new ShortCircuitInterceptor();

		var services = new ServiceCollection();
		services.AddSingleton<IEntityManagerInterceptor<Person, string>>(userInterceptor);
		var provider = services.BuildServiceProvider();

		var manager = new TimestampRecordingManager(repo, provider);
		var person = _faker.Generate();
		person.Id = "1";

		var result = await manager.AddAsync(person, TestContext.Current.CancellationToken);

		Assert.False(result.IsSuccess());
		Assert.False(manager.OnAddingEntityCalled);
		Assert.Null(person.CreatedAtUtc);
		await repo.DidNotReceive().AddAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>());
	}

	private class TimestampRecordingManager : EntityManager<Person, string> {
		public bool OnAddingEntityCalled;
		public bool OnUpdatingEntityCalled;
		public readonly ISystemTime TestTime = new TestSystemTime();
		private readonly List<string>? _callOrder;

		public TimestampRecordingManager(IRepository<Person, string> repo, IServiceProvider services, List<string>? callOrder = null)
			: base(repo, systemTime: new TestSystemTime(), services: services) {
			_callOrder = callOrder;
		}

		protected override ValueTask<Person> OnAddingEntityAsync(Person entity) {
			OnAddingEntityCalled = true;
			_callOrder?.Add("Hook.Pre");
			if (entity is IHaveTimeStamp haveTimeStamp && Time != null)
				haveTimeStamp.CreatedAtUtc = Time.UtcNow;
			return new ValueTask<Person>(entity);
		}

		protected override ValueTask<Person> OnUpdatingEntityAsync(Person entity) {
			OnUpdatingEntityCalled = true;
			_callOrder?.Add("Hook.Pre");
			if (entity is IHaveTimeStamp haveTimeStamp && Time != null)
				haveTimeStamp.UpdatedAtUtc = Time.UtcNow;
			return new ValueTask<Person>(entity);
		}
	}

	private class RecordingInterceptor : IEntityManagerInterceptor<Person, string> {
		private readonly string _name;
		private readonly List<string> _callOrder;

		public RecordingInterceptor(string name, List<string> callOrder) {
			_name = name;
			_callOrder = callOrder;
		}

		public ValueTask<IOperationResult?> PreWriteAsync(IEntityOperationContext<Person, string> context) {
			_callOrder.Add($"{_name}.Pre");
			return default;
		}

		public ValueTask PostWriteAsync(IEntityOperationContext<Person, string> context, IOperationResult result) {
			_callOrder.Add($"{_name}.Post");
			return ValueTask.CompletedTask;
		}
	}

	private class MutatingInterceptor : IEntityManagerInterceptor<Person, string> {
		private readonly Action<Person> _mutate;

		public MutatingInterceptor(Action<Person> mutate) {
			_mutate = mutate;
		}

		public ValueTask<IOperationResult?> PreWriteAsync(IEntityOperationContext<Person, string> context) {
			_mutate(context.Entity);
			return default;
		}

		public ValueTask PostWriteAsync(IEntityOperationContext<Person, string> context, IOperationResult result)
			=> ValueTask.CompletedTask;
	}

	private class ShortCircuitInterceptor : IEntityManagerInterceptor<Person, string> {
		public ValueTask<IOperationResult?> PreWriteAsync(IEntityOperationContext<Person, string> context)
			=> new(OperationResult.Fail(new OperationError("SHORT_CIRCUIT", "Test", "Short-circuited")));

		public ValueTask PostWriteAsync(IEntityOperationContext<Person, string> context, IOperationResult result)
			=> ValueTask.CompletedTask;
	}

	private sealed class TestSystemTime : ISystemTime {
		public DateTimeOffset UtcNow => new DateTimeOffset(2026, 7, 12, 10, 0, 0, TimeSpan.Zero);
		public DateTimeOffset Now => UtcNow.ToLocalTime();
	}
}