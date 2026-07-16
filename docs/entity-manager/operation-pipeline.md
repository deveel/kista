# Operation Pipeline

The `EntityManager<TEntity, TKey>` runs every write operation (`AddAsync`, `AddRangeAsync`, `UpdateAsync`, `RemoveAsync`, `RemoveRangeAsync`, `RestoreAsync`, `HardDeleteAsync`, `HardDeleteRangeAsync`) through an **extensible operation pipeline** — an ordered chain of interceptors that can observe, transform, or short-circuit each write before it reaches the repository, and react to it after it succeeds.

## Why a pipeline?

Before the pipeline, cross-cutting concerns on `EntityManager` were scattered:

- Cache warming and eviction were hardwired inline in every CRUD method.
- The only extension points were a handful of `protected virtual` hooks (`OnAddingEntityAsync`, `OnUpdatingEntityAsync`, ...) that run before the write, cannot short-circuit, and have no after-write slot.
- `RemoveRangeAsync` had no hook at all.
- Teams needing audit trails or tracing built bespoke decorators or subclassed the manager — neither approach composes when multiple concerns are involved.

The pipeline gives every cross-cutting concern — events, audit, OpenTelemetry, multi-tenancy — one ordered, testable extension point.

## The interceptor contract

```csharp
public interface IEntityManagerInterceptor<TEntity, TKey>
    where TEntity : class
    where TKey : notnull
{
    ValueTask<IOperationResult?> PreWriteAsync(IEntityOperationContext<TEntity, TKey> context);

    ValueTask PostWriteAsync(IEntityOperationContext<TEntity, TKey> context, IOperationResult result);
}
```

### `PreWriteAsync`

Invoked **before** the repository write, for each interceptor in registration order.

- Return `null` to continue the chain and proceed with the repository write.
- Return a failed `IOperationResult` to **short-circuit** the chain — the repository write is skipped, all downstream interceptors' `PreWriteAsync` are skipped, and `PostWriteAsync` is not called for any interceptor. The failed result is returned to the caller.

The interceptor may mutate `context.Entity` — the mutated instance is the one forwarded to the repository and to subsequent interceptors.

### `PostWriteAsync`

Invoked **after** a successful repository write, for each interceptor in registration order.

- Not called when the repository write throws.
- Not called when any interceptor short-circuits the chain in `PreWriteAsync`.
- Receives the `IOperationResult` of the operation (success or not-changed).

Use this slot for event emission, cache warming, audit recording, tracing span closing, etc.

## The operation context

Each write operation creates an `IEntityOperationContext<TEntity, TKey>` carrying:

| Property | Description |
|----------|-------------|
| `Kind` | The `EntityOperationKind` (`Create`, `Update`, `Remove`, `Restore`, `HardDelete`) |
| `Entity` | The mutable entity — interceptors can transform it in `PreWriteAsync` |
| `Original` | The pre-image loaded from the repository (null on `Create`) |
| `Key` | The entity key, or null if the entity has no valid key |
| `Actor` | The current user identifier from `IUserAccessor<string>`, or null |
| `Timestamp` | The operation timestamp from `ISystemTime` |
| `CancellationToken` | The cancellation token for the operation |
| `Items` | A per-operation key/value bag for sharing data between interceptors or between pre/post steps |

For range operations (`AddRangeAsync`, `RemoveRangeAsync`, `HardDeleteRangeAsync`), one context is created **per entity** in the batch, mirroring the per-entity `On*Async` hook behavior. A short-circuit on any entity aborts the entire batch.

## Registration

Interceptors are resolved lazily from DI via `IEnumerable<IEntityManagerInterceptor<TEntity, TKey>>` — zero cost when none are registered.

Register interceptors through the `EntityManagerBuilder`:

```csharp
services.AddRepositoryContext()
    .AddRepository<PersonRepository>(repo => repo
        .WithManagement(mgmt => mgmt
            .WithInterceptor<AuditInterceptor>()
            .WithInterceptor<ValidationInterceptor>()))
    .UseInMemory();
```

Interceptors run in **registration order**. The `WithInterceptor<T>()` method scans the type's implemented `IEntityManagerInterceptor<,>` (or `IEntityManagerInterceptor<>`) interfaces and registers against the matching closed generic, mirroring the `WithValidator<T>()` pattern.

## Coexistence with the `On*Async` hooks

The existing `protected virtual` hooks on `EntityManager` are preserved:

- `OnAddingEntityAsync` — stamps `CreatedAtUtc` on `IHaveTimeStamp` entities
- `OnUpdatingEntityAsync` — stamps `UpdatedAtUtc` on `IHaveTimeStamp` entities
- `OnRemovingEntityAsync` — stamps soft-delete fields on `ISoftDeletable` entities
- `OnRestoringEntityAsync` — clears soft-delete fields
- `OnHardRemovingEntityAsync` — no-op by default, available for audit/purge logging overrides

A builtin `OnHooksEntityInterceptor` wraps these hooks and is **always appended last** in the chain, after any user-registered interceptors. This means:

- Subclasses overriding `OnAddingEntityAsync` etc. keep working with no code change.
- User interceptors run **before** the framework's timestamp/soft-delete stamping, so they can observe or transform the entity before stamping.
- The pipeline and the hooks coexist cleanly.

## Short-circuit example

An interceptor that rejects writes outside business hours:

```csharp
public class BusinessHoursInterceptor<TEntity, TKey> : IEntityManagerInterceptor<TEntity, TKey>
    where TEntity : class
    where TKey : notnull
{
    public ValueTask<IOperationResult?> PreWriteAsync(IEntityOperationContext<TEntity, TKey> context) {
        if (context.Timestamp.Hour is < 9 or >= 18) {
            return new(new OperationError("OUTSIDE_HOURS", "Write rejected: outside business hours"));
        }

        return default;
    }

    public ValueTask PostWriteAsync(IEntityOperationContext<TEntity, TKey> context, IOperationResult result)
        => ValueTask.CompletedTask;
}
```

Register it:

```csharp
.WithInterceptor<BusinessHoursInterceptor<Person, string>>()
```

When a write is attempted outside business hours, the interceptor returns a failed `IOperationResult`, the repository write is skipped, and the caller receives the error result — no exception thrown.

## Post-write example

An interceptor that logs successful writes:

```csharp
public class WriteLoggerInterceptor<TEntity, TKey> : IEntityManagerInterceptor<TEntity, TKey>
    where TEntity : class
    where TKey : notnull
{
    private readonly ILogger<WriteLoggerInterceptor<TEntity, TKey>> _logger;

    public WriteLoggerInterceptor(ILogger<WriteLoggerInterceptor<TEntity, TKey>> logger) {
        _logger = logger;
    }

    public ValueTask<IOperationResult?> PreWriteAsync(IEntityOperationContext<TEntity, TKey> context)
        => default;

    public ValueTask PostWriteAsync(IEntityOperationContext<TEntity, TKey> context, IOperationResult result) {
        _logger.LogInformation("{Kind} on {EntityType} by {Actor} at {Timestamp}", context.Kind, typeof(TEntity).Name, context.Actor, context.Timestamp);
        return ValueTask.CompletedTask;
    }
}
```

## Single-key entities (`EntityManager<TEntity>`)

For entities managed through `EntityManager<TEntity>` (using `object` as the key type), implement `IEntityManagerInterceptor<TEntity>` (the single-key variant). The manager automatically wraps single-key interceptors and feeds them into the same pipeline.