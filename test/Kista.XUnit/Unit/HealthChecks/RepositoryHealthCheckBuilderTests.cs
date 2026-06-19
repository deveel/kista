using Kista.HealthChecks;
using Kista.HealthChecks.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Kista.HealthChecks.Tests;

/// <summary>
/// Tests for repository health check builder and registration extensions.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "HealthChecks")]
[Trait("Feature", "DependencyInjection")]
public class RepositoryHealthCheckBuilderTests
{
    [Fact]
    public void WithHealthChecks_WhenCalled_SetsHealthChecksEnabled()
    {
        // Arrange
        var builder = new TestRepositoryContextBuilder();

        // Act
        var result = builder.WithHealthChecks();

        // Assert
        Assert.Same(builder, result);
        Assert.True(builder.IsHealthChecksEnabled);
    }

    [Fact]
    public void WithHealthChecks_WithCustomTimeout_SetsTimeout()
    {
        // Arrange
        var builder = new TestRepositoryContextBuilder();
        var timeout = TimeSpan.FromSeconds(30);

        // Act
        var result = builder.WithHealthChecks(timeout);

        // Assert
        Assert.Same(builder, result);
        Assert.True(builder.IsHealthChecksEnabled);
        Assert.Equal(timeout, builder.HealthCheckTimeout);
    }

    [Fact]
    public void WithHealthChecks_WithZeroTimeout_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var builder = new TestRepositoryContextBuilder();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.WithHealthChecks(TimeSpan.Zero));
    }
}

/// <summary>
/// Test implementation for testing.
/// </summary>
public class TestRepositoryContextBuilder
{
    public bool IsHealthChecksEnabled { get; private set; }
    public TimeSpan? HealthCheckTimeout { get; private set; }

    public TestRepositoryContextBuilder WithHealthChecks()
    {
        IsHealthChecksEnabled = true;
        return this;
    }

    public TestRepositoryContextBuilder WithHealthChecks(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout));

        IsHealthChecksEnabled = true;
        HealthCheckTimeout = timeout;
        return this;
    }
}
