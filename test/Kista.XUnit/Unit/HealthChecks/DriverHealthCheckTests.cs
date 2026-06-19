using Kista.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Kista.HealthChecks.Tests;

/// <summary>
/// Tests for In-Memory health check implementation.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "HealthChecks")]
[Trait("Feature", "InMemoryHealthCheck")]
public class InMemoryHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_Always_ReturnsHealthy()
    {
        // Arrange
        var optionsMock = new TestOptions<InMemoryHealthCheckOptions>(
            new InMemoryHealthCheckOptions());
        var healthCheck = new InMemoryHealthCheck<TestEntity, Guid>(optionsMock);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("Test", new InMemoryDelegatedHealthCheck(), HealthStatus.Unhealthy, null, TimeSpan.FromSeconds(30))
        };
        var services = new ServiceCollection().BuildServiceProvider();

        // Act
        var result = await healthCheck.CheckHealthAsync(context, services, CancellationToken.None);

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("In-memory repository is available", result.Description);
        Assert.Contains("EntityType", result.Data.Keys);
        Assert.Contains("KeyType", result.Data.Keys);
    }
    
    private class InMemoryDelegatedHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken) =>
            Task.FromResult(HealthCheckResult.Healthy());
    }

    [Fact]
    public void InMemoryHealthCheck_ImplementsRepositoryHealthCheck()
    {
        // Arrange
        var optionsMock = new TestOptions<InMemoryHealthCheckOptions>(
            new InMemoryHealthCheckOptions());

        // Act
        var healthCheck = new InMemoryHealthCheck<TestEntity, Guid>(optionsMock);

        // Assert
        Assert.IsAssignableFrom<IRepositoryHealthCheck>(healthCheck);
        Assert.Equal("InMemory", healthCheck.DriverType);
    }
}

/// <summary>
/// Test options for testing.
/// </summary>
public class TestOptions<TOptions> : IOptions<TOptions>
    where TOptions : class, new()
{
    private readonly TOptions _value;

    public TestOptions(TOptions value)
    {
        _value = value;
    }

    public TOptions Value => _value;
}
