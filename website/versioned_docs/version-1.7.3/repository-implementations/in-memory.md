# In-Memory Repository
| Feature | Status | Notes |
| ------- | :----: | ----- |
| Base Repository | ✅ | |
| Filterable | ✅ | |
| Queryable | ✅ | Native .NET `IQueryable` |
| Pageable | ✅ | |
| Tracking | ❌ | |
| Multi-tenant | ❌ | |
| Thread-Safe | ✅ | `ReaderWriterLockSlim` for concurrent access |

The `InMemoryRepository<TEntity>` class stores entities in the memory of the running process. It is intended for **testing, prototyping, and scenarios where data persistence is not required**.

## Thread Safety

`InMemoryRepository<TEntity>` is fully thread-safe. All read operations acquire a shared read lock, and all write operations acquire an exclusive write lock via `ReaderWriterLockSlim`. Multiple threads can safely read and write to the same repository instance concurrently.

## Installation

```bash
dotnet add package Kista.InMemory
```

## Registration

Use the fluent builder API to register the In-Memory driver:

```csharp
// Program.cs
builder.Services.AddRepositoryContext()
    .UseInMemory();
```

You can also configure field mappers and initial data:

```csharp
builder.Services.AddRepositoryContext()
    .UseInMemory(b => b
        .WithFieldMapper<MyEntity, MyFieldMapper>()
        .WithInitialData(new[] { new MyEntity { /* ... */ } }));
```

### Bulk vs Per-Entity

`.UseInMemory()` registers the **open generic** `InMemoryRepository<>` so that `IRepository<Person>`, `IRepository<Order>`, and any other entity type resolve to `InMemoryRepository` automatically — no per-entity class is needed:

```csharp
builder.Services.AddRepositoryContext()
    .UseInMemory();   // IRepository<Person>, IRepository<Order>, etc. all work
```

For entities that need custom query methods or overrides, create a concrete repository class and register it with `AddRepository<T>()`. The concrete type overrides the open generic for that entity only:

```csharp
builder.Services.AddRepositoryContext()
    .UseInMemory()
    .AddRepository<PersonRepository>();   // PersonRepository used for Person
                                          // InMemoryRepository used for everything else
```

### Pre-seeding Data

You can seed initial data using the `WithInitialData` method:

```csharp
var seeded = new List<MyEntity> { /* ... */ };
builder.Services.AddRepositoryContext()
    .UseInMemory(b => b.WithInitialData(seeded));
```

Or register the repository directly with pre-seeded data:

```csharp
var seeded = new List<MyEntity> { /* ... */ };
builder.Services.AddSingleton(new InMemoryRepository<MyEntity>(seeded));
```

## Lifecycle Support

The In-Memory driver provides `InMemoryRepositoryLifecycleHandler<TEntity>` for lifecycle orchestration (create, drop, seed). It is **disabled by default** and must be explicitly enabled.

### Enabling Lifecycle

```csharp
builder.Services.AddRepositoryContext()
    .UseInMemory(b => b.WithLifecycle())
    .ConfigureLifecycle(options => {
        options.SeedStrategy = SeedStrategy.Always;
    });
```

### Handler Behavior

| Operation | Behavior |
| --------- | -------- |
| `ExistsAsync` | Always returns `false` (in-memory storage is transient). |
| `CreateAsync` | No-op (no physical resource to create). |
| `DropAsync` | No-op (no physical resource to drop). |
| `SeedAsync` | Resolves the `IRepository<TEntity>` from DI and inserts data via `AddRangeAsync` / `AddAsync`. Supports `IEnumerable<TEntity>`, `IEnumerable<object>`, and single entities. |

### Seeding Examples

**Using a provider class:**

```csharp
public class ProductSeedProvider : IRepositorySeedDataProvider<Product> {
    public IEnumerable<Product> GetSeedData() {
        yield return new Product { Name = "Widget", Price = 9.99m };
        yield return new Product { Name = "Gadget", Price = 24.99m };
    }

    IEnumerable<object> IRepositorySeedDataProvider.GetSeedData()
        => GetSeedData().Cast<object>();
}

// Program.cs
builder.Services.AddRepositoryContext()
    .UseInMemory(b => b.WithLifecycle())
    .ConfigureLifecycle(options => {
        options.SeedStrategy = SeedStrategy.Always;
    })
    .WithSeedData<Product, ProductSeedProvider>();
```

**Using inline data (no provider class needed):**

```csharp
builder.Services.AddRepositoryContext()
    .UseInMemory(b => b.WithLifecycle())
    .ConfigureLifecycle(options => {
        options.SeedStrategy = SeedStrategy.Always;
    })
    .WithSeedData<Product>(new[] {
        new Product { Name = "Widget", Price = 9.99m },
        new Product { Name = "Gadget", Price = 24.99m }
    });
```

The handler resolves the provider during seeding and inserts the entities through `IRepository<Product>.AddRangeAsync`.

## Querying

`InMemoryRepository<TEntity>` exposes its query capabilities through `protected` members on the `Repository<TEntity, TKey>` base class and through extension methods on `IRepository<TEntity>`; the legacy `IQueryableRepository` / `IFilterableRepository` interfaces were removed in 1.7.1.

**With a domain-specific method (recommended):**

Define a custom repository interface and use the protected `Queryable()` hatch inside the implementation:

```csharp
public interface IMyEntityRepository : IRepository<MyEntity, Guid> {
    Task<IReadOnlyList<MyEntity>> FindActiveSortedAsync(CancellationToken ct = default);
}

public class MyEntityRepository : InMemoryRepository<MyEntity, Guid>, IMyEntityRepository {
    public async Task<IReadOnlyList<MyEntity>> FindActiveSortedAsync(CancellationToken ct = default) {
        return await Queryable()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
    }
}

// Consumer code
var items = await repo.FindActiveSortedAsync(ct);
```

**With filter types (`IRepository<T>` extensions):**

The only supported filter type is `ExpressionQueryFilter<TEntity>` (backed by a lambda expression). You can pass it directly or use the lambda shorthand — both resolve through the extension methods on `IRepository<T>`:

```csharp
// Using ExpressionQueryFilter
var query = new Query(new ExpressionQueryFilter<MyEntity>(x => x.Name == "John"));
var items = await repository.FindAllAsync(query);

// Using lambda shorthand (extension method)
var items = await repository.FindAllAsync(x => x.Name == "John");
```

> Passing an unsupported filter type (e.g., a MongoDB-specific filter) will throw `NotSupportedException`.
