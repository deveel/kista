# Caching Entities

The `EntityManager<TEntity>` supports an optional second-level cache via the `IEntityCache<TEntity>` service. When registered, the manager transparently caches entities on write and serves them from the cache on subsequent reads, reducing calls to the underlying repository.

## Installation

Install the EasyCaching integration package:

```bash
dotnet add package Kista.Manager.EasyCaching
```

EasyCaching must be configured globally with a provider (e.g., in-memory, Redis, Memcached):

```csharp
builder.Services.AddEasyCaching(options =>
    options.UseInMemory("default"));
```

## Registration

### Via EntityManagerBuilder (recommended)

When using the per-repository `WithManagement()` callback, use `WithEasyCaching()` on the `EntityManagerBuilder`:

```csharp
services.AddRepositoryContext()
    .AddRepository<PersonRepository>(repo => repo
        .WithManagement(mgmt => mgmt
            .WithEasyCaching(opts =>
            {
                opts.DefaultExpiration = TimeSpan.FromMinutes(15);
            })))
    .UseInMemory();
```

### Global registration

Enable caching for all tracked entity types via the `RepositoryContextBuilder`:

```csharp
services.AddRepositoryContext()
    .AddRepository<PersonRepository>(_ => { })
    .AddRepository<OrderRepository>(_ => { })
    .WithEasyCaching(opts =>
    {
        opts.DefaultExpiration = TimeSpan.FromMinutes(5);
    });
```

### Direct registration (legacy)

The following methods on `IServiceCollection` are still supported but deprecated in favor of the fluent builder:

```csharp
builder.Services.AddEntityEasyCacheFor<MyEntity>();

builder.Services.AddManagerFor<MyEntity>();
```

You can configure cache options inline or from configuration:

```csharp
// Inline
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

### Cache invalidation

The manager automatically invalidates cached entries when entities are updated or removed. There is no time-based invalidation by default; expiration is controlled by the EasyCaching provider configuration (e.g., `DefaultExpiration` in `WithEasyCaching()`).

If an entity is modified outside the manager (directly through the repository), cached entries may become stale. In that case, call `RemoveAsync` through the manager or evict entries directly using the cache provider.

### Cache on Find vs FindFirst

`FindAsync` always checks the cache first. `FindFirstAsync` does **not** cache results, since queries are dynamic and caching their output is not generally safe without additional semantics.

## Cache Keys

By default, the entity's primary key (`IHaveKey<TKey>.Key`) is converted to a string and used as the cache key. The default key format is `{EntityType.Name}:{key}`.

To customize key generation, implement `IEntityCacheKeyGenerator<TEntity>` and register it via the `EntityManagerBuilder`:

```csharp
public class PersonKeyGenerator : IEntityCacheKeyGenerator<Person>
{
    public string GenerateKey(object key) => $"person:{key}";
    public string[] GenerateAllKeys(Person entity) =>
        [$"person:{entity.Id}", $"person:email:{entity.Email}"];
}

services.AddRepositoryContext()
    .AddRepository<PersonRepository>(repo => repo
        .WithManagement(mgmt => mgmt
            .WithCacheKeyGenerator<PersonKeyGenerator>()));
```

The `WithCacheKeyGenerator<TGenerator>()` method scans the type for implemented `IEntityCacheKeyGenerator<>` interfaces and registers each one matching the builder's current entity type.

Multiple keys from `GenerateAllKeys` enable lookups by alternate identifiers (e.g., email, external ID) while keeping a single entry in the cache.

For legacy direct registration (deprecated):

```csharp
builder.Services.AddEntityCacheKeyGenerator<MyEntityCacheKeyGenerator>();
```

## Cache Serialization

Some cache providers (Redis, distributed caches) require entities to be serialized to a specific format. To handle this, implement `IEntityEasyCacheConverter<TEntity, TCached>` and register a typed cache:

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

## See Also

- [Entity Validation](entity-validation.md) — configure validators and error factories
- [HTTP Request Cancellation](http-request-cancellation.md) — automatic cancellation via ASP.NET Core
