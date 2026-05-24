using Microsoft.Extensions.DependencyInjection;

namespace Deveel.Data;

/// <summary>
/// Tests for the <see cref="IQueryFilter.Initialize(IFilterContext)"/> contract,
/// covering default no-op behavior, custom filter resolution, and
/// <see cref="CombinedQueryFilter"/> propagation to child filters.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Core")]
[Trait("Feature", "QueryFilter")]
public class QueryFilterInitializeTests {
	#region IQueryFilter.Initialize default behavior

	[Fact]
	public void Initialize_DoesNothing_ByDefault() {
		var filter = QueryFilter.Empty;

		var context = new DefaultFilterContext(new ServiceCollection().BuildServiceProvider());

		var ex = Record.Exception(() => filter.Initialize(context));

		Assert.Null(ex);
	}

	#endregion

	#region Custom filter implementing Initialize

	[Fact]
	public void Initialize_CalledBeforeApply_WhenFilterNeedsContext() {
		var services = new ServiceCollection();
		services.AddSingleton<IMyCache>(new MyCache());
		var provider = services.BuildServiceProvider();

		var filter = new CacheResolvingFilter();
		var context = new DefaultFilterContext(provider);

		filter.Initialize(context);

		Assert.NotNull(filter.ResolvedCache);
		Assert.IsType<MyCache>(filter.ResolvedCache);
	}

	[Fact]
	public void Initialize_CanBeCalledMultipleTimes_Safely() {
		var services = new ServiceCollection();
		services.AddSingleton<IMyCache>(new MyCache());
		var provider = services.BuildServiceProvider();

		var filter = new CacheResolvingFilter();
		var context = new DefaultFilterContext(provider);

		filter.Initialize(context);
		filter.Initialize(context);

		Assert.NotNull(filter.ResolvedCache);
	}

	#endregion

	#region CombinedQueryFilter.Initialize propagation

	[Fact]
	public void CombinedQueryFilter_Initialize_PropagatesToAllChildren() {
		var services = new ServiceCollection();
		services.AddSingleton<IMyCache>(new MyCache());
		var provider = services.BuildServiceProvider();

		var filter1 = new CacheResolvingFilter();
		var filter2 = new CacheResolvingFilter();
		var filter3 = new CacheResolvingFilter();
		var combined = QueryFilter.Combine(filter1, filter2, filter3);

		var context = new DefaultFilterContext(provider);
		combined.Initialize(context);

		Assert.NotNull(filter1.ResolvedCache);
		Assert.NotNull(filter2.ResolvedCache);
		Assert.NotNull(filter3.ResolvedCache);
	}

	[Fact]
	public void CombinedQueryFilter_Initialize_PropagatesToNestedCombined() {
		var services = new ServiceCollection();
		services.AddSingleton<IMyCache>(new MyCache());
		var provider = services.BuildServiceProvider();

		var inner1 = new CacheResolvingFilter();
		var inner2 = new CacheResolvingFilter();
		var innerCombined = QueryFilter.Combine(inner1, inner2);

		var outer = new CacheResolvingFilter();
		var combined = QueryFilter.Combine(innerCombined, outer);

		var context = new DefaultFilterContext(provider);
		combined.Initialize(context);

		Assert.NotNull(inner1.ResolvedCache);
		Assert.NotNull(inner2.ResolvedCache);
		Assert.NotNull(outer.ResolvedCache);
	}

	[Fact]
	public void CombinedQueryFilter_Initialize_SkipsEmptyFilters() {
		var services = new ServiceCollection();
		services.AddSingleton<IMyCache>(new MyCache());
		var provider = services.BuildServiceProvider();

		var filter = new CacheResolvingFilter();
		var combined = QueryFilter.Combine(filter, QueryFilter.Empty);

		var context = new DefaultFilterContext(provider);
		combined.Initialize(context);

		Assert.NotNull(filter.ResolvedCache);
	}

	#endregion

	#region ExpressionQueryFilter (no-op Initialize)

	[Fact]
	public void ExpressionQueryFilter_Initialize_DoesNotThrow() {
		IQueryFilter filter = new ExpressionQueryFilter<Person>(x => x.FirstName == "John");
		var context = new DefaultFilterContext(new ServiceCollection().BuildServiceProvider());

		var ex = Record.Exception(() => filter.Initialize(context));

		Assert.Null(ex);
	}

	#endregion

	// Test helpers

	/// <summary>
	/// A marker interface used to verify that <see cref="IQueryFilter.Initialize(IFilterContext)"/>
	/// can resolve custom services from the <see cref="IFilterContext.Services"/> provider.
	/// </summary>
	private interface IMyCache { }

	/// <summary>
	/// Default implementation of <see cref="IMyCache"/> for filter initialization tests.
	/// </summary>
	private class MyCache : IMyCache { }

	/// <summary>
	/// A custom <see cref="IQueryFilter"/> that resolves an <see cref="IMyCache"/>
	/// service from the filter context during <see cref="Initialize"/>.
	/// Used to verify that <see cref="CombinedQueryFilter"/> propagates
	/// initialization to all children.
	/// </summary>
	private class CacheResolvingFilter : IQueryFilter {
		public IMyCache? ResolvedCache { get; private set; }

		public void Initialize(IFilterContext context) {
			ResolvedCache = context.Services.GetService(typeof(IMyCache)) as IMyCache;
		}
	}
}
