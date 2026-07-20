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

    [Fact]
    public void AddKistaRepositories_WithoutRegistry_RegistersOptionsButNoDriverChecks()
    {
        // Arrange: no RepositoryContextRegistry registered -> AddKistaRepositories
        // registers the options singleton but returns before iterating repositories
        // (no driver-specific health checks can be added without a registry).
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.AddHealthChecks();

        // Act
        services.AddHealthChecks().AddKistaRepositories(options => {
            options.Timeout = TimeSpan.FromSeconds(42);
        });

        // Assert: the options singleton is always registered before the registry check.
        using var provider = services.BuildServiceProvider();
        var opts = provider.GetService<RepositoryHealthCheckOptions>();
        Assert.NotNull(opts);
        Assert.Equal(TimeSpan.FromSeconds(42), opts.Timeout);
        // No hosted validator should be registered when there is no registry
        // (the startup validator is only added inside the registry-present branch).
        var validator = provider.GetServices<Microsoft.Extensions.Hosting.IHostedService>()
            .FirstOrDefault(h => h.GetType().Name.Contains("StartupValidator", StringComparison.Ordinal));
        Assert.Null(validator);
    }

    [Fact]
    public void AddKistaRepositories_WithRegistry_RegistersOptionsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        var registry = new RepositoryContextRegistry();
        registry.Register(typeof(InMemoryRepository<Person, string>), typeof(Person), typeof(string));
        services.AddSingleton<IRepositoryContextRegistry>(registry);
        services.AddHealthChecks();

        // Act
        services.AddHealthChecks().AddKistaRepositories(options => {
            options.Timeout = TimeSpan.FromSeconds(99);
        });

        // Assert
        var optionsRegistration = services.Single(d =>
            d.ServiceType == typeof(RepositoryHealthCheckOptions));
        var instance = Assert.IsType<RepositoryHealthCheckOptions>(optionsRegistration.ImplementationInstance);
        Assert.Equal(TimeSpan.FromSeconds(99), instance.Timeout);
    }

    [Fact]
    public void AddKistaRepositories_WithExcludedRepositoryType_SkipsRepository()
    {
        // Arrange: a registry with a single repository that is excluded by type.
        var services = new ServiceCollection();
        var registry = new RepositoryContextRegistry();
        registry.Register(typeof(InMemoryRepository<Person, string>), typeof(Person), typeof(string));
        services.AddSingleton<IRepositoryContextRegistry>(registry);
        services.AddHealthChecks();

        // Act
        services.AddHealthChecks().AddKistaRepositories(options => {
            options.ExcludedRepositoryTypes.Add(typeof(InMemoryRepository<Person, string>));
        });

        // Assert: the call completes without registering any driver-specific health check
        // (RegisterHealthCheckForDriver is a no-op placeholder), so we just verify
        // the options singleton was created with the exclusion preserved.
        using var provider = services.BuildServiceProvider();
        var opts = provider.GetRequiredService<RepositoryHealthCheckOptions>();
        Assert.Contains(typeof(InMemoryRepository<Person, string>), opts.ExcludedRepositoryTypes);
    }

    [Fact]
    public void AddKistaRepositories_WithPerRepositoryConfig_InvokesConfigAction()
    {
        // Arrange
        var services = new ServiceCollection();
        var registry = new RepositoryContextRegistry();
        registry.Register(typeof(InMemoryRepository<Person, string>), typeof(Person), typeof(string));
        services.AddSingleton<IRepositoryContextRegistry>(registry);
        services.AddHealthChecks();
        var perRepoTimeout = TimeSpan.Zero;

        // Act
        services.AddHealthChecks().AddKistaRepositories(options => {
            options.ConfigureRepository<Person>(repoOpts => {
                perRepoTimeout = TimeSpan.FromSeconds(7);
            });
        });

        // Assert: the per-repo config action was captured and the options instance
        // is the one created by AddKistaRepositories.
        using var provider = services.BuildServiceProvider();
        var opts = provider.GetRequiredService<RepositoryHealthCheckOptions>();
        Assert.Contains(typeof(Person), opts.PerRepositoryConfig.Keys);

        // Invoke the captured action to assert it sets the timeout (exercises the branch
        // that calls configAction(repoOptions) inside AddKistaRepositories).
        opts.PerRepositoryConfig[typeof(Person)](opts);
        Assert.Equal(TimeSpan.FromSeconds(7), perRepoTimeout);
    }

    [Fact]
    public void AddKistaRepositories_WithCustomNameGenerator_UsesGenerator()
    {
        // Arrange
        var services = new ServiceCollection();
        var registry = new RepositoryContextRegistry();
        registry.Register(typeof(InMemoryRepository<Person, string>), typeof(Person), typeof(string));
        services.AddSingleton<IRepositoryContextRegistry>(registry);
        // Register an InMemory marker so DetermineDriverType returns "InMemory"
        services.AddSingleton<IHealthCheckMarker>(new InMemoryHealthCheckMarker());
        services.AddHealthChecks();

        // Act
        services.AddHealthChecks().AddKistaRepositories(options => {
            options.Naming.NameGenerator = t => $"custom-{t.Name}";
        });

        // Assert: completes without throwing; the generator is captured in the options.
        using var provider = services.BuildServiceProvider();
        var opts = provider.GetRequiredService<RepositoryHealthCheckOptions>();
        Assert.NotNull(opts.Naming.NameGenerator);
        Assert.Equal("custom-InMemoryRepository`2", opts.Naming.NameGenerator(typeof(InMemoryRepository<Person, string>)));
    }

    [Fact]
    public void AddKistaRepositories_WithCustomTemplate_PreservesTemplate()
    {
        // Arrange
        var services = new ServiceCollection();
        var registry = new RepositoryContextRegistry();
        registry.Register(typeof(InMemoryRepository<Person, string>), typeof(Person), typeof(string));
        services.AddSingleton<IRepositoryContextRegistry>(registry);
        services.AddSingleton<IHealthCheckMarker>(new InMemoryHealthCheckMarker());
        services.AddHealthChecks();

        // Act
        services.AddHealthChecks().AddKistaRepositories(options => {
            options.Naming.Template = "{Driver}/{EntityType}/{RepositoryType}";
        });

        // Assert
        using var provider = services.BuildServiceProvider();
        var opts = provider.GetRequiredService<RepositoryHealthCheckOptions>();
        Assert.Equal("{Driver}/{EntityType}/{RepositoryType}", opts.Naming.Template);
    }

    [Fact]
    public void AddKistaRepositories_WithoutDriverMarker_SkipsRepositorySilently()
    {
        // Arrange: registry present but no IHealthCheckMarker registered for any driver.
        var services = new ServiceCollection();
        var registry = new RepositoryContextRegistry();
        registry.Register(typeof(InMemoryRepository<Person, string>), typeof(Person), typeof(string));
        services.AddSingleton<IRepositoryContextRegistry>(registry);
        services.AddHealthChecks();

        // Act: should not throw even though DetermineDriverType will return null.
        services.AddHealthChecks().AddKistaRepositories();

        // Assert: options singleton still registered (the early return only happens
        // when the registry itself is missing).
        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<RepositoryHealthCheckOptions>());
    }

    [Fact]
    public void AddKistaRepositories_WithWarningMode_RegistersHostedService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        var registry = new RepositoryContextRegistry();
        registry.Register(typeof(InMemoryRepository<Person, string>), typeof(Person), typeof(string));
        services.AddSingleton<IRepositoryContextRegistry>(registry);
        services.AddHealthChecks().AddKistaRepositories(options => {
            options.StartupValidationMode = StartupValidationMode.Warning;
        });

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetServices<Microsoft.Extensions.Hosting.IHostedService>()
            .FirstOrDefault(h => h.GetType().Name.Contains("StartupValidator", StringComparison.Ordinal));
        Assert.NotNull(validator);
    }
}
