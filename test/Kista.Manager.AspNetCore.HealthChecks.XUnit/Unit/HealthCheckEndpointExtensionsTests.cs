using System.Net;
using System.Text.Json;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Kista.HealthChecks.Tests;

/// <summary>
/// Integration tests for <see cref="HealthCheckEndpointExtensions.MapRepositoryHealthChecks"/>
/// using <c>TestHost</c> to exercise the JSON and text response writers, status code
/// mapping, tag filtering and caching options through the public endpoint surface.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "HealthChecks")]
[Trait("Feature", "AspNetCoreEndpoints")]
public class HealthCheckEndpointExtensionsTests {
    private const string HealthEndpoint = "/health";

    private static async Task<(WebApplication app, HttpClient client)> BuildApp(
        Action<RepositoryHealthCheckEndpointOptions>? configure = null,
        Action<IHealthChecksBuilder>? configureHealthChecks = null,
        string pattern = HealthEndpoint) {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        configureHealthChecks?.Invoke(builder.Services.AddHealthChecks());
        var app = builder.Build();
        app.MapRepositoryHealthChecks(pattern, configure);
        await app.StartAsync();
        var client = app.GetTestServer().CreateClient();
        return (app, client);
    }

    [Fact]
    public async Task MapRepositoryHealthChecks_Default_ReturnsJsonWithHealthyStatus() {
        var (app, client) = await BuildApp(configureHealthChecks: b => b.AddCheck("ok", () => HealthCheckResult.Healthy("ok")));

        using (app) {
            var response = await client.GetAsync(HealthEndpoint);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("\"status\": \"Healthy\"", body);
            Assert.Contains("\"results\"", body);
        }
    }

    [Fact]
    public async Task MapRepositoryHealthChecks_TextFormat_ReturnsPlainTextStatus() {
        var (app, client) = await BuildApp(
            configure: opts => opts.ResponseType = HealthCheckResponseFormat.Text,
            configureHealthChecks: b => b.AddCheck("ok", () => HealthCheckResult.Healthy("ok")));

        using (app) {
            var response = await client.GetAsync(HealthEndpoint);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("Healthy", body);
        }
    }

    [Fact]
    public async Task MapRepositoryHealthChecks_Unhealthy_ReturnsConfiguredStatusCode() {
        var (app, client) = await BuildApp(
            configure: opts => opts.UnhealthyStatusCode = 503,
            configureHealthChecks: b => b.AddCheck("bad", () => HealthCheckResult.Unhealthy("down")));

        using (app) {
            var response = await client.GetAsync(HealthEndpoint);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        }
    }

    [Fact]
    public async Task MapRepositoryHealthChecks_Degraded_ReturnsConfiguredStatusCode() {
        var (app, client) = await BuildApp(
            configure: opts => {
                opts.DegradedStatusCode = 200;
            },
            configureHealthChecks: b => b.AddCheck("degraded", () => HealthCheckResult.Degraded("slow")));

        using (app) {
            var response = await client.GetAsync(HealthEndpoint);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task MapRepositoryHealthChecks_CustomPattern_RoutesToGivenPath() {
        var (app, client) = await BuildApp(
            pattern: "/healthz",
            configureHealthChecks: b => b.AddCheck("ok", () => HealthCheckResult.Healthy("ok")));

        using (app) {
            var response = await client.GetAsync("/healthz");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task MapRepositoryHealthChecks_TagFilter_OnlyRunsMatchingChecks() {
        var (app, client) = await BuildApp(
            configure: opts => opts.TagFilter = tags => tags.Contains("kista"),
            configureHealthChecks: b => {
                b.AddCheck("kista-check", () => HealthCheckResult.Healthy(), new[] { "kista" });
                b.AddCheck("other-check", () => HealthCheckResult.Unhealthy("ignored"), new[] { "other" });
            });

        using (app) {
            var response = await client.GetAsync(HealthEndpoint);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("kista-check", body);
            Assert.DoesNotContain("other-check", body);
        }
    }

    [Fact]
    public async Task MapRepositoryHealthChecks_Json_IncludesEntryData() {
        var (app, client) = await BuildApp(
            configureHealthChecks: b => b.AddCheck("WithData", () =>
                HealthCheckResult.Healthy("ok", data: new Dictionary<string, object?> { { "metric", 42 } })));

        using (app) {
            var response = await client.GetAsync(HealthEndpoint);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("\"data\"", body);
            Assert.Contains("42", body);
        }
    }

    [Fact]
    public void RepositoryHealthCheckEndpointOptions_Defaults_AreExpected() {
        var opts = new RepositoryHealthCheckEndpointOptions();
        Assert.Equal(HealthCheckResponseFormat.Json, opts.ResponseType);
        Assert.Equal(200, opts.SuccessStatusCode);
        Assert.Equal(200, opts.DegradedStatusCode);
        Assert.Equal(503, opts.UnhealthyStatusCode);
        Assert.False(opts.AllowCaching);
        Assert.Null(opts.TagFilter);
    }

    [Fact]
    public void RepositoryHealthCheckEndpointOptions_Properties_RoundTrip() {
        var opts = new RepositoryHealthCheckEndpointOptions {
            ResponseType = HealthCheckResponseFormat.Text,
            SuccessStatusCode = 201,
            DegradedStatusCode = 202,
            UnhealthyStatusCode = 502,
            AllowCaching = true,
            TagFilter = _ => true
        };
        Assert.Equal(HealthCheckResponseFormat.Text, opts.ResponseType);
        Assert.Equal(201, opts.SuccessStatusCode);
        Assert.Equal(202, opts.DegradedStatusCode);
        Assert.Equal(502, opts.UnhealthyStatusCode);
        Assert.True(opts.AllowCaching);
        Assert.NotNull(opts.TagFilter);
    }
}