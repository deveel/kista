# Roadmap
The Kista framework provides a pragmatic, DDD-aligned abstraction for multi-source data access in .NET. Below is the planned roadmap of future releases.

See the [full roadmap](https://github.com/deveel/kista/blob/main/ROADMAP.md) for detailed feature descriptions, timelines, and architectural decisions.

## Release Timeline

| Version | Theme | Target | Focus |
|---------|-------|--------|-------|
| **1.5.0** | Solid Ground | Q2 2026 | Correctness, coherence, .NET 10 compatibility |
| **1.6.0** | Developer Flow | Q3 2026 | Unified setup, executable queries, DX improvements |
| **1.7.0** | Entity Lifecycle | Q4 2026 (early) | Soft delete, state machine, autonomous timestamps |
| **1.8.0** | Scale & Throughput | Q4 2026 (late) | Bulk operations, streaming, read/write split |
| **1.9.0** | Observability & Governance | Q1 2027 (early) | OpenTelemetry, audit trail, EF Core multi-tenancy |
| **2.0.0** | Platform Modernization | Q1 2027 (late) | API modernization, source generators, strategic breaking changes |
| **2.1.0** | New Database Drivers | Q2 2027 | PostgreSQL, Cosmos DB, Dapper drivers |

## Milestone 1: v1.5.0 — "Solid Ground"

**Release Target:** Q2 2026  
**Theme:** Correctness, coherence, and runtime modernization

Features: Package Namespace Correction, Thread-Safe In-Memory Repository, Expression Compilation Cache (✅ completed), Full .NET 10 Compatibility and Benchmark Baseline, XML Documentation Completeness, Conversion to ValueTask Results, General Performance Optimizations.

## Milestone 2: v1.6.0 — "Developer Flow"

**Release Target:** Q3 2026  
**Theme:** Unified setup, executable queries, seamless DI

Features: Unified Repository Setup Builder, Pluggable Cache Provider Abstraction, Automatic Timestamp and Ownership Management, Repository Health Checks, Repository Controller Lifecycle Redesign.

### Completed in v1.6.0

- **QueryBuilder Execution Extensions**: `QueryBuilder<TEntity>` now implements `IQueryBuilder<TEntity>` with terminal methods (`FirstOrDefaultAsync`, `ToListAsync`, `CountAsync`, `AnyAsync`, `GetPageAsync`). `Repository.CreateQuery()` returns a repository-bound builder that dispatches through the protected pipeline.
- **Lifecycle Service Renamed**: `IRepositoryLifecycleOrchestrator` → `IRepositoryLifecycleService` (clearer naming)
- **Builder Extensions**: `WithLifecycleHandler<TEntity, THandler>()` and `WithLifecycleProfile<TProfile>()` for fluent registration
- **Default Lifecycle Profile**: `ConfigureLifecycle()` now auto-registers `DefaultRepositoryLifecycleProfile`
- **Sample Application**: `Kista.SampleApp` with full lifecycle management and CRUD endpoints
- **Owner Scoping via Decorator**: `Kista.Owners` package with `UserScopedRepositoryDecorator` — automatic owner assignment and query filtering via `.WithOwnerScoping()` (replaces old `IUserRepository`/`EntityUserRepository`/`InMemoryUserRepository` pattern)

## Milestone 3: v1.7.0 — "Entity Lifecycle"

**Release Target:** Q4 2026 (early)  
**Theme:** Soft delete, entity states, domain event emission

Features: Soft Delete Support, Entity State Machine, Domain Event Emission from EntityManager.

### Completed in v1.7.0

- **Specification Pattern Support**: `ISpecification<TEntity>` and `Specification<TEntity>` base class with AND/OR/NOT composition. Extension methods on `IRepository` (`FindFirstAsync`, `FindAllAsync`, `CountAsync`, `ExistsAsync`) and `EntityManager` for domain-driven querying. See [Specifications documentation](specifications/).

## Milestone 4: v1.8.0 — "Scale & Throughput"

**Release Target:** Q4 2026 (late)  
**Theme:** Bulk operations, async streaming, read/write separation

Features: Batch Operations in EntityManager, Async Streaming Queries, Read/Write Repository Split.

## Milestone 5: v1.9.0 — "Observability & Governance"

**Release Target:** Q1 2027 (early)  
**Theme:** OpenTelemetry tracing, audit trails, EF Core multi-tenancy parity

Features: OpenTelemetry Integration, Audit Trail Support, EF Core Multi-Tenancy Parity.

## Milestone 6: v2.0.0 — "Platform Modernization"

**Release Target:** Q1 2027 (late)  
**Theme:** API modernization, source generators, strategic breaking changes

- **Minimum .NET 9 Baseline:** Drop .NET 8 support, adopt .NET 9+ features.
- **Simplified Repository Interface Hierarchy:** Flatten the dual-generic interface model.
- **Repository Source Generators:** Compile-time repository scaffolding via Roslyn source generators.

## Milestone 7: v2.1.0 — "New Database Drivers"

**Release Target:** Q2 2027  
**Theme:** PostgreSQL, Azure Cosmos DB, Dapper, and service-based repositories

- **PostgreSQL Native Driver:** A JSONB-backed repository driver built on Npgsql.
- **Azure Cosmos DB Driver:** Cloud-native driver for Azure Cosmos DB.
- **Dapper Repository Driver:** Lightweight Dapper-backed repository implementation.
- **Service-Based Repository Driver:** RESTful and gRPC service-backed repository implementation.
- **Neo4j Repository Driver:** A graph database repository driver built on the Neo4j .NET driver.

## Success Metrics

Each milestone release is considered successful when:

- All public APIs carry 100% XML documentation coverage — enforced in CI
- Test coverage ≥ 85% on Core and Manager packages
- No performance regressions vs. the previous milestone BenchmarkDotNet baseline
- A migration guide is published before the release NuGet packages land
- Zero P1 bug reports within 30 days of release
