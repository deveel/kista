# Implementation

This page covers how to implement a custom repository by extending `Repository<TEntity, TKey>` and using its protected query hatch.

## Extending `Repository`

All driver implementations (`EntityRepository`, `MongoRepository`, `InMemoryRepository`) inherit from `Repository<TEntity, TKey>`. Your custom repository should do the same:

```csharp
public class ProductRepository : Repository<Product, Guid>, IProductRepository {
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context) {
        _context = context;
    }

    // --- Required abstract members ---

protected override IQueryable<Product> Queryable() => _context.Set<Product>().AsQueryable();
    protected override Guid? GetEntityKey(Product entity) => entity.Id;
    protected override IServiceProvider? Services => null;

    // --- Mutation implementations ---

    public override ValueTask AddAsync(Product entity, CancellationToken ct = default) {
        _context.Add(entity);
        return ValueTask.CompletedTask;
    }

    public override ValueTask AddRangeAsync(IEnumerable<Product> entities, CancellationToken ct = default) {
        _context.AddRange(entities);
        return ValueTask.CompletedTask;
    }

    public override ValueTask<bool> UpdateAsync(Product entity, CancellationToken ct = default) {
        _context.Update(entity);
        return new ValueTask<bool>(true);
    }

    public override ValueTask<bool> RemoveAsync(Product entity, CancellationToken ct = default) {
        _context.Remove(entity);
        return new ValueTask<bool>(true);
    }

    public override ValueTask RemoveRangeAsync(IEnumerable<Product> entities, CancellationToken ct = default) {
        _context.RemoveRange(entities);
        return ValueTask.CompletedTask;
    }

    // --- Key-based look-up ---

    public override ValueTask<Product?> FindAsync(Guid key, CancellationToken ct = default) {
        return new ValueTask<Product?>(_context.Set<Product>().FindAsync(key, cancellationToken: ct).Result);
    }

    // --- Domain-specific queries ---

    public async Task<Product?> FindByCodeAsync(string productCode, CancellationToken ct = default) {
        return await Queryable()
            .FirstOrDefaultAsync(p => p.Code == productCode, ct);
    }

    public async Task<IReadOnlyList<Product>> FindByNameAsync(string name, CancellationToken ct = default) {
        return (await Queryable()
            .Where(p => p.Name.Contains(name))
            .ToListAsync(ct)).AsReadOnly();
    }
}
```

## Required Abstract Members

Every `Repository` subclass must implement three members:

| Member | Purpose |
|--------|---------|
| `Queryable()` | Returns the `IQueryable<TEntity>` for the entity set |
| `GetEntityKey(TEntity)` | Extracts the unique identifier from an entity |
| `Services` | Returns the `IServiceProvider` for filter resolution (or `null`) |

### `Queryable()`

This is the protected hatch. It returns the engine-native queryable:

```csharp
// Entity Framework Core
protected override IQueryable<Product> Queryable() => _context.Set<Product>().AsQueryable();

// MongoDB (MongoFramework)
protected override IQueryable<Product> Queryable() => _collection.AsQueryable();

// In-Memory
protected override IQueryable<Product> Queryable() => _data.AsQueryable();
```

The `Queryable()` method is **never** exposed to consumers. It is used internally by the protected query methods and by your domain-specific query methods.

### `GetEntityKey(TEntity)`

Returns the unique identifier of an entity:

```csharp
protected override Guid? GetEntityKey(Product entity) => entity.Id;
protected override string? GetEntityKey(User entity) => entity.Email;
protected override int? GetEntityKey(Order entity) => entity.OrderNumber;
```

### `Services`

Returns the service provider for filter resolution. Most implementations return `null` or forward a DI-resolved provider:

```csharp
protected override IServiceProvider? Services => _serviceProvider;
```

This is used internally by the filter initialization pipeline. If your repository does not use dynamic filters, returning `null` is fine.

## Mutation Implementations

The mutation methods (`AddAsync`, `UpdateAsync`, `RemoveAsync`, etc.) are abstract and must be implemented by each subclass. They delegate to the underlying ORM:

```csharp
public override ValueTask AddAsync(Product entity, CancellationToken ct = default) {
    _context.Set<Product>().Add(entity);
    return ValueTask.CompletedTask;
}
```

Note that `Repository` does **not** call `SaveChanges()` — that is the responsibility of the caller or a unit-of-work pattern. The driver implementations handle this according to their engine's semantics.

## Using Protected Query Methods

`Repository` provides ready-made query methods that use the `Queryable()` hatch internally. You can call these from your domain-specific methods:

### `FindAsync(IQuery)`

Executes a query and returns matching entities:

```csharp
public async Task<IReadOnlyList<Product>> FindActiveProductsAsync(CancellationToken ct = default) {
    var query = new Query(new ExpressionQueryFilter<Product>(p => p.IsActive));
    var results = await FindAsync(query, ct);
    return results;
}
```

### `QueryPageAsync(PageQuery<TEntity>)`

Executes a paginated query with filter and sort:

```csharp
public async Task<PageQueryResult<Product>> FindProductsPagedAsync(PageQuery<Product> request, CancellationToken ct = default) {
    return await QueryPageAsync(request, ct);
}
```

### `ExistsAsync(IQueryFilter)`

Checks if any entity matches a filter:

```csharp
public async Task<bool> CodeExistsAsync(string productCode, CancellationToken ct = default) {
    var filter = new ExpressionQueryFilter<Product>(p => p.Code == productCode);
    return await ExistsAsync(filter, ct);
}
```

### `CountAsync(IQueryFilter)`

Counts entities matching a filter:

```csharp
public async Task<long> CountByCategoryAsync(string category, CancellationToken ct = default) {
    var filter = new ExpressionQueryFilter<Product>(p => p.Category == category);
    return await CountAsync(filter, ct);
}
```

### `FindFirstAsync(IQuery)`

Returns the first entity matching a query:

```csharp
public async Task<Product?> FindCheapestProductAsync(CancellationToken ct = default) {
    var query = new Query(null, new ExpressionSort<Product>(p => p.Price));
    return await FindFirstAsync(query, ct);
}
```

### `FindAllAsync(IQuery)`

Returns all entities matching a query:

```csharp
public async Task<IReadOnlyList<Product>> FindProductsSortedAsync(CancellationToken ct = default) {
    var query = new Query(null, new ExpressionSort<Product>(p => p.Name));
    var results = await FindAllAsync(query, ct);
    return results;
}
```

## Using `CreateQuery()` (Fluent Builder)

As an alternative to constructing `IQuery`/`IQueryFilter` objects manually, you can use `CreateQuery()` to obtain a `QueryBuilder<TEntity>` bound to this repository. The builder provides a fluent API and dispatches terminal methods through the repository's protected pipeline:

```csharp
public async Task<Product?> FindByNameAndCategoryAsync(string name, string category, CancellationToken ct = default) {
    return await CreateQuery()
        .Where(p => p.Name.Contains(name))
        .Where(p => p.Category == category)
        .FirstOrDefaultAsync(ct);
}

public async Task<IReadOnlyList<Product>> FindActiveSortedAsync(CancellationToken ct = default) {
    return await CreateQuery()
        .Where(p => p.IsActive)
        .OrderBy(p => p.CreatedAt)
        .ToListAsync(ct);
}

public async Task<PageResult<Product>> FindActivePagedAsync(int page, int size, CancellationToken ct = default) {
    return await CreateQuery()
        .Where(p => p.IsActive)
        .GetPageAsync(page, size, ct);
}
```

The available terminal methods are:

| Method | Dispatches To |
|--------|---------------|
| `FirstOrDefaultAsync()` | `FindFirstAsync(IQuery, ...)` |
| `ToListAsync()` | `FindAllAsync(IQuery, ...)` |
| `CountAsync()` | `CountAsync(IQueryFilter, ...)` |
| `AnyAsync()` | `ExistsAsync(IQueryFilter, ...)` |
| `GetPageAsync(page, size)` | `GetPageAsync(PageQuery<TEntity>, ...)` |

> The builder is **not thread-safe**. Create a new instance per logical operation.

## Overriding Protected Hooks

You can override protected hooks to customize engine-specific behavior:

### `IsQueryable`

Controls whether the repository supports `IQueryable`-based filtering:

```csharp
protected override bool IsQueryable => true; // default is false
```

When `true`, the protected filterable methods use `Queryable()` and apply filters as `IQueryable` operations. When `false`, subclasses must override the filterable methods to provide their own filter expansion.

### `NormalizeQuery(IQueryable<TEntity>)`

Hook to normalise the queryable before materialisation:

```csharp
protected override IQueryable<Product> NormalizeQuery(IQueryable<Product> queryable) {
    // Strip redundant sub-expressions, apply global filters, etc.
    return queryable.Where(p => !p.IsDeleted);
}
```

### `CountAsync(IQueryable<TEntity>)` and `ToListAsync(IQueryable<TEntity>)`

Override these to use engine-specific async execution:

```csharp
protected override async ValueTask<long> CountAsync(IQueryable<Product> queryable, CancellationToken ct = default) {
    return await queryable.LongCountAsync(ct);
}

protected override async ValueTask<IReadOnlyList<Product>> ToListAsync(IQueryable<Product> queryable, CancellationToken ct = default) {
    return await queryable.ToListAsync(ct);
}
```

The EF Core driver overrides these to use `IQueryable` async extensions. The MongoDB driver does the same with its async pipeline.

## Explicit Interface Implementation

`Repository` implements `IFilterableRepository<TEntity, TKey>` explicitly. The filterable methods are exposed as `protected` members and forwarded through explicit interface implementations:

```csharp
// In Repository:
protected virtual ValueTask<bool> ExistsAsync(IQueryFilter filter, CancellationToken ct = default);

ValueTask<bool> IFilterableRepository<TEntity, TKey>.ExistsAsync(IQueryFilter filter, CancellationToken ct)
    => ExistsAsync(filter, ct);
```

This means consumers who resolve `IFilterableRepository<TEntity>` (deprecated) still get working methods, but the primary access path is through your custom interface's domain-specific methods.
