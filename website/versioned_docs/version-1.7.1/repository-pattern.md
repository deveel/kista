# The Repository Pattern
The `IRepository<TEntity>` interface is the core contract of the framework. All repositories — whether provided by a driver or implemented by you — implement this interface.

The full, strongly-typed form of the contract is `IRepository<TEntity, TKey>`, where `TKey` is the type of the entity's unique identifier. The single-type-parameter shorthand `IRepository<TEntity>` is a convenience alias where `TKey` defaults to `object`.

The interface exposes mutations (`AddAsync`, `UpdateAsync`, `RemoveAsync`), key-based look-up (`FindAsync`), and unsorted pagination (`GetPageAsync`). See the [Getting Started](index.md) guide for the full interface definition.

## Design Rationale

The base `IRepository<TEntity, TKey>` contract intentionally exposes only mutations, key-based look-up, and simple unsorted pagination. This is deliberate — in Domain-Driven Design, entities are identified by a unique key, and the base repository contract reflects that.

Generic query capabilities (filtering, sorting, complex pagination) are **not** part of the public interface. Instead, they are hidden behind a `protected` hatch in the `Repository<TEntity, TKey>` abstract class. This prevents the `IQueryable<T>` leak — where LINQ expressions escape the data layer and throw `NotSupportedException` at runtime far from the repository.

If you need richer queries, you have two options:

1. **Specification Pattern** (recommended): Add domain-specific query methods to your own repository interface. See [Customize the Repository](custom-repository/).
2. **Extend `Repository`**: Inherit from the abstract base class and use its protected query methods internally.

## The `Repository<TEntity, TKey>` Abstract Class

All driver implementations (`EntityRepository`, `MongoRepository`, `InMemoryRepository`) inherit from `Repository<TEntity, TKey>`. This base class provides:

- Ready-made implementations of the `IRepository<TEntity, TKey>` mutation and look-up methods
- A `protected` `Queryable()` method that returns the underlying `IQueryable<TEntity>` — accessible **only** to subclasses
- Protected query methods that use the `Queryable()` hatch internally
- Protected hooks for engine-specific async behavior

### Protected Query Hatch

```csharp
public abstract class Repository<TEntity, TKey> : IRepository<TEntity, TKey> {
    // The IQueryable hatch — protected, never exposed to consumers
    protected abstract IQueryable<TEntity> Queryable();

    // Protected query methods available to subclasses
    protected virtual ValueTask<IReadOnlyList<TEntity>> FindAsync(IQuery query, CancellationToken ct = default);
    protected virtual ValueTask<PageQueryResult<TEntity>> QueryPageAsync(PageQuery<TEntity> request, CancellationToken ct = default);
    protected virtual ValueTask<bool> ExistsAsync(IQueryFilter filter, CancellationToken ct = default);
    protected virtual ValueTask<long> CountAsync(IQueryFilter filter, CancellationToken ct = default);
    protected virtual ValueTask<TEntity?> FindFirstAsync(IQuery query, CancellationToken ct = default);
    protected virtual ValueTask<IReadOnlyList<TEntity>> FindAllAsync(IQuery query, CancellationToken ct = default);

    // Protected hooks for engine-specific behavior
    protected virtual bool IsQueryable => false;
    protected virtual IQueryable<TEntity> NormalizeQuery(IQueryable<TEntity> queryable) => queryable;
    protected virtual ValueTask<long> CountAsync(IQueryable<TEntity> queryable, CancellationToken ct = default);
    protected virtual ValueTask<IReadOnlyList<TEntity>> ToListAsync(IQueryable<TEntity> queryable, CancellationToken ct = default);

    // Protected factory for a repository-bound query builder
    protected virtual QueryBuilder<TEntity> CreateQuery();
}
```

The `Queryable()` method is the single entry point into the data layer. Subclasses return the engine-native queryable (e.g., `DbSet<TEntity>.AsQueryable()` for EF Core, `IMongoCollection<TEntity>.AsQueryable()` for MongoDB). All query translation — filter application, sorting, pagination — happens **inside** the data layer through the protected methods above.

### Public Surface

The only query methods exposed to consumer code by `Repository` are:

| Method | Description |
|--------|-------------|
| `FindAsync(TKey key)` | Single-entity look-up by unique identifier |
| `GetPageAsync(PageRequest request)` | Simple unsorted pagination (no filter, no sort) |

All other query capabilities are `protected` and available only to subclasses.

### The `CreateQuery()` Factory

The `Repository<TEntity, TKey>` base class provides a protected `CreateQuery()` factory method that returns a `QueryBuilder<TEntity>` instance **bound** to the repository. The bound builder exposes terminal methods that dispatch through the repository's protected pipeline:

```csharp
protected virtual QueryBuilder<TEntity> CreateQuery();
```

The returned builder inherits from `QueryBuilder<TEntity>` and overrides the terminal methods to call:

| Builder Terminal | Repository Pipeline |
|-----------------|---------------------|
| `FirstOrDefaultAsync()` | `FindFirstAsync(IQuery, ...)` |
| `ToListAsync()` | `FindAllAsync(IQuery, ...)` |
| `CountAsync()` | `CountAsync(IQueryFilter, ...)` |
| `AnyAsync()` | `ExistsAsync(IQueryFilter, ...)` |
| `GetPageAsync(page, size)` | `GetPageAsync(PageQuery<TEntity>, ...)` |

Subclasses can override `CreateQuery()` to return a custom query builder that adds cross-cutting concerns (logging, caching, authorization) before query execution.

> **Thread-safety:** The builder is not thread-safe. Create a new instance per logical operation.

## The Specification Pattern

Because the base repository contract does not expose generic query capabilities, the recommended approach for domain-specific queries is the **Specification Pattern**: define purpose-built query methods on your own repository interface.

See [Interface Design](custom-repository/design.md) for a full guide on defining custom repository interfaces, and [Implementation](custom-repository/implementation.md) for how to implement them using the protected `Queryable()` hatch.

### Trade-offs: Specific vs. Generic Methods

| Approach | Risk | Flexibility | Example |
|----------|------|-------------|---------|
| **Specific method** | Low — query is encapsulated, tested, and versioned | Low — each new query needs a new method | `FindByCodeAsync(string)` |
| **Generic page query** | Higher — consumer composes arbitrary filters/sorts | High — one method covers many scenarios | `FindProductsPageAsync(PageQuery<Product>)` |

See [Query Methods](custom-repository/query-methods.md) for a detailed decision guide.

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
| `QueryBuilder<TEntity>` | A fluent builder to compose filters and sort rules for a specific entity type. Implements `IQueryBuilder<TEntity>` and provides terminal methods (`FirstOrDefaultAsync`, `ToListAsync`, `CountAsync`, `AnyAsync`, `GetPageAsync`). When obtained via `Repository.CreateQuery()`, the terminal methods are bound to the repository and execute the built query. |

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

The result returned by `GetPageAsync` includes pagination metadata:

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

The result is `PageQueryResult<TEntity>`:

```csharp
public class PageQueryResult<TEntity> where TEntity : class {
    public PageQuery<TEntity> Request { get; }
    public int TotalItems { get; }
    public IReadOnlyList<TEntity> Items { get; }
}
```

See the [driver-specific documentation](repository-implementations/) for pagination behavior per data source.

## `ITrackingRepository<TEntity>`

Marks a repository as capable of tracking changes on entities returned by queries. This is used internally by the `EntityManager<TEntity>` to detect whether the underlying repository can observe mutations without explicit `UpdateAsync` calls.

Driver implementations (e.g., EF Core, MongoFramework) that support change-tracking implement this interface automatically.

## Removed Interfaces

The following interfaces were **removed in 1.7.1** and no longer exist in the framework:

- `IQueryableRepository<TEntity, TKey>` — exposed `AsQueryable()`, leaking `IQueryable<T>` to consumers
- `IPageableRepository<TEntity, TKey>` — exposed `GetPageAsync(PageQuery<TEntity>)`, leaking query composition
- `IFilterableRepository<TEntity, TKey>` — exposed filter-based queries as a public contract

All their functionality is provided through the `protected` members of `Repository<TEntity, TKey>` and through extension methods on `IRepository<TEntity>`. Code that explicitly injected or implemented these interfaces will not compile against 1.7.1 — see the [Migration from 1.7 guide](migrating-from-1.7.md) for before/after patterns and migration steps. New code should inherit from `Repository<TEntity, TKey>` and expose domain-specific query methods instead.
