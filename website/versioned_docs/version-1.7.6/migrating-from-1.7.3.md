# Migrating from 1.7.3 to 1.7.4

The **1.7.4 — Cache Alignment to the Operation Pipeline** patch release moves the entity cache from inline `SetToCacheAsync` / `EvictAsync` helpers in `EntityManager` into a builtin `CacheInterceptor<TEntity, TKey>` that runs in `PostWriteAsync` on the operation pipeline introduced in 1.7.3. This is a **non-breaking** release for the vast majority of consumers: no public API is added, removed, or renamed, and the cache backend packages (`Kista.Manager.EasyCaching`, `MemoryCache`, `DistributedCache`, `FusionCache`) register `IEntityCache<TEntity>` unchanged — only the call site moves.

The one consumer-visible behavior change is in **`RemoveRangeAsync`**, which is now consistent with `RemoveAsync` for soft-deletable entities.

For the prior 1.7.2 → 1.7.3 migration (obsolete-API removal and the operation pipeline introduction), see [Migrating from 1.7.2](migrating-from-1.7.2.md).

## Why the cache was aligned to the pipeline

Through 1.7.3 the cache was glued into `EntityManager` via private `SetToCacheAsync` / `EvictAsync` helpers called inline from nine write methods (`AddAsync`, `AddRangeAsync`, `UpdateAsync`, `RemoveAsync` soft + hard branches, `RemoveRangeAsync`, `RestoreAsync`, `HardDeleteAsync`, `HardDeleteRangeAsync`). The operation pipeline added in 1.7.3 already owned the write path for every other cross-cutting concern (audit, events, tracing, soft-delete stamping, validation short-circuit), so the cache was the last concern still bolted onto the manager rather than expressed as an interceptor.

Aligning the cache to the pipeline makes the concern **removable, reorderable, and testable in isolation**: not registering an `IEntityCache<TEntity>` removes the cache side effects entirely without subclassing the manager, and custom interceptors can observe the cache decision through the same `IEntityOperationContext` they already use.

## Behavior change: `RemoveRangeAsync` and soft-deletable entities

Before this release, `RemoveRangeAsync` **always evicted** every entity in the batch from the cache, even `ISoftDeletable` ones — inconsistent with `RemoveAsync`, which re-caches soft-deletable entities (the soft-deleted row still exists in the repository). The builtin `CacheInterceptor` now handles each entity in the batch per the same `ISoftDeletable` rule as the single `RemoveAsync`:

- **Soft-deletable entities** in a range Remove are now **re-cached** (the cached entry is refreshed with the soft-delete stamp applied), matching `RemoveAsync`.
- **Non-soft-deletable entities** in a range Remove continue to be **evicted**, matching `RemoveAsync` and `HardDeleteAsync`.

### Do I need to change my code?

In most cases, **no**. The new behavior is the correct one: the cached entry for a soft-deleted entity should reflect the soft-deleted state so subsequent `FindAsync` read-through hits return the up-to-date row. The previous per-entity eviction was a latent bug that left stale (pre-delete) entries in the cache for soft-deletable entities removed via the batch path.

You only need to act if you explicitly relied on the old per-entity eviction of soft-deletable entities in `RemoveRangeAsync` — for example, if your application treated a range Remove as a hint to drop the cache entries immediately. In that case, register a custom interceptor that calls `IEntityCache<TEntity>.RemoveAsync` in `PostWriteAsync` for `EntityOperationKind.Remove` on `ISoftDeletable` entities:

```csharp
services.AddRepositoryContext()
    .AddRepository<ContactRepository>(repo => repo
        .WithManagement(mgmt => mgmt
            .WithInterceptor<EvictOnRemoveInterceptor<Contact, Guid>>()))
    .UseInMemory();

public sealed class EvictOnRemoveInterceptor<TEntity, TKey> : IEntityManagerInterceptor<TEntity, TKey>
    where TEntity : class
    where TKey : notnull
{
    private readonly IEntityCache<TEntity> _cache;

    public EvictOnRemoveInterceptor(IEntityCache<TEntity> cache) => _cache = cache;

    public Task<IOperationResult?> PreWriteAsync(IEntityOperationContext<TEntity, TKey> context, CancellationToken ct)
        => Task.FromResult<IOperationResult?>(null);

    public async Task PostWriteAsync(IEntityOperationContext<TEntity, TKey> context, IOperationResult result, CancellationToken ct)
    {
        if (context.OperationKind == EntityOperationKind.Remove
            && context.Entity is ISoftDeletable
            && result.IsSuccess)
        {
            await _cache.RemoveAsync(context.Entity, ct);
        }
    }
}
```

> **Note:** The builtin `CacheInterceptor` runs **after** `OnHooksEntityInterceptor` and before user interceptors only in registration order if you register it explicitly; by default it is auto-appended last when an `IEntityCache<TEntity>` is present. Registering your own remove-eviction interceptor as above means both will run — to fully override the builtin behavior, do not register an `IEntityCache<TEntity>` and manage the cache yourself, or register your interceptor and accept the builtin re-cache will follow it.

## Removed inline helpers

The following private members of `EntityManager` are removed and have no public-API replacement (they were never public):

| Removed member | Replacement |
| -------------- | ----------- |
| `EntityManager.SetToCacheAsync` (private) | Builtin `CacheInterceptor.PostWriteAsync` (re-cache on `Create` / `Update` / `Restore` and soft-deletable `Remove`) |
| `EntityManager.EvictAsync` (private) | Builtin `CacheInterceptor.PostWriteAsync` (evict on non-soft-deletable `Remove` and on `HardDelete`) |

The protected `GenerateCacheKeys` / `GenerateCacheKey` extension points are **kept** — they are still used by `FindAsync`'s read-through `GetOrSetByKeyAsync` path, which remains inline (read-path caching is out of scope for this alignment).

If you subclassed `EntityManager` and called these helpers from an override (they were `protected`, not `private`, in some intermediate builds), the call will no longer compile. Move that logic into a `CacheInterceptor` or a custom interceptor registered via `WithInterceptor<T>()`.

## No change to cache backend packages

`IEntityCache<TEntity>`, `IEntityCacheKeyGenerator<TEntity>`, and the cache backend packages are unchanged:

- `Kista.Manager.EasyCaching` — registers `IEntityCache<TEntity>` through EasyCaching; consumed by the builtin `CacheInterceptor`.
- `Kista.Manager.MemoryCache` — registers `IEntityCache<TEntity>` through `Microsoft.Extensions.Caching.Memory`.
- `Kista.Manager.DistributedCache` — registers `IEntityCache<TEntity>` through `IDistributedCache`.
- `Kista.Manager.FusionCache` — registers `IEntityCache<TEntity>` through FusionCache.

No registration, configuration, or NuGet package reference needs to change. The interceptor is auto-appended to the pipeline when an `IEntityCache<TEntity>` is present in the DI container; you do not call `WithInterceptor<CacheInterceptor<,>>()` yourself.

## Read-through cache is unchanged

`FindAsync`'s read-through `GetOrSetByKeyAsync` stays inline in `EntityManager` — read-path caching is not part of this alignment. The builtin `CacheInterceptor` only owns the **write path** (`PostWriteAsync` after a successful repository write).

## Reference

- [Operation Pipeline](entity-manager/operation-pipeline.md) — the interceptor chain on `EntityManager`, including the [Builtin `CacheInterceptor`](entity-manager/operation-pipeline.md#builtin-cacheinterceptor) section
- [Caching Entities](entity-manager/caching-entities.md) — cache registration, key generators, serialization
- [Migrating from 1.7.2](migrating-from-1.7.2.md) — the prior 1.7.2 → 1.7.3 migration (obsolete-API removal and the operation pipeline introduction)
- [Soft-Delete Support](soft-delete.md) — `ISoftDeletable`, `RemoveAsync` vs. `HardDeleteAsync`, and `DeletedBy` audit stamping