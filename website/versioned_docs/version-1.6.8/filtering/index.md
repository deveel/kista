# Filtering
The _Kista_ framework provides a flexible filtering system that allows you to query repositories using a variety of filter types, from strongly-typed lambda expressions to runtime string-based Dynamic LINQ predicates.

## Filter Types

### Expression Filters

The most common filter type is `ExpressionQueryFilter<TEntity>`, which wraps a standard LINQ lambda expression:

```csharp
var filter = new ExpressionQueryFilter<Order>(x => x.Status == "Active" && x.Total > 100);
var results = await repository.FindAllAsync(filter);
```

You can also use the `Query.Where<TEntity>` factory for convenience:

```csharp
var query = Query.Where<Order>(x => x.Status == "Active");
var results = await repository.FindAllAsync(query);
```

### Dynamic LINQ Filters

The `Kista.DynamicLinq` package adds support for string-based filter expressions using [System.Linq.Dynamic.Core](https://github.com/zzzprojects/System.Linq.Dynamic.Core):

```bash
dotnet add package Kista.DynamicLinq
```

Once installed, repositories accept `DynamicLinqFilter` instances:

```csharp
var filter = new DynamicLinqFilter("x.Status == \"Active\" && x.Total > 100");
var results = await repository.FindAllAsync(filter);
```

This is particularly useful for dynamic query builders, search APIs, and scenarios where the filter predicate is not known at compile time.

### Combined Filters

Multiple filters can be combined into a single `CombinedQueryFilter` using the `QueryFilter.Combine` method:

```csharp
var statusFilter = new DynamicLinqFilter("x.Status == \"Active\"");
var dateFilter = new DynamicLinqFilter("x.CreatedDate > DateTime(2024, 1, 1)");

var combined = QueryFilter.Combine(statusFilter, dateFilter);
var results = await repository.FindAllAsync(combined);
```

Combined filters apply all child filters with AND logic.

## Repository Filter Methods

Repositories that implement `IFilterableRepository<TEntity, TKey>` expose these filtering methods:

| Method | Description |
|--------|-------------|
| `ExistsAsync(IQueryFilter, CancellationToken)` | Checks if any entity matches the filter |
| `CountAsync(IQueryFilter, CancellationToken)` | Counts matching entities |
| `FindFirstAsync(IQuery, CancellationToken)` | Returns the first matching entity |
| `FindAllAsync(IQuery, CancellationToken)` | Returns all matching entities |

The `IQuery` interface (used by `FindFirstAsync` and `FindAllAsync`) combines a filter with optional sorting. You can build queries using the fluent `QueryBuilder<TEntity>`, typically obtained from a repository via `CreateQuery()`:

```csharp
// Inside a Repository subclass (recommended)
public async Task<IReadOnlyList<Order>> FindActiveOrdersAsync(CancellationToken ct = default) {
    return await CreateQuery()
        .Where(x => x.Status == "Active")
        .OrderBy(x => x.CreatedDate)
        .ToListAsync(ct);
}
```

The builder's terminal methods dispatch through the repository's protected pipeline. Alternatively, you can construct a standalone query object and pass it to the protected methods:

```csharp
// Standalone query passed to a protected method
var query = new QueryBuilder<Order>()
    .Where(x => x.Status == "Active")
    .OrderBy(x => x.CreatedDate);

var results = await FindAllAsync(query, ct);
```

> `QueryBuilder<TEntity>` implements `IQueryBuilder<TEntity>` (which extends `IQuery`), so a standalone builder can be passed to methods that accept `IQuery`. Terminal methods on an unbound builder throw `InvalidOperationException`.

## Filter Context and Automatic Service Resolution

When a repository applies a filter to a query, it initializes the filter with an `IFilterContext` that provides access to the repository's service provider. This enables filters to resolve infrastructure services automatically:

```csharp
public interface IFilterContext {
    IServiceProvider Services { get; }
}

public interface IQueryFilter {
    void Initialize(IFilterContext context) { }
}
```

The `Initialize` method has a default no-op implementation, so existing filter implementations continue to work without modification. Filters that need infrastructure services can override it:

```csharp
public class MyFilter : IQueryFilter {
    private IExpressionCache? _cache;

    public void Initialize(IFilterContext context) {
        _cache = context.Services.GetService<IExpressionCache>();
    }
}
```

### How It Works in Practice

1. The repository receives a filter in a method like `FindAllAsync(filter)`
2. If the repository has an `IServiceProvider`, it creates a `DefaultFilterContext`
3. It calls `filter.Initialize(context)` before applying the filter
4. The filter resolves any services it needs (e.g., expression cache)
5. For `CombinedQueryFilter`, initialization propagates to all child filters

### Enabling Automatic Resolution

All built-in repository implementations accept an optional `IServiceProvider` in their constructors:

```csharp
// InMemoryRepository
var repo = new InMemoryRepository<Order>(services: serviceProvider);

// EntityRepository
var repo = new EntityRepository<Order, Guid>(dbContext, services: serviceProvider);

// MongoRepository
var repo = new MongoRepository<Order, string>(mongoContext, services: serviceProvider);
```

When `Services` is non-null, the repository initializes all filters automatically.

## Extension Methods

The `RepositoryExtensions` class provides convenience overloads for common filtering patterns:

```csharp
// Expression-based exists check
bool exists = await repository.ExistsAsync(x => x.Status == "Active");

// Expression-based count
long count = await repository.CountAsync(x => x.Total > 100);

// IQueryFilter-based (works with DynamicLinqFilter, CombinedQueryFilter, etc.)
bool exists = await repository.ExistsAsync(filter);
long count = await repository.CountAsync(filter);
```

## Next Steps

- [Filter Cache](filter-cache.md) — Bounded expression caching for high-throughput Dynamic LINQ queries with automatic service resolution
