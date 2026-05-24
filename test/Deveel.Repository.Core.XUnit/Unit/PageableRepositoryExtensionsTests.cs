namespace Deveel.Data;

/// <summary>
/// Tests for <see cref="PageableRepositoryExtensions"/> methods that provide
/// <see cref="IPageableRepository{TEntity, TKey}"/> pagination via
/// <see cref="PageQuery{T}"/> and page/size parameters.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Core")]
[Trait("Feature", "PageableRepository")]
public class PageableRepositoryExtensionsTests
{
	[Fact]
	public async Task GetPageAsyncWithPageAndSize_ShouldReturnPageResult()
	{
		// Arrange
		var repository = new TestPageableRepository();
		await repository.AddAsync(new Person { Id = "1", FirstName = "John" });
		await repository.AddAsync(new Person { Id = "2", FirstName = "Jane" });
		await repository.AddAsync(new Person { Id = "3", FirstName = "Bob" });

		// Act
		var result = await repository.GetPageAsync(1, 2);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(2, result.Items!.Count);
		Assert.Equal(3, result.TotalItems);
	}

	[Fact]
	public async Task GetPageAsyncWithPageAndSize_ShouldReturnSecondPage()
	{
		// Arrange
		var repository = new TestPageableRepository();
		await repository.AddAsync(new Person { Id = "1", FirstName = "John" });
		await repository.AddAsync(new Person { Id = "2", FirstName = "Jane" });
		await repository.AddAsync(new Person { Id = "3", FirstName = "Bob" });

		// Act
		var result = await repository.GetPageAsync(2, 2);

		// Assert
		Assert.NotNull(result);
		Assert.Single(result.Items!);
		Assert.Equal("Bob", result.Items![0].FirstName);
	}

	[Fact]
	public void GetPageSync_ShouldReturnPageResult()
	{
		// Arrange
		var repository = new TestPageableRepository();
		repository.AddAsync(new Person { Id = "1", FirstName = "John" }).GetAwaiter().GetResult();
		repository.AddAsync(new Person { Id = "2", FirstName = "Jane" }).GetAwaiter().GetResult();

		// Act
		var result = repository.GetPage(new PageQuery<Person>(1, 10));

		// Assert
		Assert.NotNull(result);
		Assert.Equal(2, result.Items!.Count);
	}

	/// <summary>
	/// An in-memory <see cref="IPageableRepository{Person, string}"/> backed by a <see cref="List{Person}"/>,
	/// used to test synchronous and asynchronous pagination extension methods.
	/// </summary>
	private class TestPageableRepository : IPageableRepository<Person, string>
	{
		private readonly List<Person> _entities = new();

		public ValueTask AddAsync(Person entity, CancellationToken cancellationToken = default)
		{
			_entities.Add(entity);
			return ValueTask.CompletedTask;
		}

		public ValueTask AddRangeAsync(IEnumerable<Person> entities, CancellationToken cancellationToken = default)
		{
			_entities.AddRange(entities);
			return ValueTask.CompletedTask;
		}

		public ValueTask<Person?> FindAsync(object key, CancellationToken cancellationToken = default)
		{
			var entity = _entities.FirstOrDefault(e => e.Id == key.ToString());
			return ValueTask.FromResult(entity);
		}

		public ValueTask<Person?> FindAsync(string key, CancellationToken cancellationToken = default)
		{
			var entity = _entities.FirstOrDefault(e => e.Id == key);
			return ValueTask.FromResult(entity);
		}

		public string? GetEntityKey(Person entity) => entity.Id;

		public ValueTask<bool> RemoveAsync(Person entity, CancellationToken cancellationToken = default)
		{
			var removed = _entities.Remove(entity);
			return ValueTask.FromResult(removed);
		}

		public ValueTask RemoveRangeAsync(IEnumerable<Person> entities, CancellationToken cancellationToken = default)
		{
			foreach (var entity in entities)
				_entities.Remove(entity);
			return ValueTask.CompletedTask;
		}

		public ValueTask<bool> UpdateAsync(Person entity, CancellationToken cancellationToken = default)
		{
			var index = _entities.FindIndex(e => e.Id == entity.Id);
			if (index < 0) return ValueTask.FromResult(false);
			_entities[index] = entity;
			return ValueTask.FromResult(true);
		}

		public ValueTask<PageResult<Person>> GetPageAsync(PageQuery<Person> query, CancellationToken cancellationToken = default)
		{
			var items = _entities
				.Skip((query.Page - 1) * query.Size)
				.Take(query.Size)
				.ToList();

			return ValueTask.FromResult(new PageResult<Person>(query, _entities.Count, items));
		}
	}
}
