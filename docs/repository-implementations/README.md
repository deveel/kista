# Repository Implementations
> **Renamed:** This project was renamed from **Deveel.Repository** to **Kista** on **May 26, 2025**. The name *Kista* is Old Norse for "chest" or "repository", better reflecting the project purpose as a data access framework.

The _Kista_ framework ships with a set of ready-to-use repository implementations for the most common data sources.

| Data Source | Package | Version |
| ----------- | ------- | ------- |
| [In-Memory](in-memory.md) | `Kista.InMemory` | [![NuGet](https://img.shields.io/nuget/v/Kista.InMemory.svg)](https://www.nuget.org/packages/Kista.InMemory/) |
| [Entity Framework Core](ef-core.md) | `Kista.EntityFramework` | [![NuGet](https://img.shields.io/nuget/v/Kista.EntityFramework.svg)](https://www.nuget.org/packages/Kista.EntityFramework/) |
| [EF Core Multi-Tenant](ef-core.md#multi-tenant-support) | `Kista.EntityFramework.MultiTenant` | [![NuGet](https://img.shields.io/nuget/v/Kista.EntityFramework.MultiTenant.svg)](https://www.nuget.org/packages/Kista.EntityFramework.MultiTenant/) |
| [MongoDB](mongodb.md) | `Kista.MongoFramework` | [![NuGet](https://img.shields.io/nuget/v/Kista.MongoFramework.svg)](https://www.nuget.org/packages/Kista.MongoFramework/) |
| [MongoDB Multi-Tenant](../multi-tenancy.md) | `Kista.MongoFramework.MultiTenant` | [![NuGet](https://img.shields.io/nuget/v/Kista.MongoFramework.MultiTenant.svg)](https://www.nuget.org/packages/Kista.MongoFramework.MultiTenant/) |

## Capability Matrix

All driver implementations inherit from `Repository<TEntity, TKey>`, which provides a unified set of capabilities:

| Capability | In-Memory | EF Core | MongoDB |
| ---------- | :-------: | :-----: | :-----: |
| Base Repository (`IRepository`) | ✅ | ✅ | ✅ |
| Protected query hatch (`Query()`) | ✅ | ✅ | ✅ |
| Protected filter/sort methods | ✅ | ✅ | ✅ |
| Protected pagination (`QueryPageAsync`) | ✅ | ✅ | ✅ |
| Tracking (`ITrackingRepository`) | ❌ | ✅ | ✅ |
| Multi-tenant | ❌ | ✅ | ✅ |
| User-scoped (`IUserRepository`) | ❌ | ✅ | ❌ |

> **Note:** The legacy extension interfaces (`IFilterableRepository`, `IQueryableRepository`, `IPageableRepository`) are deprecated. All query capabilities are now provided through `protected` members of `Repository<TEntity, TKey>` and should be exposed via domain-specific methods on your custom repository interface.

## Dynamic LINQ Support

The `Kista.DynamicLinq` package adds filter and sort support via [System.Linq.Dynamic.Core](https://github.com/zzzprojects/System.Linq.Dynamic.Core). This allows you to build queries using string-based expressions, which is useful for dynamic query builders, search APIs, and other scenarios where the filter predicate is not known at compile time.

```bash
dotnet add package Kista.DynamicLinq
```

Once installed, filterable and queryable repositories automatically accept string-based filter expressions in addition to the usual lambda-based ones.

### Filter Expression Cache

For production workloads where the same filter shapes are executed repeatedly, the framework includes a **bounded, thread-safe expression cache** that eliminates redundant parsing and compilation overhead. The cache uses LRU eviction with a configurable maximum size and exposes hit/miss statistics for monitoring.

Enable it with a single DI registration:

```csharp
builder.Services.AddFilterCache(maxCapacity: 2048);
```

Once registered, the cache is resolved automatically by `DynamicLinqFilter` when the repository is constructed with an `IServiceProvider`. No manual passing is required:

```csharp
var filter = new DynamicLinqFilter("x.Status == \"Active\"");
var results = await repository.FindAllAsync(filter);
```

If you need to override the DI-registered cache for a specific query, you can still pass it manually — the constructor-provided cache takes precedence.

See the [Filter Cache documentation](../filtering/filter-cache.md) for the full automatic resolution mechanism, configuration guidance, monitoring integration, and benchmark results.

## Design Pattern: Separation of Data Logic

One of the most valuable aspects of using a Repository pattern is that it allows you to express data access requirements at the **domain level** and swap the underlying implementation without changing any application code.

For example, consider a service library (`Foo.Service.dll`) that defines a domain interface:

```csharp
// Foo.Service.dll
public interface IDataRepository<TData> : IRepository<TData>
    where TData : class, IData
{
    Task<string> GetContentTypeAsync(TData data, CancellationToken ct = default);
    Task<byte[]> GetContentAsync(TData data, CancellationToken ct = default);
    Task SetContentAsync(TData data, string contentType, byte[] content, CancellationToken ct = default);
}
```

A MongoDB-specific assembly (`Foo.Service.MongoDb.dll`) implements this contract:

```csharp
public class MongoData : IData { /* ... */ }

public class MongoDataRepository : MongoRepository<MongoData>, IDataRepository<MongoData>
{
    public MongoDataRepository(IMongoDbContext context) : base(context) { }

    public Task SetContentAsync(MongoData data, string contentType, byte[] content, CancellationToken ct = default)
    {
        data.ContentType = contentType;
        data.Content = content;
        return Task.CompletedTask;
    }

    public Task<byte[]> GetContentAsync(MongoData data, CancellationToken ct = default)
        => Task.FromResult(data.Content);

    public Task<string> GetContentTypeAsync(MongoData data, CancellationToken ct = default)
        => Task.FromResult(data.ContentType);
}
```

And an EF Core assembly (`Foo.Service.EF.dll`) provides the relational equivalent:

```csharp
public class DbData : IData { /* ... */ }

public class EntityDataRepository : EntityRepository<DbData>, IDataRepository<DbData>
{
    public EntityDataRepository(DataDbContext context) : base(context) { }

    // ... same interface, different storage logic
}
```

The consuming application code depends only on `IDataRepository<TData>` — the storage engine is a deployment concern, not a domain concern.
