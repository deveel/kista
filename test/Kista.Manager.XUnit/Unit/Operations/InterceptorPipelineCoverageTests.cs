#pragma warning disable CS8618

using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Kista.Caching;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
[Trait("Feature", "OperationPipeline")]
public class InterceptorPipelineCoverageTests {
	private readonly PersonFaker _faker = new();

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
		var person = _faker.Generate();
		person.Id = "1";

		var result = await manager.AddAsync(person, TestContext.Current.CancellationToken);

		Assert.False(result.IsSuccess());
		await repo.DidNotReceive().AddAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Should_HandleShortCircuitResultThatIsNotOperationResult_Without_Error_Success() {
		var customResult = new CustomOperationResult(hasError: false, isSuccess: true);
		var interceptor = new CustomResultInterceptor(customResult);

		var (manager, repo) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { interceptor });
		var person = _faker.Generate();
		person.Id = "1";

		var result = await manager.AddAsync(person, TestContext.Current.CancellationToken);

		Assert.True(result.IsSuccess());
		await repo.DidNotReceive().AddAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Should_HandleShortCircuitResultThatIsNotOperationResult_Without_Error_Failure() {
		var customResult = new CustomOperationResult(hasError: false, isSuccess: false);
		var interceptor = new CustomResultInterceptor(customResult);

		var (manager, repo) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { interceptor });
		var person = _faker.Generate();
		person.Id = "1";

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
		var person = _faker.Generate();
		person.Id = "1";

		await manager.AddAsync(person, TestContext.Current.CancellationToken);

		Assert.Equal("user-99", interceptor.CapturedActor);
	}

	[Fact]
	public async Task Should_PopulateNullActor_When_UserAccessorReturnsNull() {
		var interceptor = new ContextCapturingInterceptor();
		var userAccessor = new StaticUserAccessor<string>(null);

		var (manager, _) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { interceptor }, userAccessor);
		var person = _faker.Generate();
		person.Id = "1";

		await manager.AddAsync(person, TestContext.Current.CancellationToken);

		Assert.Null(interceptor.CapturedActor);
	}

	[Fact]
	public async Task Should_PopulateNullActor_When_NoUserAccessorRegistered() {
		var interceptor = new ContextCapturingInterceptor();

		var (manager, _) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { interceptor });
		var person = _faker.Generate();
		person.Id = "1";

		await manager.AddAsync(person, TestContext.Current.CancellationToken);

		Assert.Null(interceptor.CapturedActor);
	}

	// --- Short-circuit in RemoveAsync ---

	[Fact]
	public async Task Should_ShortCircuitRemoveAsync() {
		var interceptor = new ShortCircuitInterceptor();
		var (manager, repo) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { interceptor });

		var person = _faker.Generate();
		person.Id = "1";
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(person);

		var result = await manager.RemoveAsync(person, TestContext.Current.CancellationToken);

		Assert.False(result.IsSuccess());
		await repo.DidNotReceive().RemoveAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>());
		await repo.DidNotReceive().UpdateAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>());
	}

	// --- Short-circuit in UpdateAsync ---

	[Fact]
	public async Task Should_ShortCircuitUpdateAsync() {
		var interceptor = new ShortCircuitInterceptor();
		var (manager, repo) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { interceptor });

		var existing = _faker.Generate();
		existing.Id = "1";
		existing.FirstName = "Old";
		var updated = _faker.Generate();
		updated.Id = "1";
		updated.FirstName = "New";
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(existing);

		var result = await manager.UpdateAsync(updated, TestContext.Current.CancellationToken);

		Assert.False(result.IsSuccess());
		await repo.DidNotReceive().UpdateAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>());
	}

	// --- Short-circuit in HardDeleteAsync ---

	[Fact]
	public async Task Should_ShortCircuitHardDeleteAsync() {
		var interceptor = new ShortCircuitInterceptor();
		var (manager, repo) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { interceptor });

		var person = _faker.Generate();
		person.Id = "1";
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(person);

		var result = await manager.HardDeleteAsync(person, TestContext.Current.CancellationToken);

		Assert.False(result.IsSuccess());
		await repo.DidNotReceive().HardDeleteAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>());
	}

	// --- HardDeleteAsync success path through pipeline ---

	[Fact]
	public async Task Should_RunPipeline_ForHardDeleteSuccess() {
		var callOrder = new List<string>();
		var interceptor = new RecordingInterceptor("A", callOrder);

		var (manager, repo) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { interceptor });

		var person = _faker.Generate();
		person.Id = "1";
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

		var person = _faker.Generate();
		person.Id = "1";
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(person);
		repo.HardDeleteAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(false);

		var result = await manager.HardDeleteAsync(person, TestContext.Current.CancellationToken);

		Assert.True(result.IsUnchanged());
	}

	[Fact]
	public async Task Should_ReturnNotFound_When_HardDeleteEntityNotFound() {
		var (manager, _) = BuildManager();
		var person = _faker.Generate();
		person.Id = "1";

		var result = await manager.HardDeleteAsync(person, TestContext.Current.CancellationToken);

		Assert.False(result.IsSuccess());
		Assert.NotNull(result.Error);
	}

	[Fact]
	public async Task Should_ReturnFailed_When_HardDeleteEntityHasNoKey() {
		var (manager, _) = BuildManager();
		var person = _faker.Generate();
		person.Id = null;

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
		var person = _faker.Generate();
		person.Id = "1";

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

		var person = _faker.Generate();
		person.Id = "1";

		var result = await manager.RemoveRangeAsync(new[] { person }, TestContext.Current.CancellationToken);

		Assert.False(result.IsSuccess());
		await repo.DidNotReceive().RemoveRangeAsync(Arg.Any<IEnumerable<Person>>(), Arg.Any<CancellationToken>());
	}

	// --- Short-circuit in HardDeleteRangeAsync ---

	[Fact]
	public async Task Should_ShortCircuitHardDeleteRangeAsync() {
		var interceptor = new ShortCircuitInterceptor();
		var (manager, repo) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { interceptor });

		var person = _faker.Generate();
		person.Id = "1";

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

		var existing = _faker.Generate();
		existing.Id = "1";
		existing.FirstName = "Old";
		var updated = _faker.Generate();
		updated.Id = "1";
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
		var person = _faker.Generate();
		person.Id = "1";
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
		var person = _faker.Generate();
		person.Id = "1";

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

		var person = _faker.Generate();
		person.Id = "1";

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

	[Fact]
	public async Task Should_PopulateRemoveKind_InContext() {
		var interceptor = new ContextCapturingInterceptor();
		var (manager, repo) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { interceptor });

		var person = _faker.Generate();
		person.Id = "1";
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(person);
		repo.RemoveAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(true);

		await manager.RemoveAsync(person, TestContext.Current.CancellationToken);

		Assert.Equal(EntityOperationKind.Remove, interceptor.CapturedKind);
	}

	[Fact]
	public async Task Should_PopulateHardDeleteKind_InContext() {
		var interceptor = new ContextCapturingInterceptor();
		var (manager, repo) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { interceptor });

		var person = _faker.Generate();
		person.Id = "1";
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(person);
		repo.HardDeleteAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(true);

		await manager.HardDeleteAsync(person, TestContext.Current.CancellationToken);

		Assert.Equal(EntityOperationKind.HardDelete, interceptor.CapturedKind);
	}

	[Fact]
	public async Task Should_PopulateCreateKind_InContext_ForAddRange() {
		var interceptor = new ContextCapturingInterceptor();
		var (manager, _) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { interceptor });

		var person = _faker.Generate();
		person.Id = "1";

		await manager.AddRangeAsync(new[] { person }, TestContext.Current.CancellationToken);

		Assert.Equal(EntityOperationKind.Create, interceptor.CapturedKind);
	}

	[Fact]
	public async Task Should_PopulateRemoveKind_InContext_ForRemoveRange() {
		var interceptor = new ContextCapturingInterceptor();
		var (manager, repo) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { interceptor });

		var person = _faker.Generate();
		person.Id = "1";
		repo.RemoveRangeAsync(Arg.Any<IEnumerable<Person>>(), Arg.Any<CancellationToken>()).Returns(ValueTask.CompletedTask);

		await manager.RemoveRangeAsync(new[] { person }, TestContext.Current.CancellationToken);

		Assert.Equal(EntityOperationKind.Remove, interceptor.CapturedKind);
	}

	[Fact]
	public async Task Should_PopulateHardDeleteKind_InContext_ForHardDeleteRange() {
		var interceptor = new ContextCapturingInterceptor();
		var (manager, repo) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { interceptor });

		var person = _faker.Generate();
		person.Id = "1";
		repo.HardDeleteRangeAsync(Arg.Any<IEnumerable<Person>>(), Arg.Any<CancellationToken>()).Returns(ValueTask.CompletedTask);

		await manager.HardDeleteRangeAsync(new[] { person }, TestContext.Current.CancellationToken);

		Assert.Equal(EntityOperationKind.HardDelete, interceptor.CapturedKind);
	}

	// --- PostWrite receives the success result ---

	[Fact]
	public async Task Should_PassSuccessResult_To_PostWrite() {
		var interceptor = new ResultCapturingInterceptor();
		var (manager, repo) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { interceptor });

		var person = _faker.Generate();
		person.Id = "1";
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

		var person = _faker.Generate();
		person.Id = "1";

		await manager.AddAsync(person, TestContext.Current.CancellationToken);

		Assert.True(interceptor.PostWriteSawPreWriteItem);
	}

	// --- Original pre-image on Remove and HardDelete ---

	[Fact]
	public async Task Should_PopulateOriginal_OnRemove() {
		var interceptor = new ContextCapturingInterceptor();
		var (manager, repo) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { interceptor });

		var person = _faker.Generate();
		person.Id = "1";
		person.FirstName = "LoadedFromRepo";
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(person);
		repo.RemoveAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(true);

		await manager.RemoveAsync(person, TestContext.Current.CancellationToken);

		Assert.NotNull(interceptor.CapturedOriginal);
		Assert.Same(person, interceptor.CapturedOriginal);
	}

	[Fact]
	public async Task Should_PopulateOriginal_OnHardDelete() {
		var interceptor = new ContextCapturingInterceptor();
		var (manager, repo) = BuildManager(new IEntityManagerInterceptor<Person, string>[] { interceptor });

		var person = _faker.Generate();
		person.Id = "1";
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(person);
		repo.HardDeleteAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(true);

		await manager.HardDeleteAsync(person, TestContext.Current.CancellationToken);

		Assert.NotNull(interceptor.CapturedOriginal);
	}

	// --- Test interceptors ---

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
		public MutatingInterceptor(Action<Person> mutate) => _mutate = mutate;
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

	private class CountingInterceptor : IEntityManagerInterceptor<Person, string> {
		public int PreWriteCount;
		public int PostWriteCount;
		public ValueTask<IOperationResult?> PreWriteAsync(IEntityOperationContext<Person, string> context) {
			PreWriteCount++;
			return default;
		}
		public ValueTask PostWriteAsync(IEntityOperationContext<Person, string> context, IOperationResult result) {
			PostWriteCount++;
			return ValueTask.CompletedTask;
		}
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

	private class ResultCapturingInterceptor : IEntityManagerInterceptor<Person, string> {
		public IOperationResult? CapturedResult;
		public ValueTask<IOperationResult?> PreWriteAsync(IEntityOperationContext<Person, string> context)
			=> default;
		public ValueTask PostWriteAsync(IEntityOperationContext<Person, string> context, IOperationResult result) {
			CapturedResult = result;
			return ValueTask.CompletedTask;
		}
	}

	private class ItemsPrePostInterceptor : IEntityManagerInterceptor<Person, string> {
		public bool PostWriteSawPreWriteItem;
		public ValueTask<IOperationResult?> PreWriteAsync(IEntityOperationContext<Person, string> context) {
			context.Items["pre"] = "set";
			return default;
		}
		public ValueTask PostWriteAsync(IEntityOperationContext<Person, string> context, IOperationResult result) {
			PostWriteSawPreWriteItem = context.Items.TryGetValue("pre", out var val) && Equals(val, "set");
			return ValueTask.CompletedTask;
		}
	}

	private class CustomResultInterceptor : IEntityManagerInterceptor<Person, string> {
		private readonly IOperationResult _result;
		public CustomResultInterceptor(IOperationResult result) => _result = result;
		public ValueTask<IOperationResult?> PreWriteAsync(IEntityOperationContext<Person, string> context)
			=> new(_result);
		public ValueTask PostWriteAsync(IEntityOperationContext<Person, string> context, IOperationResult result)
			=> ValueTask.CompletedTask;
	}

	private class SingleKeyCountingInterceptor : IEntityManagerInterceptor<Person> {
		public int PreWriteCount;
		public int PostWriteCount;
		public ValueTask<IOperationResult?> PreWriteAsync(IEntityOperationContext<Person, object> context) {
			PreWriteCount++;
			return default;
		}
		public ValueTask PostWriteAsync(IEntityOperationContext<Person, object> context, IOperationResult result) {
			PostWriteCount++;
			return ValueTask.CompletedTask;
		}
	}

	private class GenericShortCircuitInterceptor<T, K> : IEntityManagerInterceptor<T, K>
		where T : class where K : notnull {
		public ValueTask<IOperationResult?> PreWriteAsync(IEntityOperationContext<T, K> context)
			=> new(OperationResult.Fail(new OperationError("SHORT_CIRCUIT", "Test", "Short-circuited")));
		public ValueTask PostWriteAsync(IEntityOperationContext<T, K> context, IOperationResult result)
			=> ValueTask.CompletedTask;
	}

	private class GenericRecordingInterceptor<T, K> : IEntityManagerInterceptor<T, K>
		where T : class where K : notnull {
		private readonly string _name;
		private readonly List<string> _callOrder;
		public GenericRecordingInterceptor(string name, List<string> callOrder) {
			_name = name;
			_callOrder = callOrder;
		}
		public ValueTask<IOperationResult?> PreWriteAsync(IEntityOperationContext<T, K> context) {
			_callOrder.Add($"{_name}.Pre");
			return default;
		}
		public ValueTask PostWriteAsync(IEntityOperationContext<T, K> context, IOperationResult result) {
			_callOrder.Add($"{_name}.Post");
			return ValueTask.CompletedTask;
		}
	}

	private class GenericCountingInterceptor<T, K> : IEntityManagerInterceptor<T, K>
		where T : class where K : notnull {
		public int PreWriteCount;
		public int PostWriteCount;
		public ValueTask<IOperationResult?> PreWriteAsync(IEntityOperationContext<T, K> context) {
			PreWriteCount++;
			return default;
		}
		public ValueTask PostWriteAsync(IEntityOperationContext<T, K> context, IOperationResult result) {
			PostWriteCount++;
			return ValueTask.CompletedTask;
		}
	}

	private sealed class StaticUserAccessor<TKey> : IUserAccessor<TKey> {
		private readonly TKey? _userId;
		public StaticUserAccessor(TKey? userId) => _userId = userId;
		public TKey? GetUserId() => _userId;
	}

	/// <summary>
	/// A custom IOperationResult implementation that is not an OperationResult struct,
	/// to exercise the fallback branches of ToOperationResult.
	/// </summary>
	private class CustomOperationResult : IOperationResult {
		private readonly bool _hasError;
		private readonly bool _isSuccess;

		public CustomOperationResult(bool hasError, bool isSuccess = false) {
			_hasError = hasError;
			_isSuccess = isSuccess;
		}

		public IOperationError? Error => _hasError ? new OperationError("CUSTOM", "Test", "Custom error") : null;
		public OperationResultType ResultType => _isSuccess ? OperationResultType.Success : OperationResultType.Error;
	}
}