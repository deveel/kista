#pragma warning disable CS8618

using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Kista.Caching;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
[Trait("Feature", "OperationPipeline")]
public class InterceptorPipelineCoverageTests {
	private static readonly PersonFaker _faker = new();

	private static Person CreatePerson(string? id = "1") {
		var person = _faker.Generate();
		person.Id = id;
		return person;
	}

	private static (EntityManager<Person, string> manager, IRepository<Person, string> repo) BuildManager(
		IEnumerable<IEntityManagerInterceptor<Person, string>>? interceptors = null,
		IUserAccessor<string>? userAccessor = null,
		ISystemTime? systemTime = null,
		IEntityCache<Person>? cache = null) {
		var repo = Substitute.For<IRepository<Person, string>>();
		repo.GetEntityKey(Arg.Any<Person>()).Returns(c => c.Arg<Person>().Id);
		repo.AddAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(ValueTask.CompletedTask);
		repo.UpdateAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(true);
		repo.RemoveAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(true);
		repo.HardDeleteAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(true);
		repo.FindAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Person?)null);

		var services = new ServiceCollection();
		foreach (var interceptor in interceptors ?? Array.Empty<IEntityManagerInterceptor<Person, string>>())
			services.AddSingleton<IEntityManagerInterceptor<Person, string>>(interceptor);
		if (userAccessor != null)
			services.AddSingleton<IUserAccessor<string>>(userAccessor);
		if (systemTime != null)
			services.AddSingleton(systemTime);

		var provider = services.BuildServiceProvider();
		var manager = new EntityManager<Person, string>(repo, cache: cache, systemTime: systemTime, services: provider);
		return (manager, repo);
	}

	// --- ToOperationResult fallback branches ---

	[Fact]
	public async Task Should_HandleShortCircuitResultThatIsNotOperationResult_With_Error() {
		var customResult = new CustomOperationResult(hasError: true);
		var interceptor = new CustomResultInterceptor(customResult);

		var (manager, repo) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { interceptor });
		var person = CreatePerson("1");

		var result = await manager.AddAsync(person, TestContext.Current.CancellationToken);

		Assert.False(result.IsSuccess());
		await repo.DidNotReceive().AddAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Should_HandleShortCircuitResultThatIsNotOperationResult_Without_Error_Success() {
		var customResult = new CustomOperationResult(hasError: false, isSuccess: true);
		var interceptor = new CustomResultInterceptor(customResult);

		var (manager, repo) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { interceptor });
		var person = CreatePerson("1");

		var result = await manager.AddAsync(person, TestContext.Current.CancellationToken);

		Assert.True(result.IsSuccess());
		await repo.DidNotReceive().AddAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Should_HandleShortCircuitResultThatIsNotOperationResult_Without_Error_Failure() {
		var customResult = new CustomOperationResult(hasError: false, isSuccess: false);
		var interceptor = new CustomResultInterceptor(customResult);

		var (manager, repo) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { interceptor });
		var person = CreatePerson("1");

		var result = await manager.AddAsync(person, TestContext.Current.CancellationToken);

		Assert.False(result.IsSuccess());
		await repo.DidNotReceive().AddAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>());
	}

	// --- CreateOperationContext actor resolution branches ---

	[Fact]
	public async Task Should_PopulateActor_When_UserAccessorReturnsUserId() {
		var interceptor = new ContextCapturingInterceptor();
		var userAccessor = new StaticUserAccessor<string>("user-99");

		var (manager, _) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { interceptor }, userAccessor);
		var person = CreatePerson("1");

		await manager.AddAsync(person, TestContext.Current.CancellationToken);

		Assert.Equal("user-99", interceptor.CapturedActor);
	}

	[Fact]
	public async Task Should_PopulateNullActor_When_UserAccessorReturnsNull() {
		var interceptor = new ContextCapturingInterceptor();
		var userAccessor = new StaticUserAccessor<string>(null);

		var (manager, _) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { interceptor }, userAccessor);
		var person = CreatePerson("1");

		await manager.AddAsync(person, TestContext.Current.CancellationToken);

		Assert.Null(interceptor.CapturedActor);
	}

	[Fact]
	public async Task Should_PopulateNullActor_When_NoUserAccessorRegistered() {
		var interceptor = new ContextCapturingInterceptor();

		var (manager, _) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { interceptor });
		var person = CreatePerson("1");

		await manager.AddAsync(person, TestContext.Current.CancellationToken);

		Assert.Null(interceptor.CapturedActor);
	}

	// --- Short-circuit in RemoveAsync / UpdateAsync / HardDeleteAsync ---
	//
	// The three single-entity short-circuit tests share the same shape
	// (build manager with a ShortCircuitInterceptor, seed the repo so that
	// FindAsync returns the existing person, invoke the operation, assert
	// the result is not successful and the underlying repo call was never
	// made). They are expressed below as calls to a single helper that takes
	// the operation and the assertion-on-the-repo as delegates, so the
	// duplicated setup/assert scaffolding lives in one place.

	private static async Task AssertShortCircuits(
		Func<EntityManager<Person, string>, IRepository<Person, string>, Task> invokeAndAssertRepoUntouched) {
		var interceptor = new ShortCircuitInterceptor();
		var (manager, repo) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { interceptor });
		await invokeAndAssertRepoUntouched(manager, repo);
	}

	[Fact]
	public async Task Should_ShortCircuitRemoveAsync()
		=> await AssertShortCircuits(async (manager, repo) => {
			var person = CreatePerson("1");
			repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(person);

			var result = await manager.RemoveAsync(person, TestContext.Current.CancellationToken);

			Assert.False(result.IsSuccess());
			await repo.DidNotReceive().RemoveAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>());
			await repo.DidNotReceive().UpdateAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>());
		});

	[Fact]
	public async Task Should_ShortCircuitUpdateAsync()
		=> await AssertShortCircuits(async (manager, repo) => {
			var existing = CreatePerson("1");
			existing.FirstName = "Old";
			var updated = CreatePerson("1");
			updated.FirstName = "New";
			repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(existing);

			var result = await manager.UpdateAsync(updated, TestContext.Current.CancellationToken);

			Assert.False(result.IsSuccess());
			await repo.DidNotReceive().UpdateAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>());
		});

	[Fact]
	public async Task Should_ShortCircuitHardDeleteAsync()
		=> await AssertShortCircuits(async (manager, repo) => {
			var person = CreatePerson("1");
			repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(person);

			var result = await manager.HardDeleteAsync(person, TestContext.Current.CancellationToken);

			Assert.False(result.IsSuccess());
			await repo.DidNotReceive().HardDeleteAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>());
		});

	// --- HardDeleteAsync success path through pipeline ---

	[Fact]
	public async Task Should_RunPipeline_ForHardDeleteSuccess() {
		var callOrder = new List<string>();
		var interceptor = new RecordingInterceptor("A", callOrder);

		var (manager, repo) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { interceptor });

		var person = CreatePerson("1");
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(person);
		repo.HardDeleteAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(true);

		var result = await manager.HardDeleteAsync(person, TestContext.Current.CancellationToken);

		Assert.True(result.IsSuccess());
		Assert.Contains("A.Pre", callOrder);
		Assert.Contains("A.Post", callOrder);
		await repo.Received().HardDeleteAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Should_ReturnNotChanged_When_HardDeleteRepoReturnsFalse() {
		var interceptor = new RecordingInterceptor("A", new List<string>());

		var (manager, repo) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { interceptor });

		var person = CreatePerson("1");
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(person);
		repo.HardDeleteAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(false);

		var result = await manager.HardDeleteAsync(person, TestContext.Current.CancellationToken);

		Assert.True(result.IsUnchanged());
	}

	[Fact]
	public async Task Should_ReturnNotFound_When_HardDeleteEntityNotFound() {
		var (manager, _) = BuildManager();
		var person = CreatePerson("1");

		var result = await manager.HardDeleteAsync(person, TestContext.Current.CancellationToken);

		Assert.False(result.IsSuccess());
		Assert.NotNull(result.Error);
	}

	[Fact]
	public async Task Should_ReturnFailed_When_HardDeleteEntityHasNoKey() {
		var (manager, _) = BuildManager();
		var person = CreatePerson(null);

		var result = await manager.HardDeleteAsync(person, TestContext.Current.CancellationToken);

		Assert.False(result.IsSuccess());
		Assert.NotNull(result.Error);
	}

	// --- RestoreAsync pipeline paths ---

	[Fact]
	public async Task Should_ShortCircuitRestoreAsync() {
		var interceptor = new GenericShortCircuitInterceptor<SoftDeletablePerson, string>();

		var softPerson = new SoftDeletablePerson();
		softPerson.Id = "1";
		softPerson.FirstName = "Test";
		softPerson.IsDeleted = true;

		var softRepo = Substitute.For<IRepository<SoftDeletablePerson, string>>();
		softRepo.GetEntityKey(Arg.Any<SoftDeletablePerson>()).Returns(c => c.Arg<SoftDeletablePerson>().Id);
		softRepo.UpdateAsync(Arg.Any<SoftDeletablePerson>(), Arg.Any<CancellationToken>()).Returns(true);
		softRepo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(softPerson);

		var services = new ServiceCollection();
		services.AddSingleton<IEntityManagerInterceptor<SoftDeletablePerson, string>>(interceptor);
		var provider = services.BuildServiceProvider();
		var softManager = new EntityManager<SoftDeletablePerson, string>(softRepo, services: provider);

		var result = await softManager.RestoreAsync(softPerson, TestContext.Current.CancellationToken);

		Assert.False(result.IsSuccess());
		await softRepo.DidNotReceive().UpdateAsync(Arg.Any<SoftDeletablePerson>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Should_RunPipeline_ForRestoreSuccess() {
		var callOrder = new List<string>();
		var interceptor = new GenericRecordingInterceptor<SoftDeletablePerson, string>("A", callOrder);

		var person = new SoftDeletablePerson();
		person.Id = "1";
		person.FirstName = "Test";
		person.IsDeleted = true;

		var repo = Substitute.For<IRepository<SoftDeletablePerson, string>>();
		repo.GetEntityKey(Arg.Any<SoftDeletablePerson>()).Returns(c => c.Arg<SoftDeletablePerson>().Id);
		repo.UpdateAsync(Arg.Any<SoftDeletablePerson>(), Arg.Any<CancellationToken>()).Returns(true);
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(person);

		var services = new ServiceCollection();
		services.AddSingleton<IEntityManagerInterceptor<SoftDeletablePerson, string>>(interceptor);
		var provider = services.BuildServiceProvider();
		var manager = new EntityManager<SoftDeletablePerson, string>(repo, services: provider);

		var result = await manager.RestoreAsync(person, TestContext.Current.CancellationToken);

		Assert.True(result.IsSuccess());
		Assert.Contains("A.Pre", callOrder);
		Assert.Contains("A.Post", callOrder);
	}

	[Fact]
	public async Task Should_ReturnNotChanged_When_RestoreRepoUpdateReturnsFalse() {
		var person = new SoftDeletablePerson();
		person.Id = "1";
		person.FirstName = "Test";
		person.IsDeleted = true;

		var repo = Substitute.For<IRepository<SoftDeletablePerson, string>>();
		repo.GetEntityKey(Arg.Any<SoftDeletablePerson>()).Returns(c => c.Arg<SoftDeletablePerson>().Id);
		repo.UpdateAsync(Arg.Any<SoftDeletablePerson>(), Arg.Any<CancellationToken>()).Returns(false);
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(person);

		var manager = new EntityManager<SoftDeletablePerson, string>(repo);

		var result = await manager.RestoreAsync(person, TestContext.Current.CancellationToken);

		Assert.True(result.IsUnchanged());
	}

	[Fact]
	public async Task Should_ReturnNotFound_When_RestoreDeletedEntityNotFound() {
		var person = new SoftDeletablePerson();
		person.Id = "1";
		person.FirstName = "Test";

		var repo = Substitute.For<IRepository<SoftDeletablePerson, string>>();
		repo.GetEntityKey(Arg.Any<SoftDeletablePerson>()).Returns(c => c.Arg<SoftDeletablePerson>().Id);
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns((SoftDeletablePerson?)null);

		var manager = new EntityManager<SoftDeletablePerson, string>(repo);

		var result = await manager.RestoreAsync(person, TestContext.Current.CancellationToken);

		Assert.False(result.IsSuccess());
	}

	[Fact]
	public async Task Should_ReturnFailed_When_RestoreEntityNotSoftDeletable() {
		var (manager, _) = BuildManager();
		var person = CreatePerson("1");

		var result = await manager.RestoreAsync(person, TestContext.Current.CancellationToken);

		Assert.False(result.IsSuccess());
	}

	[Fact]
	public async Task Should_ReturnFailed_When_RestoreEntityHasNoKey() {
		var repo = Substitute.For<IRepository<SoftDeletablePerson, string>>();
		repo.GetEntityKey(Arg.Any<SoftDeletablePerson>()).Returns(c => c.Arg<SoftDeletablePerson>().Id);
		var manager = new EntityManager<SoftDeletablePerson, string>(repo);

		var person = new SoftDeletablePerson();
		person.Id = null;

		var result = await manager.RestoreAsync(person, TestContext.Current.CancellationToken);

		Assert.False(result.IsSuccess());
	}

	// --- Short-circuit in RemoveRangeAsync ---

	[Fact]
	public async Task Should_ShortCircuitRemoveRangeAsync() {
		var interceptor = new ShortCircuitInterceptor();
		var (manager, repo) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { interceptor });

		var person = CreatePerson("1");

		var result = await manager.RemoveRangeAsync(new[] { person }, TestContext.Current.CancellationToken);

		Assert.False(result.IsSuccess());
		await repo.DidNotReceive().RemoveRangeAsync(Arg.Any<IEnumerable<Person>>(), Arg.Any<CancellationToken>());
	}

	// --- Short-circuit in HardDeleteRangeAsync ---

	[Fact]
	public async Task Should_ShortCircuitHardDeleteRangeAsync() {
		var interceptor = new ShortCircuitInterceptor();
		var (manager, repo) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { interceptor });

		var person = CreatePerson("1");

		var result = await manager.HardDeleteRangeAsync(new[] { person }, TestContext.Current.CancellationToken);

		Assert.False(result.IsSuccess());
		await repo.DidNotReceive().HardDeleteRangeAsync(Arg.Any<IEnumerable<Person>>(), Arg.Any<CancellationToken>());
	}

	// --- PostWrite for range operations ---

	[Fact]
	public async Task Should_RunPipeline_ForHardDeleteRangeSuccess() {
		var interceptor = new CountingInterceptor();
		var (manager, repo) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { interceptor });

		var people = new List<Person>();
		for (var i = 0; i < 2; i++) {
			var p = _faker.Generate();
			p.Id = (i + 1).ToString();
			people.Add(p);
		}
		repo.HardDeleteRangeAsync(Arg.Any<IEnumerable<Person>>(), Arg.Any<CancellationToken>()).Returns(ValueTask.CompletedTask);

		var result = await manager.HardDeleteRangeAsync(people, TestContext.Current.CancellationToken);

		Assert.True(result.IsSuccess());
		Assert.Equal(2, interceptor.PreWriteCount);
		Assert.Equal(2, interceptor.PostWriteCount);
	}

	// --- Entity mutation in UpdateAsync pipeline ---

	[Fact]
	public async Task Should_PassMutatedEntityToRepoUpdate() {
		var interceptor = new MutatingInterceptor(p => p.FirstName = "MutatedByInterceptor");
		var (manager, repo) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { interceptor });

		var existing = CreatePerson("1");
		existing.FirstName = "Old";
		var updated = CreatePerson("1");
		updated.FirstName = "New";
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(existing);
		repo.UpdateAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(true);

		await manager.UpdateAsync(updated, TestContext.Current.CancellationToken);

		await repo.Received().UpdateAsync(
			Arg.Is<Person>(p => p.FirstName == "MutatedByInterceptor"),
			Arg.Any<CancellationToken>());
	}

	// --- Entity mutation in RemoveAsync pipeline (soft-delete path) ---

	[Fact]
	public async Task Should_RunPipeline_ForSoftDeleteRemove() {
		var interceptor = new GenericCountingInterceptor<SoftDeletablePerson, string>();

		var person = new SoftDeletablePerson();
		person.Id = "1";
		person.FirstName = "Test";

		var repo = Substitute.For<IRepository<SoftDeletablePerson, string>>();
		repo.GetEntityKey(Arg.Any<SoftDeletablePerson>()).Returns(c => c.Arg<SoftDeletablePerson>().Id);
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(person);
		repo.UpdateAsync(Arg.Any<SoftDeletablePerson>(), Arg.Any<CancellationToken>()).Returns(true);

		var services = new ServiceCollection();
		services.AddSingleton<IEntityManagerInterceptor<SoftDeletablePerson, string>>(interceptor);
		var provider = services.BuildServiceProvider();
		var manager = new EntityManager<SoftDeletablePerson, string>(repo, services: provider);

		var result = await manager.RemoveAsync(person, TestContext.Current.CancellationToken);

		Assert.True(result.IsSuccess());
		Assert.Equal(1, interceptor.PreWriteCount);
		Assert.Equal(1, interceptor.PostWriteCount);
		Assert.True(person.IsDeleted);
	}

	// --- Cache eviction error path ---

	[Fact]
	public async Task Should_NotFail_When_CacheEvictionThrows() {
		var cache = Substitute.For<IEntityCache<Person>>();
		cache.GetOrSetAsync(Arg.Any<string>(), Arg.Any<Func<ValueTask<Person?>>>(), Arg.Any<CancellationToken>())
			.Returns(callInfo => callInfo.Arg<Func<ValueTask<Person?>>>()());
		cache.RemoveAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>())
			.Throws(new InvalidOperationException("cache down"));

		var (manager, repo) = BuildManager(cache: cache);
		var person = CreatePerson("1");
		repo.RemoveAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(true);
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(person);

		var result = await manager.RemoveAsync(person, TestContext.Current.CancellationToken);

		Assert.True(result.IsSuccess());
	}

	// --- Cache set error path ---

	[Fact]
	public async Task Should_NotFail_When_CacheSetThrows() {
		var cache = Substitute.For<IEntityCache<Person>>();
		cache.SetAsync(Arg.Any<string[]>(), Arg.Any<Person>(), Arg.Any<CancellationToken>())
			.Throws(new InvalidOperationException("cache down"));

		var (manager, _) = BuildManager(cache: cache);
		var person = CreatePerson("1");

		var result = await manager.AddAsync(person, TestContext.Current.CancellationToken);

		Assert.True(result.IsSuccess());
	}

	// --- Single-key interceptor via EntityManager<TEntity> ---

	[Fact]
	public async Task Should_InvokeSingleKeyInterceptor_When_RegisteredForEntityManager_T() {
		var interceptor = new SingleKeyCountingInterceptor();

		var repo = Substitute.For<IRepository<Person>>();
		repo.GetEntityKey(Arg.Any<Person>()).Returns(c => c.Arg<Person>().Id!);
		repo.AddAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(ValueTask.CompletedTask);

		var services = new ServiceCollection();
		services.AddSingleton<IEntityManagerInterceptor<Person>>(interceptor);
		var provider = services.BuildServiceProvider();
		var manager = new EntityManager<Person>(repo, services: provider);

		var person = CreatePerson("1");

		await manager.AddAsync(person, TestContext.Current.CancellationToken);

		Assert.Equal(1, interceptor.PreWriteCount);
		Assert.Equal(1, interceptor.PostWriteCount);
	}

	// --- WithInterceptor edge cases ---

	[Fact]
	public void Should_RegisterOneKeyInterceptor_When_InterceptorImplementsSingleKeyInterface() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.AddRepository<InMemoryRepository<Person, string>>(repo => repo
				.WithManagement(mgmt => mgmt
					.WithInterceptor<SingleKeyCountingInterceptor>()))
			.UseInMemory();

		var provider = services.BuildServiceProvider();
		using var scope = provider.CreateAsyncScope();

		// The one-key interceptor should be resolvable as IEntityManagerInterceptor<Person>
		var oneKey = scope.ServiceProvider.GetService<IEnumerable<IEntityManagerInterceptor<Person>>>();
		Assert.NotNull(oneKey);
		Assert.NotEmpty(oneKey);

		// The concrete type should also be resolvable
		var concrete = scope.ServiceProvider.GetService<SingleKeyCountingInterceptor>();
		Assert.NotNull(concrete);
	}

	[Fact]
	public void Should_Throw_When_InterceptorTypeIsNotAClass() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.AddRepository<InMemoryRepository<Person, string>>(repo => repo
				.WithManagement(mgmt => {
					var act = () => mgmt.WithInterceptor<IEntityManagerInterceptor<Person, string>>();
					Assert.Throws<ArgumentException>(act);
				}))
			.UseInMemory();
	}

	// --- Pipeline context kind for each operation ---
	//
	// The Remove/HardDelete kind-capture tests share the same shape (build
	// manager with a ContextCapturingInterceptor, seed FindAsync, configure
	// the underlying repo call to return true, invoke the operation, assert
	// the captured kind). They are expressed below as calls to a single
	// helper that takes the operation and the expected kind as parameters.

	private static async Task AssertCapturesKind(
		EntityOperationKind expected,
		Func<EntityManager<Person, string>, IRepository<Person, string>, Person, Task> invokeAndSeedRepo) {
		var interceptor = new ContextCapturingInterceptor();
		var (manager, repo) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { interceptor });

		var person = CreatePerson("1");
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(person);

		await invokeAndSeedRepo(manager, repo, person);

		Assert.Equal(expected, interceptor.CapturedKind);
	}

	[Fact]
	public async Task Should_PopulateRemoveKind_InContext()
		=> await AssertCapturesKind(EntityOperationKind.Remove, async (manager, repo, person) => {
			repo.RemoveAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(true);
			await manager.RemoveAsync(person, TestContext.Current.CancellationToken);
		});

	[Fact]
	public async Task Should_PopulateHardDeleteKind_InContext()
		=> await AssertCapturesKind(EntityOperationKind.HardDelete, async (manager, repo, person) => {
			repo.HardDeleteAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(true);
			await manager.HardDeleteAsync(person, TestContext.Current.CancellationToken);
		});

	[Fact]
	public async Task Should_PopulateCreateKind_InContext_ForAddRange() {
		var interceptor = new ContextCapturingInterceptor();
		var (manager, _) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { interceptor });

		var person = CreatePerson("1");

		await manager.AddRangeAsync(new[] { person }, TestContext.Current.CancellationToken);

		Assert.Equal(EntityOperationKind.Create, interceptor.CapturedKind);
	}

	[Fact]
	public async Task Should_PopulateRemoveKind_InContext_ForRemoveRange() {
		var interceptor = new ContextCapturingInterceptor();
		var (manager, repo) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { interceptor });

		var person = CreatePerson("1");
		repo.RemoveRangeAsync(Arg.Any<IEnumerable<Person>>(), Arg.Any<CancellationToken>()).Returns(ValueTask.CompletedTask);

		await manager.RemoveRangeAsync(new[] { person }, TestContext.Current.CancellationToken);

		Assert.Equal(EntityOperationKind.Remove, interceptor.CapturedKind);
	}

	[Fact]
	public async Task Should_PopulateHardDeleteKind_InContext_ForHardDeleteRange() {
		var interceptor = new ContextCapturingInterceptor();
		var (manager, repo) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { interceptor });

		var person = CreatePerson("1");
		repo.HardDeleteRangeAsync(Arg.Any<IEnumerable<Person>>(), Arg.Any<CancellationToken>()).Returns(ValueTask.CompletedTask);

		await manager.HardDeleteRangeAsync(new[] { person }, TestContext.Current.CancellationToken);

		Assert.Equal(EntityOperationKind.HardDelete, interceptor.CapturedKind);
	}

	// --- PostWrite receives the success result ---

	[Fact]
	public async Task Should_PassSuccessResult_To_PostWrite() {
		var interceptor = new ResultCapturingInterceptor();
		var (manager, repo) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { interceptor });

		var person = CreatePerson("1");
		repo.AddAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(ValueTask.CompletedTask);

		await manager.AddAsync(person, TestContext.Current.CancellationToken);

		Assert.NotNull(interceptor.CapturedResult);
		Assert.True(interceptor.CapturedResult!.IsSuccess());
	}

	// --- Items bag shared between pre and post ---

	[Fact]
	public async Task Should_ShareItemsBag_Between_PreWrite_And_PostWrite() {
		var interceptor = new ItemsPrePostInterceptor();
		var (manager, _) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { interceptor });

		var person = CreatePerson("1");

		await manager.AddAsync(person, TestContext.Current.CancellationToken);

		Assert.True(interceptor.PostWriteSawPreWriteItem);
	}

	// --- Original pre-image on Remove and HardDelete ---
	//
	// The two Original-pre-image tests share the same shape (build manager
	// with a ContextCapturingInterceptor, seed FindAsync, configure the
	// underlying repo call to return true, invoke the operation, assert
	// the interceptor captured the original). They are expressed below as
	// calls to a single helper that takes the operation and the repo-setup
	// as delegates.

	private static async Task AssertCapturesOriginal(
		ContextCapturingInterceptor interceptor,
		Func<EntityManager<Person, string>, IRepository<Person, string>, Person, Task> invokeAndSeedRepo) {
		var (manager, repo) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { interceptor });

		var person = CreatePerson("1");
		person.FirstName = "LoadedFromRepo";
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(person);

		await invokeAndSeedRepo(manager, repo, person);

		Assert.NotNull(interceptor.CapturedOriginal);
	}

	[Fact]
	public async Task Should_PopulateOriginal_OnRemove() {
		var interceptor = new ContextCapturingInterceptor();
		await AssertCapturesOriginal(interceptor, async (manager, repo, person) => {
			repo.RemoveAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(true);
			await manager.RemoveAsync(person, TestContext.Current.CancellationToken);
			Assert.Same(person, interceptor.CapturedOriginal);
		});
	}

	[Fact]
	public async Task Should_PopulateOriginal_OnHardDelete()
		=> await AssertCapturesOriginal(new ContextCapturingInterceptor(), async (manager, repo, person) => {
			repo.HardDeleteAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(true);
			await manager.HardDeleteAsync(person, TestContext.Current.CancellationToken);
		});

}