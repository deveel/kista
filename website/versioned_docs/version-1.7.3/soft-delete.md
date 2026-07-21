# Soft-Delete Support

Soft-delete is a logical deletion pattern where records are flagged as deleted instead of being physically removed from the data store. Kista provides transparent soft-delete support across all repository drivers: when an entity implements `ISoftDeletable`, `RemoveAsync` is automatically rewritten into an update that stamps the record, and all regular queries exclude deleted records without any extra filtering code.

| Driver | Soft-Delete | Notes |
| ------ | :---------: | ----- |
| _In-Memory_ | :white_check_mark: | Automatic — in-memory `Where` filter |
| _MongoDB_ | :white_check_mark: | Automatic — in-memory `Where` filter |
| _Entity Framework Core_ | :white_check_mark: | Automatic — requires `HasSoftDeleteFilter()` in `OnModelCreating` |

## Enabling Soft-Delete

Soft-delete is activated automatically when an entity implements the `ISoftDeletable` interface, which declares three properties:

| Property | Type | Purpose |
| -------- | ---- | ------- |
| `IsDeleted` | `bool` | Authoritative flag indicating the record is logically deleted |
| `DeletedAtUtc` | `DateTimeOffset?` | UTC timestamp of the deletion, or `null` if not deleted |
| `DeletedBy` | `string?` | Optional identifier of the actor that performed the deletion |

```csharp
using Kista;

public class TaskItem : ISoftDeletable
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;

    // ISoftDeletable
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
}
```

No registration or configuration call is required for the soft-delete behavior itself — the repository detects `ISoftDeletable` at runtime and adapts `RemoveAsync` and all query methods accordingly.

## How It Works

When an entity implements `ISoftDeletable`, the repository infrastructure changes behavior in two ways:

1. **`RemoveAsync` becomes a soft-delete.** Instead of physically removing the record, the repository stamps `IsDeleted = true` and `DeletedAtUtc = DateTimeOffset.UtcNow`, then persists the change as an update. A second `RemoveAsync` on an already soft-deleted entity returns `false` and is a no-op.

2. **Queries exclude deleted records.** Every query path (`FindAsync`, `FindAllAsync`, `FindFirstAsync`, `CountAsync`, `ExistsAsync`, `GetPageAsync`) applies a `SoftDeleteMode` filter that excludes entities where `IsDeleted == true`, unless the caller explicitly requests a different mode (see [Querying Soft-Deleted Entities](#querying-soft-deleted-entities)).

```csharp
// Soft-delete: the record is flagged, not removed
await repository.RemoveAsync(task, ct);

// FindAsync returns null — deleted records are excluded by default
var found = await repository.FindAsync(task.Id, ct);

// CountAsync and ExistsAsync also exclude deleted records
var activeCount = await repository.CountAsync(ct);
var exists = await repository.ExistsAsync(task.Id, ct);  // false
```

### Hard Deletion

To physically remove a record even when it is `ISoftDeletable`, use `HardDeleteAsync`:

```csharp
// Permanently removes the record from the data store
await repository.HardDeleteAsync(task, ct);
```

`HardDeleteAsync` bypasses the soft-delete mechanism entirely and issues a physical deletion. Bulk variants (`HardDeleteRangeAsync`) are also available, and the `HardDeleteByKeyAsync` extension methods handle the find-then-delete pattern.

## Querying Soft-Deleted Entities

Kista defines a `SoftDeleteMode` enum that controls how queries treat soft-deleted records:

| Mode | Behavior |
| ---- | -------- |
| `Default` | Exclude soft-deleted entities (the default for all queries) |
| `IncludeDeleted` | Return both active and soft-deleted entities |
| `OnlyDeleted` | Return only soft-deleted entities |

The mode is applied through two surfaces: the protected `QueryBuilder<TEntity>` returned by `Repository<TEntity, TKey>.CreateQuery()` (used inside custom repository methods), and the public `PageQuery<TEntity>` class (used directly by consumer code for paging).

### From a Custom Repository (recommended)

`CreateQuery()` is a `protected` method on `Repository<TEntity, TKey>` — it is only accessible from within a derived repository class. Expose soft-delete-aware queries as public methods on your custom repository:

```csharp
using Kista;
using Kista.EntityFramework;

public class TaskRepository : EntityRepository<TaskItem, Guid>
{
    public TaskRepository(SampleDbContext context, IServiceProvider? services = null)
        : base(context, services) { }

    // Default mode: only active tasks
    public Task<List<TaskItem>> GetActiveTasksAsync(CancellationToken ct)
        => CreateQuery()
            .OrderBy(t => t.Title)
            .ToListAsync(ct);

    // Include deleted alongside active
    public Task<List<TaskItem>> GetAllTasksIncludingDeletedAsync(CancellationToken ct)
        => CreateQuery()
            .IncludeDeleted()
            .OrderBy(t => t.Title)
            .ToListAsync(ct);

    // Only deleted tasks (useful for admin / recovery UIs)
    public Task<List<TaskItem>> GetDeletedTasksAsync(CancellationToken ct)
        => CreateQuery()
            .OnlyDeleted()
            .OrderBy(t => t.DeletedAtUtc)
            .ToListAsync(ct);
}
```

Consumer code calls the public methods — it never touches `CreateQuery()` directly:

```csharp
public class TaskService(TaskRepository repository)
{
    public Task<List<TaskItem>> ActiveTasksAsync(CancellationToken ct)
        => repository.GetActiveTasksAsync(ct);

    public Task<List<TaskItem>> DeletedTasksAsync(CancellationToken ct)
        => repository.GetDeletedTasksAsync(ct);
}
```

> **Note:** `CreateQuery()` is `protected virtual` on `Repository<TEntity, TKey>`. Calling it on an `IRepository<TEntity>` instance from outside a derived class is a compile error. Always expose query results through public methods on your custom repository. See [Customize the Repository](custom-repository/implementation.md) for the full pattern.

### Paged Queries from Consumer Code

`PageQuery<TEntity>` is a public class that carries the soft-delete mode alongside paging and sorting. Construct it directly and pass it to the public `GetPageAsync(PageRequest, CancellationToken)` method — the repository detects the `PageQuery<TEntity>` and routes it through the protected pipeline:

```csharp
using Kista;

// Only deleted tasks, paged
var page = await repository.GetPageAsync(
    new PageQuery<TaskItem>(1, 20)
        .OnlyDeleted()
        .OrderBy(t => t.DeletedAtUtc),
    ct);

foreach (var task in page.Items)
    Console.WriteLine($"{task.Title} deleted at {task.DeletedAtUtc}");
```

You can also set an explicit mode via `WithSoftDeleteMode(SoftDeleteMode)` on the query builder inside a custom repository method, or pass a `QueryOptions` instance built with `QueryOptions.WithSoftDeleteMode(mode)` directly to the protected query methods.

## Entity Framework Core Configuration

When using the EF Core driver, you must register a global query filter in your `DbContext.OnModelCreating` so that EF Core itself excludes soft-deleted rows at the database level. The `HasSoftDeleteFilter()` extension method applies the `e => !e.IsDeleted` filter to all `ISoftDeletable` entity types in the model:

```csharp
using Kista;
using Microsoft.EntityFrameworkCore;

public class SampleDbContext : DbContext
{
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    public SampleDbContext(DbContextOptions<SampleDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
        });

        // Apply the soft-delete query filter to all ISoftDeletable entities
        modelBuilder.HasSoftDeleteFilter();
    }
}
```

To filter a single entity type instead of all `ISoftDeletable` types, call `HasSoftDeleteFilter<TEntity>()` on an `EntityTypeBuilder<TEntity>`:

```csharp
modelBuilder.Entity<TaskItem>().HasSoftDeleteFilter();
```

> **Note:** The EF Core repository driver relies on the global query filter for the `Default` mode. When you request `IncludeDeleted` or `OnlyDeleted`, the driver calls `IgnoreQueryFilters()` internally, so the soft-delete filter is bypassed without any extra code on your side.

For MongoDB and In-Memory, no model configuration is needed — the filtering is applied in-memory by the repository.

## Using the Entity Manager

The `EntityManager<TEntity, TKey>` (from the `Kista.Manager` package) integrates soft-delete with audit stamping and restore operations. When you call `RemoveAsync` on an `EntityManager` for an `ISoftDeletable` entity, the manager:

1. Invokes the `OnRemovingEntityAsync` hook, which stamps `IsDeleted = true`, `DeletedAtUtc`, and `DeletedBy` (when an `IUserAccessor` is registered).
2. Calls `Repository.UpdateAsync` (not `RemoveAsync`) to persist the soft-delete.

```csharp
// Register a manager (see The Entity Manager docs for full registration details)
services.AddRepositoryContext()
    .UseEntityFramework<SampleDbContext>(b => b
        .ConfigureDbContext(opts => opts.UseSqlite("Data Source=tasks.db"))
        .WithSoftDelete())
    .AddRepository<TaskRepository>(repo => repo
        .WithManagement());

// Inject and use the manager
public class TaskService(EntityManager<TaskItem, Guid> manager)
{
    // Soft-delete: stamps IsDeleted, DeletedAtUtc, and DeletedBy
    public Task<OperationResult> DeleteAsync(Guid id, CancellationToken ct)
        => manager.RemoveByKeyAsync(id, ct);

    // Restore: clears all soft-delete stamps and updates the record
    public async Task<OperationResult> RestoreAsync(Guid id, CancellationToken ct)
    {
        var result = await manager.FindAsync(id, ct);
        if (!result.IsSuccess)
            return OperationResult.Fail(result.Error);

        return await manager.RestoreAsync(result.Value!, ct);
    }

    // Hard-delete: bypasses soft-delete and physically removes the record
    public Task<OperationResult> HardDeleteAsync(Guid id, CancellationToken ct)
        => manager.HardDeleteByKeyAsync(id, ct);
}
```

### Restore

`EntityManager.RestoreAsync` clears `IsDeleted`, `DeletedAtUtc`, and `DeletedBy`, then persists the change via `UpdateAsync`, bringing the entity back into regular query results. The `OnRestoringEntityAsync` hook is invoked before the update and can be overridden in a custom manager to add domain logic.

### Hard-Delete

`EntityManager.HardDeleteAsync` bypasses the soft-delete mechanism: it invokes the `OnHardRemovingEntityAsync` hook (which does **not** stamp the entity) and then calls `Repository.HardDeleteAsync` to physically remove the record.

### Audit Stamping (`DeletedBy`)

The `DeletedBy` property is populated from an `IUserAccessor<TKey>` registered in the DI container. If no `IUserAccessor` is registered, `DeletedBy` is left `null` and the manager logs a warning. See [User Identifier Resolution](user-entities/user-identifier-resolution.md) for details on providing a user accessor.

## Configuration

Each driver builder and the `EntityManagerBuilder` expose a `WithSoftDelete()` method that reserves a hook for future soft-delete options (currently the `SoftDeleteOptions` bag is minimal):

```csharp
builder.Services.AddRepositoryContext()
    .UseEntityFramework<SampleDbContext>(b => b
        .ConfigureDbContext(opts => opts.UseSqlServer("..."))
        .WithSoftDelete())
    .AddRepository<TaskRepository>(repo => repo
        .WithManagement(mgmt => mgmt
            .WithSoftDelete()));
```

The `WithSoftDelete()` call is optional — the soft-delete behavior is active as soon as an entity implements `ISoftDeletable`. The method is provided so that future options (such as configurable default modes or retention policies) can be set without breaking existing registration code.

## Sample Application

A complete ASP.NET Core sample demonstrating soft-delete with EF Core is available in the repository:

[`samples/Kista.SampleApp.SoftDelete`](https://github.com/deveel/kista/tree/main/samples/Kista.SampleApp.SoftDelete)

The sample shows:

- A `TaskItem` entity implementing `ISoftDeletable`
- A `SampleDbContext` calling `HasSoftDeleteFilter()` in `OnModelCreating`
- Repository registration with `WithSoftDelete()` and lifecycle support
- Minimal API endpoints for listing active, all, and deleted tasks, soft-deleting, hard-deleting, and restoring

## See Also

- [The Entity Manager](entity-manager/) — restore, hard-delete, and audit stamping on top of the repository
- [Entity Framework Core driver](repository-implementations/ef-core.md) — driver-specific configuration
- [User Identifier Resolution](user-entities/user-identifier-resolution.md) — providing an `IUserAccessor` for `DeletedBy` audit data
- [The Repository Pattern](repository-pattern.md) — query types, pagination, and the `QueryBuilder` API