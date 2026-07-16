#pragma warning disable CS8618

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
[Trait("Feature", "OperationPipeline")]
public class InterceptorPipelineTests {
	private readonly PersonFaker _faker = new();

	private static (EntityManager<Person, string> manager, IRepository<Person, string> repo) BuildManager(
		IEnumerable<IEntityManagerInterceptor<Person, string>>? interceptors = null,
		IUserAccessor<string>? userAccessor = null,
		ISystemTime? systemTime = null) {
		var repo = Substitute.For<IRepository<Person, string>>();
		repo.GetEntityKey(Arg.Any<Person>()).Returns(c => c.Arg<Person>().Id);
		repo.AddAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(ValueTask.CompletedTask);
		repo.UpdateAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(true);
		repo.RemoveAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(true);
		repo.FindAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Person?)null);

		var services = new ServiceCollection();
		foreach (var interceptor in interceptors ?? Array.Empty<IEntityManagerInterceptor<Person, string>>())
			services.AddSingleton<IEntityManagerInterceptor<Person, string>>(interceptor);
		if (userAccessor != null)
			services.AddSingleton(userAccessor);
		if (systemTime != null)
			services.AddSingleton(systemTime);

		var provider = services.BuildServiceProvider();
		var manager = new EntityManager<Person, string>(repo, systemTime: systemTime, services: provider);
		return (manager, repo);
	}

	[Fact]
	public async Task Should_RunPreWriteInterceptorsInRegistrationOrder() {
		var callOrder = new List<string>();
		var interceptorA = new RecordingInterceptor("A", callOrder);
		var interceptorB = new RecordingInterceptor("B", callOrder);

		var (manager, _) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { interceptorA, interceptorB });
		var person = _faker.Generate();
		person.Id = "1";

		await manager.AddAsync(person, TestContext.Current.CancellationToken);

		Assert.Equal(new[] { "A.Pre", "B.Pre", "A.Post", "B.Post" }.AsEnumerable(), callOrder.AsEnumerable());
	}

	[Fact]
	public async Task Should_ShortCircuit_When_PreWriteReturnsFailedResult() {
		var callOrder = new List<string>();
		var interceptorA = new RecordingInterceptor("A", callOrder, shortCircuit: true);
		var interceptorB = new RecordingInterceptor("B", callOrder);

		var (manager, repo) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { interceptorA, interceptorB });
		var person = _faker.Generate();
		person.Id = "1";

		var result = await manager.AddAsync(person, TestContext.Current.CancellationToken);

		Assert.False(result.IsSuccess());
		Assert.Contains("A.Pre", callOrder);
		Assert.DoesNotContain("B.Pre", callOrder);
		Assert.DoesNotContain("A.Post", callOrder);
		Assert.DoesNotContain("B.Post", callOrder);
		await repo.DidNotReceive().AddAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Should_NotCallPostWrite_When_RepositoryThrows() {
		var callOrder = new List<string>();
		var interceptorA = new RecordingInterceptor("A", callOrder);

		var repo = Substitute.For<IRepository<Person, string>>();
		repo.GetEntityKey(Arg.Any<Person>()).Returns(c => c.Arg<Person>().Id);
		repo.AddAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Throws(new InvalidOperationException("boom"));

		var services = new ServiceCollection();
		services.AddSingleton<IEntityManagerInterceptor<Person, string>>(interceptorA);
		var provider = services.BuildServiceProvider();
		var manager = new EntityManager<Person, string>(repo, services: provider);

		var person = _faker.Generate();
		person.Id = "1";

		var result = await manager.AddAsync(person, TestContext.Current.CancellationToken);

		Assert.False(result.IsSuccess());
		Assert.Contains("A.Pre", callOrder);
		Assert.DoesNotContain("A.Post", callOrder);
	}

	[Fact]
	public async Task Should_PassMutatedEntityToRepository() {
		var interceptor = new MutatingInterceptor(p => p.FirstName = "Mutated");

		var (manager, repo) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { interceptor });
		var person = _faker.Generate();
		person.Id = "1";
		person.FirstName = "Original";

		await manager.AddAsync(person, TestContext.Current.CancellationToken);

		await repo.Received().AddAsync(
			Arg.Is<Person>(p => p.FirstName == "Mutated"),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Should_ShareItemsBagBetweenInterceptors() {
		var interceptorA = new ItemsSettingInterceptor("key", "value-from-A");
		var interceptorB = new ItemsReadingInterceptor("key", "value-from-A");

		var (manager, _) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { interceptorA, interceptorB });
		var person = _faker.Generate();
		person.Id = "1";

		await manager.AddAsync(person, TestContext.Current.CancellationToken);

		Assert.True(interceptorB.ReadValueMatched);
	}

	[Fact]
	public async Task Should_PopulateActorAndTimestampInContext() {
		var testTime = new TestSystemTime();
		var interceptor = new ContextCapturingInterceptor();
		var userAccessor = new StaticUserAccessor<string>("user-42");

		var (manager, _) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { interceptor }, userAccessor, testTime);
		var person = _faker.Generate();
		person.Id = "1";

		await manager.AddAsync(person, TestContext.Current.CancellationToken);

		Assert.Equal("user-42", interceptor.CapturedActor);
		Assert.Equal(testTime.UtcNow, interceptor.CapturedTimestamp);
	}

	[Fact]
	public async Task Should_PopulateOperationKindInContext() {
		var interceptor = new ContextCapturingInterceptor();
		var (manager, repo) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { interceptor });

		var existing = _faker.Generate();
		existing.Id = "1";
		existing.FirstName = "OldName";
		var updated = _faker.Generate();
		updated.Id = "1";
		updated.FirstName = "NewName";
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(existing);
		repo.UpdateAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(true);

		await manager.UpdateAsync(updated, TestContext.Current.CancellationToken);

		Assert.Equal(EntityOperationKind.Update, interceptor.CapturedKind);
	}

	[Fact]
	public async Task Should_PopulateOriginalOnUpdate_And_NullOnCreate() {
		var createInterceptor = new ContextCapturingInterceptor();
		var updateInterceptor = new ContextCapturingInterceptor();

		var (createManager, _) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { createInterceptor });
		var person = _faker.Generate();
		person.Id = "1";

		await createManager.AddAsync(person, TestContext.Current.CancellationToken);

		Assert.Null(createInterceptor.CapturedOriginal);

		var existing = _faker.Generate();
		existing.Id = "1";
		existing.FirstName = "OldName";
		var repo = Substitute.For<IRepository<Person, string>>();
		repo.GetEntityKey(Arg.Any<Person>()).Returns(c => c.Arg<Person>().Id);
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(existing);
		repo.UpdateAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(true);

		var services = new ServiceCollection();
		services.AddSingleton<IEntityManagerInterceptor<Person, string>>(updateInterceptor);
		var provider = services.BuildServiceProvider();
		var updateManager = new EntityManager<Person, string>(repo, services: provider);

		var updated = _faker.Generate();
		updated.Id = "1";
		updated.FirstName = "NewName";
		await updateManager.UpdateAsync(updated, TestContext.Current.CancellationToken);

		Assert.NotNull(updateInterceptor.CapturedOriginal);
		Assert.Same(existing, updateInterceptor.CapturedOriginal);
	}

	[Fact]
	public async Task Should_PopulateKeyInContext() {
		var interceptor = new ContextCapturingInterceptor();
		var (manager, _) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { interceptor });
		var person = _faker.Generate();
		person.Id = "key-123";

		await manager.AddAsync(person, TestContext.Current.CancellationToken);

		Assert.Equal("key-123", interceptor.CapturedKey);
	}

	// --- Test interceptors ---

	private class RecordingInterceptor : IEntityManagerInterceptor<Person, string> {
		private readonly string _name;
		private readonly List<string> _callOrder;
		private readonly bool _shortCircuit;

		public RecordingInterceptor(string name, List<string> callOrder, bool shortCircuit = false) {
			_name = name;
			_callOrder = callOrder;
			_shortCircuit = shortCircuit;
		}

		public ValueTask<IOperationResult?> PreWriteAsync(IEntityOperationContext<Person, string> context) {
			_callOrder.Add($"{_name}.Pre");
			if (_shortCircuit)
				return new ValueTask<IOperationResult?>(OperationResult.Fail(new OperationError("SHORT_CIRCUIT", "Test", "Short-circuited by A")));
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

	private class ItemsSettingInterceptor : IEntityManagerInterceptor<Person, string> {
		private readonly string _key;
		private readonly object? _value;

		public ItemsSettingInterceptor(string key, object? value) {
			_key = key;
			_value = value;
		}

		public ValueTask<IOperationResult?> PreWriteAsync(IEntityOperationContext<Person, string> context) {
			context.Items[_key] = _value;
			return default;
		}

		public ValueTask PostWriteAsync(IEntityOperationContext<Person, string> context, IOperationResult result)
			=> ValueTask.CompletedTask;
	}

	private class ItemsReadingInterceptor : IEntityManagerInterceptor<Person, string> {
		private readonly string _key;
		private readonly object? _expectedValue;
		public bool ReadValueMatched;

		public ItemsReadingInterceptor(string key, object? expectedValue) {
			_key = key;
			_expectedValue = expectedValue;
		}

		public ValueTask<IOperationResult?> PreWriteAsync(IEntityOperationContext<Person, string> context) {
			ReadValueMatched = context.Items.TryGetValue(_key, out var val) && Equals(val, _expectedValue);
			return default;
		}

		public ValueTask PostWriteAsync(IEntityOperationContext<Person, string> context, IOperationResult result)
			=> ValueTask.CompletedTask;
	}

	private class ContextCapturingInterceptor : IEntityManagerInterceptor<Person, string> {
		public string? CapturedActor;
		public DateTimeOffset CapturedTimestamp;
		public EntityOperationKind CapturedKind;
		public Person? CapturedOriginal;
		public string? CapturedKey;

		public ValueTask<IOperationResult?> PreWriteAsync(IEntityOperationContext<Person, string> context) {
			CapturedActor = context.Actor;
			CapturedTimestamp = context.Timestamp;
			CapturedKind = context.Kind;
			CapturedOriginal = context.Original;
			CapturedKey = context.Key;
			return default;
		}

		public ValueTask PostWriteAsync(IEntityOperationContext<Person, string> context, IOperationResult result)
			=> ValueTask.CompletedTask;
	}

	private sealed class StaticUserAccessor<TKey> : IUserAccessor<TKey> {
		private readonly TKey? _userId;
		public StaticUserAccessor(TKey? userId) => _userId = userId;
		public TKey? GetUserId() => _userId;
	}

	private sealed class TestSystemTime : ISystemTime {
		public DateTimeOffset UtcNow => new DateTimeOffset(2026, 7, 12, 10, 0, 0, TimeSpan.Zero);
		public DateTimeOffset Now => UtcNow.ToLocalTime();
	}
}