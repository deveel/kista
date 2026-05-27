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

	[Fact]
	public void ServiceCollectionExtensions_AddRepositoryController_Default_Obsolete_StillWorks() {
		var services = new ServiceCollection();
		services.AddRepositoryController();
		var provider = services.BuildServiceProvider();
		Assert.NotNull(provider.GetService<IRepositoryController>());
	}

	/// <summary>
	/// A stub implementation of <see cref="ISystemTime"/> that delegates to the real system clock,
	/// used to verify custom <see cref="ISystemTime"/> registration overloads.
	/// </summary>
	private class TestTime : ISystemTime
	{
		public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
		public DateTimeOffset Now => DateTimeOffset.Now;
	}

	private class TestRepositoryController : IRepositoryController {
		public ValueTask CreateRepositoryAsync<TEntity>(CancellationToken ct = default) where TEntity : class => ValueTask.CompletedTask;
		public ValueTask CreateRepositoryAsync<TEntity, TKey>(CancellationToken ct = default) where TEntity : class => ValueTask.CompletedTask;
		public ValueTask DropRepositoryAsync<TEntity>(CancellationToken ct = default) where TEntity : class => ValueTask.CompletedTask;
		public ValueTask DropRepositoryAsync<TEntity, TKey>(CancellationToken ct = default) where TEntity : class => ValueTask.CompletedTask;
	}
}
