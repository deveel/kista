# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- **`Kista.Manager.Events`** — a new opt-in base package providing a framework-agnostic domain event model for `EntityManager`, surfacing every meaningful lifecycle change (create, update, delete, restore) as a strongly-typed event through the [Operation Pipeline](docs/entity-manager/operation-pipeline.md).
  - `IEntityEventPublisher<TEntity>` abstraction and an `EntityEventData<TEntity>` base class with per-operation POCO subclasses — `EntityCreatedData<TEntity>`, `EntityUpdatedData<TEntity>` (carries the pre-image), `EntityDeletedData<TEntity>` (carries a `DeleteKind` `Soft`/`Hard` discriminator), and `EntityRestoredData<TEntity>`.
  - A builtin `EntityEventInterceptor<TEntity, TKey>` that publishes through `IEntityEventPublisher<TEntity>` in `PostWriteAsync` after a successful write; `Remove` maps to `EntityDeletedData { Soft }` when the entity implements `ISoftDeletable`, `{ Hard }` otherwise; `HardDelete` always maps to `{ Hard }`.
  - A default `InMemoryEntityEventPublisher<TEntity>` backed by an unbounded channel, with a `PublishedEvents` list for test assertions.
  - `WithEntityEvents()` registration on `EntityManagerBuilder` and `RepositoryContextBuilder`, wiring the interceptor and the in-memory publisher (Scoped, mirroring `WithValidator`/`WithEasyCaching`).
  - Publish failures are logged and swallowed, so an event outage never propagates to the caller of the write operation.
  - See [Domain Events](docs/entity-manager/domain-events.md).
- **`Kista.Manager.Hermodr`** — an adapter package that bridges `IEntityEventPublisher<TEntity>` with the [Hermodr](https://hermodr.deveel.org) CloudEvents framework.
  - A `HermodrEventPublisher<TEntity>` maps the base POCO data to canonical CNCF CloudEvents (`kista.entity.created`, `kista.entity.updated`, `kista.entity.deleted`, `kista.entity.restored`) and dispatches them through Hermodr's `IEventPublisher`; the `DeleteKind` is carried as a `kistadeletekind` extension attribute.
  - `WithHermodrEvents()` registration (mirroring `WithEasyCaching`) that calls `AddEventPublisher()` from `Hermodr.Publisher`, registers `HermodrEventPublisher` as `IEntityEventPublisher`, and registers `EntityEventInterceptor`.
  - Pluggable transports through Hermodr channels (Azure Service Bus, RabbitMQ, MassTransit, Webhook) with zero application code change; the transactional outbox is deferred to the v1.9.0 audit-trail milestone.
  - See [Domain Events](docs/entity-manager/domain-events.md).
- **`GetEntityKeyType(Type)`** on `RepositoryContextBuilder` — resolves the key type registered for a given entity type by scanning the registered `IRepository<,>` services, enabling cross-cutting registrations (such as `WithEntityEvents()`) to be applied to all tracked entity types.
- **Builtin `CacheInterceptor<TEntity, TKey>`** — the entity cache is aligned to the operation pipeline, replacing the former inline `SetToCacheAsync` / `EvictAsync` helpers duplicated across the write methods of `EntityManager` with an interceptor that runs in `PostWriteAsync`.
  - `Create`, `Update`, and `Restore` re-cache the written entity; `Remove` re-caches soft-deletable entities and evicts non-soft-deletable ones; `HardDelete` evicts; cache failures are logged and swallowed.
  - The interceptor is appended to the chain only when an `IEntityCache<TEntity>` is registered, so the cache concern is removable for tests or custom cache strategies without subclassing the manager.
  - The private `SetToCacheAsync` / `EvictAsync` helpers and their nine inline call sites across the write methods are removed; `GenerateCacheKeys` / `GenerateCacheKey` are kept as protected extension points used by `FindAsync`'s read-through path (unchanged).
  - No change to `IEntityCache<TEntity>`, `IEntityCacheKeyGenerator<TEntity>`, or any `Kista.Manager.*` cache backend package — only the call site moves from inline helpers to `PostWriteAsync`.
  - See [Builtin `CacheInterceptor`](docs/entity-manager/operation-pipeline.md#builtin-cacheinterceptor).
- **`Migrating from 1.7.3`** guide — covers the `RemoveRangeAsync` cache-behavior change and the removal of the inline `SetToCacheAsync` / `EvictAsync` helpers. See [Migrating from 1.7.3](docs/migrating-from-1.7.3.md).
- **`KistaParsingConfig`** (`Kista.DynamicLinq`) — a hardened `ParsingConfig` that blocks the `new` operator (`DisallowNewKeyword = true`) and fully-qualified type casts (`SupportCastingToFullyQualifiedTypeAsString = false`), closing remote-code-execution vectors when Dynamic LINQ expression strings originate from untrusted input. See [Dynamic LINQ Security](docs/filtering/dynamic-linq-security.md).

### Security

- **[Breaking] `AddHttpUserAccessor<TKey>` default chain is now claim-only** (`Kista.Owners`) — the query-string (`?user_id=`) and route (`userId`) fallback strategies have been removed from the default registration to prevent owner-scope impersonation by unauthenticated clients. Consumers who need the old behavior must explicitly opt in via `AddHttpUserAccessor<TKey>(b => b.AddClaim().AddQueryString().AddRoute())`. The query-string and route strategies are client-controlled and must only be enabled behind a trusted gateway.
- **[Breaking] Write paths verify ownership** (`Kista.Owners`) — `UpdateAsync`, `RemoveAsync`, and `RemoveRangeAsync` on `UserScopedRepositoryDecorator` now fetch the persisted entity and verify that its owner matches the current user before forwarding to the inner repository. An `UnauthorizedAccessException` is thrown on mismatch or when the entity cannot be found. Previously these methods forwarded directly with no owner check (IDOR on writes).
- **[Breaking] `UserScopingOptions.ThrowWhenUserNotSet` defaults to `true`** (`Kista.Owners`) — the decorator now fails closed by default: when no user identity is resolvable, operations throw `InvalidOperationException` instead of silently returning empty results. The XML doc previously documented `true` as the default while the actual default was `false`; the code now matches the documentation. Set `ThrowWhenUserNotSet = false` to restore the fail-open behavior.

### Changed

- **`RemoveRangeAsync` cache behavior aligned with `RemoveAsync`**: soft-deletable entities in a range Remove are now re-cached (the cached entry is refreshed with the soft-delete stamp applied) instead of evicted, matching the single `RemoveAsync`; non-soft-deletable entities in a range Remove continue to be evicted.
- **`FilterExpression` and `DynamicLinqFilter` now use `KistaParsingConfig`** instead of `ParsingConfig.Default` — the `new` operator and fully-qualified type casts in expression strings are blocked. Legitimate filters (property access, comparisons, boolean logic) are unaffected.

### Fixed

- **`UserScopingOptions.ThrowWhenUserNotSet` XML doc corrected** — the documentation claimed the default was `true` while the actual default was `false`; the default is now `true` and the documentation is accurate.

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