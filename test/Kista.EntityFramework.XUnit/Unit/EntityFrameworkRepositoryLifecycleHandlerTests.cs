using System.ComponentModel.DataAnnotations;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kista;

[Trait("Category", "Integration")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "Lifecycle")]
public class EntityFrameworkRepositoryLifecycleHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<SimpleDbContext> _options;

    private sealed class SimpleEntity
    {
        [Key]
        public string? Id { get; set; }
        public string? Name { get; set; }
    }

    private sealed class SimpleDbContext : DbContext
    {
        public SimpleDbContext(DbContextOptions<SimpleDbContext> options) : base(options) { }
        public DbSet<SimpleEntity>? Entities { get; set; }
    }

    public EntityFrameworkRepositoryLifecycleHandlerTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<SimpleDbContext>()
            .UseSqlite(_connection)
            .Options;
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }

    private SimpleDbContext CreateContext()
    {
        var ctx = new SimpleDbContext(_options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    [Fact]
    public async Task Should_ReturnTrue_When_RepositoryExists()
    {
        using var context = CreateContext();
        var handler = new EntityFrameworkRepositoryLifecycleHandler<SimpleEntity>(
            context, NullLogger<EntityFrameworkRepositoryLifecycleHandler<SimpleEntity>>.Instance);

        var exists = await handler.ExistsAsync(TestContext.Current.CancellationToken);

        Assert.True(exists);
    }

    [Fact]
    public async Task Should_CreateRepository_When_CreateAsyncCalled()
    {
        using var context = new SimpleDbContext(_options);
        var handler = new EntityFrameworkRepositoryLifecycleHandler<SimpleEntity>(
            context, NullLogger<EntityFrameworkRepositoryLifecycleHandler<SimpleEntity>>.Instance);

        await handler.CreateAsync(TestContext.Current.CancellationToken);

        Assert.True(context.Database.CanConnect());
    }

    [Fact]
    public async Task Should_SeedEntities_When_SeedAsyncCalled()
    {
        using var context = CreateContext();
        var handler = new EntityFrameworkRepositoryLifecycleHandler<SimpleEntity>(
            context, NullLogger<EntityFrameworkRepositoryLifecycleHandler<SimpleEntity>>.Instance);
        var entities = new[] {
            new SimpleEntity { Id = Guid.NewGuid().ToString(), Name = "Test1" },
            new SimpleEntity { Id = Guid.NewGuid().ToString(), Name = "Test2" },
        };

        await handler.SeedAsync(entities, TestContext.Current.CancellationToken);

        var seeded = context.Set<SimpleEntity>().ToList();
        Assert.Equal(2, seeded.Count);
    }

    [Fact]
    public async Task Should_NotThrow_When_SeedAsyncCalledWithNull()
    {
        using var context = CreateContext();
        var handler = new EntityFrameworkRepositoryLifecycleHandler<SimpleEntity>(
            context, NullLogger<EntityFrameworkRepositoryLifecycleHandler<SimpleEntity>>.Instance);

        await handler.SeedAsync(null, TestContext.Current.CancellationToken);
    }
}
