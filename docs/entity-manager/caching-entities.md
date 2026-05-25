# Caching Entities
> **Renamed:** This project was renamed from **Deveel.Repository** to **Kista** on **May 26, 2025**. The name *Kista* is Old Norse for "chest" or "repository", better reflecting the project purpose as a data access framework.

The `EntityManager<TEntity>` supports an optional second-level cache via the `IEntityCache<TEntity>` service. When registered, the manager transparently caches entities on write and serves them from the cache on subsequent reads, reducing the number of calls to the underlying repository.

## Installation

Install the EasyCaching integration package:

```bash
dotnet add package Kista.Manager.EasyCaching
```

## Registration

Register EasyCaching and the entity cache in the DI container:

```csharp
// Program.cs
builder.Services.AddEasyCaching(options =>
{
    options.UseInMemory("default");
});

// Register the default EntityEasyCache<MyEntity>
builder.Services.AddEntityEasyCacheFor<MyEntity>();

// Then register the manager (which picks up the cache automatically)
builder.Services.AddManagerFor<MyEntity>();
```

You can configure cache options inline or from configuration:

```csharp
// Inline configuration
builder.Services.AddEntityEasyCacheFor<MyEntity>(options =>
{
    options.Expiration = TimeSpan.FromMinutes(5);
});

// From appsettings.json
builder.Services.AddEntityEasyCacheFor<MyEntity>("Caching:MyEntity");
```

## How the Cache Interacts with Manager Operations

The `EntityManager<TEntity>` intercepts the following operations to read from or write to the cache:

| Manager Method | Cache Operation | Description |
| -------------- | --------------- | ----------- |
| `FindAsync(key)` | `GetOrSetAsync` | Returns the cached entity if present; otherwise calls the repository and caches the result. |
| `AddAsync` | `SetAsync` | Stores the newly added entity in the cache. |
| `AddRangeAsync` | `SetAsync` (batch) | Stores all added entities in the cache. |
| `UpdateAsync` | `SetAsync` | Updates the cached entry after a successful repository update. |
| `RemoveAsync` | `RemoveAsync` | Evicts the entity from the cache after removal. |
| `RemoveRangeAsync` | `RemoveAsync` (batch) | Evicts all removed entities from the cache. |

## Cache Keys

By default, the primary key of the entity is used to derive the cache key. To customize key generation, implement `IEntityCacheKeyGenerator<TEntity>` and register it:

```csharp
public class MyEntityCacheKeyGenerator : IEntityCacheKeyGenerator<MyEntity>
{
    public IEnumerable<string> GetKeys(MyEntity entity)
    {
        yield return $"myentity:{entity.Id}";
        yield return $"myentity:by-name:{entity.Name}";
    }
}

builder.Services.AddEntityCacheKeyGenerator<MyEntityCacheKeyGenerator>();
```

## Cache Serialization

In some scenarios, entities must be converted to a serializable form before being stored in the cache (e.g., when the cache provider serializes objects to JSON or binary). To handle this, derive from `EntityEasyCache<TEntity, TCached>` and implement `IEntityEasyCacheConverter<TEntity, TCached>`:

```csharp
public class MyEntityCacheConverter
    : IEntityEasyCacheConverter<MyEntity, MyEntityCacheModel>
{
    public MyEntityCacheModel ToCache(MyEntity entity) => new MyEntityCacheModel { /* ... */ };
    public MyEntity FromCache(MyEntityCacheModel cached) => new MyEntity { /* ... */ };
}

builder.Services.AddEntityEasyCacheConverter<MyEntityCacheConverter>();
```

Then register a typed `EntityEasyCache<MyEntity, MyEntityCacheModel>`:

```csharp
builder.Services.AddEntityEasyCache<EntityEasyCache<MyEntity, MyEntityCacheModel>>();
```
