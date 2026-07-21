# Migrating from 1.7.2 to 1.7.3

The **1.7.3 — Extensible Operation Pipeline & Obsoletes Cleanup** release ships two coordinated changes:

1. **New feature** — an extensible operation pipeline on `EntityManager` for cross-cutting concerns (audit, events, tracing, validation short-circuit). See [Operation Pipeline](entity-manager/operation-pipeline.md).
2. **Breaking removal** — every `[Obsolete]` member in the framework is gone. Code that still referenced the legacy registration extensions, the repository controller, or `EntityManager.GetPageAsync(PageQuery<T>)` will no longer compile.

This page covers the **breaking** half. For the prior 1.7.0 → 1.7.1 migration (`IQueryableRepository` / `IPageableRepository` / `IFilterableRepository` removal), see [Migrating from 1.7](migrating-from-1.7.md).

## Why the obsolete APIs were removed

The framework has been nudging consumers toward the fluent `AddRepositoryContext()` builder since v1.6.0. The legacy `IServiceCollection` extensions and `IRepositoryController` family were marked `[Obsolete]` through 1.7.x to give migration time; 1.7.3 closes that window. Keeping them any longer would mean maintaining two parallel registration surfaces indefinitely — and the new operation pipeline is wired through the builder, not the legacy extensions, so the legacy surface could not have gained the feature anyway.

## Migration table

| Before (1.7.2) | After (1.7.3) |
| -------------- | ------------- |
| `services.AddRepository<TRepo>()` | `services.AddRepositoryContext().AddRepository<TRepo>(_ => { })` then `.UseInMemory()` / `.UseEntityFramework<TContext>()` / `.UseMongoDB<TContext>()` |
| `services.AddRepository<TImpl, TService>()` | Register the concrete via `.AddRepository<TImpl>()` and resolve the service interface from DI |
| `services.AddRepositoryController<T>()` / `AddRepositoryController()` | `services.AddRepositoryContext().UseInMemory(b => b.WithLifecycle())` and resolve `IRepositoryLifecycleService` — see [Repository Lifecycle](repository-lifecycle/) |
| `IRepositoryController` / `DefaultRepositoryController` / `RepositoryControllerAdapter` / `RepositoryControllerOptions` | `IRepositoryLifecycleService` and driver-specific `IRepositoryLifecycleHandler<T>` implementations |
| `services.AddEntityManager<TManager>()` | `services.AddRepositoryContext().AddRepository<TEntityRepository>(repo => repo.WithManagement(mgmt => mgmt.UsingManager<TManager>()))` |
| `services.AddManagerFor<TEntity>()` / `AddManagerFor<TEntity, TKey>()` | `.WithManagement()` on the `RepositoryBuilder` (registers `EntityManager<TEntity>` / `EntityManager<TEntity, TKey>` automatically) |
| `services.AddEntityCacheOptions<T>()` | `.WithManagement(mgmt => mgmt.WithEasyCaching(opts => { ... }))` |
| `services.AddEntityCacheKeyGenerator<T>()` | `.WithManagement(mgmt => mgmt.WithCacheKeyGenerator<T>())` |
| `services.AddEntityValidator<T>()` | `.WithManagement(mgmt => mgmt.WithValidator<T>())` |
| `services.AddEntityRepository<T>()` | `.AddRepository<T>()` on the `RepositoryContextBuilder` |
| `services.AddMongoDbContext<TContext>()` | `services.AddRepositoryContext().UseMongoDB<TContext>()` — tenant context types (`IMongoDbTenantContext`, `MongoDbContext`, `MongoDbTenantContext`) are registered via `MongoRepositoryBuilder.RegisterAdditionalContextTypes()` |
| `services.AddEntityEasyCache<T>()` / `AddEntityEasyCacheConverter<T>()` | `.WithEasyCaching()` on the `RepositoryBuilder` / `RepositoryContextBuilder` |
| `manager.GetPageAsync(PageQuery<T>)` | `manager.GetPageAsync(new PageRequest(page, size))` for unsorted pages; for filtered/sorted pages expose a domain-specific paged method on a custom repository via the protected `QueryPageAsync(PageQuery<T>)` |

## Replace `AddRepository<T>()`

**Before (1.7.2):**

```csharp
services.AddRepository<InMemoryRepository<Person>>();
services.AddRepository<PersonRepository, IPersonRepository>();
```

**After (1.7.3):**

```csharp
services.AddRepositoryContext()
    .AddRepository<PersonRepository>(_ => { })   // registers PersonRepository, IPersonRepository, IRepository<Person>
    .UseInMemory();                              // also registers open-generic InMemoryRepository<Person>
```

> Closed repositories registered via `AddRepository<T>()` take precedence over the open generic for that specific entity. The two registration styles coexist — see [Registration](custom-repository/registration.md).

## Replace `AddEntityManager<TManager>()`

The replacement is **`UsingManager<TManager>()`** on `EntityManagerBuilder`. It inspects the manager type via reflection to infer the entity (and key) types, mirroring what `AddEntityManager<TManager>()` did internally.

**Before (1.7.2):**

```csharp
services.AddRepository<OrderRepository>();
services.AddEntityManager<OrderManager>();
```

**After (1.7.3):**

```csharp
services.AddRepositoryContext()
    .AddRepository<OrderRepository>(repo => repo
        .WithManagement(mgmt => mgmt
            .WithValidator<OrderValidator>()
            .UsingManager<OrderManager>()))
    .UseInMemory();
```

The base `EntityManager<Order>` is still registered automatically by `WithManagement()`, so DI continues to resolve both the base type and your custom subclass.

## Replace `AddMongoDbContext<TContext>()`

The obsolete extension handled three concerns at once: registering the Mongo context, registering repositories, and registering tenant context types. The builder splits them cleanly.

**Before (1.7.2):**

```csharp
services.AddMongoDbContext<TenantMongoContext>();
```

**After (1.7.3):**

```csharp
services.AddRepositoryContext()
    .UseMongoDB<TenantMongoContext>(b => b
        .WithConnectionString("mongodb://..."));
```

For multi-tenant contexts that previously surfaced `IMongoDbTenantContext` / `MongoDbTenantContext` as DI services, `MongoRepositoryBuilder.RegisterAdditionalContextTypes()` is now called internally by `UseMongoDB<TContext>()` when the context implements the tenant interfaces — no manual call is required in consumer code. Custom driver authors who build a `MongoRepositoryBuilder` directly should call `RegisterAdditionalContextTypes()` once after construction.

## Replace `IRepositoryController` and `DefaultRepositoryController`

`IRepositoryController` was a generic create/drop/seed surface that did not map cleanly to driver-specific behavior. It is replaced by `IRepositoryLifecycleService` and driver-specific `IRepositoryLifecycleHandler<T>` implementations — see [Repository Lifecycle](repository-lifecycle/).

**Before (1.7.2):**

```csharp
services.AddRepositoryController<ContactRepository>();
// ...
var controller = provider.GetRequiredService<IRepositoryController>();
await controller.CreateRepositoryAsync<Contact, Guid>(ct);
await controller.SeedRepositoryAsync<Contact, Guid>(seedData, ct);
```

**After (1.7.3):**

```csharp
services.AddRepositoryContext()
    .UseInMemory(b => b.WithLifecycle())
    .ConfigureLifecycle(options => {
        options.SeedStrategy = SeedStrategy.ByEnvironment;
    })
    .WithLifecycleHandler<Contact, ContactLifecycleHandler>();

// ...
var lifecycle = provider.GetRequiredService<IRepositoryLifecycleService>();
await lifecycle.CreateRepositoryAsync<Contact, Guid>(ct);
await lifecycle.SeedRepositoryAsync<Contact, Guid>(null, ct);
```

## Replace `AddEntityEasyCache<T>()` and cache-key generators

**Before (1.7.2):**

```csharp
services.AddRepository<PersonRepository>();
services.AddEntityEasyCache<Person>();
services.AddEntityCacheKeyGenerator<PersonCacheKeyGenerator>();
```

**After (1.7.3):**

```csharp
services.AddRepositoryContext()
    .AddRepository<PersonRepository>(repo => repo
        .WithManagement(mgmt => mgmt
            .WithEasyCaching(opts => opts.DefaultExpiration = TimeSpan.FromMinutes(15))
            .WithCacheKeyGenerator<PersonCacheKeyGenerator>()))
    .UseInMemory();
```

See [Caching Entities](entity-manager/caching-entities.md) for the full caching pipeline.

## Replace `EntityManager.GetPageAsync(PageQuery<T>)`

The `PageQuery<T>` overload on `EntityManager` is gone. Use `PageRequest` for unsorted pages, or expose a domain-specific paged method on a custom repository via the protected `QueryPageAsync(PageQuery<T>)`.

**Before (1.7.2):**

```csharp
var query = new PageQuery<Person>(1, 20)
    .Where(p => p.IsActive)
    .OrderBy(p => p.LastName);
var page = await manager.GetPageAsync(query, ct);
```

**After (1.7.3) — unsorted page:**

```csharp
var page = await manager.GetPageAsync(new PageRequest(1, 20), ct);
```

**After (1.7.3) — filtered / sorted page:**

```csharp
public interface IPersonRepository : IRepository<Person, Guid> {
    Task<PageQueryResult<Person>> FindActivePagedAsync(PageRequest request, CancellationToken ct = default);
}

public class PersonRepository : EntityRepository<Person, Guid>, IPersonRepository {
    public async Task<PageQueryResult<Person>> FindActivePagedAsync(
        PageRequest request, CancellationToken ct = default)
    {
        var query = new PageQuery<Person>(request.Page, request.Size)
            .Where(p => p.IsActive)
            .OrderBy(p => p.LastName);
        return await QueryPageAsync(query, ct);   // protected member
    }
}

// caller
var page = await personRepo.FindActivePagedAsync(new PageRequest(1, 20), ct);
```

## Operation pipeline: the new extension point

If you previously subclassed `EntityManager` to add cross-cutting concerns (audit, event emission, OpenTelemetry), or built bespoke decorators, migrate to the pipeline. The existing `protected virtual On*Async` hooks are preserved and run as a builtin interceptor appended last in the chain, so subclass overrides keep working with no code change — but new concerns should be added as `IEntityManagerInterceptor<,>` implementations registered via `WithInterceptor<T>()`.

See [Operation Pipeline](entity-manager/operation-pipeline.md) for the full contract, registration, and short-circuit examples.

## Reference

- [Operation Pipeline](entity-manager/operation-pipeline.md) — the new interceptor chain on `EntityManager`
- [The Entity Manager](entity-manager/) — registration, validation, caching, HTTP request cancellation
- [Migrating from 1.7](migrating-from-1.7.md) — the prior 1.7.0 → 1.7.1 migration (`IQueryableRepository` / `IPageableRepository` / `IFilterableRepository` removal)
- [Repository Lifecycle](repository-lifecycle/) — the `IRepositoryLifecycleService` replacement for `IRepositoryController`
- [Customize the Repository](custom-repository/) — the fluent `AddRepositoryContext()` builder API