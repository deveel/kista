# Kista — Product Roadmap

The Kista framework provides a pragmatic, DDD-aligned abstraction for multi-source data access in .NET. At v1.4.3 the framework delivers solid CRUD interfaces, a composable `QueryBuilder`, multi-driver support (EF Core, MongoDB, In-Memory, DynamicLinq), and an `EntityManager` that layers validation and caching on top.

The roadmap below takes the framework from *useful primitives* to a *complete developer toolkit*: sharper setup ergonomics, richer entity lifecycle management, first-class observability, and new database drivers — all while preserving the pragmatic, low-ambition philosophy of the project.

---

## Release Timeline

| Version | Theme | Target | Focus |
|---------|-------|--------|-------|
| [**1.5.0**](#milestone-1-v150--solid-ground) | Solid Ground | Q2 2026 | Correctness, coherence, .NET 10 compatibility |
| [**1.6.0**](#milestone-2-v160--developer-flow) | Developer Flow | Q3 2026 | Unified setup, executable queries, DX improvements |
| [**1.7.0**](#milestone-3-v170--entity-lifecycle) | Entity Lifecycle | Q4 2026 (early) | Soft delete, state machine, autonomous timestamps |
| [**1.8.0**](#milestone-4-v180--scale--throughput) | Scale & Throughput | Q4 2026 (late) | Bulk operations, streaming, read/write split |
| [**1.9.0**](#milestone-5-v190--observability--governance) | Observability & Governance | Q1 2027 (early) | OpenTelemetry, audit trail, EF Core multi-tenancy |
| [**2.0.0**](#milestone-6-v200--platform-modernization) | Platform Modernization | Q1 2027 (late) | API modernization, source generators, strategic breaking changes |
| [**2.1.0**](#milestone-7-v210--new-database-drivers) | New Database Drivers | Q2 2027 | PostgreSQL, Cosmos DB, Dapper drivers |

---

## Milestone 1: v1.5.0 — "Solid Ground"

**Release Target:** Q2 2026  
**Theme:** Correctness, coherence, and runtime modernization

The framework is in production use, but carries visible rough edges: a misspelled package folder, thread-safety gaps in the in-memory driver, unoptimized expression compilation, and patchy XML documentation. v1.5.0 eliminates friction that slows adoption and erodes confidence — making every existing feature work exactly as documented, on .NET 8, 9, and 10.

---

### Feature — Package Namespace Correction

**Title:** Rename `Deveel.Repsotiory.MongoFramework` to `Kista.MongoFramework`

**Intent**  
> "Remove a visible typo that undermines confidence in the framework's quality and makes the MongoDB package harder to find on disk and in NuGet."

**The Problem Today**  
The MongoDB driver package lives in a source folder spelled `Deveel.Repsotiory.MongoFramework` — a transposition error that propagates into the solution file, CI scripts, and developer tooling. Contributors navigating the source tree must mentally correct the spelling; consumers upgrading packages see an inconsistent name in their dependency graph.

**What We Are Building**  
- Rename the source folder, project file, and NuGet package from `Repsotiory` to `Repository`
- Publish a NuGet deprecation notice on the old package identifier that redirects to the corrected one
- Update all solution references, CI/CD pipelines, and documentation links accordingly
- Provide a one-line migration note in the changelog (package rename only, no API or namespace change)

**Benefits**
- Professional, consistent package naming across the entire framework
- Reduced contributor confusion when browsing the codebase or searching NuGet
- Correct discoverability for the package and its documentation
- Sets a clean baseline before the v2.0 breaking-change window

**Status:** ✅ Completed.

---

### Feature — Thread-Safe In-Memory Repository

**Title:** Concurrent-Access Safety for In-Memory Storage

**Intent**  
> "Make the in-memory driver a trustworthy drop-in for integration tests and local development, regardless of how many threads are accessing it simultaneously."

**The Problem Today**  
`InMemoryRepository<TEntity, TKey>` uses an unguarded field-mapping cache when resolving entity properties. When multiple test threads or hosted-service tasks run concurrently against the same in-memory instance, race conditions can produce null-reference exceptions, cached-state corruption, or silent data loss — failures that are sporadic and notoriously hard to reproduce.

**What We Are Building**  
- Wrap the field-mapping cache with a `ReaderWriterLockSlim` or `Lazy<T>` guard
- Add a comprehensive concurrency test battery (100+ simultaneous readers and writers) to lock in the guarantee
- Document the thread-safety contract on the class and its core public methods
- Benchmark single-thread vs. multi-thread access to verify no regression on the happy-path speed

**Benefits**
- Reliable in-memory driver for any concurrency model: parallel test runners, background services, benchmarks
- Eliminates the sporadically-failing integration test that can affect consumer CI pipelines
- Documents the exact thread-safety contract consumers can rely on
- Builds confidence that switching from In-Memory to a real driver in production is functionally equivalent

**Status:** ✅ Completed.

---

### Feature — Expression Compilation Cache

**Title:** Cached Compilation of Filter Expressions in the DynamicLinq Driver

**Intent**  
> "Avoid paying the expression-compilation cost more than once for the same filter, so high-frequency query paths stay fast under real workloads."

**The Problem Today**  
The DynamicLinq driver recompiles filter expressions on every query invocation. In production applications — particularly multi-tenant products where the same filter shape runs thousands of times per minute — this creates avoidable CPU overhead that surfaces as elevated p95 latencies during traffic spikes. An `IFilterCache` interface already exists but has no eviction policy, no size limit, and no statistics.

**What We Are Building**  
- A bounded, thread-safe expression cache keyed on the normalized filter expression string
- Cache eviction policy (LRU by default) with a configurable maximum entry count
- An `IFilterCacheStatistics` service that exposes hit/miss counters and current size for monitoring
- `IFilterCache` exposed as a replaceable DI service for consumers needing custom eviction logic
- BenchmarkDotNet baselines quantifying the speedup between warm-cache and cold-cache scenarios

**Benefits**
- Measurably lower CPU utilization for repetitive query patterns without any code change in consumer applications
- A tunable cache that consumers can size to their workload
- Observable behavior — cache statistics are ready to be scraped by monitoring dashboards
- No behavioral change for query results; only compilation is cached, not result sets

**Status:** ✅ Completed. See [Filter Cache documentation](docs/dynamic-linq/filter-cache.md).

---

### Feature — Full .NET 10 Compatibility and Benchmark Baseline

**Title:** .NET 10 Runtime Validation with Per-Target Performance Baselines

**Intent**  
> "Allow teams already on .NET 10 to adopt Kista without surprises, and give everyone a reliable benchmark record for detecting future regressions."

**The Problem Today**  
The framework lists .NET 10 as a target framework but has not been thoroughly validated — the CI matrix does not cover it end-to-end, and no benchmarks quantify what the new runtime's LINQ optimization gains mean in practice. Teams adopting .NET 10 hit undiscovered edge cases and miss out on runtime improvements that require nothing more than an updated dependency.

**What We Are Building**  
- .NET 10.0 added to the CI build and test matrix alongside 8.0 and 9.0
- All `Microsoft.Extensions.*` and related packages updated to their latest stable versions for each target
- A BenchmarkDotNet baseline suite covering CRUD, pagination, and filter operations across all three runtimes
- A supported-frameworks table in the README and docs with a defined deprecation schedule

**Benefits**
- Teams on .NET 10 can adopt the framework with confidence from day one
- A benchmark baseline that catches performance regressions automatically in CI before they ship
- Clear signaling of which .NET versions are supported and for how long
- Alignment with Microsoft's LTS support windows reduces long-term maintenance burden

**Status:** ✅ Completed.

---

### Feature — XML Documentation Completeness

**Title:** Full IntelliSense Coverage for Every Public API

**Intent**  
> "Make every public class, interface, and method self-describing in the IDE, so developers spend less time reading source code and more time building features."

**The Problem Today**  
`EntityManager<TEntity>` and `EntityManager<TEntity, TKey>` — the most commonly used types across most consumer applications — have incomplete `<summary>`, `<param>`, and `<returns>` tags on many methods. Developers relying on IDE tooltips encounter blank descriptions, which increases the time-to-productivity cost for both new contributors and adopters.

**What We Are Building**  
- Complete XML documentation on every public API across the Manager, Core, and driver packages
- A CI check that fails the build on any public member without a `<summary>` tag
- Usage `<example>` blocks added to the ten most-used APIs
- Updated documentation reflecting the v1.5 API surface

**Benefits**
- Rich, always-accurate IntelliSense across all packages out of the box
- Lower barrier to entry for new contributors and library adopters
- Reduced reliance on reading source code for basic usage questions
- Improved long-term maintainability score

**Status:** ✅ Completed.

---

### Feature — Conversion to ValueTask Results for Asynchronous Methods

**Title:** Replace `Task<T>` with `ValueTask<T>` Where Applicable to Reduce Allocations

**Intent**  
> "Eliminate unnecessary heap allocations from synchronous code paths by returning `ValueTask<T>` instead of `Task<T>`, so high-throughput applications spend less time in the garbage collector."

**The Problem Today**  
Repository methods that complete synchronously (e.g., In-Memory finds that hit an immediate cache) still allocate a `Task<T>` object unnecessarily. In high-throughput scenarios with thousands of requests per second, these allocations accumulate and trigger frequent GC pauses that add p95 and p99 latency spikes.

**What We Are Building**  
- Systematic conversion of all async repository methods to return `ValueTask<T>` instead of `Task<T>`
- Synchronous driver paths (In-Memory, cached results) complete without allocation
- Async driver paths (database, I/O) continue to work without extra overhead
- Benchmarking that quantifies the allocation reduction and GC pause improvements
- Documentation on when `ValueTask<T>` is preferable and API compatibility considerations
- A breaking-change note in the v1.5 migration guide for consumers who have overridden or wrapped repository methods

**Benefits**
- Measurably lower GC pressure and heap allocations in high-throughput applications
- Faster request completion for cache-hit scenarios without any behavioral change
- Industry best-practice alignment — `ValueTask<T>` is now the standard for hot-path async methods
- No change to consumer code — the conversion is purely implementation-level

**Status:** ✅ Completed.

---

### Feature — General Performance Optimizations

**Title:** Systematic Analysis and Optimization of Hot Paths

**Intent**  
> "Profile the framework in realistic workloads and systematically eliminate unnecessary allocations, boxing, and inefficiencies so Kista never becomes a performance bottleneck."

**The Problem Today**  
The framework has not undergone systematic performance profiling under realistic load. Opportunities for optimization may exist in filter expression parsing, property mapping, and query translation that we have not yet measured or addressed. Teams adopting the framework need confidence that it will not become a scaling bottleneck.

**What We Are Building**  
- End-to-end profiling of common query patterns (find-by-key, filtered page, bulk update) across all drivers using BenchmarkDotNet
- Identification and elimination of boxing, unnecessary allocations, and redundant work in hot paths
- Expression compilation optimization via caching and pooling of compiled delegates
- Property mapping optimization using generated IL instead of reflection where performance-sensitive
- Memory pool allocation strategies for filter parsing and query building
- A published performance report quantifying improvements and setting baselines for regression detection

**Benefits**
- The framework never becomes a performance liability as applications scale
- Teams adopting Kista can do so without performance anxiety
- Regression-detection baselines prevent future changes from eroding throughput
- Open-source community gains visibility into the framework's performance characteristics and can contribute optimizations

**Status:** ✅ Completed.

---

## Milestone 2: v1.6.0 — "Developer Flow"

**Release Target:** Q3 2026  
**Theme:** Unified setup, executable queries, seamless DI

Today, wiring up Kista requires knowing the specific setup pattern for each driver, composing multiple `AddRepository*` calls correctly, and discovering which interfaces a repository exposes through trial and error. The `QueryBuilder<TEntity>` composes a query but does not execute it — there is always a boilerplate bridging step. v1.6.0 collapses that friction into a single discoverable setup API and closes the gap between composing a query and running it.

---

### Feature — Unified Repository Setup Builder

**Title:** Fluent, Driver-Agnostic Repository Registration for `IServiceCollection`

**Intent**  
> "Let developers configure all repositories — regardless of driver — through one consistent, discoverable DI setup pattern that they only need to learn once."

**The Problem Today**  
Each driver ships its own `AddRepository*` extension overloads with driver-specific parameters. A project using MongoDB as its primary store and In-Memory for test overrides must know two completely different setup APIs, keep them in sync, and mentally map how options flow through each. This discourages multi-driver setups and makes onboarding expensive.

**What We Are Building**  
- A top-level `AddRepositoryContext()` entry point on `IServiceCollection` that returns a fluent core `RepositoryContextBuilder`
- Entity-focused registration on the core builder with `.WithRepository<TRepository>(...)` methods to declare repository bindings explicitly
- Driver-specific `.UseMongoDB(...)`, `.UseEntityFramework(...)`, `.UseInMemory(...)` extensions that chain off the same core builder
- Cross-cutting configuration — caching, validation, multi-tenancy, logging — registered once on the core builder and automatically applied to all repositories
- Compatibility support for existing `AddRepository<TRepository>()` registrations, with a documented migration path to the builder API
- Assembly-scanning auto-discovery of repository implementations, removing the need to list each concrete type manually

**Benefits**
- A single learnable setup pattern regardless of which driver a project uses
- Cross-cutting infrastructure (caching, validation) configured in one place instead of duplicated per repository
- Shorter, more readable `Program.cs` / `Startup.cs`
- Easier driver switching (e.g. In-Memory in tests, MongoDB in production) through environment-conditional configuration

**Status:** ✅ Completed.

---

### Feature — QueryBuilder Execution Extensions

**Title:** Repository-Bound Execution Methods on `QueryBuilder<TEntity>`

**Intent**  
> "Close the gap between composing a query and running it — queries built with `QueryBuilder` should execute directly against a repository without additional bridging code."

**The Problem Today**  
`QueryBuilder<TEntity>` provides fluent filter and ordering composition but stops short of execution: calling code must unwrap the built `IQuery`, pass it to the `Repository<TEntity, TKey>` base-class protected methods (or the `IRepository<T>` extension methods), and handle the types separately. This imposes a two-API mental model — build here, run over there — and forces consumers to write boilerplate connecting the two sides.

**What We Are Building**  
- Execution extension methods on `QueryBuilder<TEntity>` accepting a repository parameter and returning the appropriate result type (`ValueTask<TEntity?>`, `IAsyncEnumerable<TEntity>`, `ValueTask<PageResult<TEntity>>`)
- Factory extensions directly on `IRepository<TEntity>` (and the `Repository<TEntity, TKey>` base class) for starting a bound fluent chain: `repository.Query().Where(...).OrderBy(...).GetPage(1, 20)`
- Backward-compatible with all existing `IQuery` and `PageQuery<TEntity>` usages — no existing code needs to change

**Benefits**
- A single API mental model: compose and execute a query in one fluent chain
- Reduces per-query boilerplate in application and service code
- IDE auto-complete makes all query capabilities discoverable starting from the repository reference
- Establishes a pluggable execution hook exploitable by future caching and tracing layering

---

### Feature — Pluggable Cache Provider Abstraction

**Title:** Decouple Entity Caching from the EasyCaching Library

**Intent**  
> "Allow teams to plug in any caching infrastructure they already own — in-process, Redis, or custom — without adding EasyCaching as an extra dependency."

**The Problem Today**  
`EntityManager` caching is wired exclusively to EasyCaching via `Kista.Manager.EasyCaching`. Teams using StackExchange.Redis directly, `Microsoft.Extensions.Caching.Memory`, or a corporate distributed cache cannot use the entity caching pipeline without pulling in EasyCaching as an intermediate layer — a dependency they did not choose and do not want.

**What We Are Building**  
- A formalized, stable `IEntityCache<TEntity>` contract (the interface exists today but is underspecified)
- A default in-process adapter using `Microsoft.Extensions.Caching.Memory` requiring no extra packages
- An optional `IDistributedCache`-based Redis adapter as a separate package
- The EasyCaching adapter retained as an optional package for consumers already invested in it
- Documentation of the caching contract so any third-party strategy can be plugged in

**Benefits**
- Teams bring their existing caching infrastructure without adopting a new library
- Reduced dependency graph for projects that only need simple in-process caching
- The distributed Redis adapter enables correct caching semantics in multi-instance deployments
- Swapping cache providers requires only a DI configuration change — no application code changes

**Status:** ✅ Completed.

---

### Feature — Automatic Timestamp and Ownership Management

**Title:** Auto-Populate `IHaveTimeStamp` and `IHaveOwner` Properties on Create and Update

**Intent**  
> "Let entities declare their timestamp and ownership fields and have the framework populate them automatically — no manual `entity.CreatedAt = DateTime.UtcNow` in consumer code, ever."

**The Problem Today**  
`IHaveTimeStamp` and `IHaveOwner<TKey>` are defined in Core as marker interfaces but carry no automation. Consumers must either remember to set `CreatedAt`, `UpdatedAt`, and `Owner` before calling the repository, or add a custom pre-save hook in every project. This is repetitive, easy to forget, and leads to inconsistent audit data across different code paths.

**What We Are Building**  
- Detection of `IHaveTimeStamp` on entities entering `AddAsync` and `UpdateAsync` in `EntityManager`
- Automatic assignment of `CreatedAt` on first persistence using an injected `ISystemTime` service
- Automatic refresh of `UpdatedAt` on every subsequent update
- Detection of `IHaveOwner<TKey>` and resolution of the current actor from the `IUserAccessor` service already present in Core
- An opt-out attribute for entities with custom timestamp or ownership logic

**Benefits**
- Zero-boilerplate audit timestamps and ownership — declare the interface, get the behavior
- Server-authoritative timestamps that do not trust client-submitted clock values
- `ISystemTime` injection keeps timestamps deterministic and testable in unit tests
- Reduces the number of cross-cutting concerns teams must wire up manually per entity type

**Status:** ✅ Completed.

---

### Feature — Repository Health Checks

**Title:** Built-In Health Checks for Repository Connectivity and Readiness

**Intent**  
> "Give operations teams a first-class way to verify repository availability at startup and runtime, without writing custom probes for every driver."

**The Problem Today**  
Teams deploying repository-backed services usually need liveness and readiness checks, but today each application must hand-roll its own checks for each driver. This causes duplicated code, inconsistent diagnostics, and delayed detection of connectivity or configuration failures.

**What We Are Building**  
- A `AddRepositoryHealthChecks()` integration that registers repository probes with ASP.NET Core Health Checks
- Driver-specific readiness checks for EF Core, MongoDB, In-Memory, and DynamicLinq-backed repositories
- Configurable probe behavior (timeouts, failure status, tags, and per-repository opt-in/out)
- Startup validation mode to fail fast when mandatory repositories are unreachable
- Standardized diagnostic payloads exposing driver type, repository type, and failure reason

**Benefits**
- Faster incident detection for data-access outages and configuration regressions
- Reduced boilerplate in consumer applications
- Consistent health semantics across all supported drivers
- Better Kubernetes/container orchestration behavior through reliable readiness signaling

---

### Feature — Repository Controller Lifecycle Redesign

**Title:** Driver-Specific Repository Lifecycle Controller for Create, Drop, and Seed

**Intent**  
> "Replace the generic lifecycle controller model with a clearer, driver-oriented abstraction that reliably handles repository creation, destruction, and deterministic seeding."

**The Problem Today**  
`IRepositoryController` and `DefaultRepositoryController` centralize lifecycle operations but are too generic for driver-specific behavior and offer limited seeding semantics. In practice, teams need different create/drop/seed behaviors per driver and per environment, and the current model is hard to extend without custom wiring.

**What We Are Building**  
- A redesigned lifecycle contract focused on explicit create/drop/seed operations with driver-specific implementations
- A core orchestration layer that resolves the right lifecycle handler per repository and driver
- Deterministic seeding support with idempotent execution and environment profiles (dev/test/production)
- Improved options model for lifecycle behavior (delete-if-exists, seed strategy, fail-fast rules, and logging)
- Backward-compatible adapter support for existing `IRepositoryController` usages during migration
- Updated DI registration guidance to use the new lifecycle model as the primary path

**Benefits**
- Clear separation of lifecycle logic between drivers without fragile conditionals
- Safer and more repeatable repository bootstrapping in tests and deployment pipelines
- Less custom infrastructure code for consumer applications
- A clean migration path from the current controller implementation to a more maintainable model

**Status:** ✅ Completed.

---

## Milestone 3: v1.7.0 — "Entity Lifecycle"

**Release Target:** Q4 2026 (early)  
**Theme:** Soft delete, entity states, domain event emission

Applications are not just about storing data — they care deeply about what *happened* to it. Records get logically retired, workflow statuses transition, and operations somewhere downstream need to react to those changes. v1.7.0 gives the framework its first first-class lifecycle story: transparent soft deletes that protect queries from deleted data, a fully-implemented state machine built on the nascent `States.Core` package, and observable CRUD events that decouple side-effects from the repository layer.

---

### Feature — Soft Delete Support

**Title:** Transparent Soft-Delete Filtering Across All Drivers

**Intent**  
> "Enable teams to retire records without physical deletion — and never worry about accidentally surfacing deleted data in normal application queries."

**The Problem Today**  
Regulatory, operational, and undo requirements push many teams toward logical deletion. Today this is entirely DIY: add a flag column, filter it manually in every query, and hope no new query path forgets the filter. The `IRepository.RemoveAsync` path has no hook to intercept and rewrite this as a soft delete; there is no convention at all.

**What We Are Building**  
- An `ISoftDeletable` marker interface carrying `DeletedAt` and `DeletedBy` properties
- Automatic rewriting of `RemoveAsync` into a soft-delete update when the entity implements `ISoftDeletable`
- A global query filter — applied at the driver level for EF Core and MongoDB — that transparently excludes soft-deleted records from all regular queries
- `IncludeDeleted()` and `OnlyDeleted()` query modifiers for administrative, reporting, or recovery contexts
- A `RestoreAsync` operation on `EntityManager` for undeleting a record
- Full driver coverage: EF Core global query filter; MongoDB `{DeletedAt: null}` filter; In-Memory inline filter

**Benefits**
- Compliance-ready data retention with zero query-level changes in normal application code
- No risk of accidentally surfacing deleted data in read or write operations
- Self-service data recovery through the strongly-typed `RestoreAsync` API
- Full audit trail of who deleted what and when, stored directly on the entity

---

### Feature — Entity State Machine

**Title:** First-Class State Transition Management via `States.Core`

**Intent**  
> "Give entities a governed state lifecycle — with declared transitions, transition history, and guard conditions — without making every team implement their own state machine from scratch."

**The Problem Today**  
`Kista.States.Core` contains two synchronous extension wrappers and a handful of empty interfaces (`IStateRepository`, `IEntity`, `StateInfo`). It is essentially a stub. Applications modeling workflow states (order status, subscription lifecycle, document approval) must build their own state management atop the raw repository layer, duplicating the same pattern across every project.

**What We Are Building**  
- A complete `IStateRepository<TEntity, TStatus>` backed by EF Core and MongoDB drivers
- A `StateTransition<TStatus>` descriptor declaring allowed states, valid transitions, and guard predicates
- State change history records (who transitioned, from which state, to which state, when) queryable via the repository
- `EntityManager` integration: `TransitionStateAsync` respects the declared transition map and emits pre- and post-transition events
- An opt-in `StateTransitionValidator` that plugs into the existing `IEntityValidator` pipeline to reject invalid transitions before they reach the database

**Benefits**
- Centralized, auditable state lifecycle instead of ad-hoc flag columns
- Invalid transitions are rejected with a well-typed exception before the database is touched
- Complete state history queryable through the repository without custom tables or triggers
- Reusable across any entity type: orders, subscriptions, documents, registrations

---

### Feature — Domain Event Emission from EntityManager

**Title:** Observable CRUD and State-Change Events from the Manager Layer

**Intent**  
> "Surface every meaningful lifecycle change as an observable event so downstream systems — notifications, search indexers, audit logs — can react without coupling to the repository internals."

**The Problem Today**  
Repository writes are fire-and-forget from the framework's perspective. Teams needing side effects (send a notification when an entity is created, reindex a record on update, log an audit entry on delete) add glue code inside controllers or application services — scattered, untestable, and easy to miss when new code paths are added.

**What We Are Building**  
- A typed `EntityEvent<TEntity>` envelope carrying operation type, entity snapshot, actor, and timestamp
- An `IEntityEventPublisher` interface registered as an optional DI service
- Hook points on `EntityManager` for `OnCreated`, `OnUpdated`, `OnDeleted`, and `OnStateChanged`
- A default MediatR-compatible publisher adapter as a small optional package
- An `IEntityEventHandler<TEntity>` convention-based subscriber for in-process handling

**Benefits**
- Side effects live in dedicated, independently testable handlers — not scattered across service classes
- Foundation for outbox and message-bus adapters targeted in later milestones
- Works with any in-process pub/sub: MediatR, .NET `IObservable`, or a custom implementation
- Every handler receives the same strongly-typed event regardless of which driver triggered it

---

### Feature — Specification Pattern Support

**Title:** Domain-Driven Querying via Composable Specifications

**Intent**  
> "Let teams encapsulate business rules into reusable, named specification objects that compose with AND/OR/NOT and produce driver-agnostic queries — bridging domain language and data access without coupling to LINQ or any specific query engine."

**The Problem Today**  
Kista's query pipeline is built on `IQueryFilter` and `IQuery` — infrastructure-level abstractions that carry no business meaning. A filter like `customer => customer.IsActive && customer.CreatedAt > cutoff` lives inline in service code, duplicated across methods, with no way to name, reuse, or compose it. Teams practicing Domain-Driven Design have no natural place to put domain rules that drive data retrieval, and the framework offers no abstraction between the domain layer and the query infrastructure.

**What We Are Building**  
- An `ISpecification<TEntity>` interface that produces an `IQuery` — Kista's driver-agnostic query contract — keeping specs decoupled from LINQ, EF Core, or any specific engine
- An abstract `Specification<TEntity>` base class with `&` (AND), `|` (OR), and `!` (NOT) operator overloads returning typed composite specifications
- `AndSpecification<TEntity>`, `OrSpecification<TEntity>`, and `NotSpecification<TEntity>` composite types that combine inner specs' queries at the filter level
- Extension methods on `IRepository<TEntity, TKey>` and `EntityManager<TEntity, TKey>` accepting `ISpecification<TEntity>` directly, calling `ToQuery()` internally and delegating to the existing query pipeline
- An optional `IsSatisfiedBy(TEntity)` method on the base class for in-memory evaluation (testing, validation rules) — not on the interface, so drivers never need it

**Benefits**
- Domain language appears in repository calls: `repository.FindAllAsync(new ActiveCustomerSpec())` instead of inline lambdas
- Specifications are reusable, testable, and composable — `activeCustomer & fromEurope` works out of the box
- Zero driver changes: specs produce `IQuery`, which every driver already understands
- Clean separation: the domain owns the spec, the infrastructure owns the query — no LINQ leaks into domain code
- Natural companion to soft-delete and state machine in the v1.7 lifecycle theme

**Status:** ✅ Completed. See [Specifications documentation](docs/specifications/).

---

## Milestone 4: v1.8.0 — "Scale & Throughput"

**Release Target:** Q4 2026 (late)  
**Theme:** Bulk operations, async streaming, read/write separation

Single-entity CRUD is not sufficient for serious workloads. Importing a product catalog, streaming an event feed, or running a nightly reconciliation all demand operations that move many entities at once, predictably, without materializing everything in memory. v1.8.0 introduces the throughput primitives that mid-to-large teams need.

---

### Feature — Batch Operations in EntityManager

**Title:** Validated Bulk Add, Update, and Remove with Partial-Failure Control

**Intent**  
> "Let teams import, sync, or purge hundreds or thousands of entities in one managed operation — with the same validation and event guarantees as single-entity calls, not just raw driver range methods."

**The Problem Today**  
`IRepository.AddRangeAsync` and `RemoveRangeAsync` exist at the driver level but bypass the `EntityManager` entirely: no validation, no cache invalidation, no event emission. Teams needing validated bulk inserts must loop over the single-entity `CreateAsync` on the manager, which is slow and lacks transactional semantics.

**What We Are Building**  
- `CreateManyAsync`, `UpdateManyAsync`, and `DeleteManyAsync` methods on `EntityManager<TEntity, TKey>`
- Validation runs concurrently per entity up to a configurable degree of parallelism
- Partial-failure mode: collect all validation errors before aborting, or abort on first failure
- Transactional mode: wrap the whole batch in a unit of work where the driver supports it
- Progress reporting via `IProgress<BatchOperationProgress>` for long-running import scenarios
- Batch event emission: one aggregated event per batch rather than N individual events

**Benefits**
- Import, sync, and migration workflows use the same single, well-tested code path
- Partial-failure collection eliminates the "fix one error, re-run, discover another" cycle
- Progress reporting enables responsive UIs for long data-import operations
- Consistent validation guarantees whether entities are created one at a time or in large batches

---

### Feature — Async Streaming Queries

**Title:** `IAsyncEnumerable<TEntity>` Streaming for Large Dataset Traversal

**Intent**  
> "Allow consumers to walk through large collections entity-by-entity without loading everything into memory — so data exports, ETL pipelines, and report generators stay memory-efficient at any scale."

**The Problem Today**  
All current query paths materialize results as `IReadOnlyList<TEntity>` or `PageResult<TEntity>`. Traversing a large collection — all customers for an export, all records for a nightly job — requires either iterating through pages with manual continuation logic or loading the full set at once. Neither approach is memory-efficient; both require repetitive plumbing code.

**What We Are Building**  
- An `IAsyncEnumerableRepository<TEntity>` capability interface with a `FindAllAsync()` returning `IAsyncEnumerable<TEntity>`
- Streaming implementations in each driver: EF Core `AsAsyncEnumerable()`, MongoDB cursor iteration, In-Memory yield-return
- A `.ToStreamAsync(repository)` terminal execution method on `QueryBuilder<TEntity>` for filtered streaming
- Configurable back-pressure and cancellation support throughout the streaming pipeline
- Documentation guidance on when paged queries are preferable vs. streaming

**Benefits**
- Memory-constant traversal of arbitrarily large collections with `await foreach`
- ETL, export, and analytics pipelines can process data as it arrives rather than waiting for full materialization
- Natural integration with the broader .NET streaming ecosystem (channels, Pipelines, etc.)
- Back-pressure support prevents overwhelming downstream consumers in producer/consumer scenarios

---

### Feature — Read/Write Repository Split

**Title:** Separate `IReadRepository` and `IWriteRepository` Capability Interfaces

**Intent**  
> "Make the distinction between reading and writing explicit in the type system — enabling CQRS-aligned designs, read-replica routing, and tighter security boundaries by construction."

**The Problem Today**  
All current repository interfaces are read-write, even when a consumer only needs read access. A query handler or a report generator has no type-system way to declare "this service should never write." Accidentally introducing a write into a read-only context is invisible to the compiler. CQRS patterns have no natural expression in the current interface hierarchy.

**What We Are Building**  
- `IReadRepository<TEntity, TKey>` covering `FindAsync`, `GetPageAsync`, `FindAllAsync`, `CountAsync`
- `IWriteRepository<TEntity, TKey>` covering `AddAsync`, `UpdateAsync`, `RemoveAsync`, `AddRangeAsync`
- `IRepository<TEntity, TKey>` remains the combined interface extending both — no breaking change
- The EF Core driver optionally routes `IReadRepository` calls to a configured read-replica connection string
- `EntityManager` exposes a `ReadOnly` property returning an `IReadRepository`-scoped facade

**Benefits**
- The type system enforces the read/write boundary that CQRS designs intend — the compiler becomes a design guardian
- Read-replica routing becomes a driver-level concern, transparent to all application code
- Security-minded teams can inject read-only repositories into controllers knowing the compiler prevents accidental writes
- A smaller, sharper interface to implement when building custom read-only repository adapters

---

## Milestone 5: v1.9.0 — "Observability & Governance"

**Release Target:** Q1 2027 (early)  
**Theme:** OpenTelemetry tracing, audit trails, EF Core multi-tenancy parity

Enterprise deployments need to *see* what data is doing: who changed it, which tenant owns it, how long queries take. v1.9.0 wires the framework into the standard .NET observability stack, adds query-accessible audit history, and delivers EF Core multi-tenancy on par with what MongoDB users have had since v1.x.

---

### Feature — OpenTelemetry Integration

**Title:** Distributed Tracing and Metrics for Every Repository Operation

**Intent**  
> "Make Kista a first-class citizen in distributed traces — so teams can see data-layer performance, correlate slowdowns with application spans, and alert on repository-level anomalies."

**The Problem Today**  
Repository operations are completely invisible to distributed tracing systems today. A slow query shows up in application metrics only as a slow HTTP response; there is no span distinguishing "EF Core `GetPageAsync` took 450ms" from "business logic took 10ms." Teams running on Kubernetes with Datadog, Grafana, or Azure Monitor have no repository-level drill-down capability.

**What We Are Building**  
- `System.Diagnostics.Activity` spans automatically created around every `IRepository` operation (find, get-page, add, update, remove)
- Standard OpenTelemetry semantic conventions for database spans (`db.system`, `db.name`, sanitized `db.statement`)
- Custom metrics: `repository.query.duration`, `repository.write.duration`, `repository.cache.hit_ratio`
- A `AddRepositoryTelemetry()` extension that wires the framework into the host's `TracerProvider` and `MeterProvider`
- Compatible with any OTLP backend: Jaeger, Zipkin, Datadog, Azure Monitor, Grafana LGTM

**Benefits**
- Data-layer performance is visible in every existing distributed tracing dashboard with zero code changes in consumer applications
- Teams can correlate slow HTTP responses with the specific repository operation causing the delay
- Proactive alerting on query latency or cache degradation using standard OTLP metrics
- Telemetry is additive — enabling or disabling it requires only a DI configuration change

---

### Feature — Audit Trail Support

**Title:** Automatic Change Attribution and Queryable Entity History

**Intent**  
> "Record who changed what data and when — automatically, consistently, and queryably — satisfying compliance requirements without custom audit infrastructure in every consuming project."

**The Problem Today**  
`IHaveTimeStamp` and `IHaveOwner` capture creation metadata but no change history. When an auditor asks "who changed this customer's credit limit last Tuesday?", teams today have no framework answer — they have database-level triggers, manual logging in service code, or simply no audit trail at all.

**What We Are Building**  
- An `IAuditableEntity` marker interface specifying `CreatedBy`, `CreatedAt`, `ModifiedBy`, and `ModifiedAt`
- An audit record model capturing entity key, change type, changed property names with old and new values, actor, and timestamp
- `EntityManager` hooks that write audit records automatically on every create, update, and delete
- An `IAuditRepository<TEntity>` for querying the audit history of a specific entity key or actor
- EF Core implementation via a dedicated audit table; MongoDB implementation via an embedded audit array on the document
- Optional PII masking through field-level redaction rules applied before the audit record is written

**Benefits**
- Compliance with GDPR, SOX, HIPAA, and similar audit requirements without bespoke infrastructure
- The "who changed this?" question answered in a single repository call
- PII masking ensures the audit trail itself is safe to store and surface
- The same `IUserAccessor` already present in Core provides consistent actor attribution across all code paths

---

### Feature — EF Core Multi-Tenancy Parity

**Title:** Tenant-Isolated EF Core Repositories on Par with MongoDB Multi-Tenancy

**Intent**  
> "Give teams building multi-tenant applications on relational databases the same one-API, zero-leakage tenancy experience that MongoDB users have today."

**The Problem Today**  
`Kista.MongoFramework.MultiTenant` provides a robust connection-per-tenant isolation model. The EF Core driver has no equivalent: teams building multi-tenant applications on SQL Server or PostgreSQL must implement tenant filtering manually through EF Core global query filters or per-query predicates — with no repository-level convention, tooling, or isolation guarantees from the framework.

**What We Are Building**  
- An `IEntityFrameworkTenantContext` service that provides the current tenant identifier to the EF Core driver
- Automatic global query filter registration for entities implementing `IHaveOwner<TTenantId>` — no per-query filtering required
- A schema-per-tenant strategy option routing each tenant to a named database schema
- A database-per-tenant strategy option using tenant-specific connection strings, paralleling the MongoDB model
- An integration adapter for `Finbuckle.MultiTenant` (the de-facto .NET multi-tenancy library)

**Benefits**
- SQL-backed multi-tenant applications use the same repository API as single-tenant ones
- Zero cross-tenant data leakage — isolation is enforced at the driver level, not trusted to application code
- Teams already using `Finbuckle.MultiTenant` get native integration without any glue code
- Feature parity with MongoDB multi-tenancy — driver choice no longer dictates multi-tenancy capability

---

## Milestone 6: v2.0.0 — "Platform Modernization"

**Release Target:** Q1 2027 (late)  
**Theme:** API modernization, source generators, strategic breaking changes

v1.5–v1.9 delivered capabilities incrementally and carefully. v2.0 takes the structural actions that backward compatibility prevented earlier: a simplified interface hierarchy, a .NET 9 minimum baseline, and introduces Roslyn source generators and a service-backed repository driver for distributed architectures.

---

### Feature — Minimum .NET 9 Baseline

**Title:** Drop .NET 8 Support and Adopt .NET 9+ Features Throughout the Codebase

**Intent**  
> "Free the codebase from the .NET 8 ceiling so it can fully embrace modern runtime improvements and language features — reducing complexity for maintainers and improving performance for consumers."

**The Problem Today**  
.NET 8 reaches end-of-Microsoft-support in November 2026. Maintaining conditional compilation paths for features available only in .NET 9+ (primary constructors, collection expressions, improved async primitives) adds noise to the codebase and delays adoption of runtime performance improvements. Every new feature must be designed within the .NET 8 constraint.

**What We Are Building**  
- Remove `net8.0` from all target framework monikers; ship `net9.0;net10.0` (with `net11.0` when available)
- Migrate to primary constructors, collection expressions, and `params ReadOnlySpan<T>` where they add clarity or performance
- Update the CI matrix to test only .NET 9 and 10
- Document v1.5.x as the maintained LTS-compatible line for teams still on .NET 8
- Publish a framework compatibility promise: current .NET version plus the previous LTS

**Benefits**
- Simpler codebase with fewer `#if` guards and conditional package version trees
- Access to .NET 9+ performance primitives (improved `IAsyncEnumerable`, frozen collections, better allocators)
- Smaller CI matrix — faster pipelines, less flakiness across matrix dimensions
- Aligned with Microsoft's support model: no obligation to support end-of-life runtimes

---

### Feature — Simplified Repository Interface Hierarchy

**Title:** Flatten the Dual-Generic Interface Model into a Single, Composable Set

**Intent**  
> "Eliminate the cognitive overhead of choosing between `IRepository<T>` and `IRepository<T, TKey>` by making one variant canonical with a sensible default, and removing the `object`-key workaround entirely."

**The Problem Today**  
The current design maintains a parallel universe of interfaces — `IRepository<T>` and `IRepository<T, TKey>` — and the `Repository<TEntity, TKey>` base class carries both the protected query hatch and the public contract. New users face a choice that should not exist. The `object`-key variant is a historical workaround that leaks through into the base class without providing meaningful type safety. (The legacy `IFilterableRepository`/`IPageableRepository`/`IQueryableRepository` capability interfaces were already removed in v1.7.1; the remaining duplication is the two-arity `IRepository` tier.)

**What We Are Building**  
- A single canonical `IRepository<TEntity, TKey>` with `string` as the conventional default key type for `IRepository<TEntity>`
- Removal of the `object`-key interface tier; a compatibility shim published as a separate package for the v1.x → v2.x upgrade window
- Query capabilities unified on the `Repository<TEntity, TKey>` base class and `IRepository<TEntity>` extension methods (no separate capability interfaces)
- A Roslyn-based migration code-fix that renames old interface references automatically
- Adapter base classes that ease the update of custom repository implementations

**Benefits**
- One mental model instead of two — new developers implement the right interface on the first attempt
- A smaller, more focused interface surface — easier to implement custom repository adapters
- The Roslyn migration aid removes 80% of the upgrade effort for consumer codebases
- The compatibility shim package allows gradual migration without a big-bang rewrite

---

### Feature — Remove Obsolete Capability Interfaces

**Title:** Drop `IQueryableRepository`, `IPageableRepository`, and `IFilterableRepository`

**Intent**  
> "Eliminate the three interfaces that were deprecated in v1.x — their functionality is now built directly into the abstract `Repository<TEntity, TKey>` base class, making the public contract simpler and removing confusion."

**The Problem Today**  
`IQueryableRepository<TEntity, TKey>`, `IPageableRepository<TEntity, TKey>`, and `IFilterableRepository<TEntity, TKey>` — and their single-T shims — carry `[Obsolete]` with `error = false`. Consumers can still implement or inject them, but the same capabilities (filtering, pagination, queryable access) are already provided by the `Repository<TEntity, TKey>` base class through protected members. Keeping the dead interfaces bloats the type hierarchy, clutters DI auto-registration, forces every concrete repository to maintain explicit interface implementations, and leaks internal abstractions (`IQueryable<T>`, `PageQuery<T>.Query`) into consumer code.

**What We Are Building**  
- Delete the 6 interface definition files from `src/Kista/`
- Remove `IFilterableRepository`, `IPageableRepository`, and `IQueryableRepository` from all concrete repository declarations (`MongoRepository`, `InMemoryRepository`, `EntityRepository`, `UserScopedRepositoryDecorator`, `RepositoryWrapper`) and drop their explicit interface method implementations
- Delete the two extension-method files (`QueryableRepositoryExtensions.cs`, `PageableRepositoryExtensions.cs`) that target these interfaces
- Remove the `AsFilterable()` and `AsQueryable()` helper extensions from `RepositoryExtensions.cs`; refactor internal methods (`ExistsAsync`, `CountAsync`, `FindFirstAsync`, `FindAllAsync`) that fall back to queryable casts
- Remove the 6 obsolete interface entries from DI registration (`RepositoryRegistrationUtil`) and assembly scanning (`RepositoryScanner`)
- Update `EntityManager<TEntity, TKey>` to use `Repository<TEntity, TKey>` base-class checks instead of casting to obsolete interfaces
- Update all affected test files to stop casting to or asserting on these interfaces

**Benefits**
- Smaller, clearer public API surface — no redundant interface paths
- Concrete repositories no longer carry stale explicit interface implementations
- DI registration and auto-discovery are simpler and faster
- Consumers are guided toward the single `Repository<TEntity, TKey>` base class instead of choosing between multiple capability interfaces
- Every `[Obsolete]` warning in v1.x codebases is resolved simply by upgrading to v2.0

**Status:** ✅ Completed in v1.7.1. See the [Migration from 1.7 guide](docs/migrating-from-1.7.md) for before/after patterns and migration steps.

---

### Feature — Repository Source Generators

**Title:** Compile-Time Repository Scaffolding via Roslyn Source Generators

**Intent**  
> "Generate the plumbing code for custom repository classes at compile time, so teams spend zero effort on boilerplate and can focus exclusively on the domain operations unique to their business."

**The Problem Today**  
Creating a custom repository — even a trivial one adding two domain-specific query methods — requires: implementing the right interface combination, wiring DI registration, propagating `CancellationToken` correctly, adding exception wrapping, and threading logging through the constructor. Teams copy-paste patterns from documentation or from each other, and the copies drift over time.

**What We Are Building**  
- A `[Repository]` attribute and Roslyn incremental source generator that emits full repository boilerplate for any class marked with it
- Generated code covers: interface implementation, constructor DI injection, `ILogger` plumbing, `CancellationToken` propagation, and standard `RepositoryException` wrapping
- Opt-in generation of a matching `EntityManager` subclass binding to the generated repository
- A `dotnet new deveel-repository` project template that scaffolds a full domain aggregate (entity, repository, manager, DI wiring) in one command
- Generated code is transparent, visible in IDE navigation, and fully overridable — no hidden magic

**Benefits**
- Custom repositories created in seconds with zero risk of forgetting cancellation, logging, or exception handling
- All generated repositories are idiomatic and consistent — copy-paste drift across a codebase is eliminated
- The `dotnet new` template gives new team members a correct, working starting point in one command
- Generated code is fully auditable, debuggable, and ejectable — developers always remain in control

---

### Feature — Specification Extension Code Generator

**Title:** Compile-Time Named Repository Extensions from Specification Classes

**Intent**  
> "Eliminate the boilerplate of wiring specification classes to repository calls by generating named, strongly-typed extension methods at compile time — so `ActiveCustomerSpecification` automatically produces `repository.FindAllActiveCustomersAsync()`."

**The Problem Today**  
The v1.7.0 specification pattern lets teams write `repository.FindAllAsync(new ActiveCustomerSpec())`, which is already an improvement over inline filters. However, calling code still constructs the specification object explicitly at every call site. For frequently-used specifications, teams naturally want a shorthand — but writing and maintaining `FindAllActiveCustomersAsync()` extension methods by hand for every specification is repetitive and drifts over time.

**What We Are Building**  
- A `[GenerateRepositoryExtensions]` attribute that can be placed on any class implementing `ISpecification<TEntity>`
- A Roslyn incremental source generator that detects decorated specification classes and emits a static extension class targeting `IRepository<TEntity>` (and optionally `EntityManager<TEntity, TKey>`)
- Generated method names derived from the specification class name: `ActiveCustomerSpecification` → `FindAllActiveCustomersAsync()`, `FindFirstActiveCustomerAsync()`, `CountActiveCustomersAsync()`, `ExistsActiveCustomerAsync()`
- Each generated method internally calls `new ActiveCustomerSpecification().ToQuery()` and delegates to the corresponding repository method
- The generator ships in the `Kista.SourceGenerators` package alongside the repository scaffolding generator from the previous feature
- Generated code is transparent, visible in IDE navigation, and fully overridable — consumers can always fall back to the generic API

**Benefits**
- Frequently-used specifications get a one-line call site: `await repository.FindAllActiveCustomersAsync()`
- No hand-written extension methods to maintain — the source of truth is the specification class itself
- Consistent naming convention across the entire codebase
- Opt-in per specification: only classes with the attribute trigger generation
- Complements the repository scaffolding generator — both ship in the same package

---

## Milestone 7: v2.1.0 — "New Database Drivers"

**Release Target:** Q2 2027  
**Theme:** PostgreSQL, Azure Cosmos DB, Dapper, and service-based repositories

v2.0 laid the architectural foundation with interface simplification and source generators. v2.1.0 delivers new first-party database drivers for PostgreSQL, Azure Cosmos DB, and Dapper, plus a service-based repository driver for distributed architectures — letting teams use the repository abstraction with any data store or remote service without writing custom adapter code.

---

### Feature — PostgreSQL Native Driver

**Title:** `Kista.PostgreSQL` — A JSONB-Backed Repository Driver

**Intent**  
> "Give the large and growing PostgreSQL community a first-party driver that exploits JSONB storage, native full-text search, and array operators — capabilities the generic EF Core driver cannot express."

**The Problem Today**  
PostgreSQL is the dominant open-source relational database, and many .NET teams choose it for JSONB storage, rich indexing, and built-in full-text search. Using it through the EF Core driver works but leaves its most powerful features unexploited. A dedicated driver can map Kista filter and query abstractions directly to PostgreSQL's native operators instead of going through LINQ's lowest-common-denominator translation.

**What We Are Building**  
- A `Kista.PostgreSQL` package built on Npgsql
- `IRepository<TEntity, TKey>` backed by a JSONB entity column with a typed primary key column
- Native mapping of `IQueryFilter` expressions to PostgreSQL JSONB path operators and index-aware queries
- Built-in full-text search support using `tsvector`/`tsquery` operators, implementing the `IFullTextSearchRepository` contract
- Geospatial query support via PostGIS when the extension is available
- Automatic GIN index creation for JSONB and full-text columns at schema migration time

**Benefits**
- Schema-less entity storage with the ACID guarantees and operational maturity of a relational engine
- Full-text search and geospatial queries without needing a dedicated search cluster
- Teams move from EF Core mappings to PostgreSQL-native features at zero application API change cost
- Npgsql binary protocol and connection-pooling optimizations apply by default

---

### Feature — Azure Cosmos DB Driver

**Title:** `Kista.CosmosDB` — Cloud-Native Driver for Azure Cosmos DB

**Intent**  
> "Give Azure-native teams a first-party Cosmos DB driver that handles partition key management, change feed processing, and multi-region failover transparently within the repository abstraction."

**The Problem Today**  
Azure Cosmos DB is the leading serverless, globally-distributed document database for Azure workloads. Teams using it today access it through the raw .NET SDK, losing all repository abstraction benefits. Partition key management, continuation tokens, and change feed processing are sharp edges that are well worth encapsulating.

**What We Are Building**  
- A `Kista.CosmosDB` package built on the `Microsoft.Azure.Cosmos` SDK v4
- Automatic partition key resolution from entity properties or a pluggable strategy
- Transparent continuation-token pagination mapped to the framework's `PageResult<TEntity>` API
- Change feed subscription mapped to the `IEntityEventPublisher` interface introduced in v1.7
- Multi-region failover and preferred-region configuration surfaced through driver options
- Both serverless and provisioned throughput modes supported with appropriate configuration guidance

**Benefits**
- Full repository abstraction over Cosmos DB — no raw SDK wiring in application code
- Partition key complexity is hidden behind driver configuration, not leaked into query expressions
- Change feed events feed the same event handlers as every other driver without any extra wiring
- Teams can prototype on In-Memory, validate on Cosmos DB, and switch with a single DI registration change

---

### Feature — Dapper Repository Driver

**Title:** `Kista.Dapper` — Lightweight Dapper-Backed Repository Implementation

**Intent**  
> "Give teams using Dapper as their data-access strategy a first-party driver that maps Kista abstractions directly to raw SQL and stored procedures — without forcing them into a full ORM."

**The Problem Today**  
Dapper is the most widely adopted micro-ORM in the .NET ecosystem: lightweight, fast, and SQL-first. Teams using it today must either hand-roll repository wrappers around every query (losing the framework's pluggable caching, validation, event emission, and health-check benefits) or adopt EF Core just to use the framework — a non-starter for SQL-optimization-conscious teams and legacy database migrations.

**What We Are Building**  
- A `Kista.Dapper` package built on `Dapper` and `Microsoft.Data.SqlClient` / Npgsql / generic `DbConnection`
- `IRepository<TEntity, TKey>` backed by convention-over-configuration SQL generation (or overridable per-operation SQL templates)
- Native stored procedure and batch-operation execution mapped to the batch and streaming APIs from v1.8
- Flexible entity-to-SQL mapping: table name, column mapping, key strategy configurable per entity
- Automatic `IDbConnection` lifecycle management from a registered `DbDataSource` or connection-string provider
- Full driver parity: filtering from `IQueryFilter` → parameterized `WHERE` clauses, pagination → `OFFSET`/`FETCH`, soft-delete filtering, and health checks

**Benefits**
- The most .NET-native ORM becomes a first-class Kista driver — no boilerplate wrappers, no vendor lock-in
- Teams keep their existing Dapper investment (raw SQL, existing procedures, performance-tuned queries) while gaining the full framework feature set
- The driver works against any ADO.NET provider: SQL Server, PostgreSQL, MySQL, SQLite — a single abstraction across heterogeneous databases
- A smooth migration path from hand-rolled Dapper repositories to a consistent, observable, and cache-aware pattern

---

### Feature — Service-Based Repository Driver

**Title:** `Kista.Services` — RESTful and gRPC Service-Backed Repository Implementation

**Intent**  
> "Allow distributed architectures where repositories delegate to remote services — enabling event-driven microservices, federation scenarios, and API aggregation patterns without abandoning type-safe repository abstractions."

**The Problem Today**  
Microservice architectures often require aggregating data from multiple remote services — an API gateway, a data service, a business logic service. Teams today reach for the HTTP client directly or custom REST wrappers, losing the repository abstraction and the consistency it provides. There is no natural way to express "this repository's data lives on a remote gRPC endpoint" within the framework.

**What We Are Building**  
- A `Kista.Services` package providing base implementations for remote repositories
- A RESTful channel builder and HTTP-based repository backend — maps `IRepository<TEntity>` CRUD and query operations to HTTP endpoints following REST conventions
- A gRPC channel builder and gRPC-based repository backend — maps repository operations to typed gRPC service calls
- Pluggable serialization and deserialization so teams can use their preferred serializer
- Distributed transaction coordination via transaction identifiers and two-phase commit patterns when both sides support it
- Automatically typed proxy generation from repository interfaces using source generators

**Benefits**
- Microservices architectures use the same repository abstraction as local data access
- API aggregation and data federation become first-class concerns in the repository layer, not lost in boilerplate HTTP plumbing
- Teams can switch between local and remote implementations with a single DI configuration change
- Event-driven workflows maintain transactional boundaries across service calls where supported
- gRPC performance benefits for service-to-service communication without adding a new abstraction layer

---

### Feature — Neo4j Repository Driver

**Title:** `Kista.Neo4j` — A Graph Database Repository Driver

**Intent**  
> "Give teams using Neo4j as their graph database a first-party driver that maps Kista abstractions directly to Cypher queries — enabling graph-traversal queries within the familiar repository abstraction."

**The Problem Today**  
Neo4j is the leading graph database in the .NET ecosystem, used for recommendation engines, fraud detection, identity graphs, and knowledge management. Teams using it today must either use the raw Neo4j .NET driver or the Neo4jClient library directly, losing all repository abstraction benefits. There is no framework-native way to express graph relationships, traversal queries, or property-graph operations within the repository pattern.

**What We Are Building**  
- A `Kista.Neo4j` package built on the official Neo4j .NET driver
- `IRepository<TEntity, TKey>` backed by Cypher queries against labeled nodes
- Native relationship mapping — entity properties mapped to node attributes, relationships mapped to navigation properties
- Graph-traversal query support via extension methods on `QueryBuilder<TEntity>` (shortest-path, variable-length patterns, neighborhood queries)
- Automatic index management for frequently-queried node properties and relationship types
- Support for the full framework feature set: filtering, pagination, soft-delete, async streaming, health checks

**Benefits**
- Full repository abstraction over Neo4j — no raw Cypher wiring in application code
- Graph-traversal queries accessible through the familiar `QueryBuilder` API
- Teams can prototype on In-Memory, validate on Neo4j, and switch with a single DI registration change
- Relationship management becomes a first-class concern in the repository layer
---

## Post-v2.0 Backlog

The following items are not committed to any specific release but are tracked for future consideration.

| Feature | Description | Priority |
|---------|-------------|----------|
| ElasticSearch Driver | `Kista.ElasticSearch` — first-party Elastic driver with native query DSL mapping | Medium |
| GraphQL Integration | Hot Chocolate / Strawberry Shake binding to expose repositories as a typed GraphQL API | Medium |
| Reactive Change Streams | `IAsyncObservable<TEntity>` + SignalR push delivery of entity change notifications | Low |
| Message Bus Adapters | Outbox pattern with Kafka, Azure Service Bus, and RabbitMQ adapters for `IEntityEventPublisher` | Medium |
| Analytics Repository Contracts | Read-optimized repository contracts for reporting and BI workloads | Low |

---

## Success Metrics

Each milestone release is considered successful when:

- All public APIs carry 100% XML documentation coverage — enforced in CI
- Test coverage ≥ 85% on Core and Manager packages
- No performance regressions vs. the previous milestone BenchmarkDotNet baseline
- A migration guide is published before the release NuGet packages land
- Zero P1 bug reports within 30 days of release
