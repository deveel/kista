namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Core")]
[Trait("Feature", "QueryBuilder")]
public class QueryBuilderExecutionTests {
	[Fact]
	public async Task FirstOrDefaultAsync_NoFilter_ReturnsFirst() {
		var repo = new TestPersonRepository(seedCount: 10);

		var result = await repo.PublicQuery()
			.FirstOrDefaultAsync();

		Assert.NotNull(result);
		Assert.Equal(repo.Universe[0].Id, result!.Id);
	}

	[Fact]
	public async Task FirstOrDefaultAsync_WithFilter_ReturnsMatching() {
		var repo = new TestPersonRepository();

		var alice = repo.Universe.First(p => p.FirstName == "Alice");

		var result = await repo.PublicQuery()
			.Where(p => p.FirstName == "Alice")
			.FirstOrDefaultAsync();

		Assert.NotNull(result);
		Assert.Equal(alice.Id, result!.Id);
	}

	[Fact]
	public async Task FirstOrDefaultAsync_NoMatch_ReturnsNull() {
		var repo = new TestPersonRepository();

		var result = await repo.PublicQuery()
			.Where(p => p.FirstName == "NonExistent")
			.FirstOrDefaultAsync();

		Assert.Null(result);
	}

	[Fact]
	public async Task ToListAsync_NoFilter_ReturnsAll() {
		var repo = new TestPersonRepository(seedCount: 10);

		var result = await repo.PublicQuery()
			.ToListAsync();

		Assert.NotNull(result);
		Assert.Equal(10, result.Count);
	}

	[Fact]
	public async Task ToListAsync_WithFilter_ReturnsFiltered() {
		var repo = new TestPersonRepository();

		var result = await repo.PublicQuery()
			.Where(p => p.FirstName == "Alice")
			.ToListAsync();

		Assert.NotNull(result);
		Assert.All(result, p => Assert.Equal("Alice", p.FirstName));
	}

	[Fact]
	public async Task ToListAsync_WithSort_ReturnsOrdered() {
		var repo = new TestPersonRepository();

		var result = await repo.PublicQuery()
			.OrderBy(p => p.LastName)
			.ToListAsync();

		Assert.NotNull(result);
		Assert.NotEmpty(result);
	}

	[Fact]
	public async Task ToListAsync_EmptyResult_ReturnsEmptyList() {
		var repo = new TestPersonRepository();

		var result = await repo.PublicQuery()
			.Where(p => p.FirstName == "NonExistent")
			.ToListAsync();

		Assert.NotNull(result);
		Assert.Empty(result);
	}

	[Fact]
	public async Task OrderBy_Ascending_ReturnsSorted() {
		var repo = new TestPersonRepository(seedCount: 20);

		var result = await repo.PublicQuery()
			.OrderBy(p => p.LastName)
			.ToListAsync();

		var expected = repo.Universe.OrderBy(p => p.LastName).Select(x => x.Id).ToList();
		Assert.Equal(expected, result.Select(x => x.Id).ToList());
	}

	[Fact]
	public async Task OrderByDescending_ReturnsSortedDescending() {
		var repo = new TestPersonRepository(seedCount: 20);

		var result = await repo.PublicQuery()
			.OrderByDescending(p => p.LastName)
			.ToListAsync();

		var expected = repo.Universe.OrderByDescending(p => p.LastName).Select(x => x.Id).ToList();
		Assert.Equal(expected, result.Select(x => x.Id).ToList());
	}

	[Fact]
	public async Task OrderBy_ThenBy_ChainsSorts() {
		var repo = new TestPersonRepository(seedCount: 20);

		var result = await repo.PublicQuery()
			.OrderBy(p => p.LastName)
			.OrderBy(p => p.FirstName)
			.ToListAsync();

		var expected = repo.Universe
			.OrderBy(p => p.LastName)
			.ThenBy(p => p.FirstName)
			.Select(x => x.Id)
			.ToList();
		Assert.Equal(expected, result.Select(x => x.Id).ToList());
	}

	[Fact]
	public async Task OrderBy_StringFieldName_SortsByField() {
		var repo = new TestPersonRepository(seedCount: 20);

		var result = await repo.PublicQuery()
			.OrderBy("LastName")
			.ToListAsync();

		var expected = repo.Universe.OrderBy(p => p.LastName).Select(x => x.Id).ToList();
		Assert.Equal(expected, result.Select(x => x.Id).ToList());
	}

	[Fact]
	public async Task OrderByDescending_StringFieldName_SortsDescending() {
		var repo = new TestPersonRepository(seedCount: 20);

		var result = await repo.PublicQuery()
			.OrderByDescending("LastName")
			.ToListAsync();

		var expected = repo.Universe.OrderByDescending(p => p.LastName).Select(x => x.Id).ToList();
		Assert.Equal(expected, result.Select(x => x.Id).ToList());
	}

	[Fact]
	public async Task OrderBy_IQueryOrder_SortsByRule() {
		var repo = new TestPersonRepository(seedCount: 20);
		var sort = QueryOrder.OrderBy<Person>(p => p.LastName);

		var result = await repo.PublicQuery()
			.OrderBy(sort)
			.ToListAsync();

		var expected = repo.Universe.OrderBy(p => p.LastName).Select(x => x.Id).ToList();
		Assert.Equal(expected, result.Select(x => x.Id).ToList());
	}

	[Fact]
	public async Task OrderBy_WithFilter_AppliesBoth() {
		var repo = new TestPersonRepository(seedCount: 20);

		var result = await repo.PublicQuery()
			.Where(p => p.FirstName == "Alice")
			.OrderBy(p => p.LastName)
			.ToListAsync();

		Assert.All(result, p => Assert.Equal("Alice", p.FirstName));
		var expected = repo.Universe
			.Where(p => p.FirstName == "Alice")
			.OrderBy(p => p.LastName)
			.Select(x => x.Id)
			.ToList();
		Assert.Equal(expected, result.Select(x => x.Id).ToList());
	}

	[Fact]
	public async Task OrderBy_FirstOrDefault_ReturnsFirstSorted() {
		var repo = new TestPersonRepository(seedCount: 20);
		var expected = repo.Universe.OrderBy(p => p.LastName).First();

		var result = await repo.PublicQuery()
			.OrderBy(p => p.LastName)
			.FirstOrDefaultAsync();

		Assert.NotNull(result);
		Assert.Equal(expected.Id, result!.Id);
	}

	[Fact]
	public async Task OrderBy_GetPage_RespectsSort() {
		var repo = new TestPersonRepository(seedCount: 20);

		var result = await repo.PublicQuery()
			.OrderBy(p => p.LastName)
			.GetPageAsync(1, 5);

		var expected = repo.Universe.OrderBy(p => p.LastName).Take(5).Select(x => x.Id).ToList();
		Assert.Equal(expected, result.Items!.Select(x => x.Id).ToList());
	}

	[Fact]
	public async Task CountAsync_NoFilter_ReturnsTotal() {
		var repo = new TestPersonRepository(seedCount: 10);

		var result = await repo.PublicQuery()
			.CountAsync();

		Assert.Equal(10, result);
	}

	[Fact]
	public async Task CountAsync_WithFilter_ReturnsFilteredCount() {
		var repo = new TestPersonRepository();

		var expected = repo.Universe.Count(p => p.FirstName == "Alice");

		var result = await repo.PublicQuery()
			.Where(p => p.FirstName == "Alice")
			.CountAsync();

		Assert.Equal(expected, result);
	}

	[Fact]
	public async Task CountAsync_AfterOrderBy_IgnoresOrder() {
		var repo = new TestPersonRepository(seedCount: 20);

		var expected = repo.Universe.Count();

		var result = await repo.PublicQuery()
			.OrderBy(p => p.LastName)
			.CountAsync();

		Assert.Equal(expected, result);
	}

	[Fact]
	public async Task AnyAsync_WithMatch_ReturnsTrue() {
		var repo = new TestPersonRepository();

		var result = await repo.PublicQuery()
			.Where(p => p.FirstName == "Alice")
			.AnyAsync();

		Assert.True(result);
	}

	[Fact]
	public async Task AnyAsync_NoMatch_ReturnsFalse() {
		var repo = new TestPersonRepository();

		var result = await repo.PublicQuery()
			.Where(p => p.FirstName == "NonExistent")
			.AnyAsync();

		Assert.False(result);
	}

	[Fact]
	public async Task AnyAsync_AfterOrderBy_IgnoresOrder() {
		var repo = new TestPersonRepository();

		var result = await repo.PublicQuery()
			.OrderBy(p => p.LastName)
			.AnyAsync();

		Assert.True(result);
	}

	[Fact]
	public async Task GetPageAsync_ReturnsPage() {
		var repo = new TestPersonRepository(seedCount: 20);

		var result = await repo.PublicQuery()
			.OrderBy(p => p.FirstName)
			.GetPageAsync(1, 5);

		Assert.NotNull(result);
		Assert.Equal(5, result.Items!.Count);
		Assert.Equal(20, result.TotalItems);
		Assert.Equal(1, result.Request.Page);
		Assert.Equal(5, result.Request.Size);
	}

	[Fact]
	public async Task GetPageAsync_LastPage_ReturnsRemaining() {
		var repo = new TestPersonRepository(seedCount: 10);

		var result = await repo.PublicQuery()
			.GetPageAsync(2, 6);

		Assert.NotNull(result);
		Assert.Equal(4, result.Items!.Count);
		Assert.Equal(10, result.TotalItems);
	}

	[Fact]
	public async Task GetPageAsync_WithFilterAndSort_RespectsBoth() {
		var repo = new TestPersonRepository(seedCount: 20);

		var result = await repo.PublicQuery()
			.Where(p => p.FirstName == "Alice")
			.OrderBy(p => p.LastName)
			.GetPageAsync(1, 5);

		Assert.All(result.Items!, p => Assert.Equal("Alice", p.FirstName));
		var expected = repo.Universe
			.Where(p => p.FirstName == "Alice")
			.OrderBy(p => p.LastName)
			.Take(5)
			.Select(x => x.Id)
			.ToList();
		Assert.Equal(expected, result.Items!.Select(x => x.Id).ToList());
	}

	[Fact]
	public async Task GetPageAsync_NoFilter_DefaultOrder_ReturnsFirstPage() {
		var repo = new TestPersonRepository(seedCount: 12);

		var result = await repo.PublicQuery()
			.GetPageAsync(1, 5);

		Assert.Equal(5, result.Items!.Count);
		Assert.Equal(12, result.TotalItems);
	}

	[Fact]
	public async Task GetPageAsync_LastPage_AfterFilter_ReturnsRemaining() {
		var repo = new TestPersonRepository(seedCount: 20);

		var result = await repo.PublicQuery()
			.Where(p => p.FirstName == "Alice")
			.GetPageAsync(2, 5);

		var allAlice = repo.Universe.Count(p => p.FirstName == "Alice");
		Assert.Equal(allAlice, result.TotalItems);
	}

	[Fact]
	public async Task GetPageAsync_OutOfRange_ReturnsEmptyPage() {
		var repo = new TestPersonRepository(seedCount: 5);

		var result = await repo.PublicQuery()
			.GetPageAsync(10, 5);

		Assert.NotNull(result.Items);
		Assert.Empty(result.Items!);
		Assert.Equal(5, result.TotalItems);
	}

	[Fact]
	public async Task StandaloneQueryBuilder_ThrowsOnExecute() {
		var builder = new QueryBuilder<Person>();

		await Assert.ThrowsAsync<InvalidOperationException>(() => builder.FirstOrDefaultAsync().AsTask());
		await Assert.ThrowsAsync<InvalidOperationException>(() => builder.ToListAsync().AsTask());
		await Assert.ThrowsAsync<InvalidOperationException>(() => builder.CountAsync().AsTask());
		await Assert.ThrowsAsync<InvalidOperationException>(() => builder.AnyAsync().AsTask());
		await Assert.ThrowsAsync<InvalidOperationException>(() => builder.GetPageAsync(1, 10).AsTask());
	}

	[Fact]
	public void StandaloneQueryBuilder_Query_IsExposed() {
		var builder = new QueryBuilder<Person>()
			.Where(p => p.FirstName == "Alice")
			.OrderBy(p => p.LastName);

		var query = builder.Query;
		Assert.NotNull(query);
		Assert.NotNull(query.Filter);
		Assert.NotNull(query.Order);
	}

	[Fact]
	public void StandaloneQueryBuilder_Query_IsImmutable() {
		var builder = new QueryBuilder<Person>()
			.Where(p => p.FirstName == "Alice");

		var snapshot = builder.Query;

		builder.Where(p => p.LastName == "Smith");

		Assert.NotNull(snapshot.Filter);
		Assert.Null(snapshot.Order);
	}
}
