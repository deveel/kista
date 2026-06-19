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
    public void EntityFrameworkHealthCheck_WithTestQueryDisabled_HasCorrectOptions()
    {
        // Arrange
        var optionsMock = new TestOptions<EntityFrameworkHealthCheckOptions>(
            new EntityFrameworkHealthCheckOptions { TestQuery = false });

        // Act
        var healthCheck = new EntityFrameworkHealthCheck<TestEntity, Guid>(optionsMock);

        // Assert
        Assert.NotNull(healthCheck);
        Assert.Equal("EntityFramework", healthCheck.DriverType);
        Assert.Equal(typeof(IRepository<TestEntity, Guid>), healthCheck.RepositoryType);
    }
    
    [Fact]
    public void EntityFrameworkHealthCheck_DriverTypeIsEntityFramework()
    {
        // Arrange
        var optionsMock = new TestOptions<EntityFrameworkHealthCheckOptions>(
            new EntityFrameworkHealthCheckOptions());

        // Act
        var healthCheck = new EntityFrameworkHealthCheck<TestEntity, Guid>(optionsMock);

        // Assert
        Assert.Equal("EntityFramework", healthCheck.DriverType);
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
