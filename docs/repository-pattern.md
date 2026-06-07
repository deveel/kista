# The Repository Pattern
> **Renamed:** This project was renamed from **Deveel.Repository** to **Kista** on **May 26, 2025**. The name *Kista* is Old Norse for "chest" or "repository", better reflecting the project purpose as a data access framework.

The `IRepository<TEntity>` interface is the core contract of the framework. All repositories — whether provided by a driver or implemented by you — implement this interface.

The full, strongly-typed form of the contract is `IRepository<TEntity, TKey>`, where `TKey` is the type of the entity's unique identifier. The single-type-parameter shorthand `IRepository<TEntity>` is a convenience alias where `TKey` defaults to `object`.

```csharp
public interface IRepository<TEntity, TKey> where TEntity : class {
    // Retrieve the unique identifier of an entity
    TKey? GetEntityKey(TEntity entity);

    // Write operations
    ValueTask AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    ValueTask AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    ValueTask<bool> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    ValueTask<bool> RemoveAsync(TEntity entity, CancellationToken cancellationToken = default);
    ValueTask RemoveRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    // Key-based look-up
    ValueTask<TEntity?> FindAsync(TKey key, CancellationToken cancellationToken = default);

    // Unsorted pagination
    ValueTask<PageResult<TEntity>> GetPageAsync(PageRequest request, CancellationToken cancellationToken = default);
}
```

## Design Rationale

The base `IRepository<TEntity, TKey>` contract intentionally exposes only mutations, key-based look-up, and simple unsorted pagination. This is deliberate — in Domain-Driven Design, entities are identified by a unique key, and the base repository contract reflects that.

Generic query capabilities (filtering, sorting, complex pagination) are **not** part of the public interface. Instead, they are hidden behind a `protected` hatch in the `Repository<TEntity, TKey>` abstract class. This prevents the `IQueryable<T>` leak — where LINQ expressions escape the data layer and throw `NotSupportedException` at runtime far from the repository.

If you need richer queries, you have two options:

1. **Specification Pattern** (recommended): Add domain-specific query methods to your own repository interface. See [Customize the Repository](custom-repository/).
2. **Extend `Repository`**: Inherit from the abstract base class and use its protected query methods internally.

## The `Repository<TEntity, TKey>` Abstract Class

All driver implementations (`EntityRepository`, `MongoRepository`, `InMemoryRepository`) inherit from `Repository<TEntity, TKey>`. This base class provides:

- Ready-made implementations of the `IRepository<TEntity, TKey>` mutation and look-up methods
- A `protected` `Query()` method that returns the underlying `IQueryable<TEntity>` — accessible **only** to subclasses
- Protected query methods that use the `Query()` hatch internally
- Protected hooks for engine-specific async behavior

### Protected Query Hatch

```csharp
public abstract class Repository<TEntity, TKey> : IRepository<TEntity, TKey> {
    // The IQueryable hatch — protected, never exposed to consumers
    protected abstract IQueryable<TEntity> Query();

    // Protected query methods available to subclasses
    protected virtual ValueTask<IList<TEntity>> FindAsync(IQuery query, CancellationToken ct = default);
    protected virtual ValueTask<PageQueryResult<TEntity>> QueryPageAsync(PageQuery<TEntity> request, CancellationToken ct = default);
    protected virtual ValueTask<bool> ExistsAsync(IQueryFilter filter, CancellationToken ct = default);
    protected virtual ValueTask<long> CountAsync(IQueryFilter filter, CancellationToken ct = default);
    protected virtual ValueTask<TEntity?> FindFirstAsync(IQuery query, CancellationToken ct = default);
    protected virtual ValueTask<IList<TEntity>> FindAllAsync(IQuery query, CancellationToken ct = default);

    // Protected hooks for engine-specific behavior
    protected virtual bool IsQueryable => false;
    protected virtual IQueryable<TEntity> NormalizeQuery(IQueryable<TEntity> queryable) => queryable;
    protected virtual ValueTask<long> CountAsync(IQueryable<TEntity> queryable, CancellationToken ct = default);
    protected virtual ValueTask<IList<TEntity>> ToListAsync(IQueryable<TEntity> queryable, CancellationToken ct = default);
}
```

The `Query()` method is the single entry point into the data layer. Subclasses return the engine-native queryable (e.g., `DbSet<TEntity>.AsQueryable()` for EF Core, `IMongoCollection<TEntity>.AsQueryable()` for MongoDB). All query translation — filter application, sorting, pagination — happens **inside** the data layer through the protected methods above.

### Public Surface

The only query methods exposed to consumer code by `Repository` are:

| Method | Description |
|--------|-------------|
| `FindAsync(TKey key)` | Single-entity look-up by unique identifier |
| `GetPageAsync(PageRequest request)` | Simple unsorted pagination (no filter, no sort) |

All other query capabilities are `protected` and available only to subclasses.

## The Specification Pattern

Because the base repository contract does not expose generic query capabilities, the recommended approach for domain-specific queries is the **Specification Pattern**: define purpose-built query methods on your own repository interface.

### Example: Product Repository

```csharp
// Domain interface
public interface IProductRepository : IRepository<Product, Guid> {
    // Specific, safe queries
    Task<Product?> FindByCodeAsync(string productCode, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> FindByNameAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> FindByCategoryAsync(string category, CancellationToken ct = default);
    Task<bool> CodeExistsAsync(string productCode, CancellationToken ct = default);
}
```

The implementation extends `Repository` and uses the protected `Query()` hatch internally:

```csharp
public class ProductRepository : Repository<Product, Guid>, IProductRepository {
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context) {
        _context = context;
    }

    protected override IQueryable<Product> Query() => _context.Set<Product>().AsQueryable();
    protected override Guid? GetEntityKey(Product entity) => entity.Id;
    protected override IServiceProvider? Services => null;

    // Mutation implementations delegate to the ORM
    public override ValueTask AddAsync(Product entity, CancellationToken ct = default) {
        _context.Add(entity);
        return ValueTask.CompletedTask;
    }
    // ... other mutations ...

    public override ValueTask<Product?> FindAsync(Guid key, CancellationToken ct = default) {
        return new ValueTask<Product?>(_context.Set<Product>().FindAsync(key).Result);
    }

    // Domain-specific queries using the protected Query() hatch
    public async Task<Product?> FindByCodeAsync(string productCode, CancellationToken ct = default) {
        return await Query()
            .FirstOrDefaultAsync(p => p.Code == productCode, ct);
    }

    public async Task<IReadOnlyList<Product>> FindByNameAsync(string name, CancellationToken ct = default) {
        return (await Query()
            .Where(p => p.Name.Contains(name))
            .ToListAsync(ct)).AsReadOnly();
    }

    public async Task<IReadOnlyList<Product>> FindByCategoryAsync(string category, CancellationToken ct = default) {
        return (await Query()
            .Where(p => p.Category == category)
            .ToListAsync(ct)).AsReadOnly();
    }

    public async Task<bool> CodeExistsAsync(string productCode, CancellationToken ct = default) {
        return await Query().AnyAsync(p => p.Code == productCode, ct);
    }
}
```

### Trade-offs: Specific vs. Generic Methods

| Approach | Risk | Flexibility | Example |
|----------|------|-------------|---------|
| **Specific method** | Low — query is encapsulated, tested, and versioned | Low — each new query needs a new method | `FindByCodeAsync(string)` |
| **Generic page query** | Higher — consumer composes arbitrary filters/sorts | High — one method covers many scenarios | `FindProductsPageAsync(PageQuery<Product>)` |

You can expose a generic paginated query method on your repository if the use case justifies it:

```csharp
public interface IProductRepository : IRepository<Product, Guid> {
    // Specific queries
    Task<Product?> FindByCodeAsync(string productCode, CancellationToken ct = default);

    // Generic paginated query — risk accepted by domain owner
    Task<PageQueryResult<Product>> FindProductsPageAsync(PageQuery<Product> request, CancellationToken ct = default);
}
```

The implementation uses the protected `QueryPageAsync` method:

```csharp
public class ProductRepository : Repository<Product, Guid>, IProductRepository {
    // ...

    public async Task<PageQueryResult<Product>> FindProductsPageAsync(PageQuery<Product> request, CancellationToken ct = default) {
        return await QueryPageAsync(request, ct);
    }
}
```

This approach keeps the `IQueryable<T>` leak contained within the repository — the consumer never sees `AsQueryable()`, only the domain-specific methods you choose to expose.

## Query Types

### The `IQuery` Interface

Queries defined in this library are built around the `IQuery` interface, which encapsulates a filter and an optional sort order:

```csharp
public interface IQuery {
    IQueryFilter? Filter { get; }
    IQueryOrder? Order { get; }
}
```

The library provides helper types to construct queries:

| Type | Description |
| ---- | ----------- |
| `Query` | A simple immutable query struct wrapping a filter and an optional sort. |
| `PageQuery<TEntity>` | A paginated query with page number, page size, and optional filter / sort via a fluent builder. |
| `QueryBuilder<TEntity>` | A fluent builder to compose filters and sort rules for a specific entity type. |

### The `IQueryFilter` Interface

`IQueryFilter` is a marker interface. The library ships the following built-in filter types:

| Filter | Description |
| ------ | ----------- |
| `ExpressionQueryFilter<TEntity>` | Backed by a lambda `Expression<Func<TEntity, bool>>`. |
| `CombinedQueryFilter` | Combines two or more filters with a logical AND. |
| `QueryFilter.Empty` | A no-op filter (returns all items). Useful for composition or coalescing. |

Driver packages may provide their own filter types — for example, `Kista.MongoFramework` provides a `MongoGeoDistanceFilter`.

### The `IQueryOrder` Interface

`IQueryOrder` is a marker interface for sort rules. Built-in sort types:

| Sort | Description |
| ---- | ----------- |
| `ExpressionSort<TEntity>` | Sorts by a lambda expression `Expression<Func<TEntity, object?>>`, with optional `SortDirection`. |
| `CombinedOrder` | Combines two or more sort rules. |
| `FieldOrder` | Sorts by a field name string, with a `SortDirection`. Requires a field-mapper when used with `IQueryable`. |

## Pagination

### `PageRequest`

`PageRequest` is a simple request object for unsorted pagination:

```csharp
public class PageRequest {
    public int Page { get; }       // 1-based page number
    public int Size { get; }       // Maximum items per page
    public int Offset { get; }     // Computed: (Page - 1) * Size
}
```

### `PageResult<TEntity>`

The result returned by `GetPageAsync`:

```csharp
public class PageResult<TEntity> where TEntity : class {
    public PageRequest Request { get; }
    public int TotalItems { get; }
    public IReadOnlyList<TEntity>? Items { get; }

    public int TotalPages { get; }
    public bool IsFirstPage { get; }
    public bool IsLastPage { get; }
    public bool HasNextPage { get; }
    public bool HasPreviousPage { get; }
    public int? NextPage { get; }
    public int? PreviousPage { get; }
}
```

### `PageQuery<TEntity>` and `PageQueryResult<TEntity>`

For filtered and sorted pagination, use `PageQuery<TEntity>` internally within your repository:

```csharp
var query = new PageQuery<Product>(page: 1, size: 20)
    .Where(p => p.IsActive)
    .OrderBy(p => p.Name);
```

| Property | Description |
| -------- | ----------- |
| `Page` | 1-based page number. |
| `Size` | Maximum number of items per page. |
| `Offset` | Computed zero-based offset: `(Page - 1) * Size`. |
| `Query` | The inner `IQuery` composed by the fluent builder. |

The result is `PageQueryResult<TEntity>`:

```csharp
public class PageQueryResult<TEntity> where TEntity : class {
    public PageQuery<TEntity> Request { get; }
    public int TotalItems { get; }
    public IReadOnlyList<TEntity> Items { get; }
}
```

## `ITrackingRepository<TEntity>`

Marks a repository as capable of tracking changes on entities returned by queries. This is used internally by the `EntityManager<TEntity>` to detect whether the underlying repository can observe mutations without explicit `UpdateAsync` calls.

Driver implementations (e.g., EF Core, MongoFramework) that support change-tracking implement this interface automatically.

## Deprecated Interfaces

The following interfaces are marked `[Obsolete]` and should not be used in new code:

- `IQueryableRepository<TEntity, TKey>` — exposed `AsQueryable()`, leaking `IQueryable<T>` to consumers
- `IPageableRepository<TEntity, TKey>` — exposed `GetPageAsync(PageQuery<TEntity>)`, leaking query composition
- `IFilterableRepository<TEntity, TKey>` — exposed filter-based queries as a public contract

All their functionality is now provided through the `protected` members of `Repository<TEntity, TKey>`. Existing code using these interfaces will continue to compile (the obsolete attribute is non-breaking), but new code should inherit from `Repository` and implement domain-specific query methods instead.
