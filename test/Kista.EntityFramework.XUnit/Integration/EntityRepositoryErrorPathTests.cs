using System.ComponentModel.DataAnnotations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kista;

[Trait("Category", "Integration")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "EntityRepository")]
public class EntityRepositoryErrorPathTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public EntityRepositoryErrorPathTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }

    private TestDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new TestDbContext(options);
    }

    [Fact]
    public void Should_ThrowRepositoryException_When_EntityHasNoKey()
    {
        using var dbContext = CreateDbContext();

        Assert.Throws<RepositoryException>(() => new EntityRepository<NoKeyEntity, int>(dbContext));
    }

    [Fact]
    public async Task Should_ThrowRepositoryException_When_AddingDuplicateKey()
    {
        var ct = TestContext.Current.CancellationToken;
        using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync(ct);

        dbContext.Set<SimpleEntity>().Add(new SimpleEntity { Id = 1, Name = "Original" });
        await dbContext.SaveChangesAsync(ct);

        var repo = new EntityRepository<SimpleEntity, int>(dbContext);

        await Assert.ThrowsAsync<RepositoryException>(() =>
            repo.AddAsync(new SimpleEntity { Id = 1, Name = "Duplicate" }, ct).AsTask());
    }

    [Fact]
    public async Task Should_ReturnFalse_When_RemovingDetachedEntity()
    {
        var ct = TestContext.Current.CancellationToken;
        using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync(ct);

        dbContext.Set<SimpleEntity>().Add(new SimpleEntity { Id = 1, Name = "Test" });
        await dbContext.SaveChangesAsync(ct);

        var repo = new EntityRepository<SimpleEntity, int>(dbContext);
        var detached = new SimpleEntity { Id = 1, Name = "Test" };

        var result = await repo.RemoveAsync(detached, ct);

        Assert.True(result);
    }

    [Fact]
    public async Task Should_ReturnFalse_When_RemovingNonExistentEntity()
    {
        var ct = TestContext.Current.CancellationToken;
        using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync(ct);

        var repo = new EntityRepository<SimpleEntity, int>(dbContext);
        var nonExistent = new SimpleEntity { Id = 999, Name = "Ghost" };

        var result = await repo.RemoveAsync(nonExistent, ct);

        Assert.False(result);
    }

    [Fact]
    public async Task Should_ReturnFalse_When_UpdatingDetachedEntity()
    {
        var ct = TestContext.Current.CancellationToken;
        using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync(ct);

        dbContext.Set<SimpleEntity>().Add(new SimpleEntity { Id = 1, Name = "Original" });
        await dbContext.SaveChangesAsync(ct);

        var repo = new EntityRepository<SimpleEntity, int>(dbContext);
        var detached = new SimpleEntity { Id = 1, Name = "Updated" };

        var result = await repo.UpdateAsync(detached, ct);

        Assert.True(result);
    }

    [Fact]
    public async Task Should_ReturnFalse_When_UpdatingNonExistentEntity()
    {
        var ct = TestContext.Current.CancellationToken;
        using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync(ct);

        var repo = new EntityRepository<SimpleEntity, int>(dbContext);
        var nonExistent = new SimpleEntity { Id = 999, Name = "Ghost" };

        var result = await repo.UpdateAsync(nonExistent, ct);

        Assert.False(result);
    }

    [Fact]
    public async Task Should_ReturnNull_When_FindingNonExistentKey()
    {
        var ct = TestContext.Current.CancellationToken;
        using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync(ct);

        var repo = new EntityRepository<SimpleEntity, int>(dbContext);

        var result = await repo.FindAsync(999, ct);

        Assert.Null(result);
    }

    [Fact]
    public async Task Should_ThrowRepositoryException_When_ExistsAsyncFails()
    {
        var ct = TestContext.Current.CancellationToken;
        using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync(ct);

        var repo = new EntityRepository<SimpleEntity, int>(dbContext);
        dbContext.Dispose();

        await Assert.ThrowsAsync<RepositoryException>(() =>
            repo.ExistsAsync(QueryFilter.Empty, ct).AsTask());
    }

    [Fact]
    public async Task Should_ThrowRepositoryException_When_CountAsyncFails()
    {
        var ct = TestContext.Current.CancellationToken;
        using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync(ct);

        var repo = new EntityRepository<SimpleEntity, int>(dbContext);
        dbContext.Dispose();

        await Assert.ThrowsAsync<RepositoryException>(() =>
            repo.CountAsync(QueryFilter.Empty, ct).AsTask());
    }

    [Fact]
    public async Task Should_ThrowRepositoryException_When_FindFirstAsyncFails()
    {
        var ct = TestContext.Current.CancellationToken;
        using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync(ct);

        var repo = new EntityRepository<SimpleEntity, int>(dbContext);
        dbContext.Dispose();

        await Assert.ThrowsAsync<RepositoryException>(() =>
            repo.FindFirstAsync(Query.Empty, ct).AsTask());
    }

    [Fact]
    public async Task Should_ThrowRepositoryException_When_FindAllAsyncFails()
    {
        var ct = TestContext.Current.CancellationToken;
        using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync(ct);

        var repo = new EntityRepository<SimpleEntity, int>(dbContext);
        dbContext.Dispose();

        await Assert.ThrowsAsync<RepositoryException>(() =>
            repo.FindAllAsync(Query.Empty, ct).AsTask());
    }

    [Fact]
    public async Task Should_ThrowRepositoryException_When_GetPageAsyncFails()
    {
        var ct = TestContext.Current.CancellationToken;
        using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync(ct);

        var repo = new EntityRepository<SimpleEntity, int>(dbContext);
        dbContext.Dispose();

        await Assert.ThrowsAsync<RepositoryException>(() =>
            repo.GetPageAsync(new PageRequest(1, 10), ct).AsTask());
    }

    [Fact]
    public async Task Should_ThrowRepositoryException_When_AddRangeAsyncFails()
    {
        var ct = TestContext.Current.CancellationToken;
        using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync(ct);

        var repo = new EntityRepository<SimpleEntity, int>(dbContext);
        dbContext.Dispose();

        await Assert.ThrowsAsync<RepositoryException>(() =>
            repo.AddRangeAsync(new[] { new SimpleEntity { Id = 1, Name = "Test" } }, ct).AsTask());
    }

    [Fact]
    public async Task Should_ThrowRepositoryException_When_RemoveRangeAsyncFails()
    {
        var ct = TestContext.Current.CancellationToken;
        using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync(ct);

        dbContext.Set<SimpleEntity>().Add(new SimpleEntity { Id = 1, Name = "Test" });
        await dbContext.SaveChangesAsync(ct);

        var repo = new EntityRepository<SimpleEntity, int>(dbContext);
        dbContext.Dispose();

        await Assert.ThrowsAsync<RepositoryException>(() =>
            repo.RemoveRangeAsync(new[] { new SimpleEntity { Id = 1 } }, ct).AsTask());
    }

    [Fact]
    public async Task Should_ThrowRepositoryException_When_UpdateAsyncFails()
    {
        var ct = TestContext.Current.CancellationToken;
        using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync(ct);

        dbContext.Set<SimpleEntity>().Add(new SimpleEntity { Id = 1, Name = "Test" });
        await dbContext.SaveChangesAsync(ct);

        var repo = new EntityRepository<SimpleEntity, int>(dbContext);
        dbContext.Dispose();

        await Assert.ThrowsAsync<RepositoryException>(() =>
            repo.UpdateAsync(new SimpleEntity { Id = 1, Name = "Updated" }, ct).AsTask());
    }

    [Fact]
    public async Task Should_ThrowRepositoryException_When_FindAsyncFails()
    {
        var ct = TestContext.Current.CancellationToken;
        using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync(ct);

        var repo = new EntityRepository<SimpleEntity, int>(dbContext);
        dbContext.Dispose();

        await Assert.ThrowsAsync<RepositoryException>(() =>
            repo.FindAsync(1, ct).AsTask());
    }

    [Fact]
    public async Task Should_ThrowRepositoryException_When_FindOriginalAsyncFails()
    {
        var ct = TestContext.Current.CancellationToken;
        using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync(ct);

        var repo = new EntityRepository<SimpleEntity, int>(dbContext);
        dbContext.Dispose();

        await Assert.ThrowsAsync<RepositoryException>(() =>
            repo.FindOriginalAsync(1, ct).AsTask());
    }

    [Fact]
    public async Task Should_Throw_When_ConstructorWithNullContext()
    {
        Assert.Throws<ArgumentNullException>(() => new EntityRepository<SimpleEntity, int>(null!));
    }

    [Fact]
    public async Task Should_Throw_When_EntityHasCompositeKey()
    {
        var ct = TestContext.Current.CancellationToken;
        using var dbContext = CreateCompositeKeyDbContext();
        await dbContext.Database.EnsureCreatedAsync(ct);

        Assert.Throws<NotSupportedException>(() => new EntityRepository<CompositeKeyEntity, int>(dbContext));
    }

    private TestDbContext CreateCompositeKeyDbContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new TestDbContext(options);
    }

    public class SimpleEntity
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class NoKeyEntity
    {
        public string Name { get; set; } = string.Empty;
    }

    public class CompositeKeyEntity
    {
        public int Key1 { get; set; }
        public int Key2 { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        public DbSet<SimpleEntity> Entities => Set<SimpleEntity>();
        public DbSet<NoKeyEntity> NoKeyEntities => Set<NoKeyEntity>();
        public DbSet<CompositeKeyEntity> CompositeKeyEntities => Set<CompositeKeyEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<NoKeyEntity>().HasNoKey();
            modelBuilder.Entity<CompositeKeyEntity>().HasKey(x => new { x.Key1, x.Key2 });
        }
    }
}
