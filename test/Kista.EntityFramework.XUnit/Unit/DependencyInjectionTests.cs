using Kista.Entities;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "DependencyInjection")]
public class DependencyInjectionTests {
    private static bool IsSpatialiteAvailable() {
        try {
            using var conn = new SqliteConnection("Data Source=:memory:");
            conn.Open();
            conn.EnableExtensions();
            SpatialiteLoader.Load(conn);
            return true;
        } catch {
            return false;
        }
    }

    private static void ConfigurePersonDbContext(DbContextOptionsBuilder builder) {
        if (IsSpatialiteAvailable()) {
            builder.UseSqlite("Data Source=:memory:", x => x.UseNetTopologySuite());
        } else {
            builder.UseSqlite("Data Source=:memory:");
            builder.ReplaceService<IModelCustomizer, NonSpatialModelCustomizer>();
        }
    }

    [Fact]
    public void Should_ResolveEntityRepository_When_DefaultEntityRepositoryForEntityRegistered() {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<DbContext, PersonDbContext>(options => ConfigurePersonDbContext(options));

        // Act
        services.AddRepositoryContext().AddRepository<EntityRepository<DbPerson>>(_ => { });
        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();

        // Assert
        Assert.NotNull(scope.ServiceProvider.GetService<IRepository<DbPerson>>());
        Assert.NotNull(scope.ServiceProvider.GetService<EntityRepository<DbPerson>>());
    }

    [Fact]
    public void Should_ResolveEntityRepository_When_EntityRepositoryRegistered() {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<DbContext, PersonDbContext>(options => ConfigurePersonDbContext(options));

        // Act
        services.AddRepositoryContext().AddRepository<EntityRepository<DbPerson>>(ServiceLifetime.Scoped);
        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();

        // Assert
        Assert.NotNull(scope.ServiceProvider.GetService<IRepository<DbPerson>>());
        Assert.NotNull(scope.ServiceProvider.GetService<EntityRepository<DbPerson>>());
    }

    #region Support Types

    private sealed class MyEntityRepositoryWithNoKey(PersonDbContext context) : EntityRepository<DbPerson>(context) { }

    private sealed class MyEntityRepository(PersonDbContext context) : EntityRepository<DbPerson, Guid>(context) { }

    #endregion
}
