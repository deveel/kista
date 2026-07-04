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

        var repo = new TestSimpleEntityRepository(dbContext);
        var exists = await ((ITestRepository<SimpleEntity, object>)repo).ExistsAsync(new ExpressionQueryFilter<SimpleEntity>(x => x.Name == "Existing"));

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

        var repo = new TestSimpleEntityRepository(dbContext);
        var count = await ((ITestRepository<SimpleEntity, object>)repo).CountAsync(new ExpressionQueryFilter<SimpleEntity>(x => x.Name != ""));

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

        var repo = new TestSimpleEntityRepository(dbContext);
        var found = await ((ITestRepository<SimpleEntity, object>)repo).FindFirstAsync(new Query(new ExpressionQueryFilter<SimpleEntity>(x => x.Name == "First"), null));

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

        var repo = new TestSimpleEntityRepository(dbContext);
        var all = await ((ITestRepository<SimpleEntity, object>)repo).FindAllAsync(new Query(new ExpressionQueryFilter<SimpleEntity>(x => x.Name != ""), null));

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

    private sealed class TestSimpleEntityRepository : EntityRepository<SimpleEntity>, ITestRepository<SimpleEntity, object> {
        public TestSimpleEntityRepository(DbContext context) : base(context) { }

        ValueTask<SimpleEntity?> ITestRepository<SimpleEntity, object>.FindFirstAsync(IQuery query, CancellationToken cancellationToken)
            => FindFirstAsync(query, cancellationToken);

        ValueTask<IReadOnlyList<SimpleEntity>> ITestRepository<SimpleEntity, object>.FindAllAsync(IQuery query, CancellationToken cancellationToken)
            => FindAllAsync(query, cancellationToken);

        ValueTask<long> ITestRepository<SimpleEntity, object>.CountAsync(IQueryFilter filter, CancellationToken cancellationToken)
            => CountAsync(filter, cancellationToken);

        ValueTask<bool> ITestRepository<SimpleEntity, object>.ExistsAsync(IQueryFilter filter, CancellationToken cancellationToken)
            => ExistsAsync(filter, cancellationToken);

        IQueryable<SimpleEntity> ITestRepository<SimpleEntity, object>.Queryable() => Queryable();
    }
}
