# Customize the Repository

In most cases, the default CRUD operations provided by `IRepository<TEntity>` are sufficient. But when your domain requires richer queries, you need to extend the base contract with domain-specific methods.

This section covers how to design, implement, and register custom repositories that follow the new Kista model — where generic query capabilities are hidden behind `protected` members of `Repository<TEntity, TKey>`, and domain-specific queries are exposed through the **Specification Pattern**.

## Overview

The new design model separates concerns clearly:

| Layer | Responsibility |
|-------|----------------|
| `IRepository<TEntity, TKey>` | Core CRUD + key look-up + unsorted pagination |
| `Repository<TEntity, TKey>` | Protected query hatch + ready-made query methods |
| Your custom interface | Domain-specific query methods (Specification Pattern) |

## Topics

| Topic | Description |
| ----- | ----------- |
| [Interface Design](design.md) | How to define your custom repository interface following the Specification Pattern |
| [Implementation](implementation.md) | How to extend `Repository<TEntity, TKey>` and use the protected query hatch |
| [Query Methods](query-methods.md) | Trade-offs between specific methods and generic page queries |
| [Registration](registration.md) | How to register custom repositories with the DI container |

## Quick Example

```csharp
// 1. Define the domain interface
public interface IProductRepository : IRepository<Product, Guid> {
    Task<Product?> FindByCodeAsync(string code, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> FindByNameAsync(string name, CancellationToken ct = default);
}

// 2. Implement using Repository
public class ProductRepository : Repository<Product, Guid>, IProductRepository {
    protected override IQueryable<Product> Queryable() => _context.Set<Product>().AsQueryable();
    // ... implementation details ...
}

// 3. Register with DI
builder.Services.AddRepositoryContext()
    .UseEntityFramework<AppDbContext>()
    .AddRepository<ProductRepository>();
```
