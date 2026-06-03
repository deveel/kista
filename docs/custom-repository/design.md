# Interface Design

This page covers how to define your custom repository interface following the **Specification Pattern** — the recommended approach for domain-specific queries in Kista.

## The Specification Pattern

Because the base `IRepository<TEntity, TKey>` contract does not expose generic query capabilities, you define purpose-built query methods on your own repository interface. Each method represents a **specification** — a named, domain-meaningful query.

### Basic Pattern

```csharp
public interface IProductRepository : IRepository<Product, Guid> {
    // Single-entity look-ups
    Task<Product?> FindByCodeAsync(string productCode, CancellationToken ct = default);
    Task<Product?> FindBySkuAsync(string sku, CancellationToken ct = default);

    // Collection queries
    Task<IReadOnlyList<Product>> FindByNameAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> FindByCategoryAsync(string category, CancellationToken ct = default);

    // Existence checks
    Task<bool> CodeExistsAsync(string productCode, CancellationToken ct = default);

    // Count queries
    Task<long> CountByCategoryAsync(string category, CancellationToken ct = default);
}
```

### Why Named Methods?

| Benefit | Explanation |
|---------|-------------|
| **Intent is clear** | `FindByCodeAsync` communicates domain meaning; `FindAsync(filter)` does not |
| **Encapsulated logic** | The query logic lives in one place, tested once, reused everywhere |
| **Versioned contract** | Adding a new query is a new method — no breaking changes to existing consumers |
| **No IQueryable leak** | Consumers never see `IQueryable<T>` or compose arbitrary expressions |

## Generic vs. Open Interfaces

### Concrete Entity Interface

The most common pattern — the interface is tied to a specific entity type:

```csharp
public interface IOrderRepository : IRepository<Order, Guid> {
    Task<IReadOnlyList<Order>> FindByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> FindByStatusAsync(OrderStatus status, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> FindRecentAsync(int count, CancellationToken ct = default);
}
```

### Open Generic Interface

Useful when you have a common query pattern across multiple entity types:

```csharp
public interface INamedEntityRepository<TEntity> : IRepository<TEntity, Guid>
    where TEntity : class, INamedEntity {

    Task<TEntity?> FindByNameAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<TEntity>> SearchByNameAsync(string query, int maxResults, CancellationToken ct = default);
}
```

Implementations close the generic:

```csharp
public class ProductRepository : Repository<Product, Guid>, INamedEntityRepository<Product> {
    // ...
}

public class CategoryRepository : Repository<Category, Guid>, INamedEntityRepository<Category> {
    // ...
}
```

### Multi-Entity Repository

Sometimes a repository manages more than one entity type (e.g., an aggregate root with child entities):

```csharp
public interface IOrderManagementRepository : IRepository<Order, Guid> {
    // Order queries
    Task<Order?> FindWithItemsAsync(Guid orderId, CancellationToken ct = default);

    // Line item queries
    Task<IReadOnlyList<OrderItem>> FindItemsByProductAsync(Guid productId, CancellationToken ct = default);

    // Aggregate operations
    Task<decimal> CalculateTotalAsync(Guid orderId, CancellationToken ct = default);
}
```

## Pagination in the Interface

### Simple Pagination (from `IRepository`)

The base interface already provides unsorted pagination:

```csharp
ValueTask<PageResult<Product>> GetPageAsync(PageRequest request, CancellationToken ct = default);
```

No need to redeclare this on your custom interface.

### Filtered/Sorted Pagination

If your domain needs filtered and sorted pagination, expose it as a domain-specific method:

```csharp
public interface IProductRepository : IRepository<Product, Guid> {
    // Specific method — recommended
    Task<PageQueryResult<Product>> FindByCategoryPagedAsync(
        string category,
        PageRequest request,
        CancellationToken ct = default);

    // Generic method — when flexibility is needed
    Task<PageQueryResult<Product>> FindProductsPagedAsync(
        PageQuery<Product> request,
        CancellationToken ct = default);
}
```

See [Query Methods](query-methods.md) for the trade-offs between these approaches.

## Anti-Patterns to Avoid

### Don't Expose `IQueryable`

```csharp
// BAD — leaks IQueryable to consumers
public interface IProductRepository : IRepository<Product, Guid> {
    IQueryable<Product> AsQueryable();
}
```

### Don't Use Deprecated Interfaces

```csharp
// BAD — these interfaces are obsolete
public interface IProductRepository : IQueryableRepository<Product, Guid>,
                                       IPageableRepository<Product, Guid>,
                                       IFilterableRepository<Product, Guid> {
}
```

### Don't Over-Generalize

```csharp
// BAD — too generic, loses domain meaning
public interface IProductRepository : IRepository<Product, Guid> {
    Task<IReadOnlyList<Product>> FindAsync(IQueryFilter filter, CancellationToken ct = default);
}
```

Instead, name your queries after their domain intent:

```csharp
// GOOD — clear domain intent
public interface IProductRepository : IRepository<Product, Guid> {
    Task<IReadOnlyList<Product>> FindActiveProductsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Product>> FindDiscontinuedProductsAsync(CancellationToken ct = default);
}
```
