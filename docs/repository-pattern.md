# The Repository Pattern
> **Renamed:** This project was renamed from **Deveel.Repository** to **Kista** on **May 26, 2025**. The name *Kista* is Old Norse for "chest" or "repository", better reflecting the project purpose as a data access framework.

The `IRepository<TEntity>` interface is the core contract of the framework. All repositories — whether provided by a driver or implemented by you — implement this interface.

The full, strongly-typed form of the contract is `IRepository<TEntity, TKey>`, where `TKey` is the type of the entity's unique identifier. The single-type-parameter shorthand `IRepository<TEntity>` is a convenience alias where `TKey` defaults to `object`.

```csharp
public interface IRepository<TEntity, TKey> where TEntity : class {
    // Retrieve the unique identifier of an entity
    TKey? GetEntityKey(TEntity entity);

    // Write operations
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<bool> RemoveAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task RemoveRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    // Key-based look-up
    Task<TEntity?> FindAsync(TKey key, CancellationToken cancellationToken = default);
}
```

## Querying the Repository

The base `IRepository<TEntity>` contract exposes a single query method: `FindAsync(key)`. This is deliberate — in Domain-Driven Design, entities are identified by a unique key, and the base repository contract reflects that.

For richer querying, the library provides extension interfaces (see below). You can also add domain-specific query methods to your own `IRepository<TEntity>` sub-interface; see [Customize the Repository](custom-repository.md) for details.

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

## Extension Interfaces

### `IFilterableRepository<TEntity>`

Extends the base repository with query-based look-ups using `IQueryFilter` and `IQuery`:

```csharp
public interface IFilterableRepository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : class {

    Task<bool> ExistsAsync(IQueryFilter filter, CancellationToken cancellationToken = default);
    Task<long> CountAsync(IQueryFilter filter, CancellationToken cancellationToken = default);
    Task<TEntity?> FindFirstAsync(IQuery query, CancellationToken cancellationToken = default);
    Task<IList<TEntity>> FindAllAsync(IQuery query, CancellationToken cancellationToken = default);
}
```

> It is up to each driver to decide which `IQueryFilter` and `IQueryOrder` types it supports. Unsupported types may raise `NotSupportedException` or `ArgumentException`.

### `IQueryableRepository<TEntity>`

Exposes the underlying storage as `IQueryable<TEntity>`, allowing LINQ queries to be composed directly:

```csharp
public interface IQueryableRepository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : class {

    IQueryable<TEntity> AsQueryable();
}
```

### `IPageableRepository<TEntity>`

Adds paginated retrieval support:

```csharp
public interface IPageableRepository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : class {

    Task<PageResult<TEntity>> GetPageAsync(
        PageQuery<TEntity> request,
        CancellationToken cancellationToken = default);
}
```

#### `PageQuery<TEntity>`

`PageQuery<TEntity>` is a fluent request object:

```csharp
// Construct with page number and page size (both ≥ 1)
var query = new PageQuery<MyEntity>(page: 1, size: 20)
    .Where(e => e.IsActive)
    .OrderBy(e => e.Name)
    .OrderByDescending(e => e.CreatedAt);
```

Key properties:

| Property | Description |
| -------- | ----------- |
| `Page` | 1-based page number. |
| `Size` | Maximum number of items per page. |
| `Offset` | Computed zero-based offset: `(Page - 1) * Size`. |
| `Query` | The inner `IQuery` composed by the fluent builder. |

#### `PageResult<TEntity>`

The result returned by `GetPageAsync`:

```csharp
public class PageResult<TEntity> where TEntity : class {
    public PageQuery<TEntity> Request { get; }
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

### `ITrackingRepository<TEntity>`

Marks a repository as capable of tracking changes on entities returned by queries. This is used internally by the `EntityManager<TEntity>` to detect whether the underlying repository can observe mutations without explicit `UpdateAsync` calls.

Driver implementations (e.g., EF Core, MongoFramework) that support change-tracking implement this interface automatically.
