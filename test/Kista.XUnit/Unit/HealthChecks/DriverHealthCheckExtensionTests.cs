using Kista.HealthChecks;
using Kista.HealthChecks.Internal;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

using MongoDB.Bson;
using MongoDB.Driver;

using MongoFramework;
using NSubstitute.Core;

namespace Kista.HealthChecks.Tests;

/// <summary>
/// Tests for the driver-specific <c>WithHealthChecks</c> extension methods
/// registered by the <c>Kista.HealthChecks.EntityFramework</c>,
/// <c>Kista.HealthChecks.InMemory</c> and <c>Kista.HealthChecks.MongoFramework</c>
/// packages, and for the <see cref="EntityFrameworkHealthCheck{TEntity,TKey}"/>,
/// <see cref="InMemoryHealthCheck{TEntity,TKey}"/> and <see cref="MongoHealthCheck{TEntity,TKey}"/>
/// implementations driven through the public <see cref="RepositoryHealthCheckBase{TEntity,TKey}.CheckHealthAsync"/>
/// entry point.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "HealthChecks")]
[Trait("Feature", "DriverHealthChecks")]
public class DriverHealthCheckExtensionTests {
    private const string MongoTag = "mongo";
    private const string EfTag = "ef";
    private const string InMemoryTag = "im";
    private static readonly TimeSpan HealthCheckTimeout = TimeSpan.FromSeconds(30);

    private static HealthCheckContext BuildHealthCheckContext(string tag)
        => new() {
            Registration = new HealthCheckRegistration(
                tag, DummyHealthCheck.Instance, HealthStatus.Unhealthy, null, HealthCheckTimeout)
        };

    private static EntityFrameworkHealthCheck<TestEntity, Guid> CreateEfHealthCheck(bool testQuery)
        => new(new TestOptions<EntityFrameworkHealthCheckOptions>(
            new EntityFrameworkHealthCheckOptions { TestQuery = testQuery }));

    private static MongoHealthCheck<TestEntity, Guid> CreateMongoHealthCheck()
        => new(new TestOptions<MongoHealthCheckOptions>(new MongoHealthCheckOptions()));

    private static IServiceProvider BuildMongoServices(Func<CallInfo, Task<BsonDocument>> runCommand)
        => new ServiceCollection()
            .AddSingleton(BuildMongoContext(runCommand))
            .BuildServiceProvider();

    private static IMongoDbContext BuildMongoContext(Func<CallInfo, Task<BsonDocument>> runCommand) {
        var contextMock = Substitute.For<IMongoDbContext>();
        var connectionMock = Substitute.For<IMongoDbConnection>();
        var databaseMock = Substitute.For<IMongoDatabase>();
        contextMock.Connection.Returns(connectionMock);
        connectionMock.GetDatabase().Returns(databaseMock);
        databaseMock.RunCommandAsync<BsonDocument>(
            Arg.Any<BsonDocumentCommand<BsonDocument>>(),
            cancellationToken: Arg.Any<CancellationToken>())
            .Returns(runCommand);
        return contextMock;
    }
    [Fact]
    public void EntityFramework_WithHealthChecks_RegistersMarkerAndOptions() {
        var services = new ServiceCollection();
        var builder = services.AddRepositoryContext()
            .UseEntityFramework<TestDbContext>();

        builder.WithHealthChecks(options => {
            options.Timeout = TimeSpan.FromSeconds(15);
            options.TestQuery = true;
        });

        var marker = services.Single(d => d.ServiceType == typeof(IHealthCheckMarker));
        var instance = Assert.IsType<EntityFrameworkHealthCheckMarker>(marker.ImplementationInstance);
        Assert.Equal("EntityFramework", instance.DriverType);

        using var provider = services.BuildServiceProvider();
        var resolved = provider.GetRequiredService<IOptions<EntityFrameworkHealthCheckOptions>>().Value;
        Assert.Equal(TimeSpan.FromSeconds(15), resolved.Timeout);
        Assert.True(resolved.TestQuery);
    }

    [Fact]
    public void EntityFramework_WithHealthChecks_WithoutConfigure_RegistersMarkerWithDefaults() {
        var services = new ServiceCollection();
        var builder = services.AddRepositoryContext()
            .UseEntityFramework<TestDbContext>();

        builder.WithHealthChecks();

        var marker = services.Single(d => d.ServiceType == typeof(IHealthCheckMarker));
        Assert.IsType<EntityFrameworkHealthCheckMarker>(marker.ImplementationInstance);
    }

    [Fact]
    public async Task EntityFramework_HealthCheck_ReturnsHealthy_WhenCanConnect() {
        var healthCheck = CreateEfHealthCheck(testQuery: false);

        var services = new ServiceCollection()
            .AddDbContext<TestDbContext>(o => o.UseInMemoryDatabase("health-check"))
            .AddScoped<DbContext>(sp => sp.GetRequiredService<TestDbContext>())
            .BuildServiceProvider();

        using var scope = services.CreateScope();
        var result = await healthCheck.CheckHealthAsync(BuildHealthCheckContext(EfTag), scope.ServiceProvider, CancellationToken.None);
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Contains("DbContextType", result.Data.Keys);
    }

    [Fact]
    public async Task EntityFramework_HealthCheck_ReturnsHealthyWithTestQuery_WhenTestQueryEnabled() {
        var healthCheck = CreateEfHealthCheck(testQuery: true);

        var services = new ServiceCollection()
            .AddDbContext<TestDbContext>(o => o.UseInMemoryDatabase("health-check-tq"))
            .AddScoped<DbContext>(sp => sp.GetRequiredService<TestDbContext>())
            .BuildServiceProvider();

        using var scope = services.CreateScope();
        var result = await healthCheck.CheckHealthAsync(BuildHealthCheckContext(EfTag), scope.ServiceProvider, CancellationToken.None);
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Contains("EntityExists", result.Data.Keys);
    }

    [Fact]
    public async Task EntityFramework_HealthCheck_ReturnsUnhealthy_When_CannotConnect() {
        var healthCheck = CreateEfHealthCheck(testQuery: false);

        var services = new ServiceCollection();
        services.AddDbContext<InMemoryTestDbContext>(o => o.UseInMemoryDatabase("cannot-connect"));
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<InMemoryTestDbContext>());
        using var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        // Dispose the context so CanConnectAsync throws ObjectDisposedException,
        // exercising the generic catch branch of the EF health check.
        var ctx = scope.ServiceProvider.GetRequiredService<InMemoryTestDbContext>();
        await ctx.DisposeAsync();

        var result = await healthCheck.CheckHealthAsync(BuildHealthCheckContext(EfTag), scope.ServiceProvider, CancellationToken.None);
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.NotNull(result.Exception);
    }

    [Fact]
    public async Task EntityFramework_HealthCheck_ReturnsUnhealthy_When_DbContextThrowsInvalidOperationException() {
        var healthCheck = CreateEfHealthCheck(testQuery: false);

        // Registering a DbContext without a provider causes CanConnectAsync
        // to throw InvalidOperationException, exercising that catch branch.
        var services = new ServiceCollection();
        services.AddDbContext<NoProviderDbContext>();
        // Ensure the base DbContext type resolves to the same instance so the
        // health check's GetRequiredService<DbContext>() succeeds and the
        // exception is raised inside the try/catch of CheckHealthAsyncCore.
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<NoProviderDbContext>());
        using var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var result = await healthCheck.CheckHealthAsync(BuildHealthCheckContext(EfTag), scope.ServiceProvider, CancellationToken.None);
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.NotNull(result.Exception);
    }

    [Fact]
    public void InMemory_WithHealthChecks_RegistersMarkerAndOptions() {
        var services = new ServiceCollection();
        var builder = services.AddRepositoryContext().UseInMemory();

        builder.WithHealthChecks(options => {
            options.Timeout = TimeSpan.FromSeconds(20);
        });

        var marker = services.Single(d => d.ServiceType == typeof(IHealthCheckMarker));
        Assert.IsType<InMemoryHealthCheckMarker>(marker.ImplementationInstance);

        using var provider = services.BuildServiceProvider();
        var resolved = provider.GetRequiredService<IOptions<InMemoryHealthCheckOptions>>().Value;
        Assert.Equal(TimeSpan.FromSeconds(20), resolved.Timeout);
    }

    [Fact]
    public async Task InMemory_HealthCheck_AlwaysReturnsHealthy() {
        var healthCheck = new InMemoryHealthCheck<TestEntity, Guid>(
            new TestOptions<InMemoryHealthCheckOptions>(new InMemoryHealthCheckOptions()));

        var services = new ServiceCollection().BuildServiceProvider();
        var result = await healthCheck.CheckHealthAsync(BuildHealthCheckContext(InMemoryTag), services, CancellationToken.None);
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("In-memory repository is available", result.Description);
    }

    [Fact]
    public void Mongo_WithHealthChecks_RegistersMarkerAndOptions() {
        var services = new ServiceCollection();
        var builder = services.AddRepositoryContext().UseMongoDB<MongoDbContext>();

        builder.WithHealthChecks(options => {
            options.Timeout = TimeSpan.FromSeconds(10);
            options.PingTimeout = TimeSpan.FromSeconds(2);
        });

        var marker = services.Single(d => d.ServiceType == typeof(IHealthCheckMarker));
        Assert.IsType<MongoHealthCheckMarker>(marker.ImplementationInstance);

        using var provider = services.BuildServiceProvider();
        var resolved = provider.GetRequiredService<IOptions<MongoHealthCheckOptions>>().Value;
        Assert.Equal(TimeSpan.FromSeconds(10), resolved.Timeout);
        Assert.Equal(TimeSpan.FromSeconds(2), resolved.PingTimeout);
    }

    [Fact]
    public async Task Mongo_HealthCheck_ReturnsHealthy_WhenPingSucceeds() {
        var healthCheck = CreateMongoHealthCheck();
        var services = BuildMongoServices(_ => Task.FromResult(new BsonDocument()));

        var result = await healthCheck.CheckHealthAsync(BuildHealthCheckContext(MongoTag), services, CancellationToken.None);
        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task Mongo_HealthCheck_ReturnsUnhealthy_OnMongoException() {
        var healthCheck = CreateMongoHealthCheck();
        var services = BuildMongoServices(_ => throw new MongoConnectionException(null, null));

        var result = await healthCheck.CheckHealthAsync(BuildHealthCheckContext(MongoTag), services, CancellationToken.None);
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("ExceptionType", result.Data.Keys);
    }

    [Fact]
    public async Task Mongo_HealthCheck_ReturnsUnhealthy_OnTimeoutException() {
        var healthCheck = CreateMongoHealthCheck();
        var services = BuildMongoServices(_ => throw new TimeoutException("ping timed out"));

        var result = await healthCheck.CheckHealthAsync(BuildHealthCheckContext(MongoTag), services, CancellationToken.None);
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("ExceptionType", result.Data.Keys);
    }

    [Fact]
    public async Task Mongo_HealthCheck_ReturnsUnhealthy_OnGenericException() {
        var healthCheck = CreateMongoHealthCheck();
        var services = BuildMongoServices(_ => throw new InvalidOperationException("boom"));

        var result = await healthCheck.CheckHealthAsync(BuildHealthCheckContext(MongoTag), services, CancellationToken.None);
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("ExceptionType", result.Data.Keys);
    }

    [Fact]
    public async Task Mongo_HealthCheck_Throws_OnUserCancellation() {
        var healthCheck = CreateMongoHealthCheck();
        var services = BuildMongoServices(_ => throw new OperationCanceledException());

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            healthCheck.CheckHealthAsync(BuildHealthCheckContext(MongoTag), services, cts.Token).AsTask());
    }
}

/// <summary>
/// Minimal in-memory DbContext used to exercise the EF health check
/// without requiring a real database connection.
/// </summary>
file sealed class InMemoryTestDbContext : DbContext {
    public InMemoryTestDbContext(DbContextOptions<InMemoryTestDbContext> options) : base(options) { }
    public DbSet<TestEntity> Entities => Set<TestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<TestEntity>().HasKey(e => e.Id);
    }
}

/// <summary>
/// A DbContext registered without any provider, so that <c>Database.CanConnectAsync</c>
/// throws <see cref="InvalidOperationException"/> ("No database provider configured"),
/// exercising the InvalidOperationException catch branch of the EF health check.
/// </summary>
file sealed class NoProviderDbContext : DbContext {
    public NoProviderDbContext(DbContextOptions<NoProviderDbContext> options) : base(options) { }
    public DbSet<TestEntity> Entities => Set<TestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<TestEntity>().HasKey(e => e.Id);
    }
}

/// <summary>
/// A no-op <see cref="IHealthCheck"/> used only to satisfy the
/// <see cref="HealthCheckRegistration"/> constructor; the actual
/// check is performed by <see cref="RepositoryHealthCheckBase{TEntity,TKey}.CheckHealthAsync"/>
/// invoked directly in the tests.
/// </summary>
file sealed class DummyHealthCheck : IHealthCheck {
    public static readonly DummyHealthCheck Instance = new();

    private DummyHealthCheck() { }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(HealthCheckResult.Healthy());
}