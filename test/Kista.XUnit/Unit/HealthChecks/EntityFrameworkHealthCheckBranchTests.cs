#pragma warning disable CS8618

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Kista.HealthChecks.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "HealthChecks")]
[Trait("Feature", "EntityFrameworkHealthCheck")]
public class EntityFrameworkHealthCheckBranchTests {
	private static EntityFrameworkHealthCheck<TestEntity, Guid> CreateHealthCheck(bool testQuery = false) {
		var options = new TestOptions<EntityFrameworkHealthCheckOptions>(
			new EntityFrameworkHealthCheckOptions { TestQuery = testQuery });
		return new EntityFrameworkHealthCheck<TestEntity, Guid>(options);
	}

	private static IServiceProvider BuildServiceProvider(DbContext context) {
		var services = new ServiceCollection();
		services.AddSingleton(context);
		return services.BuildServiceProvider();
	}

	private static HealthCheckContext CreateHealthCheckContext() {
		var registration = new HealthCheckRegistration(
			"test", _ => new DelegatedHealthCheck(), HealthStatus.Unhealthy, new[] { "test" });
		return new HealthCheckContext { Registration = registration };
	}

	[Fact]
	public async Task Should_ReturnHealthy_When_CanConnectAndNoTestQuery() {
		var options = new DbContextOptionsBuilder<TestDbContext>()
			.UseInMemoryDatabase("healthy-noquery").Options;
		var context = new TestDbContext(options);
		var healthCheck = CreateHealthCheck(testQuery: false);
		var provider = BuildServiceProvider(context);
		var ct = TestContext.Current.CancellationToken;

		var result = await healthCheck.CheckHealthAsync(CreateHealthCheckContext(), provider, ct);

		Assert.Equal(HealthStatus.Healthy, result.Status);
	}

	[Fact]
	public async Task Should_ReturnHealthy_When_CanConnectAndTestQueryEnabled() {
		var options = new DbContextOptionsBuilder<TestDbContext>()
			.UseInMemoryDatabase("healthy-query").Options;
		var context = new TestDbContext(options);
		context.TestEntities.Add(new TestEntity { Id = Guid.NewGuid() });
		await context.SaveChangesAsync();
		var healthCheck = CreateHealthCheck(testQuery: true);
		var provider = BuildServiceProvider(context);
		var ct = TestContext.Current.CancellationToken;

		var result = await healthCheck.CheckHealthAsync(CreateHealthCheckContext(), provider, ct);

		Assert.Equal(HealthStatus.Healthy, result.Status);
		Assert.True((bool)result.Data["EntityExists"]);
	}

	[Fact]
	public async Task Should_ReturnUnhealthy_When_DbUpdateExceptionThrown() {
		var context = new ThrowingDbContext(ex => throw new DbUpdateException("Update failed", ex));
		var healthCheck = CreateHealthCheck(testQuery: false);
		var provider = BuildServiceProvider(context);
		var ct = TestContext.Current.CancellationToken;

		var result = await healthCheck.CheckHealthAsync(CreateHealthCheckContext(), provider, ct);

		Assert.Equal(HealthStatus.Unhealthy, result.Status);
		Assert.Contains("update failed", result.Description, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task Should_ReturnUnhealthy_When_InvalidOperationExceptionThrown() {
		var context = new ThrowingDbContext(_ => throw new InvalidOperationException("Connection refused"));
		var healthCheck = CreateHealthCheck(testQuery: false);
		var provider = BuildServiceProvider(context);
		var ct = TestContext.Current.CancellationToken;

		var result = await healthCheck.CheckHealthAsync(CreateHealthCheckContext(), provider, ct);

		Assert.Equal(HealthStatus.Unhealthy, result.Status);
		Assert.Contains("invalid", result.Description, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task Should_Rethrow_When_OperationCanceledByCaller() {
		var context = new ThrowingDbContext(_ => throw new OperationCanceledException());
		var healthCheck = CreateHealthCheck(testQuery: false);
		var provider = BuildServiceProvider(context);

		using var cts = new CancellationTokenSource();
		await cts.CancelAsync();

		await Assert.ThrowsAsync<OperationCanceledException>(() =>
			healthCheck.CheckHealthAsync(CreateHealthCheckContext(), provider, cts.Token).AsTask());
	}

	[Fact]
	public async Task Should_ReturnUnhealthy_When_GenericExceptionThrown() {
		var context = new ThrowingDbContext(_ => throw new NotSupportedException("Provider not configured"));
		var healthCheck = CreateHealthCheck(testQuery: false);
		var provider = BuildServiceProvider(context);
		var ct = TestContext.Current.CancellationToken;

		var result = await healthCheck.CheckHealthAsync(CreateHealthCheckContext(), provider, ct);

		Assert.Equal(HealthStatus.Unhealthy, result.Status);
		Assert.Contains("connection failed", result.Description, StringComparison.OrdinalIgnoreCase);
	}

	private sealed class DelegatedHealthCheck : IHealthCheck {
		public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
			=> Task.FromResult(HealthCheckResult.Healthy());
	}

	private sealed class ThrowingDbContext : DbContext {
		private readonly Action<Exception> _throwFactory;

		public ThrowingDbContext(Action<Exception> throwFactory) {
			_throwFactory = throwFactory;
		}

		// SONAR: S8970 — nullable warnings are disabled in test projects; the null-forgiving
		// operator is required for the EF Core DbSet initializer pattern.
		public DbSet<TestEntity> TestEntities { get; set; } = null!; // SONAR: S8970

		public override DatabaseFacade Database {
			get {
				_throwFactory(new Exception("test"));
				// SONAR: S8970 — unreachable: the throw factory always throws before this line.
				return null!; // SONAR: S8970
			}
		}
	}
}