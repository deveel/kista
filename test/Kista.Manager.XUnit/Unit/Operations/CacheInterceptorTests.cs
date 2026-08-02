#pragma warning disable CS8618

using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Kista.Caching;

namespace Kista;

/// <summary>
/// Unit tests for the builtin <see cref="CacheInterceptor{TEntity, TKey}"/>
/// that aligns the entity cache to the operation pipeline (issue #120).
/// </summary>
/// <remarks>
/// These tests verify that the cache is re-cached on Create / Update /
/// Restore, evicted on HardDelete and on the hard branch of Remove,
/// re-cached on the soft-delete branch of Remove, and that cache
/// failures are swallowed. They also verify the behavior change that
/// aligns <see cref="EntityManager{TEntity, TKey}.RemoveRangeAsync"/>
/// with <see cref="EntityManager{TEntity, TKey}.RemoveAsync"/>: soft-
/// deletable entities in a range Remove are now re-cached instead of
/// evicted.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
[Trait("Feature", "Caching")]
[Trait("Feature", "OperationPipeline")]
public class CacheInterceptorTests {
	private static readonly PersonFaker _faker = new();
	private static readonly SoftDeletablePersonFaker _softFaker = new();

	private static Person CreatePerson(string? id = "1") {
		var person = _faker.Generate();
		person.Id = id;
		return person;
	}

	private static SoftDeletablePerson CreateSoftPerson(string? id = "1", bool isDeleted = false) {
		var person = _softFaker.Generate();
		person.Id = id;
		person.IsDeleted = isDeleted;
		return person;
	}

	// --- Manager builders ---

	private static (EntityManager<Person, string> manager, IRepository<Person, string> repo) BuildManager(
		IEntityCache<Person>? cache = null,
		IEntityCacheKeyGenerator<Person>? keyGenerator = null,
		IEnumerable<IEntityManagerInterceptor<Person, string>>? interceptors = null) {
		var repo = Substitute.For<IRepository<Person, string>>();
		repo.GetEntityKey(Arg.Any<Person>()).Returns(c => c.Arg<Person>().Id);
		repo.AddAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(ValueTask.CompletedTask);
		repo.UpdateAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(true);
		repo.RemoveAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(true);
		repo.HardDeleteAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(true);
		repo.FindAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Person?)null);

		// When a cache is registered, FindAsync delegates to the read-through
		// GetOrSetAsync: route it back to the repo so seeded FindAsync returns
		// win over the (default null) cache proxy.
		if (cache != null)
			cache.GetOrSetAsync(Arg.Any<string>(), Arg.Any<Func<ValueTask<Person?>>>(), Arg.Any<CancellationToken>())
				.Returns(callInfo => callInfo.Arg<Func<ValueTask<Person?>>>()());

		var services = new ServiceCollection();
		if (keyGenerator != null)
			services.AddSingleton(keyGenerator);
		foreach (var interceptor in interceptors ?? Array.Empty<IEntityManagerInterceptor<Person, string>>())
			services.AddSingleton(interceptor);

		var provider = services.BuildServiceProvider();
		var manager = new EntityManager<Person, string>(repo, cache: cache, services: provider);
		return (manager, repo);
	}

	private static (EntityManager<SoftDeletablePerson, string> manager, IRepository<SoftDeletablePerson, string> repo) BuildSoftManager(
		IEntityCache<SoftDeletablePerson>? cache = null,
		IEntityCacheKeyGenerator<SoftDeletablePerson>? keyGenerator = null) {
		var repo = Substitute.For<IRepository<SoftDeletablePerson, string>>();
		repo.GetEntityKey(Arg.Any<SoftDeletablePerson>()).Returns(c => c.Arg<SoftDeletablePerson>().Id);
		repo.UpdateAsync(Arg.Any<SoftDeletablePerson>(), Arg.Any<CancellationToken>()).Returns(true);
		repo.FindAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((SoftDeletablePerson?)null);

		if (cache != null)
			cache.GetOrSetAsync(Arg.Any<string>(), Arg.Any<Func<ValueTask<SoftDeletablePerson?>>>(), Arg.Any<CancellationToken>())
				.Returns(callInfo => callInfo.Arg<Func<ValueTask<SoftDeletablePerson?>>>()());

		var services = new ServiceCollection();
		if (keyGenerator != null)
			services.AddSingleton(keyGenerator);

		var provider = services.BuildServiceProvider();
		var manager = new EntityManager<SoftDeletablePerson, string>(repo, cache: cache, services: provider);
		return (manager, repo);
	}

	private static IEntityCache<T> CreateCache<T>() where T : class
		=> Substitute.For<IEntityCache<T>>();

	private static IEntityCacheKeyGenerator<T> CreateKeyGenerator<T>() where T : class {
		var gen = Substitute.For<IEntityCacheKeyGenerator<T>>();
		gen.GenerateAllKeys(Arg.Any<T>()).Returns(c => new[] { $"key:{c.Arg<T>().GetHashCode()}" });
		gen.GenerateKey(Arg.Any<object>()).Returns(c => $"key:{c.Arg<object>().GetHashCode()}");
		return gen;
	}

	// --- Create ---

	[Fact]
	public async Task Should_CacheEntity_When_AddSucceeds() {
		var cache = CreateCache<Person>();
		var keyGen = CreateKeyGenerator<Person>();
		var (manager, _) = BuildManager(cache, keyGen);
		var person = CreatePerson("1");

		await manager.AddAsync(person, TestContext.Current.CancellationToken);

		await cache.Received().SetAsync(
			Arg.Is<string[]>(keys => keys.Length == 1),
			Arg.Is<Person>(p => p.Id == "1"),
			Arg.Any<CancellationToken>());
		await cache.DidNotReceive().RemoveAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Should_NotCacheEntity_When_AddShortCircuits() {
		var cache = CreateCache<Person>();
		var keyGen = CreateKeyGenerator<Person>();
		var shortCircuit = new ShortCircuitInterceptor();
		var (manager, repo) = BuildManager(cache, keyGen, new IEntityManagerInterceptor<Person, string>[] { shortCircuit });
		var person = CreatePerson("1");

		var result = await manager.AddAsync(person, TestContext.Current.CancellationToken);

		Assert.False(result.IsSuccess());
		await cache.DidNotReceive().SetAsync(Arg.Any<string[]>(), Arg.Any<Person>(), Arg.Any<CancellationToken>());
		await cache.DidNotReceive().RemoveAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>());
		await repo.DidNotReceive().AddAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>());
	}

	// --- Update ---

	[Fact]
	public async Task Should_CacheEntity_When_UpdateSucceeds() {
		var cache = CreateCache<Person>();
		var keyGen = CreateKeyGenerator<Person>();
		var (manager, repo) = BuildManager(cache, keyGen);

		var existing = CreatePerson("1");
		existing.FirstName = "Old";
		var updated = CreatePerson("1");
		updated.FirstName = "New";
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(existing);
		repo.UpdateAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(true);

		await manager.UpdateAsync(updated, TestContext.Current.CancellationToken);

		await cache.Received().SetAsync(
			Arg.Any<string[]>(),
			Arg.Is<Person>(p => p.FirstName == "New"),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Should_NotCacheEntity_When_UpdateReturnsNotChanged() {
		var cache = CreateCache<Person>();
		var keyGen = CreateKeyGenerator<Person>();
		var (manager, repo) = BuildManager(cache, keyGen);

		// The manager's AreEqual short-circuits the update (returns NotChanged)
		// when the loaded entity and the provided entity are equal: use the
		// same instance so the reference equality holds and no cache Set is
		// performed in PostWriteAsync (NotChanged is not a success result).
		var existing = CreatePerson("1");
		var updated = existing;
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(existing);
		repo.UpdateAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(true);

		await manager.UpdateAsync(updated, TestContext.Current.CancellationToken);

		await cache.DidNotReceive().SetAsync(Arg.Any<string[]>(), Arg.Any<Person>(), Arg.Any<CancellationToken>());
	}

	// --- Restore ---

	[Fact]
	public async Task Should_CacheEntity_When_RestoreSucceeds() {
		var cache = CreateCache<SoftDeletablePerson>();
		var keyGen = CreateKeyGenerator<SoftDeletablePerson>();
		var (manager, repo) = BuildSoftManager(cache, keyGen);

		var person = CreateSoftPerson("1", isDeleted: true);
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(person);
		repo.UpdateAsync(Arg.Any<SoftDeletablePerson>(), Arg.Any<CancellationToken>()).Returns(true);

		await manager.RestoreAsync(person, TestContext.Current.CancellationToken);

		await cache.Received().SetAsync(
			Arg.Any<string[]>(),
			Arg.Is<SoftDeletablePerson>(p => p.IsDeleted == false),
			Arg.Any<CancellationToken>());
	}

	// --- Remove (single) ---

	[Fact]
	public async Task Should_ReCacheEntity_When_RemoveOnSoftDeletable() {
		var cache = CreateCache<SoftDeletablePerson>();
		var keyGen = CreateKeyGenerator<SoftDeletablePerson>();
		var (manager, repo) = BuildSoftManager(cache, keyGen);

		var person = CreateSoftPerson("1", isDeleted: false);
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(person);
		repo.UpdateAsync(Arg.Any<SoftDeletablePerson>(), Arg.Any<CancellationToken>()).Returns(true);

		await manager.RemoveAsync(person, TestContext.Current.CancellationToken);

		await cache.Received().SetAsync(
			Arg.Any<string[]>(),
			Arg.Is<SoftDeletablePerson>(p => p.IsDeleted == true),
			Arg.Any<CancellationToken>());
		await cache.DidNotReceive().RemoveAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Should_EvictEntity_When_RemoveOnNonSoftDeletable() {
		var cache = CreateCache<Person>();
		var keyGen = CreateKeyGenerator<Person>();
		var (manager, repo) = BuildManager(cache, keyGen);

		var person = CreatePerson("1");
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(person);
		repo.RemoveAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(true);

		await manager.RemoveAsync(person, TestContext.Current.CancellationToken);

		await cache.Received().RemoveAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>());
		await cache.DidNotReceive().SetAsync(Arg.Any<string[]>(), Arg.Any<Person>(), Arg.Any<CancellationToken>());
	}

	// --- HardDelete (single) ---

	[Fact]
	public async Task Should_EvictEntity_When_HardDeleteSucceeds() {
		var cache = CreateCache<Person>();
		var keyGen = CreateKeyGenerator<Person>();
		var (manager, repo) = BuildManager(cache, keyGen);

		var person = CreatePerson("1");
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(person);
		repo.HardDeleteAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(true);

		await manager.HardDeleteAsync(person, TestContext.Current.CancellationToken);

		await cache.Received().RemoveAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>());
		await cache.DidNotReceive().SetAsync(Arg.Any<string[]>(), Arg.Any<Person>(), Arg.Any<CancellationToken>());
	}

	// --- Range operations ---

	[Fact]
	public async Task Should_CacheAllEntities_When_AddRangeSucceeds() {
		var cache = CreateCache<Person>();
		var keyGen = CreateKeyGenerator<Person>();
		var (manager, repo) = BuildManager(cache, keyGen);
		repo.AddRangeAsync(Arg.Any<IEnumerable<Person>>(), Arg.Any<CancellationToken>()).Returns(ValueTask.CompletedTask);

		var people = new List<Person> { CreatePerson("1"), CreatePerson("2"), CreatePerson("3") };

		await manager.AddRangeAsync(people, TestContext.Current.CancellationToken);

		await cache.Received(3).SetAsync(Arg.Any<string[]>(), Arg.Any<Person>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Should_ReCacheSoftDeletableEntity_When_RemoveRange() {
		// Behavior change in v1.7.x: RemoveRangeAsync now aligns with RemoveAsync,
		// re-caching soft-deletable entities instead of evicting them.
		var cache = CreateCache<SoftDeletablePerson>();
		var keyGen = CreateKeyGenerator<SoftDeletablePerson>();
		var (manager, repo) = BuildSoftManager(cache, keyGen);
		repo.RemoveRangeAsync(Arg.Any<IEnumerable<SoftDeletablePerson>>(), Arg.Any<CancellationToken>()).Returns(ValueTask.CompletedTask);

		var softPerson = CreateSoftPerson("1", isDeleted: false);

		await manager.RemoveRangeAsync(new[] { softPerson }, TestContext.Current.CancellationToken);

		await cache.Received().SetAsync(
			Arg.Any<string[]>(),
			Arg.Is<SoftDeletablePerson>(p => p.IsDeleted == true),
			Arg.Any<CancellationToken>());
		await cache.DidNotReceive().RemoveAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Should_EvictNonSoftDeletableEntity_When_RemoveRange() {
		var cache = CreateCache<Person>();
		var keyGen = CreateKeyGenerator<Person>();
		var (manager, repo) = BuildManager(cache, keyGen);
		repo.RemoveRangeAsync(Arg.Any<IEnumerable<Person>>(), Arg.Any<CancellationToken>()).Returns(ValueTask.CompletedTask);

		var person = CreatePerson("1");

		await manager.RemoveRangeAsync(new[] { person }, TestContext.Current.CancellationToken);

		await cache.Received().RemoveAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>());
		await cache.DidNotReceive().SetAsync(Arg.Any<string[]>(), Arg.Any<Person>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Should_EvictAllEntities_When_HardDeleteRange() {
		var cache = CreateCache<Person>();
		var keyGen = CreateKeyGenerator<Person>();
		var (manager, repo) = BuildManager(cache, keyGen);
		repo.HardDeleteRangeAsync(Arg.Any<IEnumerable<Person>>(), Arg.Any<CancellationToken>()).Returns(ValueTask.CompletedTask);

		var people = new List<Person> { CreatePerson("1"), CreatePerson("2") };

		await manager.HardDeleteRangeAsync(people, TestContext.Current.CancellationToken);

		await cache.Received(2).RemoveAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>());
		await cache.DidNotReceive().SetAsync(Arg.Any<string[]>(), Arg.Any<Person>(), Arg.Any<CancellationToken>());
	}

	// --- No cache / no key generator ---

	[Fact]
	public async Task Should_NotInteractWithCache_When_NoCacheRegistered() {
		// No cache => the interceptor is not appended to the chain at all.
		// The operation must still succeed (regression guard for the wiring).
		var keyGen = CreateKeyGenerator<Person>();
		var (manager, repo) = BuildManager(cache: null, keyGen);
		var person = CreatePerson("1");

		var result = await manager.AddAsync(person, TestContext.Current.CancellationToken);

		Assert.True(result.IsSuccess());
		await repo.Received().AddAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Should_NotInteractWithCache_When_NoKeyGeneratorRegistered() {
		// A cache is registered, but no IEntityCacheKeyGenerator: the interceptor
		// resolves an empty array of keys and skips the cache interaction.
		var cache = CreateCache<Person>();
		var (manager, _) = BuildManager(cache, keyGenerator: null);
		var person = CreatePerson("1");

		await manager.AddAsync(person, TestContext.Current.CancellationToken);

		await cache.DidNotReceive().SetAsync(Arg.Any<string[]>(), Arg.Any<Person>(), Arg.Any<CancellationToken>());
		await cache.DidNotReceive().RemoveAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>());
	}

	// --- Cache failure resilience ---

	[Fact]
	public async Task Should_NotFail_When_CacheSetThrows() {
		var cache = CreateCache<Person>();
		cache.SetAsync(Arg.Any<string[]>(), Arg.Any<Person>(), Arg.Any<CancellationToken>())
			.Throws(new InvalidOperationException("cache down"));
		var keyGen = CreateKeyGenerator<Person>();
		var (manager, _) = BuildManager(cache, keyGen);
		var person = CreatePerson("1");

		var result = await manager.AddAsync(person, TestContext.Current.CancellationToken);

		Assert.True(result.IsSuccess());
	}

	[Fact]
	public async Task Should_NotFail_When_CacheEvictionThrows() {
		var cache = CreateCache<Person>();
		cache.RemoveAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>())
			.Throws(new InvalidOperationException("cache down"));
		var keyGen = CreateKeyGenerator<Person>();
		var (manager, repo) = BuildManager(cache, keyGen);
		var person = CreatePerson("1");
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(person);
		repo.RemoveAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(true);

		var result = await manager.RemoveAsync(person, TestContext.Current.CancellationToken);

		Assert.True(result.IsSuccess());
	}

	// --- FindAsync read-through stays inline (regression guard) ---

	[Fact]
	public async Task Should_PreserveFindReadThroughCache() {
		var cache = CreateCache<Person>();
		var keyGen = CreateKeyGenerator<Person>();
		var (manager, repo) = BuildManager(cache, keyGen);

		var person = CreatePerson("1");
		// GetOrSetAsync must call the factory when the cache returns null
		cache.GetOrSetAsync(Arg.Any<string>(), Arg.Any<Func<ValueTask<Person?>>>(), Arg.Any<CancellationToken>())
			.Returns(callInfo => callInfo.Arg<Func<ValueTask<Person?>>>()());
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(person);

		var result = await manager.FindAsync("1", TestContext.Current.CancellationToken);

		Assert.True(result.IsSuccess());
		// The read-through path goes through the cache (GetOrSetAsync was called)
		await cache.Received().GetOrSetAsync(
			Arg.Any<string>(),
			Arg.Any<Func<ValueTask<Person?>>>(),
			Arg.Any<CancellationToken>());
	}

	// --- Ordering: cache runs after hooks (sees mutated entity) ---

	[Fact]
	public async Task Should_RunCacheAfterHooks_When_HookMutatesEntity() {
		var cache = CreateCache<SoftDeletablePerson>();
		var keyGen = CreateKeyGenerator<SoftDeletablePerson>();
		var (manager, repo) = BuildSoftManager(cache, keyGen);

		var person = CreateSoftPerson("1", isDeleted: false);
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(person);
		repo.UpdateAsync(Arg.Any<SoftDeletablePerson>(), Arg.Any<CancellationToken>()).Returns(true);

		await manager.RemoveAsync(person, TestContext.Current.CancellationToken);

		// The OnRemovingEntityAsync hook (builtin, runs before the cache interceptor)
		// sets IsDeleted=true before the soft-delete branch re-caches the entity.
		await cache.Received().SetAsync(
			Arg.Any<string[]>(),
			Arg.Is<SoftDeletablePerson>(p => p.IsDeleted == true),
			Arg.Any<CancellationToken>());
		Assert.True(person.IsDeleted);
	}

	// --- NotChanged / failed results leave the cache untouched (line 184) ---

	[Fact]
	public async Task Should_NotInteractWithCache_When_UpdateReturnsNotChanged_WithCacheRegistered() {
		var cache = CreateCache<Person>();
		var keyGen = CreateKeyGenerator<Person>();
		var (manager, repo) = BuildManager(cache, keyGen);

		// AreEqual short-circuits to NotChanged before the repository write,
		// so PostWriteAsync receives a not-success result and must skip the cache.
		var existing = CreatePerson("1");
		var updated = existing;
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(existing);
		repo.UpdateAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(true);

		await manager.UpdateAsync(updated, TestContext.Current.CancellationToken);

		await cache.DidNotReceive().SetAsync(Arg.Any<string[]>(), Arg.Any<Person>(), Arg.Any<CancellationToken>());
		await cache.DidNotReceive().RemoveAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Should_NotInteractWithCache_When_UpdateRepoReturnsFalse_WithCacheRegistered() {
		var cache = CreateCache<Person>();
		var keyGen = CreateKeyGenerator<Person>();
		var (manager, repo) = BuildManager(cache, keyGen);

		var existing = CreatePerson("1");
		existing.FirstName = "Old";
		var updated = CreatePerson("1");
		updated.FirstName = "New";
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(existing);
		repo.UpdateAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(false);

		await manager.UpdateAsync(updated, TestContext.Current.CancellationToken);

		await cache.DidNotReceive().SetAsync(Arg.Any<string[]>(), Arg.Any<Person>(), Arg.Any<CancellationToken>());
		await cache.DidNotReceive().RemoveAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Should_NotInteractWithCache_When_RemoveRepoReturnsFalse_WithCacheRegistered() {
		var cache = CreateCache<Person>();
		var keyGen = CreateKeyGenerator<Person>();
		var (manager, repo) = BuildManager(cache, keyGen);

		var person = CreatePerson("1");
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(person);
		repo.RemoveAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(false);

		await manager.RemoveAsync(person, TestContext.Current.CancellationToken);

		await cache.DidNotReceive().SetAsync(Arg.Any<string[]>(), Arg.Any<Person>(), Arg.Any<CancellationToken>());
		await cache.DidNotReceive().RemoveAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Should_NotInteractWithCache_When_RestoreRepoReturnsFalse_WithCacheRegistered() {
		var cache = CreateCache<SoftDeletablePerson>();
		var keyGen = CreateKeyGenerator<SoftDeletablePerson>();
		var (manager, repo) = BuildSoftManager(cache, keyGen);

		var person = CreateSoftPerson("1", isDeleted: true);
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(person);
		repo.UpdateAsync(Arg.Any<SoftDeletablePerson>(), Arg.Any<CancellationToken>()).Returns(false);

		await manager.RestoreAsync(person, TestContext.Current.CancellationToken);

		await cache.DidNotReceive().SetAsync(Arg.Any<string[]>(), Arg.Any<SoftDeletablePerson>(), Arg.Any<CancellationToken>());
		await cache.DidNotReceive().RemoveAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Should_NotInteractWithCache_When_HardDeleteRepoReturnsFalse_WithCacheRegistered() {
		var cache = CreateCache<Person>();
		var keyGen = CreateKeyGenerator<Person>();
		var (manager, repo) = BuildManager(cache, keyGen);

		var person = CreatePerson("1");
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(person);
		repo.HardDeleteAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(false);

		await manager.HardDeleteAsync(person, TestContext.Current.CancellationToken);

		await cache.DidNotReceive().SetAsync(Arg.Any<string[]>(), Arg.Any<Person>(), Arg.Any<CancellationToken>());
		await cache.DidNotReceive().RemoveAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Should_NotInteractWithCache_When_SoftDeleteRemoveReturnsFalse_WithCacheRegistered() {
		// Covers the soft-delete branch of RemoveAsync where Repository.UpdateAsync
		// returns false (the entity was not soft-deleted): PostWriteAsync receives
		// a NotChanged result and must skip the cache.
		var cache = CreateCache<SoftDeletablePerson>();
		var keyGen = CreateKeyGenerator<SoftDeletablePerson>();
		var (manager, repo) = BuildSoftManager(cache, keyGen);

		var person = CreateSoftPerson("1", isDeleted: false);
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(person);
		repo.UpdateAsync(Arg.Any<SoftDeletablePerson>(), Arg.Any<CancellationToken>()).Returns(false);

		var result = await manager.RemoveAsync(person, TestContext.Current.CancellationToken);

		Assert.True(result.IsUnchanged());
		await cache.DidNotReceive().SetAsync(Arg.Any<string[]>(), Arg.Any<SoftDeletablePerson>(), Arg.Any<CancellationToken>());
		await cache.DidNotReceive().RemoveAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Should_NotInteractWithCache_When_ShortCircuitFails_WithCacheRegistered() {
		var cache = CreateCache<Person>();
		var keyGen = CreateKeyGenerator<Person>();
		var shortCircuit = new ShortCircuitInterceptor();
		var (manager, repo) = BuildManager(cache, keyGen, new IEntityManagerInterceptor<Person, string>[] { shortCircuit });
		var person = CreatePerson("1");

		var result = await manager.AddAsync(person, TestContext.Current.CancellationToken);

		Assert.False(result.IsSuccess());
		await cache.DidNotReceive().SetAsync(Arg.Any<string[]>(), Arg.Any<Person>(), Arg.Any<CancellationToken>());
		await cache.DidNotReceive().RemoveAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Should_NotInteractWithCache_When_ValidationFails_WithCacheRegistered() {
		// A failing validator returns a ValidationFailed result (not success):
		// the repository write is skipped, PostWriteAsync is not invoked, and
		// the cache is left untouched.
		var cache = CreateCache<Person>();
		var keyGen = CreateKeyGenerator<Person>();
		var repo = Substitute.For<IRepository<Person, string>>();
		repo.GetEntityKey(Arg.Any<Person>()).Returns(c => c.Arg<Person>().Id);
		repo.FindAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Person?)null);
		cache.GetOrSetAsync(Arg.Any<string>(), Arg.Any<Func<ValueTask<Person?>>>(), Arg.Any<CancellationToken>())
			.Returns(callInfo => callInfo.Arg<Func<ValueTask<Person?>>>()());

		var validator = Substitute.For<IEntityValidator<Person, string>>();
		validator.ValidateAsync(Arg.Any<EntityManager<Person, string>>(), Arg.Any<Person>(), Arg.Any<CancellationToken>())
			.Returns(callInfo => AsyncEnumerable(new[] { new System.ComponentModel.DataAnnotations.ValidationResult("nope", new[] { "FirstName" }) }));

		var services = new ServiceCollection();
		services.AddSingleton(keyGen);
		var provider = services.BuildServiceProvider();
		var manager = new EntityManager<Person, string>(repo, validator, cache, services: provider);

		var person = CreatePerson("1");
		var result = await manager.AddAsync(person, TestContext.Current.CancellationToken);

		Assert.False(result.IsSuccess());
		await cache.DidNotReceive().SetAsync(Arg.Any<string[]>(), Arg.Any<Person>(), Arg.Any<CancellationToken>());
		await cache.DidNotReceive().RemoveAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>());
		await repo.DidNotReceive().AddAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Should_NotInteractWithCache_When_UpdateValidationFails_WithCacheRegistered() {
		// Covers the UpdateAsync validation-failure branch: the entity is found
		// and differs from the provided one, but validation fails before the
		// pipeline runs. The cache must be left untouched.
		var cache = CreateCache<Person>();
		var keyGen = CreateKeyGenerator<Person>();
		var repo = Substitute.For<IRepository<Person, string>>();
		repo.GetEntityKey(Arg.Any<Person>()).Returns(c => c.Arg<Person>().Id);

		var existing = CreatePerson("1");
		existing.FirstName = "Old";
		var updated = CreatePerson("1");
		updated.FirstName = "New";
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(existing);
		cache.GetOrSetAsync(Arg.Any<string>(), Arg.Any<Func<ValueTask<Person?>>>(), Arg.Any<CancellationToken>())
			.Returns(callInfo => callInfo.Arg<Func<ValueTask<Person?>>>()());

		var validator = Substitute.For<IEntityValidator<Person, string>>();
		validator.ValidateAsync(Arg.Any<EntityManager<Person, string>>(), Arg.Any<Person>(), Arg.Any<CancellationToken>())
			.Returns(callInfo => AsyncEnumerable(new[] { new System.ComponentModel.DataAnnotations.ValidationResult("nope", new[] { "FirstName" }) }));

		var services = new ServiceCollection();
		services.AddSingleton(keyGen);
		var provider = services.BuildServiceProvider();
		var manager = new EntityManager<Person, string>(repo, validator, cache, services: provider);

		var result = await manager.UpdateAsync(updated, TestContext.Current.CancellationToken);

		Assert.False(result.IsSuccess());
		await cache.DidNotReceive().SetAsync(Arg.Any<string[]>(), Arg.Any<Person>(), Arg.Any<CancellationToken>());
		await cache.DidNotReceive().RemoveAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>());
		await repo.DidNotReceive().UpdateAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>());
	}

	private static async IAsyncEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> AsyncEnumerable(IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> results) {
		foreach (var r in results)
			yield return r;
		await Task.CompletedTask;
	}

	// --- Read-through cache exception fallback (EntityManager_T2 GetOrSetAsync) ---

	[Fact]
	public async Task Should_FallbackToRepository_When_ReadThroughCacheThrows() {
		var cache = CreateCache<Person>();
		var keyGen = CreateKeyGenerator<Person>();
		var (manager, repo) = BuildManager(cache, keyGen);

		var person = CreatePerson("1");
		// The read-through GetOrSetAsync throws: the manager must log, swallow,
		// and fall back to the value factory (the repository FindAsync).
		cache.GetOrSetAsync(Arg.Any<string>(), Arg.Any<Func<ValueTask<Person?>>>(), Arg.Any<CancellationToken>())
			.Throws(new InvalidOperationException("cache read down"));
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(person);

		var result = await manager.FindAsync("1", TestContext.Current.CancellationToken);

		Assert.True(result.IsSuccess());
		Assert.Equal("1", result.Value!.Id);
		// The repository was consulted through the factory fallback.
		await repo.Received().FindAsync("1", Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Should_NotUseCache_When_NoCacheRegistered_ForFindAsync() {
		// FindAsync with no cache registered must go straight to the repository
		// (the GetOrSetAsync early-returns the factory result when EntityCache is null).
		var keyGen = CreateKeyGenerator<Person>();
		var (manager, repo) = BuildManager(cache: null, keyGen);

		var person = CreatePerson("1");
		repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(person);

		var result = await manager.FindAsync("1", TestContext.Current.CancellationToken);

		Assert.True(result.IsSuccess());
		await repo.Received().FindAsync("1", Arg.Any<CancellationToken>());
	}

	// --- Direct CacheInterceptor unit tests (builtin internal type) ---

	[Fact]
	public async Task Should_ReturnNullFromPreWrite_When_InvokedDirectly() {
		// PreWriteAsync never short-circuits: it always returns null so the
		// repository write proceeds. This is the contract that makes the cache
		// a pure write-path concern.
		var interceptor = new CacheInterceptor<Person, string>(
			CreateCache<Person>(),
			CreateKeyGenerator<Person>(),
			p => p.Id);

		var context = new EntityOperationContext<Person, string>(
			EntityOperationKind.Create, CreatePerson("1"), original: null, "1",
			actor: null, DateTimeOffset.UtcNow, CancellationToken.None);

		var result = await interceptor.PreWriteAsync(context);

		Assert.Null(result);
	}

	[Fact]
	public async Task Should_SkipCache_When_PostWriteReceivesNotChangedResult() {
		// Direct unit test of the !IsSuccess early-return: a NotChanged result
		// must leave the cache untouched regardless of the operation kind.
		var cache = CreateCache<Person>();
		var keyGen = CreateKeyGenerator<Person>();
		var interceptor = new CacheInterceptor<Person, string>(cache, keyGen, p => p.Id);

		var person = CreatePerson("1");
		var context = new EntityOperationContext<Person, string>(
			EntityOperationKind.Create, person, original: null, "1",
			actor: null, DateTimeOffset.UtcNow, CancellationToken.None);

		await interceptor.PostWriteAsync(context, OperationResult.NotChanged);

		await cache.DidNotReceive().SetAsync(Arg.Any<string[]>(), Arg.Any<Person>(), Arg.Any<CancellationToken>());
		await cache.DidNotReceive().RemoveAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Should_SkipCache_When_PostWriteReceivesFailedResult() {
		var cache = CreateCache<Person>();
		var keyGen = CreateKeyGenerator<Person>();
		var interceptor = new CacheInterceptor<Person, string>(cache, keyGen, p => p.Id);

		var person = CreatePerson("1");
		var context = new EntityOperationContext<Person, string>(
			EntityOperationKind.Update, person, original: person, "1",
			actor: null, DateTimeOffset.UtcNow, CancellationToken.None);

		await interceptor.PostWriteAsync(context, OperationResult.Fail(new OperationError("ERR", "Test", "boom")));

		await cache.DidNotReceive().SetAsync(Arg.Any<string[]>(), Arg.Any<Person>(), Arg.Any<CancellationToken>());
		await cache.DidNotReceive().RemoveAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Should_NotInteractWithCache_When_PostWriteKindIsUnknown() {
		// Defensive default branch of the switch: an unrecognized operation
		// kind must leave the cache untouched (no Set, no Remove).
		var cache = CreateCache<Person>();
		var keyGen = CreateKeyGenerator<Person>();
		var interceptor = new CacheInterceptor<Person, string>(cache, keyGen, p => p.Id);

		var person = CreatePerson("1");
		var context = new EntityOperationContext<Person, string>(
			(EntityOperationKind)999, person, original: null, "1",
			actor: null, DateTimeOffset.UtcNow, CancellationToken.None);

		await interceptor.PostWriteAsync(context, OperationResult.Success);

		await cache.DidNotReceive().SetAsync(Arg.Any<string[]>(), Arg.Any<Person>(), Arg.Any<CancellationToken>());
		await cache.DidNotReceive().RemoveAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Should_ReCacheEntity_When_PostWriteKindIsRestore_InvokedDirectly() {
		// Direct coverage of the Restore branch of the switch (Create/Update/Restore
		// all share the SetAsync branch).
		var cache = CreateCache<SoftDeletablePerson>();
		var keyGen = CreateKeyGenerator<SoftDeletablePerson>();
		var interceptor = new CacheInterceptor<SoftDeletablePerson, string>(cache, keyGen, p => p.Id);

		var person = CreateSoftPerson("1");
		var context = new EntityOperationContext<SoftDeletablePerson, string>(
			EntityOperationKind.Restore, person, original: person, "1",
			actor: null, DateTimeOffset.UtcNow, CancellationToken.None);

		await interceptor.PostWriteAsync(context, OperationResult.Success);

		await cache.Received().SetAsync(
			Arg.Any<string[]>(),
			Arg.Is<SoftDeletablePerson>(p => p.Id == "1"),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Should_EvictEntity_When_PostWriteKindIsHardDelete_InvokedDirectly() {
		var cache = CreateCache<Person>();
		var keyGen = CreateKeyGenerator<Person>();
		var interceptor = new CacheInterceptor<Person, string>(cache, keyGen, p => p.Id);

		var person = CreatePerson("1");
		var context = new EntityOperationContext<Person, string>(
			EntityOperationKind.HardDelete, person, original: person, "1",
			actor: null, DateTimeOffset.UtcNow, CancellationToken.None);

		await interceptor.PostWriteAsync(context, OperationResult.Success);

		await cache.Received().RemoveAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>());
		await cache.DidNotReceive().SetAsync(Arg.Any<string[]>(), Arg.Any<Person>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Should_ReCacheSoftDeletable_When_PostWriteKindIsRemove_InvokedDirectly() {
		var cache = CreateCache<SoftDeletablePerson>();
		var keyGen = CreateKeyGenerator<SoftDeletablePerson>();
		var interceptor = new CacheInterceptor<SoftDeletablePerson, string>(cache, keyGen, p => p.Id);

		var person = CreateSoftPerson("1");
		var context = new EntityOperationContext<SoftDeletablePerson, string>(
			EntityOperationKind.Remove, person, original: person, "1",
			actor: null, DateTimeOffset.UtcNow, CancellationToken.None);

		await interceptor.PostWriteAsync(context, OperationResult.Success);

		await cache.Received().SetAsync(Arg.Any<string[]>(), Arg.Any<SoftDeletablePerson>(), Arg.Any<CancellationToken>());
		await cache.DidNotReceive().RemoveAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Should_EvictNonSoftDeletable_When_PostWriteKindIsRemove_InvokedDirectly() {
		var cache = CreateCache<Person>();
		var keyGen = CreateKeyGenerator<Person>();
		var interceptor = new CacheInterceptor<Person, string>(cache, keyGen, p => p.Id);

		var person = CreatePerson("1");
		var context = new EntityOperationContext<Person, string>(
			EntityOperationKind.Remove, person, original: person, "1",
			actor: null, DateTimeOffset.UtcNow, CancellationToken.None);

		await interceptor.PostWriteAsync(context, OperationResult.Success);

		await cache.Received().RemoveAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>());
		await cache.DidNotReceive().SetAsync(Arg.Any<string[]>(), Arg.Any<Person>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Should_SkipCacheSet_When_KeyGeneratorReturnsEmptyKeys_InvokedDirectly() {
		// The interceptor's SetToCacheAsync early-returns when the key generator
		// yields an empty array of keys (the entity cannot be addressed in the cache).
		var cache = CreateCache<Person>();
		var keyGen = Substitute.For<IEntityCacheKeyGenerator<Person>>();
		keyGen.GenerateAllKeys(Arg.Any<Person>()).Returns(Array.Empty<string>());
		var interceptor = new CacheInterceptor<Person, string>(cache, keyGen, p => p.Id);

		var person = CreatePerson("1");
		var context = new EntityOperationContext<Person, string>(
			EntityOperationKind.Create, person, original: null, "1",
			actor: null, DateTimeOffset.UtcNow, CancellationToken.None);

		await interceptor.PostWriteAsync(context, OperationResult.Success);

		await cache.DidNotReceive().SetAsync(Arg.Any<string[]>(), Arg.Any<Person>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Should_LogAndContinue_When_CacheSetThrows_InvokedDirectly() {
		// Direct coverage of the catch block in SetToCacheAsync: a cache failure
		// is logged and swallowed, PostWriteAsync completes without throwing.
		var cache = CreateCache<Person>();
		cache.SetAsync(Arg.Any<string[]>(), Arg.Any<Person>(), Arg.Any<CancellationToken>())
			.Throws(new InvalidOperationException("cache down"));
		var interceptor = new CacheInterceptor<Person, string>(cache, CreateKeyGenerator<Person>(), p => p.Id);

		var person = CreatePerson("1");
		var context = new EntityOperationContext<Person, string>(
			EntityOperationKind.Create, person, original: null, "1",
			actor: null, DateTimeOffset.UtcNow, CancellationToken.None);

		await interceptor.PostWriteAsync(context, OperationResult.Success);
	}

	[Fact]
	public async Task Should_LogAndContinue_When_CacheEvictionThrows_InvokedDirectly() {
		// Direct coverage of the catch block in EvictAsync: a cache failure
		// is logged and swallowed, PostWriteAsync completes without throwing.
		var cache = CreateCache<Person>();
		cache.RemoveAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>())
			.Throws(new InvalidOperationException("cache down"));
		var interceptor = new CacheInterceptor<Person, string>(cache, CreateKeyGenerator<Person>(), p => p.Id);

		var person = CreatePerson("1");
		var context = new EntityOperationContext<Person, string>(
			EntityOperationKind.HardDelete, person, original: person, "1",
			actor: null, DateTimeOffset.UtcNow, CancellationToken.None);

		await interceptor.PostWriteAsync(context, OperationResult.Success);
	}

	[Fact]
	public void Should_Throw_When_CacheInterceptorConstructedWithNullCache() {
		// Constructor guard: the cache dependency is required (the interceptor is
		// only constructed when an IEntityCache<TEntity> is registered).
		Assert.Throws<ArgumentNullException>(() =>
			new CacheInterceptor<Person, string>(cache: null!, keyGenerator: null, p => p.Id));
	}

	[Fact]
	public void Should_Throw_When_CacheInterceptorConstructedWithNullKeyGetter() {
		Assert.Throws<ArgumentNullException>(() =>
			new CacheInterceptor<Person, string>(CreateCache<Person>(), null, getEntityKey: null!));
	}
}