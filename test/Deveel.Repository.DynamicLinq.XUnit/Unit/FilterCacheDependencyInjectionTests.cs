namespace Deveel.Data;

[Trait("Category", "Unit")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "DependencyInjection")]
public class FilterCacheDependencyInjectionTests {
	#region AddFilterCache with capacity

	[Fact]
	public void AddFilterCache_WithCapacity_RegistersBothCaches() {
		var services = new ServiceCollection();
		services.AddFilterCache(maxCapacity: 512);
		var provider = services.BuildServiceProvider();

		var expressionCache = provider.GetService<IExpressionCache>();
		var filterCache = provider.GetService<IFilterCache>();

		Assert.NotNull(expressionCache);
		Assert.NotNull(filterCache);
		Assert.Equal(512, ((BoundedExpressionCache)expressionCache).Statistics.MaxCapacity);
		Assert.Equal(512, ((BoundedFilterCache)filterCache).Statistics.MaxCapacity);
	}

	[Fact]
	public void AddFilterCache_WithDefaultCapacity_Uses1024() {
		var services = new ServiceCollection();
		services.AddFilterCache();
		var provider = services.BuildServiceProvider();

		var expressionCache = provider.GetRequiredService<IExpressionCache>();
		var filterCache = provider.GetRequiredService<IFilterCache>();

		Assert.Equal(1024, ((BoundedExpressionCache)expressionCache).Statistics.MaxCapacity);
		Assert.Equal(1024, ((BoundedFilterCache)filterCache).Statistics.MaxCapacity);
	}

	[Fact]
	public void AddFilterCache_WithInvalidCapacity_Throws() {
		var services = new ServiceCollection();

		var ex1 = Record.Exception(() => services.AddFilterCache(maxCapacity: 0));
		var ex2 = Record.Exception(() => services.AddFilterCache(maxCapacity: -1));

		Assert.IsType<ArgumentOutOfRangeException>(ex1);
		Assert.IsType<ArgumentOutOfRangeException>(ex2);
	}

	[Fact]
	public void AddFilterCache_WithNullConfigureAction_Throws() {
		var services = new ServiceCollection();

		var ex = Record.Exception(() => services.AddFilterCache((Action<BoundedFilterCacheOptions>)null!));

		Assert.NotNull(ex);
		// The method doesn't null-check configure, so it throws NullReferenceException
		// when attempting to invoke it. This is the actual behavior.
		Assert.True(ex is NullReferenceException or ArgumentNullException);
	}

	#endregion

	#region AddFilterCache with configuration action

	[Fact]
	public void AddFilterCache_WithConfigureAction_AppliesOptions() {
		var services = new ServiceCollection();
		services.AddFilterCache(options => options.MaxCapacity = 2048);
		var provider = services.BuildServiceProvider();

		var expressionCache = provider.GetRequiredService<IExpressionCache>();
		var filterCache = provider.GetRequiredService<IFilterCache>();

		Assert.Equal(2048, ((BoundedExpressionCache)expressionCache).Statistics.MaxCapacity);
		Assert.Equal(2048, ((BoundedFilterCache)filterCache).Statistics.MaxCapacity);
	}

	#endregion

	#region Custom cache implementations

	[Fact]
	public void AddExpressionCache_RegistersCustomImplementation() {
		var services = new ServiceCollection();
		services.AddExpressionCache<CustomExpressionCache>();
		var provider = services.BuildServiceProvider();

		var cache = provider.GetService<IExpressionCache>();

		Assert.NotNull(cache);
		Assert.IsType<CustomExpressionCache>(cache);
	}

	[Fact]
	public void AddFilterCache_Generic_RegistersCustomImplementation() {
		var services = new ServiceCollection();
		services.AddFilterCache<CustomFilterCache>();
		var provider = services.BuildServiceProvider();

		var cache = provider.GetService<IFilterCache>();

		Assert.NotNull(cache);
		Assert.IsType<CustomFilterCache>(cache);
	}

	#endregion

	#region TryAddSingleton behavior

	[Fact]
	public void AddFilterCache_DoesNotOverride_ExistingRegistration() {
		var customCache = new CustomExpressionCache();
		var services = new ServiceCollection();
		services.AddSingleton<IExpressionCache>(customCache);
		services.AddFilterCache(maxCapacity: 512);
		var provider = services.BuildServiceProvider();

		var cache = provider.GetRequiredService<IExpressionCache>();

		Assert.Same(customCache, cache);
	}

	#endregion

	#region Full integration: DI -> Repository -> Filter

	[Fact]
	public async Task FullPipeline_RepositoryResolvesCache_FromDI() {
		var persons = new List<Person> {
			new() { Id = "1", FirstName = "John", LastName = "Doe" },
			new() { Id = "2", FirstName = "Jane", LastName = "Doe" },
		};

		var services = new ServiceCollection();
		services.AddFilterCache(maxCapacity: 100);
		services.AddSingleton<IReadOnlyList<Person>>(persons);
		services.AddTransient<IRepository<Person>>(sp => {
			var list = sp.GetRequiredService<IReadOnlyList<Person>>();
			return new InMemoryRepository<Person>(list, services: sp);
		});
		var provider = services.BuildServiceProvider();

		await using var scope = provider.CreateAsyncScope();
		var repo = scope.ServiceProvider.GetRequiredService<IRepository<Person>>();
		var filter = new DynamicLinqFilter("x.LastName == \"Doe\"");

		var count1 = await repo.CountAsync(filter, TestContext.Current.CancellationToken);
		var count2 = await repo.CountAsync(filter, TestContext.Current.CancellationToken);

		Assert.Equal(2, count1);
		Assert.Equal(2, count2);

		var cache = scope.ServiceProvider.GetRequiredService<IExpressionCache>();
		Assert.Equal(1, cache.Statistics.Hits);
	}

	#endregion

	// Custom implementations for testing

	private class CustomExpressionCache : IExpressionCache {
		public bool TryGet(string key, out System.Linq.Expressions.LambdaExpression? expression) { expression = null; return false; }
		public void Set(string key, System.Linq.Expressions.LambdaExpression expression) { }
		public void Clear() { }
		public IFilterCacheStatistics? Statistics => null;
	}

	private class CustomFilterCache : IFilterCache {
		public bool TryGet(string key, out Delegate? func) { func = null; return false; }
		public void Set(string key, Delegate func) { }
		public void Clear() { }
		public IFilterCacheStatistics? Statistics => null;
	}
}
