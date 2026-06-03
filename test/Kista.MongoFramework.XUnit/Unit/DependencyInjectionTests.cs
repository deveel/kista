using Kista.Utils;

using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using MongoDB.Bson;

using MongoFramework;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "DependencyInjection")]
public class DependencyInjectionTests {
    #region MongoDbContext

    [Fact]
    public void Should_ResolveMongoDbContext_When_DefaultConnectionStringProvided() {
        // Arrange
        var services = new ServiceCollection();
        services.AddMongoDbContext<MongoDbContext>(builder => {
            builder.UseConnection("mongodb://localhost:27017/testdb");
        });

        // Act
        var provider = services.BuildServiceProvider();
        var dbContext = provider.GetService<IMongoDbContext>();

        // Assert
        Assert.NotNull(provider.GetService<MongoDbContext>());
        Assert.NotNull(dbContext);
        Assert.IsType<MongoDbContext>(dbContext);
        Assert.NotNull(dbContext.Connection);
        Assert.IsAssignableFrom<IMongoDbConnection>(dbContext.Connection);
        var connection = Assert.IsType<MongoDbConnection<MongoDbContext>>(dbContext.Connection);
        Assert.NotNull(connection.Url);
        Assert.Equal("mongodb://localhost/testdb", connection.Url.ToString());
    }

    [Fact]
    public void Should_ResolveMongoDbContext_When_TenantConnectionUsed() {
        // Arrange
        var services = new ServiceCollection();
        services.AddMongoDbContext<MongoDbContext>(builder => builder.UseTenantConnection());
        services.AddMongoTenantContext(new MongoDbTenantInfo {
            Id = Guid.NewGuid().ToString(),
            Identifier = "test-tenant",
            ConnectionString = "mongodb://localhost:27017/testdb"
        });

        // Act
        var provider = services.BuildServiceProvider();
        var dbContext = provider.GetService<IMongoDbContext>();

        // Assert
        Assert.NotNull(provider.GetService<MongoDbContext>());
        Assert.NotNull(dbContext);
        Assert.IsType<MongoDbContext>(dbContext);
        var connection = Assert.IsType<MongoDbTenantConnection<MongoDbContext>>(dbContext!.Connection);
        Assert.NotNull(connection.Url);
        Assert.Equal("mongodb://localhost/testdb", connection.Url.ToString());
    }

    [Fact]
    public void Should_ResolveMultiTenantContext_When_TenantConnectionStringProvided() {
        // Arrange
        var services = new ServiceCollection();
        services.AddMongoTenantContext(new MongoDbTenantInfo {
            Id = Guid.NewGuid().ToString(),
            Identifier = "test-tenant"
        });
        services.AddMongoDbContext<MongoDbMultiTenantContext>(builder => {
            builder.UseTenantConnection("mongodb://localhost:27017/testdb");
        });

        // Act
        var provider = services.BuildServiceProvider();
        var dbContext = provider.GetService<IMongoDbTenantContext>();

        // Assert
        Assert.NotNull(provider.GetService<MongoDbMultiTenantContext>());
        Assert.NotNull(dbContext);
        Assert.IsType<MongoDbMultiTenantContext>(dbContext);
        var connection = Assert.IsType<MongoDbTenantConnection<MongoDbMultiTenantContext>>(dbContext!.Connection);
        Assert.NotNull(connection.Url);
        Assert.Equal("mongodb://localhost/testdb", connection.Url.ToString());
    }

    #endregion

    #region MongoRepository

    [Fact]
    public void Should_ResolveAllRepositoryInterfaces_When_DefaultMongoRepositoryRegistered() {
        // Arrange
        var services = new ServiceCollection();
        services.AddMongoDbContext<MongoDbContext>(builder => {
            builder.UseConnection("mongodb://localhost:27017/testdb");
        });

        // Act
        services.AddRepository<MongoRepository<MongoPerson>>();
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<MongoRepository<MongoPerson>>());
        Assert.NotNull(provider.GetService<IRepository<MongoPerson>>());
        Assert.NotNull(provider.GetService<IPageableRepository<MongoPerson>>());
        Assert.NotNull(provider.GetService<IFilterableRepository<MongoPerson>>());
    }

    [Fact]
    public void Should_ResolveCustomRepository_When_CustomMongoRepositoryRegistered() {
        // Arrange
        var services = new ServiceCollection();
        services.AddMongoDbContext<MongoDbContext>(builder => {
            builder.UseConnection("mongodb://localhost:27017/testdb");
        });

        // Act
        services.AddRepository<MyMongoPersonRepositoryNoKey>();
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<MyMongoPersonRepositoryNoKey>());
        Assert.NotNull(provider.GetService<MongoRepository<MongoPerson>>());
        Assert.NotNull(provider.GetService<IRepository<MongoPerson>>());
        Assert.NotNull(provider.GetService<IPageableRepository<MongoPerson>>());
        Assert.NotNull(provider.GetService<IFilterableRepository<MongoPerson>>());
    }

    #endregion

    #region Support Types

    private interface IMyMongoPersonRepository : IRepository<MongoPerson> { }

    private sealed class MyMongoPersonRepositoryNoKey : MongoRepository<MongoPerson>, IMyMongoPersonRepository {
        public MyMongoPersonRepositoryNoKey(IMongoDbContext context, ILogger<MyMongoPersonRepositoryNoKey>? logger = null)
            : base(context, logger) { }
    }

    private sealed class MyMongoPersonRepository : MongoRepository<MongoPerson, ObjectId> {
        public MyMongoPersonRepository(IMongoDbContext context, ILogger? logger = null)
            : base(context, logger) { }
    }

    #endregion
}
