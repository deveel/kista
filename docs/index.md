# Getting Started

Kista is a framework that provides a set of abstractions and implementations of the [_Repository Pattern_](https://en.wikipedia.org/wiki/Repository_pattern), to simplify the access to data sources in your application while keeping your domain model decoupled from any specific persistence technology.

Below you will find a quick and generic guide to start using the framework in your application.

To learn about the specific usage of the framework, you can read the following documentation:

| Topic | Description |
| ----- | ----------- |
| [_Using the Entity Framework Core Repository_](repository-implementations/ef-core.md) | Learn how to use the Repository pattern with [Entity Framework Core](https://github.com/dotnet/efcore) |
| [_Using the MongoDB Repository_](repository-implementations/mongodb.md) | Accessing [MongoDB](https://mongodb.com) databases through the Repository pattern |
| [_Using the In-Memory Repository_](repository-implementations/in-memory.md) | Interface a volatile and in-process storage using a Repository pattern. |
| [_Filtering_](filtering/) | Expression-based and Dynamic LINQ filtering with automatic service resolution |
| [_Filter Cache_](filtering/filter-cache.md) | Bounded expression caching for high-throughput Dynamic LINQ queries |
| [_The Entity Manager_](entity-manager/) | Provide your application with a business layer on top of the Repository for additional functions (_logging_, _validation_, _caching_, _event sourcing_, etc.) |
| [_Repository Lifecycle_](repository-lifecycle/) | Automate repository creation, teardown, and seeding during application startup |
| [_Extending the Repository_](custom-repository/) | Learn how to create a custom repository to access your data source, according to your specific data logic |
| [_Multi-Tenancy_](multi-tenancy.md) | Learn how to use the framework in a multi-tenant application |
| [_User Entities_](user-entities/) | Learn how to define and query entities that are scoped to a specific user |
| [_Sample Application_](sample-app.md) | A complete ASP.NET Core reference app with lifecycle management and CRUD endpoints |

## Installation

The framework is organized into a _kernel_ package (`Kista`) that provides the basic interfaces and abstractions, and a set of _driver_ packages that implement those abstractions for specific data sources.

When you install any driver package, the kernel package is automatically pulled in as a transitive dependency — you do not need to install it explicitly.

### Requirements

The library targets the following .NET runtimes:

| .NET Runtime | Supported |
| ------------ | :-------: |
| .NET 8.0     | ✅ |
| .NET 9.0     | ✅ |
| .NET 10.0    | ✅ |

> Support for .NET 6.0 and .NET 7.0 was dropped. Please ensure your project targets .NET 8.0 or later.

### The Kernel Package

All driver packages are built on top of the _kernel_ package that provides the core interfaces and abstractions. If you want to develop your own driver for a specific data source, depend only on `Kista` and implement the `IRepository<TEntity>` interface.

```bash
dotnet add package Kista
```

### The Drivers

| Driver | Package | Description |
| ------ | ------- | ----------- |
| [_In-Memory_](repository-implementations/in-memory.md) | `Kista.InMemory` | A volatile, in-process repository — ideal for testing and prototyping. |
| [_MongoDB_](repository-implementations/mongodb.md) | `Kista.MongoFramework` | Stores entities in a MongoDB database via [MongoFramework](https://github.com/turnersoftware/mongoframework). |
| [_MongoDB Multi-Tenant_](multi-tenancy.md) | `Kista.MongoFramework.MultiTenant` | Multi-tenant MongoDB connection management via [Finbuckle.MultiTenant](https://github.com/Finbuckle/Finbuckle.MultiTenant). |
| [_Entity Framework Core_](repository-implementations/ef-core.md) | `Kista.EntityFramework` | Stores entities in any relational database supported by [Entity Framework Core](https://github.com/dotnet/efcore). |
| [_EF Core Multi-Tenant_](repository-implementations/ef-core.md#multi-tenant-support) | `Kista.EntityFramework.MultiTenant` | Multi-tenant EF Core with database-per-tenant or shared-database strategies. |
| [_Dynamic LINQ_](repository-implementations/README.md#dynamic-linq-support) | `Kista.DynamicLinq` | Runtime string-based filter expressions via [System.Linq.Dynamic.Core](https://github.com/zzzprojects/System.Linq.Dynamic.Core). |
| [_Entity Manager_](entity-manager/) | `Kista.Manager` | Business layer with validation, normalization, caching, and event sourcing. |
| [_Entity Manager EasyCaching_](entity-manager/caching-entities.md) | `Kista.Manager.EasyCaching` | Second-level caching for EntityManager via [EasyCaching](https://github.com/dotnetcore/EasyCaching). |
| [_Entity Manager ASP.NET Core_](entity-manager/http-request-cancellation.md) | `Kista.Manager.AspNetCore` | ASP.NET Core integration for automatic HTTP request cancellation. |
| [_User Entities / Owner Scoping_](user-entities/) | `Kista.Owners` | Decorator-based user scoping with automatic owner assignment and query filtering. |
| [_Entity States_](#) | `Kista.States.Core` | Entity state management abstractions (experimental). |

## Instrumentation

The library provides a fluent builder API via `AddRepositoryContext()` to configure repositories, drivers, and cross-cutting concerns in a unified way.

### Using the Fluent Builder

```csharp
// Program.cs

// In-Memory driver
builder.Services.AddRepositoryContext()
    .UseInMemory();

// Entity Framework Core driver
builder.Services.AddRepositoryContext()
    .UseEntityFramework<MyDbContext>(b => b
        .ConfigureDbContext(opts => opts.UseSqlServer("...")));

// MongoDB driver
builder.Services.AddRepositoryContext()
    .UseMongoDB<MyMongoContext>(b => b
        .WithConnectionString("mongodb://..."));
```

Each `Use*()` method registers **open-generic** repository types (`InMemoryRepository<>`, `EntityRepository<>`, `MongoRepository<>`) with the DI container. This means `IRepository<AnyEntity>` resolves automatically — you do **not** need to write a repository class for every entity type. When you also register a concrete repository via `AddRepository<MyRepo>()`, the closed generic takes precedence over the open generic for that specific entity, letting you mix bulk defaults with per-entity customization.

Each driver call returns to the parent builder, allowing you to chain multiple concerns:

```csharp
builder.Services.AddRepositoryContext()
    .UseEntityFramework<AppDbContext>(b => b
        .ConfigureDbContext(opts => opts.UseSqlServer("...")))
    .WithManagement()
    .WithEasyCaching();
```

### Assembly Scanning

Use `ScanRepositories()` to automatically discover and register repository types from one or more assemblies:

```csharp
builder.Services.AddRepositoryContext()
    .UseInMemory()
    .ScanRepositories(typeof(MyEntityRepository).Assembly);
```

Open generic repositories are registered as open generics; closed repositories are registered via their service interfaces.

### Legacy Registration

The older `AddRepository<T>` extension method is still available for backward compatibility:

```csharp
builder.Services.AddRepository<InMemoryRepository<MyEntity>>();
```

> **Note:** `AddRepository<T>` is marked `[Obsolete]` but not as an error. It will continue to work. Prefer `AddRepositoryContext()` for new code.

### Consuming the Repository

After registration, the following service types become available in the DI container (availability depends on which interfaces the concrete repository implements):

| Service | Description |
| ------- | ----------- |
| `MyCustomRepository` | The concrete repository implementation. |
| `IRepository<MyEntity>` | Core CRUD, key-based look-up (`FindAsync`), and unsorted pagination (`GetPageAsync`). |
| `IMyCustomRepository` | The custom interface (if defined) that extends `IRepository<MyEntity>` with domain-specific query methods. |
| `ITrackingRepository<MyEntity>` | Change-tracking queries (if implemented). |

> **Note:** The legacy extension interfaces `IQueryableRepository`, `IPageableRepository`, and `IFilterableRepository` are deprecated. Query capabilities are now provided through `protected` members of `Repository<TEntity, TKey>` and should be exposed via domain-specific methods on your custom repository interface. See [The Repository Pattern](repository-pattern.md) and [Customize the Repository](custom-repository/) for details.

### Quick Example

After registration, inject `IRepository<TEntity>` into your application code. Here is a minimal ASP.NET Core API that manages a `Person` entity:

```csharp
// Entity
public class Person {
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

// Registration in Program.cs
builder.Services.AddRepositoryContext()
    .UseInMemory();

// Minimal API endpoint
app.MapGet("/people", async (IRepository<Person> repo) => {
    var people = await repo.FindAllAsync();
    return Results.Ok(people);
});

app.MapPost("/people", async (Person person, IRepository<Person> repo) => {
    await repo.AddAsync(person);
    return Results.Created($"/people/{person.Id}", person);
});

app.MapGet("/people/{id}", async (Guid id, IRepository<Person> repo) => {
    var person = await repo.FindAsync(id);
    return person is not null ? Results.Ok(person) : Results.NotFound();
});
```

The repository handles all CRUD operations — no `DbContext`, no `IMongoCollection`, no SQL. The underlying driver translates calls into the appropriate storage commands.
