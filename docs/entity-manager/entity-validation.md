# Entity Validation

> **Renamed:** This project was renamed from **Deveel.Repository** to **Kista** on **May 26, 2025**. The name *Kista* is Old Norse for "chest" or "repository", better reflecting the project purpose as a data access framework.

The `EntityManager<TEntity>` validates entities before creating or updating them. Validation is pluggable: implement a validator interface, register it via the fluent builder, and the manager invokes it automatically on every `AddAsync` and `UpdateAsync` call.

## How Validation Works

When `AddAsync` or `UpdateAsync` is called, the manager:

1. Collects all registered `IEntityValidator<TEntity>` instances from the DI container.
2. Calls `ValidateAsync` on each validator, passing the manager instance and the entity.
3. If **any** validator yields validation errors, the operation returns `OperationResult.Fail` with the aggregated errors — the entity is **not** persisted.
4. If no errors are produced, the operation proceeds to the repository.

## Validator Interface

Implement `IEntityValidator<TEntity>` (or `IEntityValidator<TEntity, TKey>` for keyed entities):

```csharp
public class PersonValidator : IEntityValidator<Person>
{
    public async IAsyncEnumerable<ValidationResult> ValidateAsync(
        EntityManager<Person> manager,
        Person entity,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(entity.Name))
            yield return new ValidationResult("Name is required");

        if (entity.Age < 0)
            yield return new ValidationResult("Age must be non-negative");
    }
}
```

The interface uses `IAsyncEnumerable<ValidationResult>`, allowing validators to yield zero, one, or multiple errors. Validation is lazily enumerated: all results are collected before the operation proceeds or fails.

### Rules

- Validators run **before** any repository write.
- Multiple validators can be registered for the same entity type; all are invoked.
- Validation errors are surfaced as structured `ValidationResult` objects inside the `OperationResult.Error`.
- Validators receive the `EntityManager` instance, enabling cross-entity validation (e.g., checking uniqueness via the manager).

## Registration

### Via EntityManagerBuilder (recommended)

Use `WithValidator<T>()` on the `EntityManagerBuilder`:

```csharp
services.AddRepositoryContext()
    .AddRepository<PersonRepository>(repo => repo
        .WithManagement(mgmt => mgmt
            .WithValidator<PersonValidator>()))
    .UseInMemory();
```

The validator type is **scanned** for all implemented `IEntityValidator<>` and `IEntityValidator<,>` interfaces. Only those matching the builder's current entity type are registered. This means a single class can implement validators for multiple entities:

```csharp
public class CompositeValidator :
    IEntityValidator<Person>,
    IEntityValidator<Order>
{
    // ...
}

// Both Person and Order validators are registered
services.AddRepositoryContext()
    .AddRepository<PersonRepository>(repo => repo
        .WithManagement(mgmt => mgmt.WithValidator<CompositeValidator>()))
    .AddRepository<OrderRepository>(repo => repo
        .WithManagement(mgmt => mgmt.WithValidator<CompositeValidator>()));
```

### Legacy registration

Register validators directly on `IServiceCollection`:

```csharp
builder.Services.AddEntityValidator<Person, PersonValidator>();
```

This is still supported but the fluent builder is preferred.

## Error Factories

Error factories control the error codes and messages returned when operations fail. They are tightly coupled to validation because validation failures are returned through the same `OperationResult` mechanism.

### Implementing an Error Factory

Extend `OperationErrorFactory` (the concrete class, not merely `IOperationErrorFactory`):

```csharp
public class PersonErrorFactory : OperationErrorFactory { }
```

The base `OperationErrorFactory` provides default error codes for common failure modes (not-found, validation failed, concurrency conflict). Override methods to customize:

```csharp
public class PersonErrorFactory : OperationErrorFactory
{
    protected override string GetNotFoundErrorCode() => "PERSON_NOT_FOUND";
    protected override string GetValidationErrorCode() => "PERSON_INVALID";
}
```

### Registration

```csharp
services.AddRepositoryContext()
    .AddRepository<PersonRepository>(repo => repo
        .WithManagement(mgmt => mgmt
            .WithValidator<PersonValidator>()
            .WithOperationErrorFactory<PersonErrorFactory>()))
    .UseInMemory();
```

> The factory type **must** extend `OperationErrorFactory` (the concrete class), not merely implement `IOperationErrorFactory`. This constraint exists because the registration code wraps the factory in an `OperationErrorFactoryDecorator<TEntity>`, which requires the base class.

### Why extend OperationErrorFactory?

The decorator pattern adds entity-type context to errors at runtime. The decorator's constructor signature takes `OperationErrorFactory`, so a custom factory that only implements the interface cannot be wrapped. Extending the concrete class ensures compatibility.

## Summary

| Concern | Interface / Base | Registration |
|---------|-----------------|-------------|
| Validation | `IEntityValidator<TEntity>` | `WithValidator<T>()` |
| Error codes | `OperationErrorFactory` (class) | `WithOperationErrorFactory<T>()` |

## See Also

- [Caching Entities](caching-entities.md) — second-level cache configuration
- [HTTP Request Cancellation](http-request-cancellation.md) — automatic cancellation via ASP.NET Core
