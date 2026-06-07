using System.Linq.Expressions;
using System.Reflection;

namespace Kista;

/// <summary>
/// Tests for the abstract <see cref="Repository{TEntity,TKey}"/> base
/// class. The base class owns the unpacking, sorting, filtering and pagination
/// of <see cref="IQuery"/> and <see cref="PageQuery{TEntity}"/> instances: these
/// tests assert the pipeline end-to-end through the protected hatch by using a
/// small in-memory subclass.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Core")]
[Trait("Feature", "Repository")]
public class RepositoryTests {
	#region FindAsync(IQuery)

	[Fact]
	public async Task FindAsync_EmptyQuery_ReturnsAll() {
		var sut = new TestRepository();

		var result = await sut.PublicFindAsync(Query.Empty);

		Assert.Equal(sut.Universe.Count, result.Count);
	}

	[Fact]
	public async Task FindAsync_AppliesFilter() {
		var sut = new TestRepository();
		var aliceCount = sut.Universe.Count(p => p.FirstName == "Alice");

		var query = Query.Where<Person>(p => p.FirstName == "Alice");
		var result = await sut.PublicFindAsync(query);

		Assert.Equal(aliceCount, result.Count);
		Assert.All(result, p => Assert.Equal("Alice", p.FirstName));
	}

	[Fact]
	public async Task FindAsync_AppliesOrder() {
		var sut = new TestRepository();
		var sorted = sut.Universe.OrderBy(p => p.LastName).ToList();

		var query = new Query(
			QueryFilter.Empty,
			new ExpressionSort<Person>(p => p.LastName));
		var result = await sut.PublicFindAsync(query);

		Assert.Equal(sorted.Count, result.Count);
		for (var i = 0; i < sorted.Count; i++) {
			Assert.Equal(sorted[i].Id, result[i].Id);
		}
	}

	[Fact]
	public async Task FindAsync_NullQuery_Throws() {
		var sut = new TestRepository();

		await Assert.ThrowsAsync<ArgumentNullException>(
			() => sut.PublicFindAsync(null!).AsTask());
	}

	[Fact]
	public async Task FindAsync_CancelledToken_Throws() {
		var sut = new TestRepository();
		using var cts = new CancellationTokenSource();
		cts.Cancel();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(
			() => sut.PublicFindAsync(Query.Empty, cts.Token).AsTask());
	}

	#endregion

	#region QueryPageAsync(PageQuery<TEntity>)

	[Fact]
	public async Task GetPageAsync_FirstPage_ReturnsSlice() {
		var sut = new TestRepository(seedCount: 25);

		var result = await sut.PublicQueryPageAsync(new PageQuery<Person>(1, 10));

		Assert.Equal(25, result.TotalItems);
		Assert.Equal(3, result.TotalPages);
		Assert.Equal(10, result.Items!.Count);
	}

	[Fact]
	public async Task GetPageAsync_SecondPage_Advances() {
		var sut = new TestRepository(seedCount: 25);

		var result = await sut.PublicQueryPageAsync(new PageQuery<Person>(2, 10));

		Assert.Equal(10, result.Items!.Count);
	}

	[Fact]
	public async Task GetPageAsync_TotalCount_IndependentOfPage() {
		var sut = new TestRepository(seedCount: 25);

		var page1 = await sut.PublicQueryPageAsync(new PageQuery<Person>(1, 5));
		var page3 = await sut.PublicQueryPageAsync(new PageQuery<Person>(3, 5));

		Assert.Equal(25, page1.TotalItems);
		Assert.Equal(25, page3.TotalItems);
	}

	[Fact]
	public async Task GetPageAsync_Filter_NarrowsTotalAndItems() {
		var sut = new TestRepository(seedCount: 25);
		var aliceCount = sut.Universe.Count(p => p.FirstName == "Alice");

		var query = new PageQuery<Person>(1, 10)
			.Where(p => p.FirstName == "Alice");

		var result = await sut.PublicQueryPageAsync(query);

		Assert.Equal(aliceCount, result.TotalItems);
		Assert.All(result.Items!, p => Assert.Equal("Alice", p.FirstName));
	}

	[Fact]
	public async Task GetPageAsync_Order_AppliedBeforeSkip() {
		var sut = new TestRepository(seedCount: 20);
		var expected = sut.Universe.OrderBy(p => p.LastName).Skip(5).Take(5).ToList();

		var query = new PageQuery<Person>(2, 5)
			.OrderBy(p => p.LastName);

		var result = await sut.PublicQueryPageAsync(query);

		for (var i = 0; i < expected.Count; i++) {
			Assert.Equal(expected[i].Id, result.Items![i].Id);
		}
	}

	[Fact]
	public async Task GetPageAsync_NullRequest_Throws() {
		var sut = new TestRepository();

		await Assert.ThrowsAsync<ArgumentNullException>(
			() => sut.PublicQueryPageAsync(null!).AsTask());
	}

	[Fact]
	public async Task GetPageAsync_CancelledToken_Throws() {
		var sut = new TestRepository();
		using var cts = new CancellationTokenSource();
		cts.Cancel();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(
			() => sut.PublicQueryPageAsync(new PageQuery<Person>(1, 10), cts.Token).AsTask());
	}

	#endregion

	#region Engine hooks

	[Fact]
	public async Task ToListAsync_DefaultHook_MaterialisesList() {
		var sut = new TestRepository();

		var result = await sut.PublicFindAsync(Query.Empty);

		Assert.IsType<List<Person>>(result);
	}

	[Fact]
	public async Task CountAsync_DefaultHook_ReturnsUniverseSize() {
		var sut = new TestRepository(seedCount: 12);

		var result = await sut.PublicQueryPageAsync(new PageQuery<Person>(1, 5));

		Assert.Equal(12, result.TotalItems);
	}

	#endregion

	#region ExistsAsync

	[Fact]
	public async Task ExistsAsync_FilterMatch_ReturnsTrue() {
		var sut = new TestRepository();

		var result = await sut.PublicExistsAsync(new ExpressionQueryFilter<Person>(p => p.FirstName == "Alice"));

		Assert.True(result);
	}

	[Fact]
	public async Task ExistsAsync_FilterNoMatch_ReturnsFalse() {
		var sut = new TestRepository();

		var result = await sut.PublicExistsAsync(new ExpressionQueryFilter<Person>(p => p.FirstName == "NonExistent"));

		Assert.False(result);
	}

	[Fact]
	public async Task ExistsAsync_NullFilter_ReturnsAny() {
		var sut = new TestRepository();

		var result = await sut.PublicExistsAsync((IQueryFilter?)null);

		Assert.True(result);
	}

	[Fact]
	public async Task ExistsAsync_PredicateMatch_ReturnsTrue() {
		var sut = new TestRepository();

		var result = await sut.PublicExistsAsync((Expression<Func<Person, bool>>)(p => p.FirstName == "Alice"));

		Assert.True(result);
	}

	[Fact]
	public async Task ExistsAsync_PredicateNoMatch_ReturnsFalse() {
		var sut = new TestRepository();

		var result = await sut.PublicExistsAsync((Expression<Func<Person, bool>>)(p => p.FirstName == "NonExistent"));

		Assert.False(result);
	}

	#endregion

	#region CountAsync

	[Fact]
	public async Task CountAsync_FilterMatch_ReturnsCount() {
		var sut = new TestRepository(seedCount: 10);
		var expected = sut.Universe.Count(p => p.FirstName == "Alice");

		var result = await sut.PublicCountAsync(new ExpressionQueryFilter<Person>(p => p.FirstName == "Alice"));

		Assert.Equal(expected, result);
	}

	[Fact]
	public async Task CountAsync_NullFilter_ReturnsTotal() {
		var sut = new TestRepository(seedCount: 10);

		var result = await sut.PublicCountAsync((IQueryFilter?)null);

		Assert.Equal(10, result);
	}

	[Fact]
	public async Task CountAsync_Predicate_ReturnsMatchCount() {
		var sut = new TestRepository(seedCount: 10);
		var expected = sut.Universe.Count(p => p.FirstName == "Alice");

		var result = await sut.PublicCountAsync((Expression<Func<Person, bool>>)(p => p.FirstName == "Alice"));

		Assert.Equal(expected, result);
	}

	#endregion

	#region FindFirstAsync

	[Fact]
	public async Task FindFirstAsync_Match_ReturnsEntity() {
		var sut = new TestRepository();

		var result = await sut.PublicFindFirstAsync(Query.Where<Person>(p => p.FirstName == "Alice"));

		Assert.NotNull(result);
		Assert.Equal("Alice", result!.FirstName);
	}

	[Fact]
	public async Task FindFirstAsync_NoMatch_ReturnsNull() {
		var sut = new TestRepository();

		var result = await sut.PublicFindFirstAsync(Query.Where<Person>(p => p.FirstName == "NonExistent"));

		Assert.Null(result);
	}

	[Fact]
	public async Task FindFirstAsync_PredicateMatch_ReturnsEntity() {
		var sut = new TestRepository();

		var result = await sut.PublicFindFirstAsync((Expression<Func<Person, bool>>)(p => p.FirstName == "Alice"));

		Assert.NotNull(result);
		Assert.Equal("Alice", result!.FirstName);
	}

	[Fact]
	public async Task FindFirstAsync_PredicateNoMatch_ReturnsNull() {
		var sut = new TestRepository();

		var result = await sut.PublicFindFirstAsync((Expression<Func<Person, bool>>)(p => p.FirstName == "NonExistent"));

		Assert.Null(result);
	}

	#endregion

	#region FindAllAsync

	[Fact]
	public async Task FindAllAsync_Match_ReturnsEntities() {
		var sut = new TestRepository();
		var expected = sut.Universe.Count(p => p.FirstName == "Alice");

		var result = await sut.PublicFindAllAsync(Query.Where<Person>(p => p.FirstName == "Alice"));

		Assert.Equal(expected, result.Count);
		Assert.All(result, p => Assert.Equal("Alice", p.FirstName));
	}

	[Fact]
	public async Task FindAllAsync_NoMatch_ReturnsEmpty() {
		var sut = new TestRepository();

		var result = await sut.PublicFindAllAsync(Query.Where<Person>(p => p.FirstName == "NonExistent"));

		Assert.Empty(result);
	}

	[Fact]
	public async Task FindAllAsync_PredicateMatch_ReturnsEntities() {
		var sut = new TestRepository();
		var expected = sut.Universe.Count(p => p.FirstName == "Alice");

		var result = await sut.PublicFindAllAsync((Expression<Func<Person, bool>>)(p => p.FirstName == "Alice"));

		Assert.Equal(expected, result.Count);
	}

	[Fact]
	public async Task FindAllAsync_PredicateNoMatch_ReturnsEmpty() {
		var sut = new TestRepository();

		var result = await sut.PublicFindAllAsync((Expression<Func<Person, bool>>)(p => p.FirstName == "NonExistent"));

		Assert.Empty(result);
	}

	#endregion

	#region GetPageAsync(PageRequest)

	[Fact]
	public async Task GetPageAsync_WithPageRequest_ReturnsSlice() {
		var sut = new TestRepository(seedCount: 25);

		var result = await sut.PublicGetPageAsync(new PageRequest(2, 10));

		Assert.Equal(25, result.TotalItems);
		Assert.Equal(10, result.Items!.Count);
	}

	[Fact]
	public async Task GetPageAsync_PageRequest_Null_Throws() {
		var sut = new TestRepository();

		await Assert.ThrowsAsync<ArgumentNullException>(
			() => sut.PublicGetPageAsync(null!).AsTask());
	}

	[Fact]
	public async Task GetPageAsync_PageRequest_Cancelled_Throws() {
		var sut = new TestRepository();
		using var cts = new CancellationTokenSource();
		cts.Cancel();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(
			() => sut.PublicGetPageAsync(new PageRequest(1, 10), cts.Token).AsTask());
	}

	[Fact]
	public async Task GetPageAsync_WithPageQuery_DelegatesToQueryPage() {
		var sut = new TestRepository(seedCount: 25);

		var result = await sut.PublicGetPageAsync(new PageQuery<Person>(1, 10));

		Assert.Equal(25, result.TotalItems);
	}

	#endregion

	#region NotQueryable

	[Fact]
	public async Task ExistsAsync_NotQueryable_ThrowsNotSupported() {
		var sut = new NonQueryableRepository();

		await Assert.ThrowsAsync<NotSupportedException>(
			() => sut.PublicExistsAsync(new ExpressionQueryFilter<Person>(p => p.FirstName == "Alice")).AsTask());
	}

	[Fact]
	public async Task CountAsync_NotQueryable_ThrowsNotSupported() {
		var sut = new NonQueryableRepository();

		await Assert.ThrowsAsync<NotSupportedException>(
			() => sut.PublicCountAsync(new ExpressionQueryFilter<Person>(p => p.FirstName == "Alice")).AsTask());
	}

	[Fact]
	public async Task FindFirstAsync_NotQueryable_ThrowsNotSupported() {
		var sut = new NonQueryableRepository();

		await Assert.ThrowsAsync<NotSupportedException>(
			() => sut.PublicFindFirstAsync(Query.Where<Person>(p => p.FirstName == "Alice")).AsTask());
	}

	[Fact]
	public async Task FindAllAsync_NotQueryable_ThrowsNotSupported() {
		var sut = new NonQueryableRepository();

		await Assert.ThrowsAsync<NotSupportedException>(
			() => sut.PublicFindAllAsync(Query.Where<Person>(p => p.FirstName == "Alice")).AsTask());
	}

	#endregion

	#region IFilterableRepository explicit interface

	[Fact]
	public async Task FilterableInterface_ExistsAsync_Delegates() {
		IFilterableRepository<Person, string> sut = new TestRepository();

		var result = await sut.ExistsAsync(new ExpressionQueryFilter<Person>(p => p.FirstName == "Alice"));

		Assert.True(result);
	}

	[Fact]
	public async Task FilterableInterface_CountAsync_Delegates() {
		IFilterableRepository<Person, string> sut = new TestRepository(seedCount: 10);

		var result = await sut.CountAsync(new ExpressionQueryFilter<Person>(p => p.FirstName == "Alice"));

		Assert.True(result > 0);
	}

	[Fact]
	public async Task FilterableInterface_FindFirstAsync_Delegates() {
		IFilterableRepository<Person, string> sut = new TestRepository();

		var result = await sut.FindFirstAsync(Query.Where<Person>(p => p.FirstName == "Alice"));

		Assert.NotNull(result);
	}

	[Fact]
	public async Task FilterableInterface_FindAllAsync_Delegates() {
		IFilterableRepository<Person, string> sut = new TestRepository();

		var result = await sut.FindAllAsync(Query.Where<Person>(p => p.FirstName == "Alice"));

		Assert.NotEmpty(result);
	}

	#endregion

	#region IRepository explicit interface

	[Fact]
	public void RepositoryInterface_Services_ReturnsNull() {
		IRepository<Person, string> sut = new TestRepository();

		Assert.Null(sut.Services);
	}

	[Fact]
	public void RepositoryInterface_GetEntityKey_ReturnsId() {
		IRepository<Person, string> sut = new TestRepository();
		var person = new PersonFaker().Generate();

		var key = sut.GetEntityKey(person);

		Assert.Equal(person.Id, key);
	}

	#endregion

	#region Negative coverage — Query() hatch is protected

	[Fact]
	public void QueryHatch_IsProtected() {
		// The Query() declared on Repository<,> must be protected so the
		// IQueryable hatch is hidden from consumer code.
		var declared = typeof(Repository<Person, string>)
			.GetMethod("Query", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
		Assert.NotNull(declared);
		Assert.False(declared!.IsPublic, "Query() hatch must not be public");
		Assert.False(declared.IsPrivate, "Query() hatch must not be private");
		Assert.True(declared.IsFamily || declared.IsFamilyOrAssembly || declared.IsFamilyAndAssembly,
			"Query() hatch must be protected (family) — at least one of: IsFamily, IsFamilyOrAssembly, IsFamilyAndAssembly");
	}

	#endregion

	#region Test fixture

	/// <summary>
	/// A minimal <see cref="Repository{TEntity,TKey}"/> subclass that
	/// exposes the protected <c>FindAsync(IQuery)</c> and
	/// <c>QueryPageAsync(PageQuery{TEntity})</c> methods as public passthroughs
	/// so the test class can exercise them.
	/// </summary>
	private sealed class TestRepository : Repository<Person, string> {
		private readonly List<Person> _people;

		public TestRepository(int seedCount = 20) {
			var faker = new PersonFaker();
			_people = faker.Generate(Math.Max(1, seedCount));
			// Pin a deterministic Alice so filter assertions are easy.
			_people[0].FirstName = "Alice";
		}

		/// <summary>The seed list backing the in-memory entity set.</summary>
		public IReadOnlyList<Person> Universe => _people;

		/// <inheritdoc />
		protected override IServiceProvider? Services => null;

		/// <inheritdoc />
		protected override string? GetEntityKey(Person entity) => entity.Id;

		/// <inheritdoc />
		protected override IQueryable<Person> Query() => _people.AsQueryable();

		/// <inheritdoc />
		protected override bool IsQueryable => true;

		/// <summary>Public passthrough used by tests to invoke the protected
		/// <c>FindAsync(IQuery, CancellationToken)</c> entry point.</summary>
		public ValueTask<IList<Person>> PublicFindAsync(IQuery query, CancellationToken cancellationToken = default)
			=> FindAsync(query, cancellationToken);

		/// <summary>Public passthrough used by tests to invoke the protected
		/// <c>QueryPageAsync(PageQuery{TEntity}, CancellationToken)</c> entry
		/// point.</summary>
		public ValueTask<PageQueryResult<Person>> PublicQueryPageAsync(PageQuery<Person> request, CancellationToken cancellationToken = default)
			=> QueryPageAsync(request, cancellationToken);

		public ValueTask<bool> PublicExistsAsync(IQueryFilter? filter, CancellationToken cancellationToken = default)
			=> ExistsAsync(filter, cancellationToken);

		public ValueTask<bool> PublicExistsAsync(Expression<Func<Person, bool>> predicate, CancellationToken cancellationToken = default)
			=> ExistsAsync(predicate, cancellationToken);

		public ValueTask<long> PublicCountAsync(IQueryFilter? filter, CancellationToken cancellationToken = default)
			=> CountAsync(filter, cancellationToken);

		public ValueTask<long> PublicCountAsync(Expression<Func<Person, bool>> predicate, CancellationToken cancellationToken = default)
			=> CountAsync(predicate, cancellationToken);

		public ValueTask<Person?> PublicFindFirstAsync(IQuery query, CancellationToken cancellationToken = default)
			=> FindFirstAsync(query, cancellationToken);

		public ValueTask<Person?> PublicFindFirstAsync(Expression<Func<Person, bool>> predicate, CancellationToken cancellationToken = default)
			=> FindFirstAsync(predicate, cancellationToken);

		public ValueTask<IList<Person>> PublicFindAllAsync(IQuery query, CancellationToken cancellationToken = default)
			=> FindAllAsync(query, cancellationToken);

		public ValueTask<IList<Person>> PublicFindAllAsync(Expression<Func<Person, bool>> predicate, CancellationToken cancellationToken = default)
			=> FindAllAsync(predicate, cancellationToken);

		public ValueTask<PageResult<Person>> PublicGetPageAsync(PageRequest request, CancellationToken cancellationToken = default)
			=> GetPageAsync(request, cancellationToken);

		/// <inheritdoc />
		public override ValueTask AddAsync(Person entity, CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(entity);
			_people.Add(entity);
			return ValueTask.CompletedTask;
		}

		/// <inheritdoc />
		public override ValueTask AddRangeAsync(IEnumerable<Person> entities, CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(entities);
			_people.AddRange(entities);
			return ValueTask.CompletedTask;
		}

		/// <inheritdoc />
		public override ValueTask<bool> UpdateAsync(Person entity, CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(entity);
			var idx = _people.FindIndex(p => p.Id == entity.Id);
			if (idx < 0) return ValueTask.FromResult(false);
			_people[idx] = entity;
			return ValueTask.FromResult(true);
		}

		/// <inheritdoc />
		public override ValueTask<bool> RemoveAsync(Person entity, CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(entity);
			return ValueTask.FromResult(_people.Remove(entity));
		}

		/// <inheritdoc />
		public override ValueTask RemoveRangeAsync(IEnumerable<Person> entities, CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(entities);
			foreach (var e in entities.ToList())
				_people.Remove(e);
			return ValueTask.CompletedTask;
		}

		/// <inheritdoc />
		public override ValueTask<Person?> FindAsync(string key, CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(key);
			return ValueTask.FromResult(_people.FirstOrDefault(p => p.Id == key));
		}
	}

	private sealed class NonQueryableRepository : Repository<Person, string> {
		protected override IServiceProvider? Services => null;
		protected override string? GetEntityKey(Person entity) => entity.Id;
		protected override IQueryable<Person> Query() => throw new NotSupportedException();

		public ValueTask<bool> PublicExistsAsync(IQueryFilter filter, CancellationToken cancellationToken = default)
			=> ExistsAsync(filter, cancellationToken);

		public ValueTask<long> PublicCountAsync(IQueryFilter filter, CancellationToken cancellationToken = default)
			=> CountAsync(filter, cancellationToken);

		public ValueTask<Person?> PublicFindFirstAsync(IQuery query, CancellationToken cancellationToken = default)
			=> FindFirstAsync(query, cancellationToken);

		public ValueTask<IList<Person>> PublicFindAllAsync(IQuery query, CancellationToken cancellationToken = default)
			=> FindAllAsync(query, cancellationToken);

		public override ValueTask AddAsync(Person entity, CancellationToken cancellationToken = default) => throw new NotSupportedException();
		public override ValueTask AddRangeAsync(IEnumerable<Person> entities, CancellationToken cancellationToken = default) => throw new NotSupportedException();
		public override ValueTask<bool> UpdateAsync(Person entity, CancellationToken cancellationToken = default) => throw new NotSupportedException();
		public override ValueTask<bool> RemoveAsync(Person entity, CancellationToken cancellationToken = default) => throw new NotSupportedException();
		public override ValueTask RemoveRangeAsync(IEnumerable<Person> entities, CancellationToken cancellationToken = default) => throw new NotSupportedException();
		public override ValueTask<Person?> FindAsync(string key, CancellationToken cancellationToken = default) => throw new NotSupportedException();
	}

	#endregion
}
