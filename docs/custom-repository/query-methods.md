# Query Methods

This page covers the trade-offs between specific domain query methods and generic paginated queries, and how to decide which approach to use.

## The Spectrum

Query methods on your custom repository fall on a spectrum from **highly specific** to **fully generic**:

```
Specific ←————————————————————————————→ Generic

FindByCodeAsync(string)          FindProductsPagedAsync(PageQuery<Product>)
FindByNameAsync(string)          FindAllAsync(IQuery)
FindByCategoryAsync(string)      QueryPageAsync(PageQuery<Product>)
CodeExistsAsync(string)          ExistsAsync(IQueryFilter)
```

## Specific Methods (Recommended)

Named methods that encapsulate a single domain query:

```csharp
public interface IProductRepository : IRepository<Product, Guid> {
    Task<Product?> FindByCodeAsync(string code, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> FindByNameAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> FindByCategoryAsync(string category, CancellationToken ct = default);
    Task<bool> CodeExistsAsync(string code, CancellationToken ct = default);
    Task<long> CountByCategoryAsync(string category, CancellationToken ct = default);
}
```

### Advantages

| Benefit | Explanation |
|---------|-------------|
| **Clear intent** | The method name communicates the domain meaning |
| **Encapsulated logic** | Query logic is tested once, in one place |
| **Safe** | No risk of runtime `NotSupportedException` from arbitrary expressions |
| **Versioned** | Adding a new query is a new method — no breaking changes |
| **Discoverable** | IDE autocomplete shows all available queries |

### Disadvantages

| Drawback | Mitigation |
|----------|------------|
| More methods to write | Each method is small and focused |
| New query = new method | This is a feature, not a bug — it forces deliberate design |

### When to Use

- The query has a clear domain meaning (`FindByCode`, `FindActiveOrders`)
- The query is used in multiple places
- You want to guarantee the query works (tested, versioned)
- The query involves complex logic that should not be duplicated

## Generic Page Queries

Exposing a method that accepts a `PageQuery<TEntity>` and delegates to the protected `QueryPageAsync`:

```csharp
public interface IProductRepository : IRepository<Product, Guid> {
    Task<PageQueryResult<Product>> FindProductsPagedAsync(
        PageQuery<Product> request,
        CancellationToken ct = default);
}

public class ProductRepository : Repository<Product, Guid>, IProductRepository {
    public async Task<PageQueryResult<Product>> FindProductsPagedAsync(
        PageQuery<Product> request,
        CancellationToken ct = default) {
        return await QueryPageAsync(request, ct);
    }
}
```

### Advantages

| Benefit | Explanation |
|---------|-------------|
| **Flexible** | One method covers many query combinations |
| **Less code** | No need to write a method for every query variation |
| **Composable** | Consumers can compose filters and sorts at runtime |

### Disadvantages

| Drawback | Explanation |
|----------|-------------|
| **Risk accepted by domain owner** | Consumers can compose arbitrary filters that may not be supported by the underlying engine |
| **Less discoverable** | Consumers must know how to construct `PageQuery<TEntity>` and `IQueryFilter` |
| **Runtime errors** | Unsupported filter types may throw `NotSupportedException` at runtime |
| **Leaky abstraction** | Consumers depend on Kista's query types, not just your domain interface |

### When to Use

- You have many query variations that would require too many specific methods
- You are building a search API where filters are dynamic
- You accept the risk and document which filter types are supported
- The consumer is internal (same team) and understands the query model

## Hybrid Approach

Combine specific methods for common queries with a generic method for ad-hoc queries:

```csharp
public interface IProductRepository : IRepository<Product, Guid> {
    // Common, safe queries
    Task<Product?> FindByCodeAsync(string code, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> FindActiveProductsAsync(CancellationToken ct = default);

    // Generic paginated query — risk documented
    Task<PageQueryResult<Product>> FindProductsPagedAsync(
        PageQuery<Product> request,
        CancellationToken ct = default);
}
```

## Paginated Specific Queries

You can also combine specificity with pagination:

```csharp
public interface IProductRepository : IRepository<Product, Guid> {
    Task<PageQueryResult<Product>> FindByCategoryPagedAsync(
        string category,
        PageRequest request,
        CancellationToken ct = default);

    Task<PageQueryResult<Product>> FindByNamePagedAsync(
        string name,
        PageRequest request,
        CancellationToken ct = default);
}
```

Implementation uses the protected `QueryPageAsync`:

```csharp
public async Task<PageQueryResult<Product>> FindByCategoryPagedAsync(
    string category,
    PageRequest request,
    CancellationToken ct = default) {

    var pageQuery = new PageQuery<Product>(request.Page, request.Size)
        .Where(p => p.Category == category);

    return await QueryPageAsync(pageQuery, ct);
}
```

## Decision Guide

| Scenario | Recommended Approach |
|----------|---------------------|
| Single-entity look-up by natural key | Specific method (`FindByCodeAsync`) |
| Simple list query | Specific method (`FindActiveProductsAsync`) |
| Existence check | Specific method (`CodeExistsAsync`) |
| Count by criteria | Specific method (`CountByCategoryAsync`) |
| Paginated list with fixed filter | Paginated specific method (`FindByCategoryPagedAsync`) |
| Search API with dynamic filters | Generic method (`FindProductsPagedAsync`) |
| Admin dashboard with arbitrary filters | Generic method, document supported filter types |
