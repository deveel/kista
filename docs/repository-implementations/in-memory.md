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
dotnet add package Deveel.Repository.InMemory
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

## Querying

`InMemoryRepository<TEntity>` implements both `IQueryableRepository<TEntity>` and `IFilterableRepository<TEntity>`.

**With LINQ (`IQueryableRepository`):**

```csharp
var items = repository.AsQueryable()
    .Where(x => x.IsActive)
    .OrderBy(x => x.Name)
    .ToList();
```

**With filter types (`IFilterableRepository`):**

The only supported filter type is `ExpressionQueryFilter<TEntity>` (backed by a lambda expression). You can pass it directly or use the convenience `Query.Where<TEntity>` factory:

```csharp
// Using ExpressionQueryFilter
var query = new Query(new ExpressionQueryFilter<MyEntity>(x => x.Name == "John"));
var items = await repository.FindAllAsync(query);

// Using lambda shorthand (extension method)
var items = await repository.FindAllAsync(x => x.Name == "John");
```

> Passing an unsupported filter type (e.g., a MongoDB-specific filter) will throw `NotSupportedException`.
