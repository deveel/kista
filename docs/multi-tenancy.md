# Multi-Tenancy of Data Sources
> **Renamed:** This project was renamed from **Deveel.Repository** to **Kista** on **May 26, 2025**. The name *Kista* is Old Norse for "chest" or "repository", better reflecting the project purpose as a data access framework.

Software-as-a-Service (SaaS) applications and Enterprise-level applications often need to segregate data between different _tenants_ of the application, that could be different customers or different departments of the same company.

The preferred approach of the library is to use the [Finbuckle.MultiTenant](https://www.finbuckle.com/MultiTenant) framework to implement multi-tenant applications, and to use the `ITenantInfo` interface to retrieve the current tenant information: this is obtained by scanning the current HTTP request, and retrieving the tenant information from the request.

| Driver | Multi-Tenancy | Notes |
| ------ | ------------- | ----- |
| _In-Memory_ | :x: | Not supported |
| _MongoDB_ | :white_check_mark: | Via `Kista.MongoFramework.MultiTenant` |
| _Entity Framework Core_ | :white_check_mark: | Via `Kista.EntityFramework.MultiTenant` |

## Multi-Tenancy in Entity Framework Core

The `Kista.EntityFramework.MultiTenant` package provides two multi-tenancy strategies, both built on [Finbuckle.MultiTenant](https://www.finbuckle.com/MultiTenant).

### Strategy 1: Database-per-Tenant

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

The `DbContext` resolves the connection string in `OnConfiguring`:

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

### Strategy 2: Shared Database

All tenants share a single database. Data isolation is achieved by filtering queries based on the current tenant's ID. The `DbContext` must derive from `Finbuckle.MultiTenant.EntityFrameworkCore.MultiTenantDbContext` and entities must be configured with `IsMultiTenant()`.

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

### Choosing a Strategy

| Aspect | Database-per-Tenant | Shared Database |
| ------ | ------------------- | --------------- |
| **Data isolation** | Complete (separate databases) | Logical (TenantId column) |
| **Schema management** | Per-database migrations needed | Single schema for all tenants |
| **Cost** | Higher (one DB per tenant) | Lower (shared infrastructure) |
| **Tenant onboarding** | Create new database | Add tenant record |
| **Compliance** | Easier for data residency | Requires careful query filtering |

## Multi-Tenancy in MongoDB

The repository implementation to interface the MongoDB database in the Kista library is based on the [MongoFramework](https://github.com/TurnerSoftware/MongoFramework) project, that provides a set of abstractions to handle multi-tenancy in MongoDB.

To use multi-tenancy in MongoDB, you need to install the `Kista.MongoFramework.MultiTenant` package, and configure it to be used in your application.

First, configure Finbuckle.MultiTenant:

```csharp
builder.Services.AddMultiTenant<MongoDbTenantInfo>()
    .WithConfigurationStore()
    .WithRouteStrategy("tenant");
```

Then register a tenant-aware MongoDB context (derived from `MongoDbTenantContext`) and the repository:

```csharp
builder.Services.AddMongoDbContext<MyMongoTenantContext>(connectionBuilder =>
    connectionBuilder.UseConnection("mongodb://..."));

builder.Services.AddRepository<MongoRepository<MyEntity>>();
```

The tenant context resolves the correct database connection for each tenant automatically.
