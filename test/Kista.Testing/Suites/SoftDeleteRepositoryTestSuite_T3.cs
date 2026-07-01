using System.Linq.Expressions;

using Bogus;

using Microsoft.Extensions.DependencyInjection;

namespace Kista;

/// <summary>
/// An abstract test suite that verifies the soft-delete behaviour of a
/// repository driver, for entities implementing both
/// <see cref="IPerson{TKey}"/> and <see cref="ISoftDeletable"/>.
/// </summary>
public abstract class SoftDeleteRepositoryTestSuite<TPerson, TKey, TRelationship> : RepositoryTestSuite<TPerson, TKey, TRelationship>
	where TPerson : class, IPerson<TKey>, ISoftDeletable
	where TKey : notnull
	where TRelationship : class, IRelationship {

	protected SoftDeleteRepositoryTestSuite(ITestOutputHelper? testOutput) : base(testOutput) {
	}

	/// <summary>
	/// Gets the repository cast to <see cref="Repository{TEntity, TKey}"/>
	/// for access to the query pipeline, or <c>null</c> if the repository
	/// is not a <see cref="Repository{TEntity, TKey}"/>.
	/// </summary>
	protected Repository<TPerson, TKey>? RepositoryBase => Repository as Repository<TPerson, TKey>;

	private IQueryOptions IncludeDeletedOptions => QueryOptions.WithSoftDeleteMode(SoftDeleteMode.IncludeDeleted);
	private IQueryOptions OnlyDeletedOptions => QueryOptions.WithSoftDeleteMode(SoftDeleteMode.OnlyDeleted);

	private IQuery KeyQuery(TKey key) =>
		new Query(QueryFilter.Where<TPerson>(p => p.Id!.Equals(key)));

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

		var results = RepositoryBase!.Queryable()
			.Where(p => p.Id!.Equals(personId))
			.ToList();

		Assert.Empty(results);
	}

	[Fact]
	public async Task Should_ExcludeDeleted_FromCount() {
		var person = await RandomPersonAsync();

		var countBefore = RepositoryBase!.Queryable().Count();

		await Repository.RemoveAsync(person, TestContext.Current.CancellationToken);

		var countAfter = RepositoryBase!.Queryable().Count();

		Assert.Equal(countBefore - 1, countAfter);
	}

	[Fact]
	public async Task Should_IncludeDeleted_When_ModeSet() {
		var person = await RandomPersonAsync();
		var personId = Repository.GetEntityKey(person)!;

		await Repository.RemoveAsync(person, TestContext.Current.CancellationToken);

		var found = await RepositoryBase!.FindFirstAsync(KeyQuery(personId), IncludeDeletedOptions, TestContext.Current.CancellationToken);

		Assert.NotNull(found);
		Assert.True(found!.IsDeleted);
	}

	[Fact]
	public async Task Should_OnlyReturnDeleted_When_OnlyDeletedMode() {
		var person = await RandomPersonAsync();
		var personId = Repository.GetEntityKey(person)!;

		await Repository.RemoveAsync(person, TestContext.Current.CancellationToken);

		var found = await RepositoryBase!.FindFirstAsync(KeyQuery(personId), OnlyDeletedOptions, TestContext.Current.CancellationToken);

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

		var found = await RepositoryBase!.FindFirstAsync(KeyQuery(personId), IncludeDeletedOptions, TestContext.Current.CancellationToken);
		Assert.Null(found);
	}

	[Fact]
	public async Task Should_HardDelete_When_AlreadySoftDeleted() {
		var person = await RandomPersonAsync();
		var personId = Repository.GetEntityKey(person)!;

		await Repository.RemoveAsync(person, TestContext.Current.CancellationToken);

		var hardDeleted = await Repository.HardDeleteAsync(person, TestContext.Current.CancellationToken);
		Assert.True(hardDeleted);

		var found = await RepositoryBase!.FindFirstAsync(KeyQuery(personId), IncludeDeletedOptions, TestContext.Current.CancellationToken);
		Assert.Null(found);
	}

	[Fact]
	public async Task Should_StampDeletedAtUtc_When_SoftDeleted() {
		var person = await RandomPersonAsync();
		var personId = Repository.GetEntityKey(person)!;

		await Repository.RemoveAsync(person, TestContext.Current.CancellationToken);

		var found = await RepositoryBase!.FindFirstAsync(KeyQuery(personId), OnlyDeletedOptions, TestContext.Current.CancellationToken);

		Assert.NotNull(found);
		Assert.True(found!.IsDeleted);
		Assert.NotNull(found.DeletedAtUtc);
	}

	[Fact]
	public async Task Should_HardDeleteRange_When_EntitiesSoftDeleted() {
		var persons = await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => RandomPersonAsync()));

		await Repository.HardDeleteRangeAsync(persons, TestContext.Current.CancellationToken);

		foreach (var person in persons) {
			var personId = Repository.GetEntityKey(person)!;
			var found = await RepositoryBase!.FindFirstAsync(KeyQuery(personId), IncludeDeletedOptions, TestContext.Current.CancellationToken);
			Assert.Null(found);
		}
	}
}