using Microsoft.Extensions.DependencyInjection;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Core")]
[Trait("Feature", "Infrastructure")]
public class RepositoryDriverBuilderTests {
	[Fact]
	public void WithLifecycle_EnablesLifecycle() {
		var services = new ServiceCollection();
		var contextBuilder = new RepositoryContextBuilder(services);
		var sut = new TestDriverBuilder(contextBuilder, false);

		sut.WithLifecycle<TestDriverBuilder>();

		Assert.True(sut.EnableLifecyclePublic);
	}

	[Fact]
	public void WithoutLifecycle_DisablesLifecycle() {
		var services = new ServiceCollection();
		var contextBuilder = new RepositoryContextBuilder(services);
		var sut = new TestDriverBuilder(contextBuilder, true);

		sut.WithoutLifecycle<TestDriverBuilder>();

		Assert.False(sut.EnableLifecyclePublic);
	}

	[Fact]
	public void Constructor_NullParent_Throws() {
		Assert.Throws<ArgumentNullException>(
			() => new TestDriverBuilder(null!, true));
	}

	[Fact]
	public void Services_ReturnsParentServices() {
		var services = new ServiceCollection();
		var contextBuilder = new RepositoryContextBuilder(services);
		var sut = new TestDriverBuilder(contextBuilder, true);

		Assert.Same(services, sut.Services);
	}

	[Fact]
	public void WithLifecycle_ReturnsSelf() {
		var services = new ServiceCollection();
		var contextBuilder = new RepositoryContextBuilder(services);
		var sut = new TestDriverBuilder(contextBuilder, false);

		var result = sut.WithLifecycle<TestDriverBuilder>();

		Assert.Same(sut, result);
	}

	[Fact]
	public void WithoutLifecycle_ReturnsSelf() {
		var services = new ServiceCollection();
		var contextBuilder = new RepositoryContextBuilder(services);
		var sut = new TestDriverBuilder(contextBuilder, true);

		var result = sut.WithoutLifecycle<TestDriverBuilder>();

		Assert.Same(sut, result);
	}

	[Fact]
	public void RegisterLifecycleHandler_Enabled_RegistersHandler() {
		var services = new ServiceCollection();
		var contextBuilder = new RepositoryContextBuilder(services);
		var sut = new TestDriverBuilder(contextBuilder, true);

		sut.CallRegisterLifecycleHandler(typeof(TestLifecycleHandler));

		var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IRepositoryLifecycleHandler<>));
		Assert.NotNull(descriptor);
		Assert.Equal(typeof(TestLifecycleHandler), descriptor!.ImplementationType);
	}

	[Fact]
	public void RegisterLifecycleHandler_Disabled_DoesNotRegister() {
		var services = new ServiceCollection();
		var contextBuilder = new RepositoryContextBuilder(services);
		var sut = new TestDriverBuilder(contextBuilder, false);

		sut.CallRegisterLifecycleHandler(typeof(TestLifecycleHandler));

		var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IRepositoryLifecycleHandler<>));
		Assert.Null(descriptor);
	}

	private sealed class TestDriverBuilder : RepositoryDriverBuilder {
		public TestDriverBuilder(RepositoryContextBuilder parent, bool enableLifecycle)
			: base(parent, enableLifecycle) {
		}

		public bool EnableLifecyclePublic => EnableLifecycle;

		public void CallRegisterLifecycleHandler(Type handlerType)
			=> RegisterLifecycleHandler(handlerType);
	}

	private sealed class TestLifecycleHandler : RepositoryLifecycleHandler<Person> {
		public override ValueTask<bool> ExistsAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(true);
		public override ValueTask CreateAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public override ValueTask DropAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		protected override ValueTask SeedEntitiesAsync(IEnumerable<Person> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
	}
}
