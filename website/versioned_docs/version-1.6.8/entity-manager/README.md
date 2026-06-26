# The Entity Manager

The `EntityManager<TEntity>` (and `EntityManager<TEntity, TKey>`) is an optional application-layer service that wraps an `IRepository<TEntity>` and enriches every operation with cross-cutting concerns:

- **Validation** — entities are validated before being added or updated.
- **Caching** — frequently accessed entities can be served from a second-level cache.
- **Timestamping** — entities implementing `IHaveTimeStamp` are automatically stamped on create/update.
- **Structured error handling** — operations return structured error results rather than throwing raw exceptions.
- **Logging** — all operations are logged through the standard `ILogger` infrastructure.

`EntityManager<TEntity>` is designed to sit between your controllers / use-case handlers and the underlying repository, at the **application service layer**.

## Prerequisites

The entity manager is packaged separately from the core repository:

| Package | Description |
|---------|-------------|
| `Kista.Manager` | Base manager, validation abstractions, error factories |
| `Kista.Manager.EasyCaching` | Second-level caching via EasyCaching |
| `Kista.Manager.AspNetCore` | Automatic HTTP request cancellation for ASP.NET Core |
| `Kista.Manager.DynamicLinq` | Dynamic LINQ query extensions for the manager |

```bash
dotnet add package Kista.Manager
```

## Registration

There are two ways to register an entity manager: **per-repository** (explicit, recommended) and **global** (convenience for bulk registration).

### Per-repository (recommended)

Use `WithManagement()` on the `RepositoryBuilder` returned by `AddRepository<T>()`. This binds the manager to a specific repository and entity type:

```csharp
services.AddRepositoryContext()
    .AddRepository<PersonRepository>(repo => repo
        .WithManagement(mgmt => mgmt
            .WithValidator<PersonValidator>()
            .WithCacheKeyGenerator<PersonKeyGenerator>()
            .WithOperationErrorFactory<PersonErrorFactory>()
            .WithEasyCaching(opts =>
            {
                opts.DefaultExpiration = TimeSpan.FromMinutes(15);
            })))
    .UseInMemory();
```

To register a manager with no additional services (the EntityManager itself is still registered):

```csharp
services.AddRepositoryContext()
    .AddRepository<PersonRepository>(repo => repo
        .WithManagement())
    .UseInMemory();
```

### Global (convenience)

Use `WithManagement()` on the `RepositoryContextBuilder` to register managers for **all tracked entity types** in one call:

```csharp
services.AddRepositoryContext()
    .AddRepository<PersonRepository>(_ => { })
    .AddRepository<OrderRepository>(_ => { })
    .WithManagement()              // registers EntityManager<Person> and EntityManager<Order>
    .UseInMemory();
```

You can pass a `ManagementOptions` delegate to control auto-registration:

```csharp
services.AddRepositoryContext()
    .AddRepository<PersonRepository>(_ => { })
    .WithManagement(opts =>
    {
        opts.AutoRegisterManagers = true;   // default: true
    });
```

> Both approaches coexist. Use per-repository when you need entity-specific configuration; use global when you want quick setup with no extras.

## Custom Managers

Derive from `EntityManager<TEntity>` (or `EntityManager<TEntity, TKey>`) to add domain-specific business operations:

```csharp
public class OrderManager : EntityManager<Order>
{
    public OrderManager(
        IRepository<Order> repository,
        IEntityValidator<Order>? validator = null,
        ILoggerFactory? loggerFactory = null)
        : base(repository, validator, loggerFactory: loggerFactory) { }

    public async Task<OperationResult> ShipAsync(
        string orderId, CancellationToken ct = default)
    {
        var order = await FindAsync(orderId, ct);
        if (order == null)
            return OperationResult.Fail(...);

        order.Ship();
        return await UpdateAsync(order, ct);
    }
}
```

Register a custom manager by registering it directly in the DI container:

```csharp
services.AddScoped<OrderManager>();
```

The base `EntityManager<Order>` is registered automatically when using `WithManagement()`, so DI can resolve both the base type and your custom type.

## Operation Results

EntityManager methods return `OperationResult` (for void-like operations) or `OperationResult<T>` (for operations that return a value), rather than throwing exceptions for expected failures:

| Method | Returns |
|--------|---------|
| `AddAsync(entity)` | `OperationResult` |
| `UpdateAsync(entity)` | `OperationResult` |
| `RemoveAsync(entity)` | `OperationResult` |
| `FindAsync(key)` | `OperationResult<TEntity>` |
| `FindFirstAsync(query)` | `OperationResult<TEntity>` |

```csharp
var result = await manager.AddAsync(person);
if (result.IsSuccess)
{
    // entity added
}
else
{
    var error = result.Error;
    // error.ErrorCode, error.Message
}
```

This allows the caller to handle validation failures, not-found conditions, and infrastructure errors uniformly without try/catch blocks.

## Operation Cancellation

Every async method accepts an optional `CancellationToken`. When no token is supplied, the manager checks for an `IOperationCancellationSource` registered in the DI container and uses its token automatically.

For ASP.NET Core, install `Kista.Manager.AspNetCore` and call `AddHttpRequestTokenSource()`:

```csharp
builder.Services.AddHttpRequestTokenSource();
```

When the HTTP client disconnects, all in-flight repository operations are cancelled automatically.

See [HTTP Request Cancellation](http-request-cancellation.md) for full details.

## See Also

- [Entity Validation](entity-validation.md) — validators, error factories, and the validation flow
- [Caching Entities](caching-entities.md) — cache registration, key generators, serialization
- [HTTP Request Cancellation](http-request-cancellation.md) — automatic cancellation via ASP.NET Core
