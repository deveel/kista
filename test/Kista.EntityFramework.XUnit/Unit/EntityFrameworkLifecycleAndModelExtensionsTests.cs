using Kista.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace Kista;

/// <summary>
/// Unit tests for <see cref="EntityFrameworkRepositoryLifecycleHandler{TEntity}"/>
/// and the soft-delete / owner-filter EF Core model configuration extensions
/// (<see cref="SoftDeleteModelBuilderExtensions"/>,
/// <see cref="SoftDeleteEntityTypeConfiguration{TEntity}"/>,
/// <see cref="EntityTypeBuilderExtensions"/>).
/// These tests use the EF Core In-Memory provider to avoid external database
/// dependencies and exercise the public API surface only.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "EntityFrameworkLifecycle")]
public class EntityFrameworkRepositoryLifecycleHandlerTests {
    private static DbContextOptions<LifecycleDbContext> CreateInMemoryOptions(string name)
        => new DbContextOptionsBuilder<LifecycleDbContext>()
            .UseInMemoryDatabase(name)
            .Options;

    [Fact]
    public async Task ExistsAsync_ReturnsTrue_WhenDatabaseCanBeReached() {
        using var context = new LifecycleDbContext(CreateInMemoryOptions("exists-true"));
        var handler = new EntityFrameworkRepositoryLifecycleHandler<SoftDeletableDbPerson>(context);

        var exists = await handler.ExistsAsync();
        Assert.True(exists);
    }

    [Fact]
    public async Task CreateAsync_EnsureCreatedAsync_Succeeds() {
        using var context = new LifecycleDbContext(CreateInMemoryOptions("create-ok"));
        var handler = new EntityFrameworkRepositoryLifecycleHandler<SoftDeletableDbPerson>(context);

        await handler.CreateAsync();
        // No exception means success; EnsureCreatedAsync on in-memory is idempotent.
    }

    [Fact]
    public async Task DropAsync_EnsureDeletedAsync_Succeeds() {
        using var context = new LifecycleDbContext(CreateInMemoryOptions("drop-ok"));
        var handler = new EntityFrameworkRepositoryLifecycleHandler<SoftDeletableDbPerson>(context);

        await handler.CreateAsync();
        await handler.DropAsync();
    }

    [Fact]
    public async Task SeedEntitiesAsync_AddsEntitiesAndSaves() {
        using var context = new LifecycleDbContext(CreateInMemoryOptions("seed"));
        var handler = new EntityFrameworkRepositoryLifecycleHandler<SoftDeletableDbPerson>(context);

        await handler.CreateAsync();
        // SeedAsync is the public entry point that dispatches to the protected SeedEntitiesAsync.
        await handler.SeedAsync(new[] {
            new SoftDeletableDbPerson { Id = Guid.NewGuid(), FirstName = "A", LastName = "A" },
            new SoftDeletableDbPerson { Id = Guid.NewGuid(), FirstName = "B", LastName = "B" }
        });

        Assert.Equal(2, await context.Set<SoftDeletableDbPerson>().CountAsync());
    }

    [Fact]
    public async Task SeedAsync_WithSingleEntity_SeedsIt() {
        using var context = new LifecycleDbContext(CreateInMemoryOptions("seed-single"));
        var handler = new EntityFrameworkRepositoryLifecycleHandler<SoftDeletableDbPerson>(context);

        await handler.CreateAsync();
        await handler.SeedAsync(new SoftDeletableDbPerson { Id = Guid.NewGuid(), FirstName = "Solo", LastName = "S" });

        Assert.Single(await context.Set<SoftDeletableDbPerson>().ToListAsync());
    }

    [Fact]
    public async Task SeedAsync_WithNullSeedData_DoesNothing() {
        using var context = new LifecycleDbContext(CreateInMemoryOptions("seed-null"));
        var handler = new EntityFrameworkRepositoryLifecycleHandler<SoftDeletableDbPerson>(context);

        await handler.CreateAsync();
        await handler.SeedAsync(null);
        await handler.SeedAsync(new object());

        Assert.Empty(await context.Set<SoftDeletableDbPerson>().ToListAsync());
    }

    [Fact]
    public async Task SeedAsync_WithObjectEnumerable_FiltersAndSeedsMatchingTypes() {
        using var context = new LifecycleDbContext(CreateInMemoryOptions("seed-objects"));
        var handler = new EntityFrameworkRepositoryLifecycleHandler<SoftDeletableDbPerson>(context);

        await handler.CreateAsync();
        await handler.SeedAsync(new object[] {
            new SoftDeletableDbPerson { Id = Guid.NewGuid(), FirstName = "Match", LastName = "M" },
            new object()
        });

        Assert.Single(await context.Set<SoftDeletableDbPerson>().ToListAsync());
    }

    [Fact]
    public async Task ExistsAsync_WrapsExceptionsInRepositoryException() {
        // A disposed DbContext throws ObjectDisposedException when accessing Database;
        // the handler should wrap it in RepositoryException.
        var context = new LifecycleDbContext(CreateInMemoryOptions("exists-throws"));
        await context.DisposeAsync();
        var handler = new EntityFrameworkRepositoryLifecycleHandler<SoftDeletableDbPerson>(context);

        var ex = await Assert.ThrowsAsync<RepositoryException>(() => handler.ExistsAsync().AsTask());
        Assert.NotNull(ex.InnerException);
    }

    [Fact]
    public async Task CreateAsync_WrapsExceptionsInRepositoryException() {
        var context = new LifecycleDbContext(CreateInMemoryOptions("create-throws"));
        await context.DisposeAsync();
        var handler = new EntityFrameworkRepositoryLifecycleHandler<SoftDeletableDbPerson>(context);

        var ex = await Assert.ThrowsAsync<RepositoryException>(() => handler.CreateAsync().AsTask());
        Assert.NotNull(ex.InnerException);
    }

    [Fact]
    public async Task DropAsync_WrapsExceptionsInRepositoryException() {
        var context = new LifecycleDbContext(CreateInMemoryOptions("drop-throws"));
        await context.DisposeAsync();
        var handler = new EntityFrameworkRepositoryLifecycleHandler<SoftDeletableDbPerson>(context);

        var ex = await Assert.ThrowsAsync<RepositoryException>(() => handler.DropAsync().AsTask());
        Assert.NotNull(ex.InnerException);
    }
}

/// <summary>
/// Unit tests for <see cref="SoftDeleteModelBuilderExtensions"/> and
/// <see cref="SoftDeleteEntityTypeConfiguration{TEntity}"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "SoftDelete")]
public class SoftDeleteModelBuilderExtensionsTests {
    private sealed class NonSoftDeletableEntity {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class SoftDeleteDbContext : DbContext {
        public SoftDeleteDbContext(DbContextOptions<SoftDeleteDbContext> options) : base(options) { }
        public DbSet<SoftDeletableDbPerson> People => Set<SoftDeletableDbPerson>();
        public DbSet<NonSoftDeletableEntity> Others => Set<NonSoftDeletableEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            // Apply the convention-style call that covers all ISoftDeletable entities.
            modelBuilder.HasSoftDeleteFilter();
            // Apply the per-entity configuration for explicit registration coverage.
            modelBuilder.ApplyConfiguration(new SoftDeleteEntityTypeConfiguration<SoftDeletableDbPerson>());
            // Apply HasSoftDeleteFilter to a non-soft-deletable entity to cover the no-op branch.
            modelBuilder.Entity<NonSoftDeletableEntity>().HasSoftDeleteFilter();
        }
    }

    private static DbContextOptions<SoftDeleteDbContext> CreateOptions(string name)
        => new DbContextOptionsBuilder<SoftDeleteDbContext>()
            .UseInMemoryDatabase(name)
            .Options;

    [Fact]
    public void HasSoftDeleteFilter_OnNonSoftDeletableEntity_ReturnsBuilderUnchanged() {
        using var context = new SoftDeleteDbContext(CreateOptions("non-soft"));
        var model = context.Model;
        // The non-soft-deletable entity should NOT have a query filter applied.
        var entityType = model.FindEntityType(typeof(NonSoftDeletableEntity));
        Assert.NotNull(entityType);
        Assert.Null(entityType.GetQueryFilter());
    }

    [Fact]
    public void HasSoftDeleteFilter_OnSoftDeletableEntity_AppliesQueryFilter() {
        using var context = new SoftDeleteDbContext(CreateOptions("soft"));
        var entityType = context.Model.FindEntityType(typeof(SoftDeletableDbPerson));
        Assert.NotNull(entityType);
        Assert.NotNull(entityType.GetQueryFilter());
    }

    [Fact]
    public void SoftDeleteEntityTypeConfiguration_AppliesFilterWhenApplied() {
        using var context = new SoftDeleteDbContext(CreateOptions("config"));
        var entityType = context.Model.FindEntityType(typeof(SoftDeletableDbPerson));
        Assert.NotNull(entityType);
        Assert.NotNull(entityType.GetQueryFilter());
    }

    [Fact]
    public async Task SoftDeleteFilter_ExcludesDeletedRecordsFromQueries() {
        using var context = new SoftDeleteDbContext(CreateOptions("filter-query"));
        context.People.Add(new SoftDeletableDbPerson { Id = Guid.NewGuid(), FirstName = "Active", LastName = "A", IsDeleted = false });
        context.People.Add(new SoftDeletableDbPerson { Id = Guid.NewGuid(), FirstName = "Deleted", LastName = "D", IsDeleted = true });
        await context.SaveChangesAsync();

        var visible = await context.People.ToListAsync();
        Assert.Single(visible);
        Assert.Equal("Active", visible[0].FirstName);
    }

    [Fact]
    public async Task HasSoftDeleteFilter_OnModelBuilder_AppliesToAllSoftDeletableTypes() {
        using var context = new SoftDeleteDbContext(CreateOptions("all-types"));
        // Both soft-deletable entities should have a filter; non-soft-deletable should not.
        var peopleType = context.Model.FindEntityType(typeof(SoftDeletableDbPerson));
        var otherType = context.Model.FindEntityType(typeof(NonSoftDeletableEntity));
        Assert.NotNull(peopleType?.GetQueryFilter());
        Assert.Null(otherType?.GetQueryFilter());
        await Task.CompletedTask;
    }

    [Fact]
    public void HasSoftDeleteFilter_ThrowsOnNullBuilder() {
        EntityTypeBuilder<SoftDeletableDbPerson>? builder = null;
        Assert.Throws<ArgumentNullException>(() => builder!.HasSoftDeleteFilter());
    }

    [Fact]
    public void HasSoftDeleteFilter_OnModelBuilder_ThrowsOnNullBuilder() {
        ModelBuilder? modelBuilder = null;
        Assert.Throws<ArgumentNullException>(() => modelBuilder!.HasSoftDeleteFilter());
    }
}

/// <summary>
/// Unit tests for <see cref="EntityTypeBuilderExtensions.HasOwnerFilter{TEntity,TUserKey}"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "OwnerFilter")]
public class EntityTypeBuilderExtensionsTests {
    private sealed class OwnedEntity : IHaveOwner<string> {
        public Guid Id { get; set; }
        [DataOwner]
        public string OwnerId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public string Owner => OwnerId;
        public void SetOwner(string owner) => OwnerId = owner;
    }

    private sealed class OwnerDbContext : DbContext {
        public OwnerDbContext(DbContextOptions<OwnerDbContext> options) : base(options) { }
        public DbSet<OwnedEntity> Owned => Set<OwnedEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            // Cover the DataOwnerAttribute auto-discovery branch (no property name passed).
            modelBuilder.Entity<OwnedEntity>().HasOwnerFilter(new StaticUserAccessor<string>("u1"));
            // Cover the explicit-propertyName branch for string keys (uses Expression.Equal).
            modelBuilder.Entity<OwnedEntity>().HasOwnerFilter(nameof(OwnedEntity.OwnerId), new StaticUserAccessor<string>("u1"));
        }
    }

    private static DbContextOptions<OwnerDbContext> CreateOptions(string name)
        => new DbContextOptionsBuilder<OwnerDbContext>()
            .UseInMemoryDatabase(name)
            .Options;

    private sealed class StaticUserAccessor<TUserKey> : IUserAccessor<TUserKey> {
        private readonly TUserKey _userId;
        public StaticUserAccessor(TUserKey userId) { _userId = userId; }
        public TUserKey? GetUserId() => _userId;
    }

    [Fact]
    public void HasOwnerFilter_WithAttributeDiscovery_AppliesQueryFilter() {
        using var context = new OwnerDbContext(CreateOptions("attr"));
        var entityType = context.Model.FindEntityType(typeof(OwnedEntity));
        Assert.NotNull(entityType);
        Assert.NotNull(entityType.GetQueryFilter());
    }

    [Fact]
    public async Task HasOwnerFilter_FiltersRecordsByCurrentUser() {
        using var context = new OwnerDbContext(CreateOptions("filtering"));
        context.Owned.Add(new OwnedEntity { Id = Guid.NewGuid(), OwnerId = "u1", Name = "Mine" });
        context.Owned.Add(new OwnedEntity { Id = Guid.NewGuid(), OwnerId = "u2", Name = "Theirs" });
        await context.SaveChangesAsync();

        var visible = await context.Owned.ToListAsync();
        Assert.Single(visible);
        Assert.Equal("Mine", visible[0].Name);
    }

    [Fact]
    public void HasOwnerFilter_ThrowsOnUnknownPropertyName() {
        using var context = new OwnerDbContext(CreateOptions("unknown-prop"));
        var modelBuilder = new ModelBuilder();
        var entityBuilder = modelBuilder.Entity<OwnedEntity>();

        var ex = Assert.Throws<RepositoryException>(() =>
            entityBuilder.HasOwnerFilter("NonExistent", new StaticUserAccessor<string>("u1")));
        Assert.Contains("NonExistent", ex.Message);
    }

    [Fact]
    public void HasOwnerFilter_ThrowsWhenNoDataOwnerAttributeAndNoPropertyName() {
        // An entity with no [DataOwner] attribute and no explicit property name
        // triggers the "property name was not specified" branch.
        var modelBuilder = new ModelBuilder();
        var entityBuilder = modelBuilder.Entity<NoAttributeEntity>();

        var ex = Assert.Throws<RepositoryException>(() =>
            entityBuilder.HasOwnerFilter(new StaticUserAccessor<string>("u1")));
        Assert.Contains(nameof(NoAttributeEntity), ex.Message);
    }

    private sealed class NoAttributeEntity : IHaveOwner<string> {
        public Guid Id { get; set; }
        public string OwnerId { get; set; } = string.Empty;

        public string Owner => OwnerId;
        public void SetOwner(string owner) => OwnerId = owner;
    }
}

/// <summary>
/// Minimal DbContext used by the lifecycle handler tests.
/// </summary>
internal sealed class LifecycleDbContext : DbContext {
    public LifecycleDbContext(DbContextOptions<LifecycleDbContext> options) : base(options) { }
    public DbSet<SoftDeletableDbPerson> People => Set<SoftDeletableDbPerson>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<SoftDeletableDbPerson>().HasKey(e => e.Id);
    }
}