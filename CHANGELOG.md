# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- **Builtin `CacheInterceptor<TEntity, TKey>`** — the entity cache is aligned to the operation pipeline, replacing the former inline `SetToCacheAsync` / `EvictAsync` helpers duplicated across the write methods of `EntityManager` with an interceptor that runs in `PostWriteAsync`.
  - `Create`, `Update`, and `Restore` re-cache the written entity; `Remove` re-caches soft-deletable entities and evicts non-soft-deletable ones; `HardDelete` evicts; cache failures are logged and swallowed.
  - The interceptor is appended to the chain only when an `IEntityCache<TEntity>` is registered, so the cache concern is removable for tests or custom cache strategies without subclassing the manager.
  - The private `SetToCacheAsync` / `EvictAsync` helpers and their nine inline call sites across the write methods are removed; `GenerateCacheKeys` / `GenerateCacheKey` are kept as protected extension points used by `FindAsync`'s read-through path (unchanged).
  - No change to `IEntityCache<TEntity>`, `IEntityCacheKeyGenerator<TEntity>`, or any `Kista.Manager.*` cache backend package — only the call site moves from inline helpers to `PostWriteAsync`.
  - See [Builtin `CacheInterceptor`](docs/entity-manager/operation-pipeline.md#builtin-cacheinterceptor).
- **`Migrating from 1.7.3`** guide — covers the `RemoveRangeAsync` cache-behavior change and the removal of the inline `SetToCacheAsync` / `EvictAsync` helpers. See [Migrating from 1.7.3](docs/migrating-from-1.7.3.md).

### Changed

- **`RemoveRangeAsync` cache behavior aligned with `RemoveAsync`**: soft-deletable entities in a range Remove are now re-cached (the cached entry is refreshed with the soft-delete stamp applied) instead of evicted, matching the single `RemoveAsync`; non-soft-deletable entities in a range Remove continue to be evicted.

### Fixed

- None.

### Chores

- Bump `@docusaurus/*` from 3.10.1 to 3.10.2 and override 11 transitive vulnerable dependencies in `website/package-lock.json` (7 high, 3 medium, 1 low — `brace-expansion`, `body-parser`, `js-yaml`, `webpack-dev-server`, `shell-quote`, `fast-uri`, `svgo`); `npm audit` reports 0 vulnerabilities.

## [1.7.3] - 2026-07-21

### Added

- **Extensible Operation Pipeline on `EntityManager`** — every write operation (`AddAsync`, `AddRangeAsync`, `UpdateAsync`, `RemoveAsync`, `RemoveRangeAsync`, `RestoreAsync`, `HardDeleteAsync`, `HardDeleteRangeAsync`) now runs through an ordered chain of interceptors that can observe, transform, or short-circuit each write before it reaches the repository, and react to it after it succeeds.
  - `IEntityManagerInterceptor<TEntity, TKey>` (and single-key `IEntityManagerInterceptor<TEntity>`) with `PreWriteAsync` / `PostWriteAsync`.
  - `IEntityOperationContext<TEntity, TKey>` carrying operation kind, mutable entity, pre-image, key, actor, timestamp, cancellation token, and a per-operation `Items` bag.
  - `EntityOperationKind` (`Create`, `Update`, `Remove`, `Restore`, `HardDelete`).
  - Short-circuit: a failed `IOperationResult` returned from `PreWriteAsync` skips the repository write and all downstream interceptors.
  - `EntityManagerBuilder.WithInterceptor<T>()` registration, resolved lazily from DI via `IEnumerable<IEntityManagerInterceptor<TEntity, TKey>>` — zero cost when none are registered.
  - Builtin `OnHooksEntityInterceptor` wraps the existing `protected virtual On*Async` hooks and is always appended last, so subclass overrides keep working and user interceptors run before timestamp / soft-delete stamping.
  - `RemoveRangeAsync` and `AddRangeAsync` now flow through the pipeline (one context per entity, mirroring the per-entity `On*Async` behavior).
  - See [Operation Pipeline](docs/entity-manager/operation-pipeline.md).
- **`UsingManager<TManager>()`** on `EntityManagerBuilder` — registers a custom `EntityManager` subclass against the entity / key types inferred from the manager type, replacing the obsolete `AddEntityManager<TManager>()` extension.
- **`RegisterAdditionalContextTypes()`** on `MongoRepositoryBuilder` — preserves tenant context type registrations (`IMongoDbTenantContext`, `MongoDbContext`, `MongoDbTenantContext`) previously handled by the obsolete `AddMongoDbContext`.
- **`Kista.SampleApp.OperationPipeline`** sample app — ASP.NET Core reference demonstrating `AuditInterceptor` and `BusinessHoursInterceptor` (short-circuit) wired through `WithInterceptor<T>()`. See [Sample Application](docs/sample-app.md).
- Test coverage raised above 90% via public-API tests; test suites reorganized into shared `RepositoryTestSuiteBase` / `EntityManagerTestSuiteBase` to deduplicate driver-specific tests.

### Removed

- **`IRepositoryController`, `DefaultRepositoryController`, `RepositoryControllerAdapter`, `RepositoryControllerOptions`** — removed. Use `IRepositoryLifecycleService` and the driver-specific lifecycle handlers (see [Repository Lifecycle](docs/repository-lifecycle/)).
- **Obsolete `IServiceCollection` extension methods** — all removed:
  - `AddRepository<T>()`, `AddRepository(Type)`, `AddRepository<TImplementation, TService>()`
  - `AddRepositoryController<T>()`, `AddRepositoryController()`
  - `AddEntityManager<TManager>()`, `AddManagerFor<TEntity>()`, `AddManagerFor<TEntity, TKey>()`
  - `AddEntityCacheOptions<T>()`, `AddEntityCacheKeyGenerator<T>()`, `AddEntityValidator<T>()`, `AddEntityRepository<T>()`
  - `AddMongoDbContext<TContext>()`
  - `AddEntityEasyCache<T>()`, `AddEntityEasyCacheConverter<T>()`
  - Use the fluent `AddRepositoryContext()` builder instead. See [Migrating from 1.7.2](docs/migrating-from-1.7.2.md).
- **`EntityManager.GetPageAsync(PageQuery<T>)`** — removed. Use `GetPageAsync(PageRequest)` for unsorted pages, or expose a domain-specific paged method on a custom repository via the protected `QueryPageAsync(PageQuery<T>)`.
- `BackwardCompatibilityTests` and obsolete-specific tests in `LifecycleTests` / `ServiceCollectionExtensionsTests` — deleted.
- `ParameterReplacer` expression visitor in `Kista` core — removed (only consumed by previously removed queryable-composition paths).

### Changed

- Repository registration logic inlined into `RepositoryContextBuilder`; the obsolete extension methods no longer delegate to a separate surface.
- `Kista.SampleApp.OperationPipeline` joins the samples solution alongside `Kista.SampleApp`, `Kista.SampleApp.Owners`, and `Kista.SampleApp.SoftDelete`.
- Sonar code-smell cleanup (43 PR code smells cleared) and new-code duplication reduced below 3%.
- Test interceptor helpers deduplicated across the operation-pipeline test suite.

### Fixed

- Reliability bugs flagged by Sonar resolved as part of the code-quality pass on PR #121.