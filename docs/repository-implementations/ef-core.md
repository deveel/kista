# Entity Framework Core Repository

| Feature | Status | Notes |
| ------- | :----: | ----- |
| Base Repository | ✅ | |
| Filterable | ✅ | |
| Queryable | ✅ | Native EF Core `IQueryable` |
| Pageable | ✅ | |
| Tracking | ✅ | EF Core change tracking |
| Multi-tenant | ✅ | Via `Deveel.Repository.EntityFramework.MultiTenant` |

The _Deveel Repository_ `Deveel.Repository.EntityFramework` package provides an implementation of the repository pattern that uses [Entity Framework Core](https://github.com/dotnet/efcore), enabling access to any relational database that EF Core supports (SQL Server, PostgreSQL, SQLite, MySQL, and others).

The `EntityRepository<TEntity>` class wraps a `DbContext` and exposes the full `IRepository<TEntity>` interface, including filterable, queryable, pageable, and tracking capabilities.

## Installation

```bash
dotnet add package Deveel.Repository.EntityFramework
```

## Registration

Use the fluent builder API to register the EF Core driver. The builder handles both `DbContext` and repository registration in a single call:

```csharp
// Program.cs
builder.Services.AddRepositoryContext()
    .UseEntityFramework<MyDbContext>(b => b
        .ConfigureDbContext(opts =>
            opts.UseSqlServer(builder.Configuration.GetConnectionString("Default"))));
```

For a custom repository that derives from `EntityRepository<TEntity>`:

```csharp
builder.Services.AddRepositoryContext()
    .UseEntityFramework<MyDbContext>(b => b
        .ConfigureDbContext(opts => opts.UseSqlServer("...")))
    .AddRepository<MyEntityRepository>();
```

### Configuration Options

| Method | Description |
| ------ | ----------- |
| `ConfigureDbContext(Action<DbContextOptionsBuilder>)` | Configures the `DbContext` options (provider, connection string, etc.) |
| `WithLifetime(ServiceLifetime)` | Sets the service lifetime for the `DbContext` and repositories (default: `Scoped`) |

## Multi-tenant Support

Install the `Deveel.Repository.EntityFramework.MultiTenant` package:

```bash
dotnet add package Deveel.Repository.EntityFramework.MultiTenant
```

Two multi-tenancy strategies are supported, both built on [Finbuckle.MultiTenant](https://www.finbuckle.com/MultiTenant):

### Strategy 1: Database-per-Tenant (`WithDatabasePerTenant`)

Each tenant connects to its own database. Your `ITenantInfo` implementation must have a `ConnectionString` property.

```csharp
builder.Services.AddMultiTenant<AppTenantInfo>()
    .WithInMemoryStore()
    .WithRouteStrategy();

builder.Services.AddRepositoryContext()
    .UseEntityFramework<AppDbContext>()
    .WithDatabasePerTenant<AppTenantInfo>(defaultConnection: "Data Source=default.db")
    .Build();
```

Your `DbContext` resolves the connection string in `OnConfiguring`:

```csharp
public class AppDbContext : DbContext
{
    private readonly IMultiTenantContextAccessor<AppTenantInfo> _tenantAccessor;
    private readonly IOptions<EntityFrameworkTenantConnectionOptions> _options;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor,
        IOptions<EntityFrameworkTenantConnectionOptions> options) : base(options)
    {
        _tenantAccessor = tenantAccessor;
        _options = options;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var connectionString = _tenantAccessor.MultiTenantContext?.TenantInfo?.ConnectionString
            ?? _options.Value.DefaultConnectionString;

        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException("No connection string available.");

        optionsBuilder.UseSqlServer(connectionString);
    }
}
```

### Strategy 2: Shared Database (`WithSharedTenantDatabase`)

All tenants share a single database. Data isolation is achieved by filtering queries based on the current tenant's ID. Your `DbContext` must derive from `Finbuckle.MultiTenant.EntityFrameworkCore.MultiTenantDbContext` and entities must be configured with `IsMultiTenant()`.

```csharp
builder.Services.AddMultiTenant<AppTenantInfo>()
    .WithInMemoryStore()
    .WithRouteStrategy();

builder.Services.AddRepositoryContext()
    .UseEntityFramework<AppDbContext>(b => b
        .ConfigureDbContext(opts => opts.UseSqlServer("..."))
        .WithSharedTenantDatabase());
```

```csharp
public class AppDbContext : MultiTenantDbContext
{
    public AppDbContext(
        IMultiTenantContextAccessor multiTenantContextAccessor,
        DbContextOptions<AppDbContext> options) : base(multiTenantContextAccessor, options) { }

    public DbSet<Order> Orders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>().IsMultiTenant();
    }
}
```

## Querying

`EntityRepository<TEntity>` implements both `IQueryableRepository<TEntity>` and `IFilterableRepository<TEntity>`.

**With lambda expressions (shorthand):**

```csharp
var entities = await repository.FindAllAsync(x => x.Name == "John");
```

**With `ExpressionQueryFilter<TEntity>`:**

```csharp
var filter = new ExpressionQueryFilter<MyEntity>(x => x.Name == "John");
var query   = new Query(filter);
var entities = await repository.FindAllAsync(query);
```

> The EF Core driver only supports `ExpressionQueryFilter<TEntity>` for filtering. Passing any other filter type will throw `NotSupportedException`.

## Notes

- The `UseEntityFramework<TDbContext>()` builder method handles `DbContext` registration — you do not need to call `AddDbContext` separately.
- Refer to the [Entity Framework Core documentation](https://learn.microsoft.com/en-us/ef/core/) for migration and schema configuration details.
- Refer to the [Finbuckle.MultiTenant documentation](https://www.finbuckle.com/MultiTenant) for multi-tenant configuration.
