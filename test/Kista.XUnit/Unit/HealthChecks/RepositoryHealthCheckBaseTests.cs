using Kista.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Kista.HealthChecks.Tests;

/// <summary>
/// Tests for <see cref="RepositoryHealthCheckBase{TEntity, TKey}"/> base class.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "HealthChecks")]
[Trait("Feature", "HealthCheckBase")]
public class RepositoryHealthCheckBaseTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenRepositoryIsHealthy_ReturnsHealthy()
    {
        // Arrange
        var healthCheck = new TestHealthCheck(shouldSucceed: true);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("Test", new DelegatedHealthCheck(), HealthStatus.Unhealthy, null, TimeSpan.FromSeconds(30))
        };
        var services = new ServiceCollection().BuildServiceProvider();

        // Act
        var result = await healthCheck.CheckHealthAsync(context, services, CancellationToken.None);

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("Repository is healthy", result.Description);
        Assert.Contains("EntityType", result.Data.Keys);
        Assert.Contains("KeyType", result.Data.Keys);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenRepositoryThrowsException_ReturnsUnhealthy()
    {
        // Arrange
        var healthCheck = new TestHealthCheck(shouldSucceed: false);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("Test", new DelegatedHealthCheck(), HealthStatus.Unhealthy, null, TimeSpan.FromSeconds(30))
        };
        var services = new ServiceCollection().BuildServiceProvider();

        // Act
        var result = await healthCheck.CheckHealthAsync(context, services, CancellationToken.None);

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.StartsWith("Health check failed:", result.Description);
        Assert.NotNull(result.Exception);
    }
    
    private class DelegatedHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken) =>
            Task.FromResult(HealthCheckResult.Healthy());
    }

    [Fact]
    public async Task CheckHealthAsync_WithShortTimeout_ReturnsUnhealthy()
    {
        // Arrange
        var healthCheck = new SlowTestHealthCheck(TimeSpan.FromMilliseconds(200));
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("Test", new DelegatedHealthCheck2(), HealthStatus.Unhealthy, null, TimeSpan.FromMilliseconds(50))
        };
        var services = new ServiceCollection().BuildServiceProvider();

        // Act
        var result = await healthCheck.CheckHealthAsync(context, services, CancellationToken.None);

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_WithPreCancelledToken_ThrowsTaskCanceledException()
    {
        // Arrange
        var healthCheck = new SlowTestHealthCheck(TimeSpan.FromSeconds(10));
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("Test", new DelegatedHealthCheck2(), HealthStatus.Unhealthy, null, TimeSpan.FromSeconds(30))
        };
        var services = new ServiceCollection().BuildServiceProvider();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - TaskCanceledException is thrown when cancellation token is already cancelled
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await healthCheck.CheckHealthAsync(context, services, cts.Token));
    }
    
    private class DelegatedHealthCheck2 : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken) =>
            Task.FromResult(HealthCheckResult.Healthy());
    }

    [Fact]
    public void CreateDiagnosticData_IncludesEntityTypeAndKeyType()
    {
        // Act
        var data = TestHealthCheck.CreateDiagnosticDataPublic();

        // Assert
        Assert.Equal(nameof(TestEntity), data["EntityType"]);
        Assert.Equal(nameof(Guid), data["KeyType"]);
        Assert.Equal("Healthy", data["ResponseType"]);
    }

    [Fact]
    public void CreateDiagnosticData_WithAdditionalData_IncludesAllNonNullValues()
    {
        // Arrange
        var additionalData = new[]
        {
            KeyValuePair.Create<string, object?>("Key1", "Value1"),
            KeyValuePair.Create<string, object?>("Key2", null),
            KeyValuePair.Create<string, object?>("Key3", 42)
        };

        // Act
        var data = TestHealthCheck.CreateDiagnosticDataPublic(additionalData);

        // Assert
        Assert.Equal("Value1", data["Key1"]);
        Assert.Equal(42, data["Key3"]);
        Assert.DoesNotContain("Key2", data.Keys);
    }
}

/// <summary>
/// Test entity for health check tests.
/// </summary>
public class TestEntity
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
}

/// <summary>
/// Test health check implementation.
/// </summary>
public class TestHealthCheck : RepositoryHealthCheckBase<TestEntity, Guid>
{
    private readonly bool _shouldSucceed;

    public TestHealthCheck(bool shouldSucceed = true)
    {
        _shouldSucceed = shouldSucceed;
    }

    /// <inheritdoc/>
    public override string DriverType => "Test";

    protected override ValueTask<HealthCheckResult> CheckHealthAsyncCore(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        if (!_shouldSucceed)
            throw new InvalidOperationException("Repository is unavailable");

        return new ValueTask<HealthCheckResult>(HealthCheckResult.Healthy(
            "Repository is healthy",
            data: CreateDiagnosticData()));
    }

    public static Dictionary<string, object> CreateDiagnosticDataPublic(
        params KeyValuePair<string, object?>[] additionalData) =>
        CreateDiagnosticData(additionalData);
}

/// <summary>
/// Slow test health check for timeout testing.
/// </summary>
public class SlowTestHealthCheck : RepositoryHealthCheckBase<TestEntity, Guid>
{
    private readonly TimeSpan _delay;

    public SlowTestHealthCheck(TimeSpan delay)
    {
        _delay = delay;
    }

    /// <inheritdoc/>
    public override string DriverType => "Test";

    protected override async ValueTask<HealthCheckResult> CheckHealthAsyncCore(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        await Task.Delay(_delay, cancellationToken);
        return HealthCheckResult.Healthy("Repository is healthy");
    }
}
