using Kista.Utils;

using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using MongoFramework;

namespace Kista;

/// <summary>
/// Unit tests for the <c>WithMongoMultiTenancy</c> extension method on
/// <see cref="MongoRepositoryBuilder"/>, and for the
/// <see cref="MongoDbTenantConnection{TContext}"/> resolution paths
/// (tenant connection string, fallback to default, missing tenant info,
/// empty connection string).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "MultiTenant")]
public class MongoMultiTenancyExtensionsTests {
    [Fact]
    public void WithMongoMultiTenancy_RegistersTenantConnectionAndOptions() {
        var services = new ServiceCollection();
        services.AddMongoTenantContext(new MongoDbTenantInfo {
            Id = Guid.NewGuid().ToString(),
            Identifier = "t1",
            ConnectionString = "mongodb://localhost:27017/tenant1"
        });

        services.AddRepositoryContext().UseMongoDB<MongoDbContext>(mongoBuilder => {
            mongoBuilder.WithMongoMultiTenancy<MongoDbTenantInfo>("mongodb://localhost:27017/default");
        });

        using var provider = services.BuildServiceProvider();
        var connection = provider.GetService<IMongoDbConnection<MongoDbContext>>();
        Assert.NotNull(connection);
        var typed = Assert.IsType<MongoDbTenantConnection<MongoDbContext>>(connection);
        Assert.Equal("mongodb://localhost/tenant1", typed.Url.ToString());

        var opts = provider.GetRequiredService<IOptions<MongoTenantConnectionOptions>>().Value;
        Assert.Equal("mongodb://localhost:27017/default", opts.DefaultConnectionString);
    }

    [Fact]
    public void WithMongoMultiTenancy_FallsBackToDefault_WhenTenantHasNoConnectionString() {
        var services = new ServiceCollection();
        services.AddMongoTenantContext(new MongoDbTenantInfo {
            Id = Guid.NewGuid().ToString(),
            Identifier = "t2",
            ConnectionString = null
        });

        services.AddRepositoryContext().UseMongoDB<MongoDbContext>(mongoBuilder => {
            mongoBuilder.WithMongoMultiTenancy<MongoDbTenantInfo>("mongodb://localhost:27017/fallback");
        });

        using var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<IMongoDbConnection<MongoDbContext>>();
        var typed = Assert.IsType<MongoDbTenantConnection<MongoDbContext>>(connection);
        Assert.Equal("mongodb://localhost/fallback", typed.Url.ToString());
    }

    [Fact]
    public void WithMongoMultiTenancy_Throws_WhenTenantInfoMissing() {
        var services = new ServiceCollection();
        // No tenant context registered -> MultiTenantContext is null
        services.AddRepositoryContext().UseMongoDB<MongoDbContext>(mongoBuilder => {
            mongoBuilder.WithMongoMultiTenancy<MongoDbTenantInfo>("mongodb://localhost:27017/default");
        });

        using var provider = services.BuildServiceProvider();
        // The exception is thrown when the connection is actually constructed
        // (the service descriptor uses a factory).
        Assert.Throws<InvalidOperationException>(() =>
            provider.GetRequiredService<IMongoDbConnection<MongoDbContext>>());
    }

    [Fact]
    public void WithMongoMultiTenancy_Throws_WhenNoConnectionStringAnywhere() {
        var services = new ServiceCollection();
        services.AddMongoTenantContext(new MongoDbTenantInfo {
            Id = Guid.NewGuid().ToString(),
            Identifier = "t3",
            ConnectionString = null
        });

        services.AddRepositoryContext().UseMongoDB<MongoDbContext>(mongoBuilder => {
            mongoBuilder.WithMongoMultiTenancy<MongoDbTenantInfo>(defaultConnection: null);
        });

        using var provider = services.BuildServiceProvider();
        Assert.Throws<InvalidOperationException>(() =>
            provider.GetRequiredService<IMongoDbConnection<MongoDbContext>>());
    }

    [Fact]
    public void WithMongoMultiTenancy_RegistersNonGenericConnectionAlias() {
        var services = new ServiceCollection();
        services.AddMongoTenantContext(new MongoDbTenantInfo {
            Id = Guid.NewGuid().ToString(),
            Identifier = "t4",
            ConnectionString = "mongodb://localhost:27017/t4"
        });

        services.AddRepositoryContext().UseMongoDB<MongoDbContext>(mongoBuilder => {
            mongoBuilder.WithMongoMultiTenancy<MongoDbTenantInfo>();
        });

        using var provider = services.BuildServiceProvider();
        var generic = provider.GetService<IMongoDbConnection<MongoDbContext>>();
        var nonGeneric = provider.GetService<IMongoDbConnection>();
        Assert.NotNull(generic);
        Assert.NotNull(nonGeneric);
        Assert.Same(generic, nonGeneric);
    }

    [Fact]
    public void WithMongoMultiTenancy_WithCustomContextType_ResolvesConnection() {
        var services = new ServiceCollection();
        services.AddMongoTenantContext(new MongoDbTenantInfo {
            Id = Guid.NewGuid().ToString(),
            Identifier = "t5",
            ConnectionString = "mongodb://localhost:27017/t5"
        });

        services.AddRepositoryContext().UseMongoDB<MongoDbMultiTenantContext>(mongoBuilder => {
            mongoBuilder.WithMongoMultiTenancy<MongoDbTenantInfo>();
        });

        using var provider = services.BuildServiceProvider();
        var connection = provider.GetService<IMongoDbConnection<MongoDbMultiTenantContext>>();
        Assert.NotNull(connection);
        Assert.IsType<MongoDbTenantConnection<MongoDbMultiTenantContext>>(connection);
    }

    [Fact]
    public void MongoDbTenantInfo_Properties_RoundTrip() {
        var info = new MongoDbTenantInfo {
            Id = "id-1",
            Identifier = "ident-1",
            Name = "Display",
            ConnectionString = "mongodb://localhost:27017/x"
        };
        Assert.Equal("id-1", info.Id);
        Assert.Equal("ident-1", info.Identifier);
        Assert.Equal("Display", info.Name);
        Assert.Equal("mongodb://localhost:27017/x", info.ConnectionString);

        // ITenantInfo explicit interface mapping (net8/net9) returns the same values.
        var typed = (ITenantInfo)info;
        Assert.Equal("id-1", typed.Id);
        Assert.Equal("ident-1", typed.Identifier);
    }

    [Fact]
    public void MongoTenantConnectionOptions_DefaultConnectionString_RoundTrip() {
        var opts = new MongoTenantConnectionOptions {
            DefaultConnectionString = "mongodb://localhost:27017/d"
        };
        Assert.Equal("mongodb://localhost:27017/d", opts.DefaultConnectionString);
    }
}