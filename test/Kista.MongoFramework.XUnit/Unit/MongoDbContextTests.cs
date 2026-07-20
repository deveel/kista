using Kista.Utils;

using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;

using Microsoft.Extensions.DependencyInjection;

using MongoFramework;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "MongoDbContext")]
public class MongoDbContextTests {
    #region Default Context

    [Fact]
    public void Should_ResolveDefaultContext_When_DirectConnectionConfigured() {
        // Arrange
        var services = new ServiceCollection();
        services.AddRepositoryContext().UseMongoDB<MongoDbContext>(mongoBuilder => mongoBuilder.WithConnection(builder => {
            builder.UseConnection("mongodb://localhost:27017/testdb");
        }));

        // Act
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<IMongoDbContext>());
        Assert.NotNull(provider.GetService<MongoDbContext>());
    }

    [Fact]
    public void Should_ResolveDefaultContext_When_TenantConnectionUsed() {
        // Arrange
        var services = new ServiceCollection();
        services.AddRepositoryContext().UseMongoDB<MongoDbContext>(mongoBuilder => mongoBuilder.WithConnection(builder => builder.UseTenantConnection()));
        services.AddMongoTenantContext(new MongoDbTenantInfo {
            Id = Guid.NewGuid().ToString(),
            Identifier = "test-tenant",
            ConnectionString = "mongodb://localhost:27017/testdb"
        });

        // Act
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<IMongoDbContext>());
        Assert.NotNull(provider.GetService<MongoDbContext>());
    }

    #endregion

    #region MultiTenant Context

    [Fact]
    public void Should_ResolveMultiTenantContext_When_SharedConnectionConfigured() {
        // Arrange
        var services = new ServiceCollection();
        services.AddMongoTenantContext(new MongoDbTenantInfo {
            Id = Guid.NewGuid().ToString(),
            Identifier = "test-tenant"
        });
        services.AddRepositoryContext().UseMongoDB<MongoDbMultiTenantContext>(mongoBuilder => mongoBuilder.WithConnection(builder => {
            builder.UseConnection("mongodb://localhost:27017/testdb");
        }));

        // Act
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<IMongoDbContext>());
        Assert.NotNull(provider.GetService<MongoDbMultiTenantContext>());
        Assert.NotNull(provider.GetService<IMongoDbConnection<MongoDbMultiTenantContext>>());
    }

    [Fact]
    public void Should_ResolveMultiTenantContext_When_TenantConnectionUsed() {
        // Arrange
        var services = new ServiceCollection();
        services.AddRepositoryContext().UseMongoDB<MongoDbMultiTenantContext>(mongoBuilder => mongoBuilder.WithConnection(builder => builder.UseTenantConnection()));
        services.AddMongoTenantContext(new MongoDbTenantInfo {
            Id = Guid.NewGuid().ToString(),
            Identifier = "test-tenant",
            ConnectionString = "mongodb://localhost:27017/testdb"
        });

        // Act
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<IMongoDbContext>());
        Assert.NotNull(provider.GetService<MongoDbMultiTenantContext>());
        Assert.NotNull(provider.GetService<IMongoDbConnection<MongoDbMultiTenantContext>>());
    }

    #endregion

    #region Custom Context

    [Fact]
    public void Should_ResolveCustomContext_When_SimpleContextRegistered() {
        // Arrange
        var services = new ServiceCollection();
        services.AddRepositoryContext().UseMongoDB<MyMongoContext>(mongoBuilder => mongoBuilder.WithConnection(builder => {
            builder.UseConnection("mongodb://localhost:27017/testdb");
        }));

        // Act
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<IMongoDbContext>());
        Assert.NotNull(provider.GetService<MyMongoContext>());
        Assert.NotNull(provider.GetService<IMongoDbConnection<MyMongoContext>>());
    }

    [Fact]
    public void Should_ResolveCustomContext_When_TenantConnectionConfigured() {
        // Arrange
        var services = new ServiceCollection();
        services.AddRepositoryContext().UseMongoDB<MyMongoContext>(mongoBuilder => mongoBuilder.WithConnection(builder => builder.UseTenantConnection()));
        services.AddMongoTenantContext(new MongoDbTenantInfo {
            Id = Guid.NewGuid().ToString(),
            Identifier = "test-tenant",
            ConnectionString = "mongodb://localhost:27017/testdb"
        });

        // Act
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<IMongoDbContext>());
        Assert.NotNull(provider.GetService<MyMongoContext>());
        Assert.NotNull(provider.GetService<IMongoDbConnection<MyMongoContext>>());
    }

    [Fact]
    public void Should_ResolveContextWithBaseConnection_When_ContextAcceptsBaseConnectionType() {
        // Arrange
        var services = new ServiceCollection();
        services.AddRepositoryContext().UseMongoDB<MyMongoContextWithConnection>(mongoBuilder => mongoBuilder.WithConnection(builder => {
            builder.UseConnection("mongodb://localhost:27017/testdb");
        }));

        // Act
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<IMongoDbContext>());
        Assert.NotNull(provider.GetService<MyMongoContextWithConnection>());
        Assert.NotNull(provider.GetService<IMongoDbConnection>());
    }

    [Fact]
    public void Should_ResolveCustomTenantContext_When_SharedConnectionConfigured() {
        // Arrange
        var services = new ServiceCollection();
        services.AddMongoTenantContext(new MongoDbTenantInfo {
            Id = Guid.NewGuid().ToString(),
            Identifier = "test-tenant"
        });
        services.AddRepositoryContext().UseMongoDB<MyMongoTenantContext>(mongoBuilder => mongoBuilder.WithConnection(builder => {
            builder.UseConnection("mongodb://localhost:27017/testdb");
        }));

        // Act
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<IMongoDbContext>());
        Assert.NotNull(provider.GetService<MyMongoTenantContext>());
        Assert.NotNull(provider.GetService<IMongoDbConnection<MyMongoTenantContext>>());
    }

    [Fact]
    public void Should_ResolveCustomTenantContext_When_TenantConnectionConfigured() {
        // Arrange
        var services = new ServiceCollection();
        services.AddRepositoryContext().UseMongoDB<MyMongoTenantContext>(mongoBuilder => mongoBuilder.WithConnection(builder => builder.UseTenantConnection()));
        services.AddMongoTenantContext(new MongoDbTenantInfo {
            Id = Guid.NewGuid().ToString(),
            Identifier = "test-tenant",
            ConnectionString = "mongodb://localhost:27017/testdb"
        });

        // Act
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<IMongoDbContext>());
        Assert.NotNull(provider.GetService<MyMongoTenantContext>());
        Assert.NotNull(provider.GetService<IMongoDbConnection<MyMongoTenantContext>>());
    }

    #endregion

    #region Support Types

    private sealed class MyMongoContext(IMongoDbConnection<MyMongoContext> connection) : MongoDbContext(connection) { }

    private sealed class MyMongoTenantContext(
        IMongoDbConnection<MyMongoTenantContext> connection,
        IMultiTenantContextAccessor multiTenantContextAccessor)
        : MongoDbMultiTenantContext(connection, multiTenantContextAccessor) { }

    private sealed class MyMongoContextWithConnection(IMongoDbConnection connection) : MongoDbContext(connection) { }

    #endregion
}
