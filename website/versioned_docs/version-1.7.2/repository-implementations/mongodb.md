# MongoDB Repository
| Feature | Status | Notes |
| ------- | :----: | ----- |
| Base Repository | ✅ | |
| Filterable | ✅ | |
| Queryable | ✅ | Via MongoFramework `IQueryable` |
| Pageable | ✅ | |
| Tracking | ✅ | MongoFramework change tracking |
| Multi-tenant | ✅ | Via `Kista.MongoFramework.MultiTenant` |

The `MongoRepository<TEntity>` class is an implementation of the repository pattern that stores entities in a [MongoDB](https://www.mongodb.com) database, built on top of [MongoFramework](https://github.com/TurnerSoftware/MongoFramework).

MongoFramework is a lightweight library that maps .NET objects to MongoDB documents using a design similar to Entity Framework Core.

## Installation

```bash
dotnet add package Kista.MongoFramework
```

## Registration

Use the fluent builder API to register the MongoDB driver:

```csharp
// Program.cs
builder.Services.AddRepositoryContext()
    .UseMongoDB<MyMongoContext>(b => b
        .WithConnectionString("mongodb://localhost:27017/my_database"));
```

You can also use a builder delegate to configure the connection:

```csharp
builder.Services.AddRepositoryContext()
    .UseMongoDB<MyMongoContext>(b => b
        .WithConnection(conn => conn.UseConnection("mongodb://localhost:27017/my_database")));
```

The following configuration methods are available on the MongoDB driver builder:

| Method | Description |
| ------ | ----------- |
| `WithConnectionString(string)` | Sets the MongoDB connection string. |
| `WithConnection(Action<MongoConnectionBuilder>)` | Configures the connection using a builder delegate. |
| `WithLifetime(ServiceLifetime)` | Sets the service lifetime (default: `Scoped`). |
| `WithLifecycle()` | Enables lifecycle support (default: enabled) |
| `WithoutLifecycle()` | Disables lifecycle support |

### Custom Context Type

If you derive from `MongoDbContext` (or `MongoDbTenantContext` for multi-tenant scenarios), register your concrete type:

```csharp
builder.Services.AddRepositoryContext()
    .UseMongoDB<MyMongoDbContext>(b => b
        .WithConnectionString("mongodb://..."));
```

## Multi-tenant Support

Multi-tenant support uses [Finbuckle.MultiTenant](https://github.com/Finbuckle/Finbuckle.MultiTenant). First, configure Finbuckle:

```csharp
builder.Services.AddMultiTenant<MongoDbTenantInfo>()
    .WithConfigurationStore()
    .WithRouteStrategy("tenant");
```

Then register a tenant-aware MongoDB context (derived from `MongoDbTenantContext`) and the repository:

```csharp
builder.Services.AddRepositoryContext()
    .UseMongoDB<MyMongoTenantContext>(b => b
        .WithConnection(conn => conn.UseConnection("mongodb://...")))
    .WithMongoMultiTenancy<MongoDbTenantInfo>(defaultConnection: "mongodb://...");
```

The tenant context resolves the correct database connection for each tenant automatically.

## Lifecycle Support

The MongoDB driver provides `MongoRepositoryLifecycleHandler<TEntity>` for lifecycle orchestration. It is **enabled by default** and can be disabled via `.WithoutLifecycle()`.

### Handler Behavior

| Operation | Behavior |
| --------- | -------- |
| `ExistsAsync` | Lists collection names on the database to check whether the entity's collection exists. |
| `CreateAsync` | Creates the collection via `CreateCollectionAsync`, then builds all entity indexes defined in the MongoFramework entity mapping. |
| `DropAsync` | Drops all indexes, then drops the collection. |
| `SeedAsync` | Inserts data directly into the raw MongoDB collection via `InsertManyAsync` / `InsertOneAsync`. Supports `IEnumerable<TEntity>`, `IEnumerable<object>`, and single entities. |

### Seeding Examples

**Using a provider class:**

```csharp
public class ProductSeedProvider : IRepositorySeedDataProvider<Product> {
    public IEnumerable<Product> GetSeedData() {
        yield return new Product { Name = "Widget", Price = 9.99m };
        yield return new Product { Name = "Gadget", Price = 24.99m };
    }

    IEnumerable<object> IRepositorySeedDataProvider.GetSeedData()
        => GetSeedData().Cast<object>();
}

// Program.cs
builder.Services.AddRepositoryContext()
    .UseMongoDB<MyMongoContext>(b => b
        .WithConnectionString("mongodb://..."))
    .ConfigureLifecycle(options => {
        options.SeedStrategy = SeedStrategy.Always;
    })
    .WithSeedData<Product, ProductSeedProvider>();
```

**Using inline data (no provider class needed):**

```csharp
builder.Services.AddRepositoryContext()
    .UseMongoDB<MyMongoContext>(b => b
        .WithConnectionString("mongodb://..."))
    .ConfigureLifecycle(options => {
        options.SeedStrategy = SeedStrategy.Always;
    })
    .WithSeedData<Product>(new[] {
        new Product { Name = "Widget", Price = 9.99m },
        new Product { Name = "Gadget", Price = 24.99m }
    });
```

The handler inserts the seed documents directly into the MongoDB collection via `InsertManyAsync`.

### Disabling Lifecycle

```csharp
builder.Services.AddRepositoryContext()
    .UseMongoDB<MyMongoContext>(b => b
        .WithConnectionString("mongodb://...")
        .WithoutLifecycle());
```

## Querying

`MongoRepository<TEntity>` exposes its query capabilities through `protected` members on the `Repository<TEntity, TKey>` base class and through extension methods on `IRepository<TEntity>`; the legacy `IQueryableRepository` / `IFilterableRepository` / `IPageableRepository` interfaces were removed in 1.7.1.

**With a domain-specific method (recommended):**

Define a custom repository interface and use the protected `Queryable()` hatch inside the implementation:

```csharp
public interface IMyEntityRepository : IRepository<MyEntity, Guid> {
    Task<IReadOnlyList<MyEntity>> FindActiveSortedAsync(CancellationToken ct = default);
}

public class MyEntityRepository : MongoRepository<MyEntity>, IMyEntityRepository {
    public async Task<IReadOnlyList<MyEntity>> FindActiveSortedAsync(CancellationToken ct = default) {
        return await Queryable()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
    }
}

// Consumer code
var items = await repo.FindActiveSortedAsync(ct);
```

**With filter types:**

```csharp
// Lambda shorthand (extension method)
var items = await repository.FindAllAsync(x => x.IsActive);

// ExpressionQueryFilter
var filter = new ExpressionQueryFilter<MyEntity>(x => x.IsActive);
var items  = await repository.FindAllAsync(new Query(filter));

// MongoDB-specific geo-distance filter
var geoFilter = new MongoGeoDistanceFilter(
    fieldName: "Location",
    center: new GeoPoint(lat, lon),
    maxDistanceKm: 10);
var items = await repository.FindAllAsync(new Query(geoFilter));
```

## Notes

- MongoFramework does not natively expose DI integration; the `AddMongoDbContext` extensions provided by this package fill that gap.
- Refer to the [MongoFramework documentation](https://github.com/TurnerSoftware/MongoFramework) for entity mapping and index configuration.
- Refer to the [Finbuckle.MultiTenant documentation](https://www.finbuckle.com/MultiTenant) for multi-tenant configuration.
