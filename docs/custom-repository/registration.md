# Registration

This page covers how to register custom repositories with the DI container using the Kista fluent builder API.

## Basic Registration

Use `AddRepository<T>()` on the repository context builder:

```csharp
builder.Services.AddRepositoryContext()
    .UseEntityFramework<AppDbContext>(b => b
        .ConfigureDbContext(opts => opts.UseSqlServer("...")))
    .AddRepository<ProductRepository>();
```

## What Gets Registered

The `AddRepository<T>()` method scans the specified type and registers all relevant services:

```csharp
builder.Services.AddRepositoryContext()
    .UseInMemory()
    .AddRepository<ProductRepository>();
```

This registers:

| Service Type | Description |
| --- | --- |
| `ProductRepository` | The concrete implementation |
| `IProductRepository` | Your custom interface |
| `IRepository<Product, Guid>` | The base repository contract |

All registrations are **scoped** by default, matching the typical request lifetime for data access.

## Lifetime Configuration

Override the default lifetime:

```csharp
builder.Services.AddRepositoryContext()
    .UseInMemory()
    .AddRepository<ProductRepository>(ServiceLifetime.Singleton);
```

Available lifetimes:

| Lifetime | Use Case |
|----------|----------|
| `Scoped` (default) | Request-scoped data access, typical web apps |
| `Transient` | Stateless repositories, no shared state |
| `Singleton` | Read-only repositories, cached data |

## Open Generic Repositories

Open generic repositories are registered as open generics:

```csharp
public class NamedEntityRepository<TEntity> : RepositoryBase<TEntity, Guid>, INamedEntityRepository<TEntity>
    where TEntity : class, INamedEntity {
    // ...
}

builder.Services.AddRepositoryContext()
    .UseEntityFramework<AppDbContext>()
    .AddRepository(typeof(NamedEntityRepository<>));
```

This allows resolution of `INamedEntityRepository<Product>`, `INamedEntityRepository<Category>`, etc.

## Multiple Repositories

Register multiple repositories in sequence:

```csharp
builder.Services.AddRepositoryContext()
    .UseEntityFramework<AppDbContext>()
    .AddRepository<ProductRepository>()
    .AddRepository<OrderRepository>()
    .AddRepository<CustomerRepository>();
```

Or chain them:

```csharp
var context = builder.Services.AddRepositoryContext()
    .UseEntityFramework<AppDbContext>();

context.AddRepository<ProductRepository>();
context.AddRepository<OrderRepository>();
context.AddRepository<CustomerRepository>();
```

## Assembly Scanning

Use `ScanRepositories()` to automatically discover and register repository types from one or more assemblies:

```csharp
builder.Services.AddRepositoryContext()
    .UseEntityFramework<AppDbContext>()
    .ScanRepositories(typeof(ProductRepository).Assembly);
```

This scans the assembly for all types that implement `IRepository<TEntity>` and registers them automatically.

## Consuming the Repository

After registration, resolve the repository through DI:

```csharp
public class ProductService {
    private readonly IProductRepository _products;

    public ProductService(IProductRepository products) {
        _products = products;
    }

    public async Task<Product?> GetByCodeAsync(string code, CancellationToken ct = default) {
        return await _products.FindByCodeAsync(code, ct);
    }
}
```

You can also resolve the base interface:

```csharp
public class GenericService {
    private readonly IRepository<Product, Guid> _products;

    public GenericService(IRepository<Product, Guid> products) {
        _products = products;
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default) {
        return await _products.FindAsync(id, ct);
    }
}
```

## Deprecated Interface Registration

The legacy extension interfaces (`IQueryableRepository`, `IPageableRepository`, `IFilterableRepository`) are **not** registered automatically for new repositories. If your custom repository implements these deprecated interfaces (for backward compatibility), they will still be registered:

```csharp
// Legacy — not recommended for new code
public class LegacyRepository : RepositoryBase<Product, Guid>,
                                 IQueryableRepository<Product, Guid> {
    // ...
}

// This would register IQueryableRepository<Product, Guid> as well
builder.Services.AddRepositoryContext()
    .UseInMemory()
    .AddRepository<LegacyRepository>();
```

New code should not rely on these interfaces. Use domain-specific methods on your custom interface instead.
