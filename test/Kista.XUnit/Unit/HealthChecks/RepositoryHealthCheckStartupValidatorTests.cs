using Kista.HealthChecks;
using Kista.HealthChecks.Internal;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kista.HealthChecks.Tests;

/// <summary>
/// Tests for the <see cref="RepositoryHealthCheckStartupValidator"/> hosted service,
/// driven indirectly via <see cref="ServiceCollectionExtensions.AddKistaRepositories"/>
/// with the various <see cref="StartupValidationMode"/> values, since the validator
/// itself is internal and only constructable through the public DI surface.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "HealthChecks")]
[Trait("Feature", "StartupValidation")]
public class RepositoryHealthCheckStartupValidatorTests {
    private static (ServiceProvider, IHostedService) BuildValidatorHost(
        StartupValidationMode mode,
        params Action<IHealthChecksBuilder>[] configure) {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();

        // AddKistaRepositories only registers the validator when a
        // RepositoryContextRegistry is present. We register a fake one with
        // a single dummy repository so the startup validator path is exercised.
        var registry = new RepositoryContextRegistry();
        registry.Register(typeof(InMemoryRepository<Person, string>), typeof(Person), typeof(string));
        services.AddSingleton<IRepositoryContextRegistry>(registry);

        var builder = services.AddHealthChecks();
        foreach (var cfg in configure) cfg(builder);
        builder.AddKistaRepositories(options => options.StartupValidationMode = mode);

        var provider = services.BuildServiceProvider();
        // The validator is registered after the publisher(s) from AddHealthChecks().
        var hosted = provider.GetServices<IHostedService>().Last(h =>
            h.GetType().FullName?.Contains("StartupValidator", StringComparison.Ordinal) == true);
        return (provider, hosted);
    }

    [Fact]
    public async Task StartAsync_DoesNothing_When_ModeIsNone() {
        // When StartupValidationMode is None, the validator is not registered
        // (only the default HealthCheckPublisherHostedService from AddHealthChecks() is present).
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        var registry = new RepositoryContextRegistry();
        registry.Register(typeof(InMemoryRepository<Person, string>), typeof(Person), typeof(string));
        services.AddSingleton<IRepositoryContextRegistry>(registry);
        services.AddHealthChecks().AddKistaRepositories(options => {
            options.StartupValidationMode = StartupValidationMode.None;
        });
        var provider = services.BuildServiceProvider();

        var hostedServices = provider.GetServices<IHostedService>().ToList();
        Assert.All(hostedServices, h => Assert.False(h.GetType().Name.Contains("StartupValidator", StringComparison.Ordinal)));
        await Task.CompletedTask;
    }

    [Fact]
    public async Task StartAsync_LogsWarning_When_ModeIsWarningAndChecksFail() {
        var (provider, hosted) = BuildValidatorHost(
            StartupValidationMode.Warning,
            b => b.AddCheck("failing", () => HealthCheckResult.Unhealthy("boom")));

        using (provider) {
            // Should not throw in Warning mode, even when checks are unhealthy.
            await hosted.StartAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task StartAsync_Throws_When_ModeIsFailFastAndChecksFail() {
        var (provider, hosted) = BuildValidatorHost(
            StartupValidationMode.FailFast,
            b => b.AddCheck("failing", () => HealthCheckResult.Unhealthy("boom")));

        using (provider) {
            await Assert.ThrowsAsync<InvalidOperationException>(() => hosted.StartAsync(CancellationToken.None));
        }
    }

    [Fact]
    public async Task StartAsync_Passes_When_ModeIsFailFastAndChecksAreHealthy() {
        var (provider, hosted) = BuildValidatorHost(
            StartupValidationMode.FailFast,
            b => b.AddCheck("ok", () => HealthCheckResult.Healthy("ok")));

        using (provider) {
            await hosted.StartAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task StartAsync_LogsException_When_ModeIsWarningAndHealthCheckServiceThrows() {
        var (provider, hosted) = BuildValidatorHost(
            StartupValidationMode.Warning,
            b => b.AddCheck("throwing", ct => throw new InvalidOperationException("check-thrown")));

        using (provider) {
            // In Warning mode the exception is swallowed (only logged) so StartAsync should not throw.
            await hosted.StartAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task StartAsync_RethrowsException_When_ModeIsFailFastAndHealthCheckServiceThrows() {
        var (provider, hosted) = BuildValidatorHost(
            StartupValidationMode.FailFast,
            b => b.AddCheck("throwing", ct => throw new InvalidOperationException("check-thrown")));

        using (provider) {
            await Assert.ThrowsAsync<InvalidOperationException>(() => hosted.StartAsync(CancellationToken.None));
        }
    }

    [Fact]
    public async Task StopAsync_ReturnsCompletedTask() {
        var (provider, hosted) = BuildValidatorHost(
            StartupValidationMode.Warning,
            b => b.AddCheck("ok", () => HealthCheckResult.Healthy("ok")));

        using (provider) {
            await hosted.StartAsync(CancellationToken.None);
            await hosted.StopAsync(CancellationToken.None);
        }
    }
}