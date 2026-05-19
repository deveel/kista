using Microsoft.Extensions.DependencyInjection;

namespace Deveel.Data;

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

	private interface IMyService { }
	private class MyService : IMyService { }
}
