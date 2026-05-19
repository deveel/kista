using Microsoft.Extensions.DependencyInjection;

namespace Deveel.Data;

[Trait("Category", "Unit")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "DynamicLinq")]
public class DynamicLinqFilterInitializeTests {
	#region Constructor-provided cache takes precedence

	[Fact]
	public void Initialize_DoesNotOverride_ConstructorCache() {
		var constructorCache = new BoundedExpressionCache(100);
		var filter = new DynamicLinqFilter("x.FirstName == \"John\"", constructorCache);

		var services = new ServiceCollection();
		services.AddSingleton<IExpressionCache>(new DifferentCache());
		var provider = services.BuildServiceProvider();

		var context = new DefaultFilterContext(provider);
		filter.Initialize(context);

		Assert.Same(constructorCache, filter.Cache);
	}

	[Fact]
	public void Initialize_WithNullConstructorCache_ResolvesFromContext() {
		var resolvedCache = new BoundedExpressionCache(100);
		var filter = new DynamicLinqFilter("x.FirstName == \"John\"");

		var services = new ServiceCollection();
		services.AddSingleton<IExpressionCache>(resolvedCache);
		var provider = services.BuildServiceProvider();

		var context = new DefaultFilterContext(provider);
		filter.Initialize(context);

		Assert.Same(resolvedCache, filter.Cache);
	}

	#endregion

	#region No cache in context

	[Fact]
	public void Initialize_WithNoRegisteredCache_LeavesCacheNull() {
		var filter = new DynamicLinqFilter("x.FirstName == \"John\"");

		var services = new ServiceCollection();
		var provider = services.BuildServiceProvider();

		var context = new DefaultFilterContext(provider);
		filter.Initialize(context);

		Assert.Null(filter.Cache);
	}

	[Fact]
	public void Initialize_WithNullContextServices_DoesNotThrow() {
		var filter = new DynamicLinqFilter("x.FirstName == \"John\"");

		var ex = Record.Exception(() => filter.Initialize(new NullServicesFilterContext()));

		Assert.Null(ex);
		Assert.Null(filter.Cache);
	}

	#endregion

	#region Multiple Initialize calls

	[Fact]
	public void Initialize_CalledMultipleTimes_UsesFirstResolvedCache() {
		var cache1 = new BoundedExpressionCache(100);
		var cache2 = new BoundedExpressionCache(200);
		var filter = new DynamicLinqFilter("x.FirstName == \"John\"");

		var services1 = new ServiceCollection();
		services1.AddSingleton<IExpressionCache>(cache1);
		var provider1 = services1.BuildServiceProvider();

		var services2 = new ServiceCollection();
		services2.AddSingleton<IExpressionCache>(cache2);
		var provider2 = services2.BuildServiceProvider();

		filter.Initialize(new DefaultFilterContext(provider1));
		filter.Initialize(new DefaultFilterContext(provider2));

		Assert.Same(cache1, filter.Cache);
	}

	#endregion

	#region Functional: cached expression is reused after Initialize

	[Fact]
	public void AsLambda_UsesResolvedCache_AfterInitialize() {
		var cache = new BoundedExpressionCache(100);
		var filter = new DynamicLinqFilter("x.FirstName == \"John\"");

		var services = new ServiceCollection();
		services.AddSingleton<IExpressionCache>(cache);
		var provider = services.BuildServiceProvider();

		var context = new DefaultFilterContext(provider);
		filter.Initialize(context);

		// First call - cache miss, then stored
		var lambda1 = filter.AsLambda<Person>();
		Assert.Equal(0, cache.Statistics.Hits);

		// Second call - cache hit
		var lambda2 = filter.AsLambda<Person>();
		Assert.Equal(1, cache.Statistics.Hits);

		Assert.NotNull(lambda1);
		Assert.NotNull(lambda2);
	}

	#endregion

	// Test helpers

	private class DifferentCache : IExpressionCache {
		public bool TryGet(string key, out System.Linq.Expressions.LambdaExpression? expression) { expression = null; return false; }
		public void Set(string key, System.Linq.Expressions.LambdaExpression expression) { }
		public void Clear() { }
		public IFilterCacheStatistics? Statistics => null;
	}

	private class NullServicesFilterContext : IFilterContext {
		public IServiceProvider Services => new NullServiceProvider();

		private class NullServiceProvider : IServiceProvider {
			public object? GetService(Type serviceType) => null;
		}
	}
}
