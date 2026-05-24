namespace Deveel.Data;

/// <summary>
/// Tests for <see cref="SystemTime"/> and the <see cref="ISystemTime"/> contract,
/// verifying that <see cref="ISystemTime.UtcNow"/> and <see cref="ISystemTime.Now"/>
/// return values close to the actual system clock.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Core")]
[Trait("Feature", "SystemTime")]
public class SystemTimeTests
{
	[Fact]
	public void UtcNow_ShouldReturnCloseToDateTimeOffsetUtcNow()
	{
		// Arrange
		var systemTime = new SystemTime();

		// Act
		var result = systemTime.UtcNow;
		var expected = DateTimeOffset.UtcNow;

		// Assert
		Assert.True((expected - result).Duration() < TimeSpan.FromSeconds(1));
	}

	[Fact]
	public void Now_ShouldReturnCloseToDateTimeOffsetNow()
	{
		// Arrange
		var systemTime = new SystemTime();

		// Act
		var result = systemTime.Now;
		var expected = DateTimeOffset.Now;

		// Assert
		Assert.True((expected - result).Duration() < TimeSpan.FromSeconds(1));
	}

	[Fact]
	public void Default_ShouldReturnInstanceOfSystemTime()
	{
		// Act
		var result = SystemTime.Default;

		// Assert
		Assert.NotNull(result);
		Assert.IsType<SystemTime>(result);
	}
}
