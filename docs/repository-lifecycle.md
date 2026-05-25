# Repository Lifecycle
> **Renamed:** This project was renamed from **Deveel.Repository** to **Kista** on **May 26, 2025**. The name *Kista* is Old Norse for "chest" or "repository", better reflecting the project purpose as a data access framework.

The repository lifecycle feature provides a formalized mechanism to **create**, **drop**, and **seed** repositories during application startup. It replaces the obsolete `IRepositoryController` / `RepositoryControllerAdapter` model with a cleaner handler-based abstraction.

## Overview

The lifecycle is orchestrated by `IRepositoryLifecycleService` (default implementation: `RepositoryLifecycleService`). On startup you call one or more of its methods:

| Method | Description |
| ------ | ----------- |
| `CreateRepositoryAsync<TEntity>` / `<TEntity, TKey>` | Creates the repository (e.g. a database or collection) |
| `DropRepositoryAsync<TEntity>` / `<TEntity, TKey>` | Drops the repository |
| `SeedRepositoryAsync<TEntity>` / `<TEntity, TKey>` | Seeds initial data |

Each operation is delegated to a registered `IRepositoryLifecycleHandler<TEntity>`. If no handler is found, the service falls back to an `IControllableRepository` (a repository that can manage its own storage).

## Registration

Use the `AddRepositoryContext()` fluent builder:

```csharp
builder.Services.AddRepositoryContext()
    .UseEntityFramework<AppDbContext>(...)
    .ConfigureLifecycle(options => {
        options.DeleteIfExists = true;
        options.SeedStrategy = SeedStrategy.Always;
    });
```

Or register the lifecycle service directly:

```csharp
builder.Services.AddRepositoryLifecycleOrchestrator(options => {
    options.FailFast = true;
});
```

### Registering Lifecycle Handlers

Use the `WithLifecycleHandler()` extensions on `RepositoryContextBuilder`:

```csharp
// By type
builder.Services.AddRepositoryContext()
    .UseEntityFramework<AppDbContext>(...)
    .WithLifecycleHandler<MyEntity, MyEntityLifecycleHandler>();

// By factory delegate
builder.Services.AddRepositoryContext()
    .UseEntityFramework<AppDbContext>(...)
    .WithLifecycleHandler<MyEntity>(sp => new MyEntityLifecycleHandler(sp.GetRequiredService<ILogger>()));

// By instance
builder.Services.AddRepositoryContext()
    .UseEntityFramework<AppDbContext>(...)
    .WithLifecycleHandler(new MyEntityLifecycleHandler());
```

### Registering Lifecycle Profiles

Use the `WithLifecycleProfile()` extensions:

```csharp
// By type
builder.Services.AddRepositoryContext()
    .UseEntityFramework<AppDbContext>(...)
    .WithLifecycleProfile<StagingLifecycleProfile>();

// By instance
builder.Services.AddRepositoryContext()
    .UseEntityFramework<AppDbContext>(...)
    .WithLifecycleProfile(new StagingLifecycleProfile());
```

> `ConfigureLifecycle()` automatically registers a `DefaultRepositoryLifecycleProfile` if no profile is already registered.

## Configuration

`RepositoryLifecycleOptions` controls every aspect of lifecycle behavior:

| Property | Default | Description |
| -------- | ------- | ----------- |
| `DeleteIfExists` | `true` | Drop and re-create if the repository already exists. |
| `DontCreateExisting` | `true` | Skip creation if the repository already exists. |
| `FailFast` | `false` | Throw if no lifecycle handler is found. |
| `SeedStrategy` | `Never` | Determines when seeding occurs (see below). |
| `EnvironmentName` | `null` | Overrides the hosting environment name. |
| `SeedAction` | `null` | Custom action invoked instead of the handler's `SeedAsync`. |

### Conflict Resolution

When both `DeleteIfExists` and `DontCreateExisting` are `false` and the repository exists, `CreateRepositoryAsync` throws `RepositoryException`.

## Seed Strategies

The `SeedStrategy` enum controls when seed data is applied:

| Value | Behavior |
| ----- | -------- |
| `Never` | No seeding is performed. |
| `Always` | Seeding is always performed. |
| `IfMissing` | Seeding is performed only when the repository does not exist. |
| `ByEnvironment` | The strategy is delegated to an `IRepositoryLifecycleProfile`. |

### Environment-Aware Seeding

When `SeedStrategy` is `ByEnvironment`, the service:

1. Reads `EnvironmentName` from options (or resolves it from `IHostEnvironment`).
2. Queries the registered `IRepositoryLifecycleProfile` for the effective strategy.
3. Falls back to `Always` if no profile is registered.

### Seed Data Sources

Seed data can come from three sources (evaluated in order):

1. **Explicit data** passed to `SeedRepositoryAsync(data)`.
2. An **`IRepositorySeedDataProvider<TEntity>`** registered in DI.
3. A **`SeedAction`** callback on `RepositoryLifecycleOptions`.

If no source provides data, seeding is silently skipped.

### Automatic Discovery

When you call `ConfigureLifecycle()`, the builder **automatically scans the entry assembly** for any class implementing `IRepositorySeedDataProvider<TEntity>` and registers it with `TryAdd` — so explicit registrations always take priority.

```csharp
// This provider is auto-discovered — no explicit registration needed
public class ProductSeedProvider : IRepositorySeedDataProvider<Product> {
    public IEnumerable<Product> GetSeedData() {
        yield return new Product { Name = "Widget", Price = 9.99m };
    }
}

// ConfigureLifecycle triggers the auto-scan
builder.Services.AddRepositoryContext()
    .UseEntityFramework<AppDbContext>(...)
    .ConfigureLifecycle(options => {
        options.SeedStrategy = SeedStrategy.Always;
    });
```

### Explicit Registration

When you need to register providers from multiple assemblies or want to be explicit, use `WithSeedDataFrom()`:

```csharp
builder.Services.AddRepositoryContext()
    .UseInMemory(b => b.WithLifecycle())
    .ConfigureLifecycle(options => { ... })
    .WithSeedDataFrom(typeof(ProductSeedProvider).Assembly,
                           typeof(OtherSeedProvider).Assembly);
```

### Seeding Examples

#### Via `WithSeedData` (Recommended)

The `RepositoryContextBuilder` provides two overloads:

**Using a provider class:**

```csharp
public class MyEntitySeedProvider : IRepositorySeedDataProvider<MyEntity> {
    public IEnumerable<MyEntity> GetSeedData() {
        yield return new MyEntity { Name = "Default Item", IsActive = true };
        yield return new MyEntity { Name = "Another Item", IsActive = true };
    }

    IEnumerable<object> IRepositorySeedDataProvider.GetSeedData()
        => GetSeedData().Cast<object>();
}

// Program.cs
builder.Services.AddRepositoryContext()
    .UseEntityFramework<AppDbContext>(...)
    .ConfigureLifecycle(options => {
        options.SeedStrategy = SeedStrategy.Always;
    })
    .WithSeedData<MyEntity, MyEntitySeedProvider>();
```

**Using inline data (no provider class needed):**

```csharp
builder.Services.AddRepositoryContext()
    .UseEntityFramework<AppDbContext>(...)
    .ConfigureLifecycle(options => {
        options.SeedStrategy = SeedStrategy.Always;
    })
    .WithSeedData<MyEntity>(new[] {
        new MyEntity { Name = "Default Item", IsActive = true },
        new MyEntity { Name = "Another Item", IsActive = true }
    });
```

All overloads register an `IRepositorySeedDataProvider<TEntity>` in DI behind the scenes. The service resolves it during `SeedRepositoryAsync<TEntity>` and the driver's lifecycle handler inserts the data.

> Explicit `WithSeedData` calls use `Add` (always registers). Auto-discovered providers use `TryAdd` — so explicit wins if both are present.

#### Via `SeedAction` Callback

Override the default seeding behavior with a custom action:

```csharp
builder.Services.AddRepositoryContext()
    .UseEntityFramework<AppDbContext>(...)
    .ConfigureLifecycle(options => {
        options.SeedStrategy = SeedStrategy.Always;
        options.SeedAction = (sp, entityType, seedData) => {
            if (entityType == typeof(MyEntity) && seedData is IEnumerable<MyEntity> entities) {
                var context = sp.GetRequiredService<AppDbContext>();
                context.Set<MyEntity>().AddRange(entities);
                context.SaveChanges();
            }
        };
    });
```

The `SeedAction` receives the service provider, the entity type being seeded, and the resolved seed data (or `null`).

#### Via Explicit Call

Trigger lifecycle operations explicitly from your application:

```csharp
var lifecycleService = app.Services.GetRequiredService<IRepositoryLifecycleService>();

// Create the repository and seed in one flow
await lifecycleService.CreateRepositoryAsync<MyEntity>();
await lifecycleService.SeedRepositoryAsync<MyEntity>(new[] {
    new MyEntity { Name = "Startup Item" }
});

// Or use the combined lifecycle flow (create + seed)
await lifecycleService.SeedRepositoryAsync<MyEntity>(new[] {
    new MyEntity { Name = "Startup Item" }
});
```

> When `SeedRepositoryAsync` is called with explicit data and `SeedStrategy` is not `Never`, the data is passed directly to the driver's lifecycle handler without consulting `IRepositorySeedDataProvider` or `SeedAction`.

## Lifecycle Handlers

Implement `IRepositoryLifecycleHandler<TEntity>` to control how a specific entity type is created, dropped, and seeded:

```csharp
public class MyEntityHandler : IRepositoryLifecycleHandler<MyEntity> {
    public async ValueTask<bool> ExistsAsync(CancellationToken ct) { ... }
    public async ValueTask CreateAsync(CancellationToken ct) { ... }
    public async ValueTask DropAsync(CancellationToken ct) { ... }
    public async ValueTask SeedAsync(object? seedData, CancellationToken ct) { ... }
}
```

Register the handler via `WithLifecycleHandler()` on the builder or directly in DI:

```csharp
// Via builder (recommended)
builder.Services.AddRepositoryContext()
    .UseEntityFramework<AppDbContext>(...)
    .WithLifecycleHandler<MyEntity, MyEntityHandler>();

// Or directly
builder.Services.AddSingleton<IRepositoryLifecycleHandler<MyEntity>, MyEntityHandler>();
```

### Controllable Repository Fallback

If no `IRepositoryLifecycleHandler<TEntity>` is registered for an entity type, the service falls back to checking whether the registered repository itself implements `IControllableRepository` — meaning the repository **self-manages its own storage lifecycle**.

```csharp
public interface IControllableRepository {
    ValueTask<bool> ExistsAsync(CancellationToken cancellationToken = default);
    ValueTask CreateAsync(CancellationToken cancellationToken = default);
    ValueTask DropAsync(CancellationToken cancellationToken = default);
}
```

#### How the Fallback Works

Inside `ResolveHandler<TEntity>()` (and its `TEntity, TKey` variant), the service:

1. Checks the service container for an `IRepositoryLifecycleHandler<TEntity>`.
2. If not found, resolves `IRepository<TEntity>` and tests it for `IControllableRepository`.
3. If the repository implements `IControllableRepository`, it wraps it in an internal `ControllableRepositoryHandler<TEntity>` that delegates `ExistsAsync`, `CreateAsync`, and `DropAsync` directly to the repository.
4. If neither exists and `FailFast` is `true`, throws `RepositoryException`. Otherwise returns `null` (operation is silently skipped).

```csharp
// Pseudocode of the fallback logic
var handler = serviceProvider.GetService<IRepositoryLifecycleHandler<TEntity>>();
if (handler != null) return handler;

var repository = serviceProvider.GetService<IRepository<TEntity>>();
if (repository is IControllableRepository controllable)
    return new ControllableRepositoryHandler<TEntity>(controllable);

// No handler, no controllable repository — skip or fail
```

#### Seeding Note

`ControllableRepositoryHandler<TEntity>.SeedAsync` is a **no-op** — the controllable interface has no seed operation. When using the controllable-repository fallback, seeding must be done either:
- Via an `IRepositorySeedDataProvider<TEntity>` (auto-discovered or explicit)
- Via a `SeedAction` callback in `RepositoryLifecycleOptions`
- By passing explicit data to `SeedRepositoryAsync(data)`

The service's `SeedRepository` method itself handles data resolution (provider → service → explicit), so seed data flows normally even though the handler's `SeedAsync` is a no-op.

#### Handler vs Controllable

| Approach | When to Use |
| -------- | ----------- |
| `IRepositoryLifecycleHandler<TEntity>` | Custom lifecycle logic that is separate from the repository (e.g., creating a database schema, running migrations). Also required when seeding data. |
| `IControllableRepository` on the repository class | The repository **is** the storage and can create/drop itself. Common when the repository directly wraps a database collection or table. |

#### Driver Support

| Driver | Implements `IControllableRepository`? | Default |
| ------ | :------------------------------------: | :-----: |
| MongoDB | ✅ — `MongoRepository<TEntity>` implements the interface natively | Enabled (use `WithoutLifecycle()` to disable) |
| In-Memory | ❌ — uses `InMemoryRepositoryLifecycleHandler<TEntity>` instead | Disabled (use `WithLifecycle()` to enable) |
| EF Core | ❌ — uses a dedicated lifecycle handler | Enabled (use `WithoutLifecycle()` to disable) |

When a driver does **not** implement `IControllableRepository`, the driver's `.WithLifecycle()` (or default-enabled lifecycle) registers a handler that fits the `IRepositoryLifecycleHandler<TEntity>` contract, and the fallback path is not needed.

## Lifecycle Profiles

`IRepositoryLifecycleProfile` provides environment-specific seed strategies and data:

```csharp
public class StagingProfile : IRepositoryLifecycleProfile {
    public SeedStrategy GetSeedStrategy(string? environmentName)
        => environmentName == "Staging" ? SeedStrategy.Always : SeedStrategy.Never;

    public object? GetSeedData<TEntity>() where TEntity : class => null;
    public object? GetSeedData(Type entityType) => null;
}
```

## Error Handling

- `NotSupportedException` is re-thrown as-is.
- `RepositoryException` is re-thrown as-is.
- Any other exception is wrapped in a `RepositoryException`.

When `FailFast` is `true` and no handler or controllable repository is found, a `RepositoryException` is thrown immediately.

## Sample Project

The `Kista.SampleApp` demonstrates a complete ASP.NET Core application using lifecycle management and CRUD endpoints. It includes:

- **Model**: `Contact` entity with `GuidId`
- **Custom Repository**: `ContactRepository` extending `IRepository<Contact, Guid>`
- **Lifecycle Handler**: `ContactLifecycleHandler` for create/drop/seed operations
- **Lifecycle Profile**: `SampleLifecycleProfile` for environment-aware seeding
- **Seed Data**: `DefaultContactSeedData` implementing `IRepositorySeedDataProvider<Contact>`
- **Endpoints**: 
  - `/api/lifecycle/create` — Create the repository
  - `/api/lifecycle/drop` — Drop the repository
  - `/api/lifecycle/seed` — Seed the repository
  - `/api/lifecycle/initialize` — Drop, create, and seed in one call
  - `/api/contacts` — Full CRUD for contacts

### Registration

```csharp
// Extension method (see sample for full implementation)
builder.Services.AddContactRepository(builder.Configuration);
```

### Lifecycle Endpoint Usage

```csharp
// Create
await service.CreateRepositoryAsync<Contact, Guid>(ct);

// Drop
await service.DropRepositoryAsync<Contact, Guid>(ct);

// Seed (uses registered seed data provider)
await service.SeedRepositoryAsync<Contact, Guid>(null, ct);

// Full initialization
await service.DropRepositoryAsync<Contact, Guid>(ct);
await service.CreateRepositoryAsync<Contact, Guid>(ct);
await service.SeedRepositoryAsync<Contact, Guid>(null, ct);
```
