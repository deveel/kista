# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- None.

### Changed
- None.

### Fixed
- None.

### Chores
- None.

## [1.7.6] - 2026-08-08

### Added

- **`KistaParsingConfig`** (`Kista.DynamicLinq`) — a hardened `ParsingConfig` that blocks the `new` operator (`DisallowNewKeyword = true`) and fully-qualified type casts (`SupportCastingToFullyQualifiedTypeAsString = false`), closing remote-code-execution vectors when Dynamic LINQ expression strings originate from untrusted input. See [Dynamic LINQ Security](docs/filtering/dynamic-linq-security.md).
- **`IQueryOptions.TrackEntities`** (`Kista.EntityFramework`) — opt-in flag for callers who need change tracking on entities returned by `FindFirstAsync`, `FindAllAsync`, and `GetPageAsync`; read paths default to `AsNoTracking()`.
- **`InMemoryEventPublisherOptions`** (`Kista.Manager.Events`) — capacity and `BoundedChannelFullMode` configuration for `InMemoryEntityEventPublisher`, replacing the unbounded channel with backpressure.
- **`DefaultEntityCacheKeyGenerator`** (`Kista.Manager`) — validates length, URL-encodes the key segment, and prefixes with the entity type name to prevent cache-key injection; used when no custom `IEntityCacheKeyGenerator` is registered.
- **`IncludeExceptionDetails`** on `HealthCheckEndpointOptions` (`Kista.Manager.AspNetCore.HealthChecks`) — gates `Description`/`Data` serialization into the health-check JSON response (default `false`); production emits aggregate status + entry names only.
- **`HealthCheckOptions.CacheDuration`** (`Kista.HealthChecks`) — per-probe result cache (default 5s) with `SemaphoreSlim(1,1)` coalescing of concurrent probes; `TestQuery` defaults to `false`.
- **`Migrating from 1.7.5`** guide — covers the owner-scope, IDOR, Dynamic LINQ, `AsNoTracking`, and health-check payload security changes. See [Migrating from 1.7.5](docs/migrating-from-1.7.5.md).

### Security

- **[Breaking] `AddHttpUserAccessor<TKey>` default chain is now claim-only** (`Kista.Owners`) — the query-string (`?user_id=`) and route (`userId`) fallback strategies have been removed from the default registration to prevent owner-scope impersonation by unauthenticated clients. Consumers who need the old behavior must explicitly opt in via `AddHttpUserAccessor<TKey>(b => b.AddClaim().AddQueryString().AddRoute())`. The query-string and route strategies are client-controlled and must only be enabled behind a trusted gateway.
- **[Breaking] Write paths verify ownership** (`Kista.Owners`) — `UpdateAsync`, `RemoveAsync`, and `RemoveRangeAsync` on `UserScopedRepositoryDecorator` now fetch the persisted entity and verify that its owner matches the current user before forwarding to the inner repository. An `UnauthorizedAccessException` is thrown on mismatch or when the entity cannot be found. Previously these methods forwarded directly with no owner check (IDOR on writes).
- **[Breaking] `UserScopingOptions.ThrowWhenUserNotSet` defaults to `true`** (`Kista.Owners`) — the decorator now fails closed by default: when no user identity is resolvable, operations throw `InvalidOperationException` instead of silently returning empty results. The XML doc previously documented `true` as the default while the actual default was `false`; the code now matches the documentation. Set `ThrowWhenUserNotSet = false` to restore the fail-open behavior.
- **[Breaking] `KistaParsingConfig` blocks `new` and fully-qualified casts** (`Kista.DynamicLinq`) — `FilterExpression.AsLambda` and `Compile` use `KistaParsingConfig` instead of `ParsingConfig.Default`; the `new` operator and fully-qualified type casts in expression strings are blocked at parse time. Legitimate filters (property access, comparisons, boolean logic) are unaffected.
- **[Breaking] Health-check JSON omits exception details by default** (`Kista.Manager.AspNetCore.HealthChecks`) — `Description`/`Data` serialization is gated behind `IHostEnvironment.IsDevelopment()` / `IncludeExceptionDetails` (default `false`); production emits only aggregate `Status` + entry names. Polymorphic `JsonSerializer.Serialize` on `Data` values replaced with `typeof(object)` + a cached `static readonly JsonSerializerOptions`.
- **`InMemoryEntityEventPublisher` bounded channel** (`Kista.Manager.Events`) — replaces the unbounded channel plus redundant `ConcurrentQueue` with `Channel.CreateBounded` and `BoundedChannelFullMode.Wait` for backpressure, removing unbounded memory growth / OOM risk under sustained write load.
- **`OperationErrorFactory.CreateError` no longer surfaces raw `exception.Message`** (`Kista.Manager`) — for non-`OperationException` cases, `errorMessage` is `null` (the error code conveys meaning; host maps to a localized message). `OperationException.Message` (application-controlled) is still honored.
- **`EntityMemoryCache` defaults to 5-minute expiration** (`Kista.Manager`) — aligning with `EntityDistributedCache` and `EntityFusionCache`; previously cached entities indefinitely when `EntityCacheOptions.Expiration` was not set.
- **`MongoRepository.Field(string)` validates against known members** (`Kista.MongoFramework`) — `fieldName` is validated against a cached `HashSet<string>` of entity members to prevent schema-oracle attacks from user-controlled input; method made `protected internal`.

### Changed

- **EF Core read paths use `AsNoTracking()` by default** (`Kista.EntityFramework`) — `FindFirstAsync`, `FindAllAsync`, and `GetPageAsync` no longer register returned entities in the `ChangeTracker`; callers who mutate returned entities set `IQueryOptions.TrackEntities = true` or call `UpdateAsync` explicitly.
- **`BoundedCache` uses `ReaderWriterLockSlim`** (`Kista.DynamicLinq`) — replaces the global `SemaphoreSlim(1,1)` so concurrent Dynamic LINQ filter cache reads no longer serialize through one lock; `SetCore` updates existing nodes in place instead of allocating new `CacheEntry` instances.
- **Mongo `CountAsync(IQueryable)` is now async** (`Kista.MongoFramework`) — uses `await queryable.ToAsyncEnumerable().CountAsync(ct).ConfigureAwait(false)` instead of the synchronous `queryable.Count()`; cancellation token honored.
- **`SoftDeleteRangeAsync` / `HardDeleteRangeAsync` build a `Dictionary` for O(1) lookup** (`Kista.EntityFramework`) — builds `Dictionary<TKey,TEntity>` from `Entities.Local` once instead of `Context.Entry(item)` per entity plus O(N) `Local.FirstOrDefault` scans, fixing O(N²) change-tracker cost.
- **`GetPageAsync` skips `CountAsync` for partial pages** (`Kista.EntityFramework`, `Kista.MongoFramework`) — when `items.Count < request.Size`, the total is implied and the count round-trip is skipped.
- **`EfQueryNormalizer` emits a bare `LIKE`** (`Kista.EntityFramework`) — drops the leading null predicates that could prevent the provider from using the index on the LIKE prefix; SQL `LIKE` is NULL-safe.
- **`RepositoryWrapper` caches compiled filter delegates** (`Kista`) — `Expression.Compile()` runs once per filter identity (keyed in a `ConcurrentDictionary`) instead of per call.
- **`EntityRepository.GetEntityKey` caches the `IGetter`** (`Kista.EntityFramework`) — captures `PrimaryKey.Properties[0]` + `IGetter` in `readonly` fields at construction instead of allocating `PrimaryKey.Properties.ToList()` per call.
- **`EntityRepository.AddRangeAsync` skips intermediate materialization** (`Kista.EntityFramework`) — when `OnAddingEntity` is identity (default), `entities` passes straight through instead of `.Select(OnAddingEntity).ToList()`.
- **`HermodrEventPublisher` caches the source `Uri`** (`Kista.Manager.Hermodr`) — stores the `Uri` in a `readonly` field instead of allocating a new string + `Uri` per event.
- **`MongoRepository.Collection` cached in `Lazy<>`** (`Kista.MongoFramework`) — `IMongoCollection<TEntity>` resolved once via `readonly Lazy<IMongoCollection<TEntity>>` instead of re-resolving `EntityMapping.GetOrCreateDefinition` + `GetDatabase().GetCollection` per access.
- **`FindIncludingDeletedAsync` caches `PropertyInfo`** (`Kista.Manager`) — `typeof(TEntity).GetProperty("Id")` is cached in a `static readonly` field per closed generic type instead of a metadata scan per call.
- **Mongo multi-tenant setup caches a compiled factory** (`Kista.MongoFramework.MultiTenant`) — `ActivatorUtilities.CreateInstance` per request replaced with a cached `Func<IServiceProvider, object>` per closed context type in a `static ConcurrentDictionary`; removes reflection on the private `_contextType` field.
- **19 sync-over-async wrappers in `RepositoryExtensions` marked `[Obsolete]`** (`Kista`) — each carries `"Use the async overload. Sync-over-async can cause threadpool starvation."`.
- **`InMemoryRepository.Dispose` no longer nulls the entities field** (`Kista.InMemory`) — was breaking in-flight readers with `NullReferenceException`.
- **Deduplicate health-check cache probe, range-delete core, and no-user test arrange** — `RepositoryHealthCheckBase` gains a protected `ExecuteCachedProbeAsync` helper; EF and Mongo health checks delegate to it. `EntityRepository_T2` extracts shared `ApplyRangeStateAsync` for soft/hard range delete. `UserScopedRepositorySecurityTests` extracts `BuildNoUserServices`.
- **SonarQube clear-down on PR #125** — 2× S2955 reliability bugs, 14 maintainability smells, ternary assignments, and redundant `ToString` resolved; new-code duplicated-lines density reduced from ~8% to near 0%.

### Fixed

- **`UserScopingOptions.ThrowWhenUserNotSet` XML doc corrected** — the documentation claimed the default was `true` while the actual default was `false`; the default is now `true` and the documentation is accurate.

### Chores

- Bump `System.Linq.Dynamic.Core` from 1.7.2 to 1.7.3 to absorb security patches.
- Add `dotnet list package --vulnerable --include-transitive` step to the CI pipeline to catch future CVEs.
- Add `EntityFramework.MultiTenant`, `Owners`, `Manager.Events`, `Manager.Hermodr`, `Manager.FusionCache`, and `Manager.DistributedCache` to the package cleanup workflow; re-add FusionCache and DistributedCache after the missing-package failure.
- Add missing `Release|x64/x86` and `Debug|x64/x86` solution configurations for `Kista.Manager.FusionCache`, `Kista.Manager.DistributedCache`, and `Kista.Manager.MemoryCache.XUnit` in `Kista.sln` so `dotnet pack -c Release` no longer skips these projects.
- PII logging guidance added to `LoggerExtensions` for `{EntityKey}` / `{Query}` parameters; `HermodrEventPublisher` XML doc documents synchronous-through behavior; `EntityRepository.Queryable()` XML doc documents `AsSplitQuery` guardrail; `ApplySoftDeleteMode` XML doc warns about `IgnoreQueryFilters` × tenant-filter interaction.

## [1.7.5] - 2026-08-05

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

### Changed
- None.

### Fixed
- None.

### Chores

- Removed the `Kista.SampleApp.DomainEvents` sample from `Kista.sln` (net9.0-only, incompatible with the net8.0 CI matrix leg); the sample now ships its own `.slnx`, aligning with the `OperationPipeline`/`SoftDelete`/`Owners` sample pattern.
- `Kista.sln`, `Directory.Packages.props`, and `website/sidebars.ts` updated for the two new packages, the new test projects (`Kista.Manager.Events.XUnit`, `Kista.Manager.Hermodr.XUnit`), and the Domain Events sample; `ROADMAP.md` updated, with the Domain Events feature marked ✅ Completed.

## [1.7.4] - 2026-08-02

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