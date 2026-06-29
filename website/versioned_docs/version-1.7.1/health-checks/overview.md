# Repository Health Checks

Kista provides built-in health check support for monitoring repository connectivity and readiness. This feature integrates seamlessly with the standard ASP.NET Core health check system, allowing you to monitor your data stores alongside other application dependencies.

## Quick Start

### Basic Configuration

```csharp
var builder = WebApplication.CreateBuilder(args);

// Configure repositories with health checks
builder.Services
    .AddRepositoryContext()
    .UseEntityFramework<MyDbContext>(ef => ef.WithHealthChecks())
    .UseMongoDB<MyMongoContext>(mongo => mongo.WithHealthChecks())
    .UseInMemory(inMem => inMem.WithHealthChecks());

// Register health checks
builder.Services
    .AddHealthChecks()
    .AddKistaRepositories();

var app = builder.Build();

// Expose health check endpoint
app.MapHealthChecks("/health");

app.Run();
```

### Health Check Response

The health check endpoint returns a JSON response like:

```json
{
  "status": "Healthy",
  "results": {
    "Kista:EntityFramework:ProductEntity": {
      "status": "Healthy",
      "description": "Database connection successful",
      "data": {
        "EntityType": "ProductEntity",
        "KeyType": "Int32",
        "DbContextType": "MyApp.Data.MyDbContext",
        "ResponseType": "Healthy"
      }
    },
    "Kista:MongoDB:CustomerEntity": {
      "status": "Healthy",
      "description": "MongoDB connection successful",
      "data": {
        "EntityType": "CustomerEntity",
        "KeyType": "String",
        "ResponseType": "Healthy"
      }
    },
    "Kista:InMemory:CacheEntity": {
      "status": "Healthy",
      "description": "In-memory repository is available",
      "data": {
        "EntityType": "CacheEntity",
        "KeyType": "Int32",
        "ResponseType": "Healthy"
      }
    }
  }
}
```

## How It Works

1. **Configuration Phase**: When you call `.WithHealthChecks()` on a driver builder, the configuration is stored for later use.

2. **Registration Phase**: When you call `.AddKistaRepositories()`, the system:
   - Auto-discovers all repositories registered via `AddRepositoryContext()`
   - Creates health checks for each repository based on its driver type
   - Applies driver-level defaults and per-repository overrides

3. **Execution Phase**: When the health endpoint is called, each repository health check:
   - Tests connectivity to the underlying data store
   - Returns a health status (Healthy, Degraded, or Unhealthy)
   - Includes diagnostic data for troubleshooting

## Features

- ✅ **Auto-Discovery**: Automatically finds repositories registered via `AddRepositoryContext()`
- ✅ **Driver-Specific Checks**: Each driver (EF Core, MongoDB, In-Memory) has optimized health checks
- ✅ **Configurable Naming**: Customize health check names with templates or custom generators
- ✅ **Per-Repository Configuration**: Override defaults for specific repositories
- ✅ **Startup Validation**: Optionally validate health at application startup
- ✅ **Standard Integration**: Works with ASP.NET Core health check ecosystem

## Supported Drivers

| Driver | Package | Health Check Features |
|--------|---------|----------------------|
| Entity Framework Core | `Kista.HealthChecks.EntityFramework` | Connection test, optional query execution |
| MongoDB | `Kista.HealthChecks.MongoFramework` | Ping command, cluster connectivity |
| In-Memory | `Kista.HealthChecks.InMemory` | Always healthy (no external dependencies) |

## Next Steps

- [Configuration Guide](configuration.md) - Detailed configuration options
- [Driver-Specific Guides](driver-specific.md) - EF Core, MongoDB, and In-Memory details
- [Advanced Scenarios](advanced-scenarios.md) - Readiness/liveness probes, custom naming, and more
