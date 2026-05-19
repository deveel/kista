using Microsoft.Extensions.DependencyInjection;

namespace Deveel.Data;

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

	private class TestTime : ISystemTime
	{
		public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
		public DateTimeOffset Now => DateTimeOffset.Now;
	}
}
