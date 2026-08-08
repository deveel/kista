# Specifications

The Specification Pattern allows you to encapsulate business rules into reusable, named specification objects that compose with AND/OR/NOT and produce driver-agnostic queries — bridging domain language and data access without coupling to LINQ or any specific query engine.

## Overview

Kista's query pipeline is built on `IQueryFilter` and `IQuery` — infrastructure-level abstractions that carry no business meaning. A filter like `customer => customer.IsActive && customer.CreatedAt > cutoff` lives inline in service code, duplicated across methods, with no way to name, reuse, or compose it.

Specifications solve this by letting you:

- Encapsulate business rules into named, reusable objects
- Compose specifications with `&` (AND), `|` (OR), and `!` (NOT) operators
- Execute specifications directly against any repository or EntityManager
- Keep domain logic decoupled from infrastructure

## Core Types

### `ISpecification<TEntity>`

The interface that all specifications implement. It has a single method:

```csharp
public interface ISpecification<TEntity> where TEntity : class {
    IQuery ToQuery();
}
```

`ToQuery()` produces a driver-agnostic `IQuery` that any repository can execute.

### `Specification<TEntity>`

An abstract base class that implements `ISpecification<TEntity>` and provides operator overloads for composition:

```csharp
public abstract class Specification<TEntity> : ISpecification<TEntity> where TEntity : class {
    public abstract IQuery ToQuery();

    public static Specification<TEntity> operator &(Specification<TEntity> left, Specification<TEntity> right)
        => new AndSpecification<TEntity>(left, right);

    public static Specification<TEntity> operator |(Specification<TEntity> left, Specification<TEntity> right)
        => new OrSpecification<TEntity>(left, right);

    public static Specification<TEntity> operator !(Specification<TEntity> spec)
        => new NotSpecification<TEntity>(spec);
}
```

### Composite Specifications

| Type | Operator | Description |
|------|----------|-------------|
| `AndSpecification<TEntity>` | `&` | Both specifications must be satisfied |
| `OrSpecification<TEntity>` | `|` | At least one specification must be satisfied |
| `NotSpecification<TEntity>` | `!` | The specification must not be satisfied |

## Creating a Specification

Create a class that extends `Specification<TEntity>` and implements `ToQuery()`:

```csharp
public class ActiveCustomerSpec : Specification<Customer> {
    public override IQuery ToQuery() {
        return new Query<Customer>(customer => customer.IsActive);
    }
}

public class HighValueCustomerSpec : Specification<Customer> {
    private readonly decimal _minimumTotal;

    public HighValueCustomerSpec(decimal minimumTotal) {
        _minimumTotal = minimumTotal;
    }

    public override IQuery ToQuery() {
        return new Query<Customer>(customer => customer.TotalOrders >= _minimumTotal);
    }
}
```

## Using Specifications

### With a Repository

Extension methods on `IRepository<TEntity, TKey>` accept specifications directly:

```csharp
public class CustomerService {
    private readonly IRepository<Customer> _customers;

    public async Task<IReadOnlyList<Customer>> GetActiveCustomersAsync() {
        return await _customers.FindAllAsync(new ActiveCustomerSpec());
    }

    public async Task<bool> HasHighValueCustomersAsync(decimal threshold) {
        return await _customers.ExistsAsync(new HighValueCustomerSpec(threshold));
    }

    public async Task<long> CountActiveHighValueAsync(decimal threshold) {
        var spec = new ActiveCustomerSpec() & new HighValueCustomerSpec(threshold);
        return await _customers.CountAsync(spec);
    }
}
```

Available extension methods:

| Method | Description |
|--------|-------------|
| `FindFirstAsync(specification)` | Returns the first matching entity or `null` |
| `FindAllAsync(specification)` | Returns all matching entities |
| `CountAsync(specification)` | Returns the count of matching entities |
| `ExistsAsync(specification)` | Returns `true` if any entity matches |

### With EntityManager

The same methods are available on `EntityManager<TEntity, TKey>`:

```csharp
public class CustomerManager(EntityManager<Customer> manager) {
    public async Task<OperationResult<Customer>> FindFirstActiveAsync() {
        return await manager.FindFirstAsync(new ActiveCustomerSpec());
    }

    public async Task<IReadOnlyList<Customer>> FindAllActiveAsync() {
        return await manager.FindAllAsync(new ActiveCustomerSpec());
    }

    public async Task<long> CountActiveAsync() {
        return await manager.CountAsync(new ActiveCustomerSpec());
    }
}
```

EntityManager methods return `OperationResult<TEntity>` for single-entity lookups, providing consistent error handling with the rest of the manager API.

## Composing Specifications

Combine specifications using the standard boolean operators:

```csharp
// AND: both conditions must be true
var activeHighValue = new ActiveCustomerSpec() & new HighValueCustomerSpec(1000m);

// OR: either condition can be true
var vipOrNew = new VipCustomerSpec() | new NewCustomerSpec();

// NOT: negate a condition
var inactiveCustomers = !new ActiveCustomerSpec();

// Complex: (active AND highValue) OR vip
var complex = (new ActiveCustomerSpec() & new HighValueCustomerSpec(500m))
            | new VipCustomerSpec();
```

## Best Practices

- **Name specifications after business concepts**: `ActiveCustomerSpec`, `OverdueInvoiceSpec`, `PendingOrderSpec` — not `FilterByStatusSpec`
- **Keep specifications stateless** when possible: accept parameters via constructor for parameterized specs
- **Compose, don't subclass**: use `&`, `|`, `!` instead of creating ever-more-specific subclasses
- **Test specifications in isolation**: since `ToQuery()` returns an `IQuery`, you can unit-test the query structure without a database
- **Use with EntityManager** for validated, cached, and event-emitting queries
