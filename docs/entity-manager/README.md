# The Entity Manager
> **Renamed:** This project was renamed from **Deveel.Repository** to **Kista** on **May 26, 2025**. The name *Kista* is Old Norse for "chest" or "repository", better reflecting the project purpose as a data access framework.

The `EntityManager<TEntity>` class is an optional application-layer service that wraps an `IRepository<TEntity>` and enriches every operation with cross-cutting concerns:

- **Validation** — entities are validated (via `IEntityValidator<TEntity>`) before being added or updated.
- **Caching** — frequently accessed entities can be served from a second-level cache (via `IEntityCache<TEntity>`).
- **Timestamping** — entities implementing `IHaveTimeStamp` are automatically stamped on create/update.
- **Structured error handling** — operations return structured error results rather than throwing raw exceptions.
- **Logging** — all operations are logged through the standard `ILogger` infrastructure.

`EntityManager<TEntity>` is designed to be used at the **application service layer**, sitting between your controllers / use-case handlers and the underlying repository.

## Constructor

```csharp
public EntityManager<TEntity>(
    IRepository<TEntity>           repository,
    IEntityValidator<TEntity>?     validator    = null,
    IEntityCache<TEntity>?         cache        = null,
    ISystemTime?                   systemTime   = null,
    IOperationErrorFactory<TEntity>? errorFactory = null,
    IServiceProvider?              services     = null,
    ILoggerFactory?                loggerFactory = null)
```

All parameters beyond `repository` are optional and resolved from the DI container at registration time (see below).

## Registration

Use `AddEntityManager<TManager>` to register a concrete manager type (or a custom sub-class):

```csharp
// Program.cs

// Register the default EntityManager for MyEntity
builder.Services.AddManagerFor<MyEntity>();

// Or register a custom sub-class that derives from EntityManager<MyEntity>
builder.Services.AddEntityManager<MyEntityManager>();
```

After registration, the DI container provides both the concrete type and all `EntityManager<TEntity>` base-type projections.

To register a custom validator:

```csharp
builder.Services.AddEntityValidator<MyEntityValidator>();
```

## Custom Managers

Derive from `EntityManager<TEntity>` to add domain-specific business operations:

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
            return NotFound("ORDER_NOT_FOUND");

        order.Ship();
        return await UpdateAsync(order, ct);
    }
}
```

## Operation Cancellation

Every async method accepts an optional `CancellationToken`. When no token is supplied (or `null` is passed), the manager checks for an `IOperationCancellationSource` registered in the DI container and uses its token automatically.

This is useful for tying operation lifetime to an HTTP request:

```csharp
// Register the ASP.NET Core HTTP cancellation source
// (provided by Kista.Manager.AspNetCore)
builder.Services.AddHttpRequestTokenSource();
```

With this in place, when the HTTP request is aborted, all in-flight repository operations are cancelled without any additional code in your manager.

See [HTTP Request Cancellation](http-request-cancellation.md) for full details.

## Caching

See [Caching Entities](caching-entities.md) for details on integrating second-level caching via [EasyCaching](https://easycaching.readthedocs.io/en/latest/).
