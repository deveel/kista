namespace Kista;

[Trait("Category", "Integration")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "InMemoryRepository")]
public class InMemoryRepositoryFilterInitializationTests {
	private readonly List<Person> _persons;

	public InMemoryRepositoryFilterInitializationTests() {
		_persons = new List<Person> {
			new() { Id = "1", FirstName = "John", LastName = "Doe", Email = "john@example.com" },
			new() { Id = "2", FirstName = "Jane", LastName = "Doe", Email = "jane@example.com" },
			new() { Id = "3", FirstName = "John", LastName = "Smith", Email = "john.smith@example.com" },
		};
	}

	#region Services property

	[Fact]
	public void Services_IsNull_WhenNoServiceProviderProvided() {
		var repo = new InMemoryRepository<Person>(_persons);

		Assert.Null(repo.Services);
	}

	[Fact]
	public void Services_ExposesProvider_WhenServiceProviderProvided() {
		var services = new ServiceCollection().BuildServiceProvider();
		var repo = new InMemoryRepository<Person>(_persons, services: services);

		Assert.Same(services, repo.Services);
	}

	#endregion

	#region DynamicLinqFilter auto-resolution

	[Fact]
	public async Task CountAsync_UsesResolvedCache_WhenServiceProviderProvided() {
		var cache = new BoundedExpressionCache(100);
		var services = new ServiceCollection();
		services.AddSingleton<IExpressionCache>(cache);
		var provider = services.BuildServiceProvider();

		var repo = new InMemoryRepository<Person>(_persons, services: provider);
		var filter = new DynamicLinqFilter("x.FirstName == \"John\"");

		var count1 = await repo.CountAsync(filter);
		var count2 = await repo.CountAsync(filter);

		Assert.Equal(2, count1);
		Assert.Equal(2, count2);
		Assert.Equal(1, cache.Statistics.Hits);
	}

	[Fact]
	public async Task ExistsAsync_UsesResolvedCache_WhenServiceProviderProvided() {
		var cache = new BoundedExpressionCache(100);
		var services = new ServiceCollection();
		services.AddSingleton<IExpressionCache>(cache);
		var provider = services.BuildServiceProvider();

		var repo = new InMemoryRepository<Person>(_persons, services: provider);
		var filter = new DynamicLinqFilter("x.FirstName == \"Jane\"");

		var exists1 = await repo.ExistsAsync(filter);
		var exists2 = await repo.ExistsAsync(filter);

		Assert.True(exists1);
		Assert.True(exists2);
		Assert.Equal(1, cache.Statistics.Hits);
	}

	[Fact]
	public async Task FindFirstAsync_UsesResolvedCache_WhenServiceProviderProvided() {
		var cache = new BoundedExpressionCache(100);
		var services = new ServiceCollection();
		services.AddSingleton<IExpressionCache>(cache);
		var provider = services.BuildServiceProvider();

		var repo = new InMemoryRepository<Person>(_persons, services: provider);
		var filter = new DynamicLinqFilter("x.LastName == \"Smith\"");

		var result1 = await repo.FindFirstAsync(new Query(filter));
		var result2 = await repo.FindFirstAsync(new Query(filter));

		Assert.NotNull(result1);
		Assert.Equal("John", result1.FirstName);
		Assert.Equal(1, cache.Statistics.Hits);
	}

	[Fact]
	public async Task FindAllAsync_UsesResolvedCache_WhenServiceProviderProvided() {
		var cache = new BoundedExpressionCache(100);
		var services = new ServiceCollection();
		services.AddSingleton<IExpressionCache>(cache);
		var provider = services.BuildServiceProvider();

		var repo = new InMemoryRepository<Person>(_persons, services: provider);
		var filter = new DynamicLinqFilter("x.LastName == \"Doe\"");

		var results1 = await repo.FindAllAsync(new Query(filter));
		var results2 = await repo.FindAllAsync(new Query(filter));

		Assert.Equal(2, results1.Count);
		Assert.Equal(2, results2.Count);
		Assert.Equal(1, cache.Statistics.Hits);
	}

	[Fact]
	public async Task GetPageAsync_UsesResolvedCache_WhenServiceProviderProvided() {
		var cache = new BoundedExpressionCache(100);
		var services = new ServiceCollection();
		services.AddSingleton<IExpressionCache>(cache);
		var provider = services.BuildServiceProvider();

		var repo = new InMemoryRepository<Person>(_persons, services: provider);
		var filter = new DynamicLinqFilter("x.FirstName == \"John\"");
		var pageQuery = new PageQuery<Person>(1, 10) {
			Query = new Query(filter)
		};

		var result1 = await repo.GetPageAsync(pageQuery);
		var result2 = await repo.GetPageAsync(pageQuery);

		Assert.Equal(2, result1.TotalItems);
		Assert.Equal(2, result2.TotalItems);
		Assert.Equal(1, cache.Statistics.Hits);
	}

	#endregion

	#region No service provider - no cache resolution

	[Fact]
	public async Task CountAsync_DoesNotCache_WhenNoServiceProvider() {
		var repo = new InMemoryRepository<Person>(_persons);
		var filter = new DynamicLinqFilter("x.FirstName == \"John\"");

		var count1 = await repo.CountAsync(filter);
		var count2 = await repo.CountAsync(filter);

		Assert.Equal(2, count1);
		Assert.Equal(2, count2);
		Assert.Null(filter.Cache);
	}

	[Fact]
	public async Task ExistsAsync_DoesNotCache_WhenNoServiceProvider() {
		var repo = new InMemoryRepository<Person>(_persons);
		var filter = new DynamicLinqFilter("x.FirstName == \"Jane\"");

		var exists1 = await repo.ExistsAsync(filter);
		var exists2 = await repo.ExistsAsync(filter);

		Assert.True(exists1);
		Assert.True(exists2);
		Assert.Null(filter.Cache);
	}

	#endregion

	#region Constructor-provided cache takes precedence over DI

	[Fact]
	public async Task CountAsync_UsesConstructorCache_OverResolvedCache() {
		var constructorCache = new BoundedExpressionCache(100);
		var diCache = new BoundedExpressionCache(200);

		var services = new ServiceCollection();
		services.AddSingleton<IExpressionCache>(diCache);
		var provider = services.BuildServiceProvider();

		var repo = new InMemoryRepository<Person>(_persons, services: provider);
		var filter = new DynamicLinqFilter("x.FirstName == \"John\"", constructorCache);

		await repo.CountAsync(filter);
		await repo.CountAsync(filter);

		Assert.Same(constructorCache, filter.Cache);
		Assert.Equal(1, constructorCache.Statistics.Hits);
		Assert.Equal(0, diCache.Statistics.Hits);
	}

	#endregion

	#region CombinedQueryFilter propagation through repository

	[Fact]
	public async Task CountAsync_InitializesAllChildren_WhenCombinedFilterUsed() {
		var cache = new BoundedExpressionCache(100);
		var services = new ServiceCollection();
		services.AddSingleton<IExpressionCache>(cache);
		var provider = services.BuildServiceProvider();

		var repo = new InMemoryRepository<Person>(_persons, services: provider);

		var filter1 = new DynamicLinqFilter("x.FirstName == \"John\"");
		var filter2 = new DynamicLinqFilter("x.LastName == \"Doe\"");
		var combined = QueryFilter.Combine(filter1, filter2);

		var count = await repo.CountAsync(combined);

		Assert.Equal(1, count);
		Assert.NotNull(filter1.Cache);
		Assert.NotNull(filter2.Cache);
	}

	#endregion

	#region ExpressionQueryFilter (no cache needed)

	[Fact]
	public async Task CountAsync_WorksWithExpressionFilter_WhenServiceProviderProvided() {
		var services = new ServiceCollection();
		services.AddSingleton<IExpressionCache>(new BoundedExpressionCache(100));
		var provider = services.BuildServiceProvider();

		var repo = new InMemoryRepository<Person>(_persons, services: provider);
		var filter = new ExpressionQueryFilter<Person>(x => x.FirstName == "John");

		var count = await repo.CountAsync(filter);

		Assert.Equal(2, count);
	}

	#endregion
}
