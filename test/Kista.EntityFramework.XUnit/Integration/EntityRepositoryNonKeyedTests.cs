using System.ComponentModel.DataAnnotations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kista;

[Trait("Category", "Integration")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "EntityRepository")]
public sealed class EntityRepositoryNonKeyedTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public EntityRepositoryNonKeyedTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Should_DelegateExists_When_CalledThroughNonKeyedInterface()
    {
        var ct = TestContext.Current.CancellationToken;
        using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync(ct);

        dbContext.Set<SimpleEntity>().Add(new SimpleEntity { Name = "Existing" });
        await dbContext.SaveChangesAsync(ct);

        var repo = new EntityRepository<SimpleEntity>(dbContext);
        var exists = new ExpressionQueryFilter<SimpleEntity>(x => x.Name == "Existing").Apply<SimpleEntity>(((Repository<SimpleEntity, object>)repo).Queryable()).Any();

        Assert.True(exists);
    }

    [Fact]
    public async Task Should_DelegateCount_When_CalledThroughNonKeyedInterface()
    {
        var ct = TestContext.Current.CancellationToken;
        using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync(ct);

        dbContext.Set<SimpleEntity>().AddRange(
            new SimpleEntity { Name = "A" },
            new SimpleEntity { Name = "B" });
        await dbContext.SaveChangesAsync(ct);

        var repo = new EntityRepository<SimpleEntity>(dbContext);
        var count = new ExpressionQueryFilter<SimpleEntity>(x => x.Name != "").Apply<SimpleEntity>(((Repository<SimpleEntity, object>)repo).Queryable()).LongCount();

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task Should_DelegateFindFirst_When_CalledThroughNonKeyedInterface()
    {
        var ct = TestContext.Current.CancellationToken;
        using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync(ct);

        dbContext.Set<SimpleEntity>().AddRange(
            new SimpleEntity { Name = "First" },
            new SimpleEntity { Name = "Second" });
        await dbContext.SaveChangesAsync(ct);

        var repo = new EntityRepository<SimpleEntity>(dbContext);
        var found = new Query(new ExpressionQueryFilter<SimpleEntity>(x => x.Name == "First"), null).Apply(((Repository<SimpleEntity, object>)repo).Queryable()).FirstOrDefault();

        Assert.NotNull(found);
        Assert.Equal("First", found.Name);
    }

    [Fact]
    public async Task Should_DelegateFindAll_When_CalledThroughNonKeyedInterface()
    {
        var ct = TestContext.Current.CancellationToken;
        using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync(ct);

        dbContext.Set<SimpleEntity>().AddRange(
            new SimpleEntity { Name = "X" },
            new SimpleEntity { Name = "Y" });
        await dbContext.SaveChangesAsync(ct);

        var repo = new EntityRepository<SimpleEntity>(dbContext);
        var all = new Query(new ExpressionQueryFilter<SimpleEntity>(x => x.Name != ""), null).Apply(((Repository<SimpleEntity, object>)repo).Queryable()).ToList();

        Assert.Collection(all.OrderBy(x => x.Name),
            x => Assert.Equal("X", x.Name),
            x => Assert.Equal("Y", x.Name));
    }

    private SimpleDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SimpleDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new SimpleDbContext(options);
    }

    public class SimpleEntity
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class SimpleDbContext : DbContext
    {
        public SimpleDbContext(DbContextOptions<SimpleDbContext> options) : base(options)
        {
        }

        public DbSet<SimpleEntity> Entities => Set<SimpleEntity>();
    }
}
