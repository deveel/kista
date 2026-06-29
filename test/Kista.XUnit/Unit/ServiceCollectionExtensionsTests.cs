using Microsoft.Extensions.DependencyInjection;

namespace Kista;

/// <summary>
/// Tests for the <c>AddSystemTime</c> extension methods on <see cref="IServiceCollection"/>,
/// verifying registration of default, typed, and instance-based <see cref="ISystemTime"/> services.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Core")]
[Trait("Feature", "DependencyInjection")]
public class ServiceCollectionExtensionsTests
{
	[Fact]
	public void AddSystemTime_WithDefault_ShouldRegisterSystemTime()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddSystemTime();

		// Act
		var provider = services.BuildServiceProvider();
		var systemTime = provider.GetService<ISystemTime>();

		// Assert
		Assert.NotNull(systemTime);
		Assert.IsType<SystemTime>(systemTime);
	}

	[Fact]
	public void AddSystemTime_WithInstance_ShouldRegisterGivenInstance()
	{
		// Arrange
		var services = new ServiceCollection();
		var testTime = new TestTime();
		services.AddSystemTime(testTime);

		// Act
		var provider = services.BuildServiceProvider();
		var systemTime = provider.GetService<ISystemTime>();

		// Assert
		Assert.Same(testTime, systemTime);
	}

	[Fact]
	public void AddSystemTime_WithType_ShouldRegisterSpecifiedType()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddSystemTime<TestTime>();

		// Act
		var provider = services.BuildServiceProvider();
		var systemTime = provider.GetService<ISystemTime>();

		// Assert
		Assert.NotNull(systemTime);
		Assert.IsType<TestTime>(systemTime);
	}

	[Fact]
	public void AddSystemTime_WithInstanceAndType_ShouldRegisterGivenInstance()
	{
		// Arrange
		var services = new ServiceCollection();
		var testTime = new TestTime();
		services.AddSystemTime<TestTime>(testTime);

		// Act
		var provider = services.BuildServiceProvider();
		var systemTime = provider.GetService<ISystemTime>();

		// Assert
		Assert.Same(testTime, systemTime);
	}

	[Fact]
	public void ServiceCollectionExtensions_AddRepositoryController_Obsolete_StillWorks() {
		var services = new ServiceCollection();
		services.AddRepositoryController<TestRepositoryController>();
		var provider = services.BuildServiceProvider();
		Assert.NotNull(provider.GetService<IRepositoryController>());
	}

	/// <summary>
	/// A stub implementation of <see cref="ISystemTime"/> that delegates to the real system clock,
	/// used to verify custom <see cref="ISystemTime"/> registration overloads.
	/// </summary>
	private sealed class TestTime : ISystemTime
	{
		public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
		public DateTimeOffset Now => DateTimeOffset.Now;
	}

	[Fact]
	public void AddRepositoryLifecycleOrchestrator_Default_RegistersService() {
		var services = new ServiceCollection();

		services.AddRepositoryLifecycleOrchestrator();

		var provider = services.BuildServiceProvider();
		Assert.NotNull(provider.GetService<IRepositoryLifecycleService>());
	}

	[Fact]
	public void AddRepositoryLifecycleOrchestrator_WithConfigure_AppliesOptions() {
		var services = new ServiceCollection();
		services.AddRepositoryLifecycleOrchestrator(o => o.DeleteIfExists = false);

		var provider = services.BuildServiceProvider();
		var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<RepositoryLifecycleOptions>>();
		Assert.False(options.Value.DeleteIfExists);
	}

	[Fact]
	public void WithLifecycleHandler_Type_RegistersHandler() {
		var services = new ServiceCollection();
		var builder = new RepositoryContextBuilder(services);

		builder.WithLifecycleHandler<Person, TestLifecycleHandler>();

		var provider = services.BuildServiceProvider();
		Assert.NotNull(provider.GetService<IRepositoryLifecycleHandler<Person>>());
	}

	[Fact]
	public void WithLifecycleHandler_Factory_RegistersHandler() {
		var services = new ServiceCollection();
		var builder = new RepositoryContextBuilder(services);

		builder.WithLifecycleHandler<Person, TestLifecycleHandler>(sp => new TestLifecycleHandler());

		var provider = services.BuildServiceProvider();
		Assert.NotNull(provider.GetService<IRepositoryLifecycleHandler<Person>>());
	}

	[Fact]
	public void WithLifecycleHandler_Instance_RegistersSingleton() {
		var services = new ServiceCollection();
		var builder = new RepositoryContextBuilder(services);
		var handler = new TestLifecycleHandler();

		builder.WithLifecycleHandler<Person>(handler);

		var provider = services.BuildServiceProvider();
		Assert.Same(handler, provider.GetService<IRepositoryLifecycleHandler<Person>>());
	}

	[Fact]
	public void WithLifecycleProfile_Type_RegistersProfile() {
		var services = new ServiceCollection();
		var builder = new RepositoryContextBuilder(services);

		builder.WithLifecycleProfile<TestLifecycleProfile>();

		var provider = services.BuildServiceProvider();
		Assert.NotNull(provider.GetService<IRepositoryLifecycleProfile>());
	}

	[Fact]
	public void WithLifecycleProfile_Instance_RegistersSingleton() {
		var services = new ServiceCollection();
		var builder = new RepositoryContextBuilder(services);
		var profile = new TestLifecycleProfile();

		builder.WithLifecycleProfile(profile);

		var provider = services.BuildServiceProvider();
		Assert.Same(profile, provider.GetService<IRepositoryLifecycleProfile>());
	}

	[Fact]
	public void ConfigureLifecycle_WithAction_RegistersOrchestratorAndProfile() {
		var services = new ServiceCollection();
		var builder = new RepositoryContextBuilder(services);

		builder.ConfigureLifecycle(o => o.DeleteIfExists = false);

		var provider = services.BuildServiceProvider();
		Assert.NotNull(provider.GetService<IRepositoryLifecycleService>());
		Assert.NotNull(provider.GetService<IRepositoryLifecycleProfile>());
	}

	[Fact]
	public void ConfigureLifecycle_Default_RegistersOrchestratorAndProfile() {
		var services = new ServiceCollection();
		var builder = new RepositoryContextBuilder(services);

		builder.ConfigureLifecycle();

		var provider = services.BuildServiceProvider();
		Assert.NotNull(provider.GetService<IRepositoryLifecycleService>());
		Assert.NotNull(provider.GetService<IRepositoryLifecycleProfile>());
	}

	private sealed class TestLifecycleHandler : IRepositoryLifecycleHandler<Person> {
		public ValueTask<bool> ExistsAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(true);
		public ValueTask CreateAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask DropAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask SeedAsync(object? seedData = null, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
	}

	private sealed class TestLifecycleProfile : IRepositoryLifecycleProfile {
		public bool AutoCreate => true;
		public bool AutoDrop => false;
		public SeedStrategy GetSeedStrategy(string? environmentName = null) => SeedStrategy.Never;
		public object? GetSeedData(Type entityType) => null;
	}

	private sealed class TestRepositoryController : IRepositoryController {
		public ValueTask CreateRepositoryAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class => ValueTask.CompletedTask;
		public ValueTask CreateRepositoryAsync<TEntity, TKey>(CancellationToken cancellationToken = default) where TEntity : class => ValueTask.CompletedTask;
		public ValueTask DropRepositoryAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class => ValueTask.CompletedTask;
		public ValueTask DropRepositoryAsync<TEntity, TKey>(CancellationToken cancellationToken = default) where TEntity : class => ValueTask.CompletedTask;
	}
}
