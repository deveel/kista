# Migrating from 1.7 to 1.7.1

The **1.7.1 — Obsoletes Cleanup** release removes the long-deprecated `IQueryableRepository`, `IPageableRepository`, and `IFilterableRepository` interfaces (in both `<TEntity>` and `<TEntity, TKey>` arities), together with their associated extension classes:

- `Kista.PageableRepositoryExtensions`
- `Kista.QueryableRepositoryExtensions`
- The `AsFilterable()` and `AsQueryable()` helper extensions on `RepositoryExtensions`

All functionality they provided is available through the `Repository<TEntity, TKey>` base class (via `protected` members) and through extension methods on `IRepository<TEntity>`. This is a **breaking** change for code that explicitly implemented, injected, or called the removed types — such code will no longer compile.

## Why the interfaces were removed

The three legacy interfaces exposed query internals (`IQueryable<T>`, `PageQuery<T>`, `IQueryFilter`) as a public contract, leaking the data layer into consumer code. The same capabilities are already provided by the `Repository<TEntity, TKey>` abstract class through `protected` members, which keeps query composition inside the data layer and avoids the `IQueryable<T>` leak that produced `NotSupportedException`s far from the repository.

See the [Repository Pattern](repository-pattern.md) page for the design rationale, and [Customize the Repository](custom-repository/) for the recommended patterns going forward.

## Migration table

| Before (1.7.0) | After (1.7.1) |
| -------------- | ------------- |
| Inject `IQueryableRepository<T>` / `IQueryableRepository<T, TKey>` | Inject `IRepository<T>` and call a domain-specific method on a custom repository interface |
| Inject `IPageableRepository<T>` / `IPageableRepository<T, TKey>` | Inject `IRepository<T>` and call `GetPageAsync(PageRequest)` for unsorted pagination, or a domain-specific paged method for filtered/sorted pages |
| Inject `IFilterableRepository<T>` / `IFilterableRepository<T, TKey>` | Inject `IRepository<T>` and call extension methods (`FindAllAsync`, `CountAsync`, `ExistsAsync`, `FindFirstAsync`) that accept `IQueryFilter` / lambda expressions |
| `repository.AsQueryable()` | Define a domain-specific method on a custom repository (e.g. `IProductRepository.FindDiscontinuedAsync`) implemented via the `protected Queryable()` hatch |
| `repository.AsFilterable()` | Call `FindAllAsync`, `CountAsync`, `ExistsAsync`, `FindFirstAsync` directly on `IRepository<T>` (extension methods) or on the `Repository<T,TKey>` base class from a subclass |
| `repository.GetPageAsync(PageQuery<T>)` | `repository.GetPageAsync(PageRequest)` for unsorted pages; for filtered/sorted pages expose a domain-specific paged method implemented via the protected `QueryPageAsync(PageQuery<T>)` |
| Class `: Repository<T, TKey>, IQueryableRepository<T, TKey>` | Class `: Repository<T, TKey>` only — the `Repository` base class already provides the query capabilities through `protected` members |

## Replace `AsQueryable()` consumer code

The `AsQueryable()` extension leaked the underlying `IQueryable<T>` to consumers. Replace it with a domain-specific method on a custom repository, implemented through the protected `Queryable()` hatch.

**Before (1.7.0):**

```csharp
// Consumer code — leaks IQueryable
public class ProductSearch {
    private readonly IQueryableRepository<Product> _products;

    public ProductSearch(IQueryableRepository<Product> products) {
        _products = products;
    }

    public IReadOnlyList<Product> ActiveSorted() {
        return _products.AsQueryable()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToList();
    }
}
```

**After (1.7.1):**

```csharp
// Domain interface — query is encapsulated
public interface IProductRepository : IRepository<Product, Guid> {
    Task<IReadOnlyList<Product>> FindActiveSortedAsync(CancellationToken ct = default);
}

// Implementation — uses the protected Queryable() hatch
public class ProductRepository : InMemoryRepository<Product, Guid>, IProductRepository {
    public async Task<IReadOnlyList<Product>> FindActiveSortedAsync(CancellationToken ct = default) {
        return await Queryable()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);
    }
}

// Consumer code — depends on the domain contract, not on IQueryable
public class ProductSearch {
    private readonly IProductRepository _products;

    public ProductSearch(IProductRepository products) {
        _products = products;
    }

    public Task<IReadOnlyList<Product>> ActiveSorted(CancellationToken ct = default)
        => _products.FindActiveSortedAsync(ct);
}
```

## Replace `AsFilterable()` and filter-based queries

Filter-based queries no longer require resolving `IFilterableRepository<T>`. Call the extension methods on `IRepository<T>` directly, or use the `protected` members on a `Repository<T, TKey>` subclass.

**Before (1.7.0):**

```csharp
var filterable = repository.AsFilterable();
var items = await filterable.FindAllAsync(filter);
var count = await filterable.CountAsync(filter);
var exists = await filterable.ExistsAsync(filter);
```

**After (1.7.1):**

```csharp
// Extension methods on IRepository<T> accept IQueryFilter or lambdas
var items  = await repository.FindAllAsync(filter);
var count  = await repository.CountAsync(filter);
var exists = await repository.ExistsAsync(filter);

// Lambda shorthand also works
var active = await repository.FindAllAsync(p => p.IsActive);
```

From inside a `Repository<T, TKey>` subclass, call the `protected` members directly:

```csharp
public async Task<bool> CodeExistsAsync(string code, CancellationToken ct = default) {
    var filter = new ExpressionQueryFilter<Product>(p => p.Code == code);
    return await ExistsAsync(filter, ct);   // protected member
}
```

## Replace `GetPageAsync(PageQuery<T>)`

The public `IPageableRepository<T>.GetPageAsync(PageQuery<T>)` overload is gone. For **unsorted** pagination, use `IRepository<T>.GetPageAsync(PageRequest)`:

```csharp
var page = await repository.GetPageAsync(new PageRequest(page: 1, size: 20));
```

For **filtered/sorted** pagination, expose a domain-specific paged method on your custom repository, implemented through the protected `QueryPageAsync(PageQuery<T>)`:

```csharp
public interface IProductRepository : IRepository<Product, Guid> {
    Task<PageQueryResult<Product>> FindByCategoryPagedAsync(
        string category, PageRequest request, CancellationToken ct = default);
}

public class ProductRepository : InMemoryRepository<Product, Guid>, IProductRepository {
    public async Task<PageQueryResult<Product>> FindByCategoryPagedAsync(
        string category, PageRequest request, CancellationToken ct = default) {
        var query = new PageQuery<Product>(request.Page, request.Size)
            .Where(p => p.Category == category)
            .OrderBy(p => p.Name);
        return await QueryPageAsync(query, ct);   // protected member
    }
}
```

## Remove the legacy interfaces from repository declarations

Concrete repositories that explicitly implemented the removed interfaces must drop those declarations. The `Repository<TEntity, TKey>` base class already provides the equivalent `protected` members.

**Before (1.7.0):**

```csharp
public class LegacyRepository : Repository<Product, Guid>,
                               IQueryableRepository<Product, Guid>,
                               IPageableRepository<Product, Guid>,
                               IFilterableRepository<Product, Guid> {
    // explicit interface method implementations...
}
```

**After (1.7.1):**

```csharp
public class LegacyRepository : Repository<Product, Guid>, IProductRepository {
    // domain-specific methods using protected members (Queryable(), ExistsAsync, etc.)
}
```

Drop any explicit interface method implementations (`IFilterableRepository<...>.ExistsAsync`, `IPageableRepository<...>.GetPageAsync`, etc.) — the base class now handles these through its `protected` members, and the public extension methods on `IRepository<T>` cover the consumer-side surface.

## DI registration

`AddRepository<T>()` and `ScanRepositories()` no longer register `IQueryableRepository<T>`, `IPageableRepository<T>`, or `IFilterableRepository<T>` as services. This is expected — those interfaces no longer exist. Resolve `IRepository<T>` or your custom interface instead. See [Registration](custom-repository/registration.md) for the current behaviour.

## `Queryable()` is now `protected`

In a follow-on tightening to the 1.7.1 cleanup, the `Repository<TEntity, TKey>.Queryable()` method changed from `public abstract` to `protected abstract`. The in-memory `RepositoryWrapper.Queryable()` override was removed entirely. This closes the last public `IQueryable<T>` escape hatch on the base class — the queryable is now reachable only from inside a subclass and from the base-class query pipeline.

### Who is affected

- **Consumer code that called `repo.Queryable()` directly** from outside a subclass (or from another assembly) — this was already undocumented and discouraged, but it compiled in 1.7.1. It no longer compiles.
- **Companion-assembly authors** who reached into `Queryable()` from another assembly to apply filters, counts, or existence checks. The Kista-owned companions (`UserScopedRepositoryDecorator`, `SpecificationRepositoryExtensions`) were migrated to the new dispatch path (see below); third-party companions doing the same must follow the same migration.
- **Subclasses** that override `Queryable()` must change the override from `public override` to `protected override` to match the new base-class visibility.

### Migration

**From outside the subclass / another assembly** — define a domain-specific method on a custom repository interface, implemented through the `protected Queryable()` hatch inside the subclass. This is the same pattern already documented above in [Replace `AsQueryable()` consumer code](#replace-asqueryable-consumer-code): the consumer depends on a domain contract, not on `IQueryable<T>`.

**From a Kista-owned companion assembly** — use the new `internal` dispatch entry points on `Repository<TEntity, TKey>` instead of calling `Queryable()` directly:

| Old (1.7.1) | New (1.7.x) |
| ----------- | ----------- |
| `query.Apply(repo.Queryable()).FirstOrDefault()` | `repo.FindFirstAsyncInternal(query, ct)` |
| `query.Apply(repo.Queryable()).ToList()` | `repo.FindAllAsyncInternal(query, ct)` |
| `filter.Apply(repo.Queryable()).LongCount()` | `repo.CountAsyncInternal(filter, ct)` |
| `filter.Apply(repo.Queryable()).Any()` | `repo.ExistsAsyncInternal(filter, ct)` |

These `internal` methods forward to the existing `protected` virtual filterable methods (`FindFirstAsync`, `FindAllAsync`, `CountAsync`, `ExistsAsync`) and are accessible to Kista-owned assemblies through `InternalsVisibleTo`. They are **not** part of the public API surface — consumer code must not call them.

**In subclasses** — change the override visibility:

```csharp
// Before (1.7.1)
public override IQueryable<Product> Queryable() => _context.Set<Product>().AsQueryable();

// After (1.7.x)
protected override IQueryable<Product> Queryable() => _context.Set<Product>().AsQueryable();
```

The `protected` member is still overridable in subclasses; the change only closes the cross-assembly public escape. Subclass code that called `Queryable()` internally (to implement domain-specific query methods) is unaffected.

> The `ParameterReplacer` expression visitor in `Kista` core was removed in the same change — it was only consumed by the removed queryable-composition paths. Consumer code never referenced it directly.

## Reference

- [The Repository Pattern](repository-pattern.md) — the `Repository<TEntity, TKey>` base class and its protected query hatch
- [Customize the Repository](custom-repository/) — defining and implementing custom repository interfaces
- [Filtering](filtering/) — the filter system, extension methods, and the `CreateQuery()` fluent builder
- [Repository Implementations](repository-implementations/) — driver-specific query behaviour