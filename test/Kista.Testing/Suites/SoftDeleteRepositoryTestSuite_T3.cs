using System.Linq.Expressions;

using Bogus;

using Microsoft.Extensions.DependencyInjection;

namespace Kista;

/// <summary>
/// An abstract test suite that verifies the soft-delete behaviour of a
/// repository driver, for entities implementing both
/// <see cref="IPerson{TKey}"/> and <see cref="ISoftDeletable"/>.
/// </summary>
public abstract class SoftDeleteRepositoryTestSuite<TPerson, TKey> : RepositoryTestSuite<TPerson, TKey>
	where TPerson : class, IPerson<TKey>, ISoftDeletable
	where TKey : notnull {

	protected SoftDeleteRepositoryTestSuite(ITestOutputHelper? testOutput) : base(testOutput) {
	}

	private static IQueryOptions IncludeDeletedOptions => QueryOptions.WithSoftDeleteMode(SoftDeleteMode.IncludeDeleted);
	private static IQueryOptions OnlyDeletedOptions => QueryOptions.WithSoftDeleteMode(SoftDeleteMode.OnlyDeleted);

	private ITestRepository<TPerson, TKey> TestRepo => (ITestRepository<TPerson, TKey>)Repository;

	private static IQuery KeyQuery(TKey key) =>
		new Query(QueryFilter.Where<TPerson>(p => p.Id!.Equals(key)));

	private static IQuery IncludeDeletedQuery(TKey key) =>
		new Query(KeyQuery(key).Filter ?? QueryFilter.Empty, null, IncludeDeletedOptions);

	private static IQuery OnlyDeletedQuery(TKey key) =>
		new Query(KeyQuery(key).Filter ?? QueryFilter.Empty, null, OnlyDeletedOptions);

	[Fact]
	public async Task Should_SoftDelete_When_EntityIsSoftDeletable() {
		var person = await RandomPersonAsync();
		var personId = Repository.GetEntityKey(person)!;

		var removed = await Repository.RemoveAsync(person, TestContext.Current.CancellationToken);
		Assert.True(removed);

		var found = await Repository.FindAsync(personId, TestContext.Current.CancellationToken);
		Assert.Null(found);
	}

	[Fact]
	public async Task Should_ExcludeDeleted_FromFindAll_Default() {
		var person = await RandomPersonAsync();
		var personId = Repository.GetEntityKey(person)!;

		await Repository.RemoveAsync(person, TestContext.Current.CancellationToken);

		var results = await TestRepo.FindAllAsync(KeyQuery(personId), TestContext.Current.CancellationToken);

		Assert.Empty(results);
	}

	[Fact]
	public async Task Should_ExcludeDeleted_FromCount() {
		var person = await RandomPersonAsync();

		var countBefore = await TestRepo.CountAsync(QueryFilter.Empty, TestContext.Current.CancellationToken);

		await Repository.RemoveAsync(person, TestContext.Current.CancellationToken);

		var countAfter = await TestRepo.CountAsync(QueryFilter.Empty, TestContext.Current.CancellationToken);

		Assert.Equal(countBefore - 1, countAfter);
	}

	[Fact]
	public async Task Should_IncludeDeleted_When_ModeSet() {
		var person = await RandomPersonAsync();
		var personId = Repository.GetEntityKey(person)!;

		await Repository.RemoveAsync(person, TestContext.Current.CancellationToken);

		var found = await TestRepo.FindFirstAsync(IncludeDeletedQuery(personId), TestContext.Current.CancellationToken);

		Assert.NotNull(found);
		Assert.True(found!.IsDeleted);
	}

	[Fact]
	public async Task Should_OnlyReturnDeleted_When_OnlyDeletedMode() {
		var person = await RandomPersonAsync();
		var personId = Repository.GetEntityKey(person)!;

		await Repository.RemoveAsync(person, TestContext.Current.CancellationToken);

		var found = await TestRepo.FindFirstAsync(OnlyDeletedQuery(personId), TestContext.Current.CancellationToken);

		Assert.NotNull(found);
		Assert.True(found!.IsDeleted);
		Assert.Equal(personId, Repository.GetEntityKey(found));
	}

	[Fact]
	public async Task Should_ReturnFalse_When_RemovingAlreadySoftDeleted() {
		var person = await RandomPersonAsync();

		await Repository.RemoveAsync(person, TestContext.Current.CancellationToken);

		var secondRemove = await Repository.RemoveAsync(person, TestContext.Current.CancellationToken);
		Assert.False(secondRemove);
	}

	[Fact]
	public async Task Should_HardDelete_When_EntityIsSoftDeletable() {
		var person = await RandomPersonAsync();
		var personId = Repository.GetEntityKey(person)!;

		var deleted = await Repository.HardDeleteAsync(person, TestContext.Current.CancellationToken);
		Assert.True(deleted);

		var found = await TestRepo.FindFirstAsync(IncludeDeletedQuery(personId), TestContext.Current.CancellationToken);
		Assert.Null(found);
	}

	[Fact]
	public async Task Should_HardDelete_When_AlreadySoftDeleted() {
		var person = await RandomPersonAsync();
		var personId = Repository.GetEntityKey(person)!;

		await Repository.RemoveAsync(person, TestContext.Current.CancellationToken);

		var hardDeleted = await Repository.HardDeleteAsync(person, TestContext.Current.CancellationToken);
		Assert.True(hardDeleted);

		var found = await TestRepo.FindFirstAsync(IncludeDeletedQuery(personId), TestContext.Current.CancellationToken);
		Assert.Null(found);
	}

	[Fact]
	public async Task Should_StampDeletedAtUtc_When_SoftDeleted() {
		var person = await RandomPersonAsync();
		var personId = Repository.GetEntityKey(person)!;

		await Repository.RemoveAsync(person, TestContext.Current.CancellationToken);

		var found = await TestRepo.FindFirstAsync(OnlyDeletedQuery(personId), TestContext.Current.CancellationToken);

		Assert.NotNull(found);
		Assert.True(found!.IsDeleted);
		Assert.NotNull(found.DeletedAtUtc);
	}

	[Fact]
	public async Task Should_HardDeleteRange_When_EntitiesSoftDeleted() {
		var persons = await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => RandomPersonAsync()));

		await Repository.HardDeleteRangeAsync(persons, TestContext.Current.CancellationToken);

		foreach (var personId in persons.Select(p => Repository.GetEntityKey(p)!)) {
			var found = await TestRepo.FindFirstAsync(IncludeDeletedQuery(personId), TestContext.Current.CancellationToken);
			Assert.Null(found);
		}
	}

	[Fact]
	public async Task Should_SoftDeleteRange_StampsAllEntities() {
		var persons = await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => RandomPersonAsync()));

		// Pre-set DeletedBy on the entities to verify the driver preserves
		// a caller-provided audit attribution through the range soft-delete path.
		foreach (var person in persons)
			person.DeletedBy = "caller-actor";

		await Repository.RemoveRangeAsync(persons, TestContext.Current.CancellationToken);

		foreach (var person in persons) {
			var personId = Repository.GetEntityKey(person)!;

			Assert.True(person.IsDeleted);
			Assert.NotNull(person.DeletedAtUtc);
			Assert.Equal("caller-actor", person.DeletedBy);

			var found = await TestRepo.FindFirstAsync(OnlyDeletedQuery(personId), TestContext.Current.CancellationToken);
			Assert.NotNull(found);
			Assert.True(found!.IsDeleted);
			Assert.NotNull(found.DeletedAtUtc);
			Assert.Equal("caller-actor", found.DeletedBy);
		}
	}
}