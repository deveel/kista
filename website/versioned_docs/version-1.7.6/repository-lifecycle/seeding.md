# Seeding

Seeding populates a repository with initial data during application startup. The lifecycle service handles data resolution and delegates insertion to the driver's lifecycle handler.

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

## Seed Data Sources

Seed data can come from three sources (evaluated in order):

1. **Explicit data** passed to `SeedRepositoryAsync(data)`.
2. An **`IRepositorySeedDataProvider<TEntity>`** registered in DI.
3. A **`SeedAction`** callback on `RepositoryLifecycleOptions`.

If no source provides data, seeding is silently skipped.

## Automatic Discovery

When you call `ConfigureLifecycle()`, the builder **automatically scans the entry assembly** for any class implementing `IRepositorySeedDataProvider<TEntity>` and registers it with `TryAdd` — so explicit registrations always take priority.

```csharp
public class ProductSeedProvider : IRepositorySeedDataProvider<Product> {
    public IEnumerable<Product> GetSeedData() {
        yield return new Product { Name = "Widget", Price = 9.99m };
    }
}

builder.Services.AddRepositoryContext()
    .UseEntityFramework<AppDbContext>(...)
    .ConfigureLifecycle(options => {
        options.SeedStrategy = SeedStrategy.Always;
    });
```

## Explicit Registration

When you need to register providers from multiple assemblies or want to be explicit, use `WithSeedDataFrom()`:

```csharp
builder.Services.AddRepositoryContext()
    .UseInMemory(b => b.WithLifecycle())
    .ConfigureLifecycle(options => { ... })
    .WithSeedDataFrom(typeof(ProductSeedProvider).Assembly,
                           typeof(OtherSeedProvider).Assembly);
```

## Seeding Methods

### Via `WithSeedData` (Recommended)

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

### Via `SeedAction` Callback

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

### Via Explicit Call

Trigger lifecycle operations explicitly from your application:

```csharp
var lifecycleService = app.Services.GetRequiredService<IRepositoryLifecycleService>();

await lifecycleService.CreateRepositoryAsync<MyEntity>();
await lifecycleService.SeedRepositoryAsync<MyEntity>(new[] {
    new MyEntity { Name = "Startup Item" }
});
```

> When `SeedRepositoryAsync` is called with explicit data and `SeedStrategy` is not `Never`, the data is passed directly to the driver's lifecycle handler without consulting `IRepositorySeedDataProvider` or `SeedAction`.
