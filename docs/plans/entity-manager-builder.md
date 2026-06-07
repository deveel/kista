# EntityManagerBuilder Plan

> **Status: Implemented** — See `EntityManagerBuilder.cs`, `RepositoryBuilderExtensions.cs`, and `EntityManagerBuilderTests.cs`.

## Goal
Create an `EntityManagerBuilder` that provides a unified, scoped fluent API for registering entity-specific services (validators, cache key generators, error factories, caching) within `WithManagement()`, harmonized with `RepositoryBuilder` (per-repository level).

## Design Decisions

| Decision | Choice |
|----------|--------|
| Per-entity cache options | **Provider-specific** — `EasyCachingOptions`, etc. |
| Builder surface area | **Callback return only** — `EntityManagerBuilder` is scoped via `RepositoryBuilder.WithManagement()` callback |
| Entity-scoped methods | **No `*For` suffix** — entity type implied from `RepositoryBuilder` context |
| Registration level | **Per-repository** — `WithManagement()` is on `RepositoryBuilder` (opt-in); global `WithManagement()` on `RepositoryContextBuilder` remains for bulk registration |

## API Surface

### Entry point — two `WithManagement()` overloads on `RepositoryBuilder` (extension methods in `Kista.Manager`)

```csharp
// Overload 1: Simple — registers EntityManager<TEntity, TKey> only
public static RepositoryBuilder WithManagement(
    this RepositoryBuilder builder,
    ServiceLifetime lifetime = ServiceLifetime.Scoped)

// Overload 2: Callback — registers EntityManager + configures optional services
public static RepositoryBuilder WithManagement(
    this RepositoryBuilder builder,
    Action<EntityManagerBuilder> configure,
    ServiceLifetime lifetime = ServiceLifetime.Scoped)
```

### EntityManagerBuilder methods (all return `EntityManagerBuilder`)

| Method | What it registers |
|--------|-------------------|
| `WithValidator<TValidator>()` | Scans `IEntityValidator<>` / `IEntityValidator<,>` interfaces on `TValidator`, filters for `EntityType` |
| `WithCacheKeyGenerator<TGenerator>()` | Scans `IEntityCacheKeyGenerator<>` interfaces on `TGenerator`, filters for `EntityType` |
| `WithOperationErrorFactory<TFactory>()` | Delegates to `services.AddOperationErrorFactory(EntityType, typeof(TFactory))` |
| `WithEasyCaching(Action<EasyCachingOptions>?)` | Per-entity EasyCaching (extension in `Kista.Manager.EasyCaching`) |

### Target fluent usage

```csharp
services.AddRepositoryContext()
    .AddRepository<PersonRepository>(repo => repo
        .WithManagement(mgmt => mgmt
            .WithValidator<PersonValidator>()
            .WithCacheKeyGenerator<PersonKeyGenerator>()
            .WithOperationErrorFactory<PersonErrorFactory>()
            .WithEasyCaching(opts => {
                opts.DefaultExpiration = TimeSpan.FromMinutes(15);
            })))
    .AddRepository<OrderRepository>(repo => repo
        .WithManagement())   // EntityManager only, no extras
    .WithManagement(mgmt => mgmt   // global — bulk register for all OTHER repos
        .WithEasyCaching())
    .UseInMemory();
```

## Files Created

### `src/Kista.Manager/EntityManagerBuilder.cs`

New class in `Kista` namespace with `internal` constructor accepting `RepositoryBuilder` + `ServiceLifetime`. No `*For` suffix on methods — entity type implied from context. Methods:

- `WithValidator<TValidator>()` — runtime scanning of `IEntityValidator<>` / `IEntityValidator<,>` filtered by `EntityType`, registered with `TryAdd`
- `WithCacheKeyGenerator<TGenerator>()` — runtime scanning of `IEntityCacheKeyGenerator<>` filtered by `EntityType`, registered with `TryAdd`
- `WithOperationErrorFactory<TFactory>()` — delegates to `services.AddOperationErrorFactory(EntityType, typeof(TFactory))`

### `src/Kista.Manager/RepositoryBuilderExtensions.cs`

Extension methods on `RepositoryBuilder`:

- `WithManagement(ServiceLifetime)` — registers `EntityManager<TEntity, TKey>` (or `EntityManager<TEntity>` if key is `object`)
- `WithManagement(Action<EntityManagerBuilder>, ServiceLifetime)` — same + configures optional services via callback

## Files Modified

### `src/Kista.Manager/RepositoryContextBuilderExtensions.cs`

- Fixed duplicate `TryAdd` (was registering `managerType` twice)
- Removed unused `repositoryInterface` variable

### `src/Kista.Manager.EasyCaching/Caching/RepositoryContextBuilderExtensions.cs`

- Added `EntityManagerBuilderExtensions.WithEasyCaching()` extension method

## What Stays Unchanged

- All `With*Caching()` methods on `RepositoryContextBuilder` (global config)
- All existing cache extension methods on `IServiceCollection`
- All existing tests (add new tests, don't break old)
- `WithManagement(Action<ManagementOptions>?, ServiceLifetime)` overload signature
- `AddOperationErrorFactory` on `IServiceCollection` (not obsolete)
- `MemoryCacheExtensions`, `FusionCache` and `DistributedCache` projects (not implemented yet)

## Migration Path

- Obsolete methods (`AddEntityValidator`, `AddEntityCacheKeyGenerator`, etc.) remain obsolete — their replacements live on `EntityManagerBuilder`
- Consider extracting internal helpers from obsolete `ServiceCollectionExtensions` methods to avoid duplication with `EntityManagerBuilder`
