# MongoDB Repository

| Feature | Status | Notes |
| ------- | :----: | ----- |
| Base Repository | ✅ | |
| Filterable | ✅ | |
| Queryable | ✅ | Via MongoFramework `IQueryable` |
| Pageable | ✅ | |
| Tracking | ✅ | MongoFramework change tracking |
| Multi-tenant | ✅ | Via `Deveel.Repository.MongoFramework.MultiTenant` |

The `MongoRepository<TEntity>` class is an implementation of the repository pattern that stores entities in a [MongoDB](https://www.mongodb.com) database, built on top of [MongoFramework](https://github.com/TurnerSoftware/MongoFramework).

MongoFramework is a lightweight library that maps .NET objects to MongoDB documents using a design similar to Entity Framework Core.

## Installation

```bash
dotnet add package Deveel.Repository.MongoFramework
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

## Querying

`MongoRepository<TEntity>` implements `IQueryableRepository<TEntity>`, `IFilterableRepository<TEntity>`, and `IPageableRepository<TEntity>`.

**With LINQ:**

```csharp
var items = repository.AsQueryable()
    .Where(x => x.IsActive)
    .OrderBy(x => x.Name)
    .ToList();
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
