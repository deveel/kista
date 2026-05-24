using Microsoft.Extensions.DependencyInjection;

namespace Deveel.Data;

/// <summary>
/// Tests for <see cref="DefaultFilterContext"/> and the <see cref="IFilterContext"/> contract,
/// verifying service provider exposure and null-guard behavior.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Core")]
[Trait("Feature", "FilterContext")]
public class FilterContextTests {
	#region DefaultFilterContext

	[Fact]
	public void Constructor_WithNullServices_Throws() {
		Assert.Throws<ArgumentNullException>(() => new DefaultFilterContext(null!));
	}

	[Fact]
	public void Constructor_WithServices_ExposesSameProvider() {
		var services = new ServiceCollection().BuildServiceProvider();

		var context = new DefaultFilterContext(services);

		Assert.Same(services, context.Services);
	}

	[Fact]
	public void Services_CanResolveRegisteredServices() {
		var services = new ServiceCollection();
		services.AddSingleton<IMyService, MyService>();
		var provider = services.BuildServiceProvider();

		var context = new DefaultFilterContext(provider);
		var resolved = context.Services.GetService<IMyService>();

		Assert.NotNull(resolved);
		Assert.IsType<MyService>(resolved);
	}

	[Fact]
	public void Services_ReturnsNullForUnregisteredService() {
		var services = new ServiceCollection().BuildServiceProvider();
		var context = new DefaultFilterContext(services);

		var resolved = context.Services.GetService<IMyService>();

		Assert.Null(resolved);
	}

	#endregion

	/// <summary>
	/// A marker interface used to verify service resolution through
	/// <see cref="DefaultFilterContext.Services"/>.
	/// </summary>
	private interface IMyService { }

	/// <summary>
	/// Default implementation of <see cref="IMyService"/> for filter context tests.
	/// </summary>
	private class MyService : IMyService { }
}
