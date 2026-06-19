using Kista.HealthChecks;
using Kista.HealthChecks.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Kista.HealthChecks.Tests;

/// <summary>
/// Tests for service collection health check extensions.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "HealthChecks")]
[Trait("Feature", "DependencyInjection")]
public class ServiceCollectionHealthCheckExtensionsTests
{
    [Fact]
    public void AddKistaRepositories_WhenCalled_RegistersMarker()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHealthChecks();

        // Act
        services.AddHealthChecks().AddKistaRepositories();

        // Assert
        var marker = services.FirstOrDefault(d => d.ServiceType == typeof(Internal.IKistaHealthChecksRegistered));
        Assert.NotNull(marker);
    }

    [Fact]
    public void AddKistaRepositories_WithCustomConfiguration_AppliesConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHealthChecks();
        var configured = false;

        // Act
        services.AddHealthChecks().AddKistaRepositories(options =>
        {
            configured = true;
        });

        // Assert
        Assert.True(configured);
    }

    [Fact]
    public void AddKistaRepositories_WhenCalledMultipleTimes_DoesNotDoubleRegister()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHealthChecks();

        // Act
        services.AddHealthChecks().AddKistaRepositories();
        services.AddHealthChecks().AddKistaRepositories();

        // Assert
        var registrations = services.Count(d => d.ServiceType == typeof(Internal.IKistaHealthChecksRegistered));
        Assert.Equal(1, registrations);
    }
}
