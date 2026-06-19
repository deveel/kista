using Kista.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Kista.HealthChecks.Tests;

/// <summary>
/// Tests for Entity Framework health check implementation.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "HealthChecks")]
[Trait("Feature", "EntityFrameworkHealthCheck")]
public class EntityFrameworkHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenDatabaseIsAccessible_ReturnsHealthy()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase("TestDb_Healthy"));
        var provider = services.BuildServiceProvider();

        var optionsMock = new TestOptions<EntityFrameworkHealthCheckOptions>(
            new EntityFrameworkHealthCheckOptions { TestQuery = false });

        var healthCheck = new EntityFrameworkHealthCheck<TestEntity, Guid>(optionsMock);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("Test", new EfTestDelegatedHealthCheck(), HealthStatus.Unhealthy, null, TimeSpan.FromSeconds(30))
        };

        // Act
        var result = await healthCheck.CheckHealthAsync(context, provider, CancellationToken.None);

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Contains("EntityType", result.Data.Keys);
    }
    
    private class EfTestDelegatedHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken) =>
            Task.FromResult(HealthCheckResult.Healthy());
    }

    [Fact]
    public void EntityFrameworkHealthCheck_ImplementsRepositoryHealthCheck()
    {
        // Arrange
        var optionsMock = new TestOptions<EntityFrameworkHealthCheckOptions>(
            new EntityFrameworkHealthCheckOptions());

        // Act
        var healthCheck = new EntityFrameworkHealthCheck<TestEntity, Guid>(optionsMock);

        // Assert
        Assert.IsAssignableFrom<IRepositoryHealthCheck>(healthCheck);
        Assert.Equal("EntityFramework", healthCheck.DriverType);
    }
}

/// <summary>
/// Test DbContext for health check tests.
/// </summary>
public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }

    public DbSet<TestEntity> TestEntities { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<TestEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
        });
    }
}
