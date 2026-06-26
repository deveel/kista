# Health Check Configuration

This guide covers all configuration options for Kista repository health checks.

## Global Configuration

Configure health checks globally when calling `AddKistaRepositories()`:

```csharp
builder.Services
    .AddHealthChecks()
    .AddKistaRepositories(options => {
        // Set default timeout for all health checks
        options.Timeout = TimeSpan.FromSeconds(10);
        
        // Set failure status (Degraded or Unhealthy)
        options.FailureStatus = HealthStatus.Degraded;
        
        // Apply default tags to all repository health checks
        options.Tags = ["kista", "repository", "data"];
        
        // Configure startup validation mode
        options.StartupValidationMode = StartupValidationMode.Warning;
        
        // Customize health check naming
        options.Naming.Template = "Kista:{Driver}:{EntityType}";
    });
```

### Configuration Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Timeout` | `TimeSpan` | 5 seconds | Default timeout for all health checks |
| `FailureStatus` | `HealthStatus` | `Degraded` | Status to report when a health check fails |
| `Tags` | `string[]` | `["kista", "repository"]` | Default tags applied to all health checks |
| `StartupValidationMode` | `StartupValidationMode` | `None` | How to handle health checks at startup |
| `Naming.Template` | `string` | `"Kista:{Driver}:{EntityType}"` | Template for generating health check names |
| `Naming.NameGenerator` | `Func<Type, string>` | `null` | Custom function to generate health check names |
| `ExcludedRepositoryTypes` | `HashSet<Type>` | Empty | Repository types to exclude from health checks |
| `PerRepositoryConfig` | `Dictionary<Type, Action>` | Empty | Per-repository configuration overrides |

## Driver-Level Configuration

Configure health checks during driver setup:

### Entity Framework Core

```csharp
builder.Services
    .AddRepositoryContext()
    .UseEntityFramework<MyDbContext>(ef => ef
        .WithHealthChecks(options => {
            options.Timeout = TimeSpan.FromSeconds(10);
            options.TestQuery = true;  // Run a lightweight query
            options.Tags = ["data", "sql", "ready"];
            options.FailureStatus = HealthStatus.Unhealthy;
        }));
```

**EntityFrameworkHealthCheckOptions:**

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Timeout` | `TimeSpan` | 5 seconds | Timeout for the health check |
| `TestQuery` | `bool` | `false` | Whether to run a test query (`AnyAsync`) |
| `Tags` | `string[]` | `["kista", "entityframework", "repository"]` | Tags for the health check |
| `FailureStatus` | `HealthStatus` | `Degraded` | Failure status to report |

### MongoDB

```csharp
builder.Services
    .AddRepositoryContext()
    .UseMongoDB<MyMongoContext>(mongo => mongo
        .WithHealthChecks(options => {
            options.Timeout = TimeSpan.FromSeconds(5);
            options.PingTimeout = TimeSpan.FromSeconds(3);
            options.Tags = ["data", "mongo", "ready"];
        }));
```

**MongoHealthCheckOptions:**

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Timeout` | `TimeSpan` | 5 seconds | Timeout for the health check |
| `PingTimeout` | `TimeSpan` | 3 seconds | Timeout for the MongoDB ping command |
| `Tags` | `string[]` | `["kista", "mongodb", "repository"]` | Tags for the health check |
| `FailureStatus` | `HealthStatus` | `Degraded` | Failure status to report |

### In-Memory

```csharp
builder.Services
    .AddRepositoryContext()
    .UseInMemory(inMem => inMem
        .WithHealthChecks(options => {
            options.Tags = ["cache", "live"];
        }));
```

**InMemoryHealthCheckOptions:**

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Timeout` | `TimeSpan` | 1 second | Timeout for the health check |
| `Tags` | `string[]` | `["kista", "inmemory", "repository"]` | Tags for the health check |
| `FailureStatus` | `HealthStatus` | `Degraded` | Failure status to report |

## Per-Repository Configuration

Override global or driver-level defaults for specific repository types:

```csharp
builder.Services
    .AddHealthChecks()
    .AddKistaRepositories(options => {
        // Global defaults
        options.Timeout = TimeSpan.FromSeconds(5);
        
        // Override for critical entities
        options.ConfigureRepository<ProductEntity>(repoOptions => {
            repoOptions.Timeout = TimeSpan.FromSeconds(30);
            repoOptions.Tags = ["critical", "data", "ready"];
        });
        
        // Override for cache entities
        options.ConfigureRepository<CacheEntity>(repoOptions => {
            repoOptions.Timeout = TimeSpan.FromSeconds(1);
            repoOptions.Tags = ["cache", "live"];
        });
    });
```

## Excluding Repositories

Exclude specific repository types from health checks:

```csharp
builder.Services
    .AddHealthChecks()
    .AddKistaRepositories(options => {
        // Exclude internal or test repositories
        options.ExcludedRepositoryTypes.Add(typeof(InternalCacheEntity));
        options.ExcludedRepositoryTypes.Add(typeof(TestEntity));
    });
```

## Custom Naming

### Using Templates

```csharp
builder.Services
    .AddHealthChecks()
    .AddKistaRepositories(options => {
        // Available tokens: {Driver}, {EntityType}, {RepositoryType}
        options.Naming.Template = "Repository:{EntityType}({Driver})";
    });
```

**Example output:**
- `"Repository:ProductEntity(EntityFramework)"`
- `"Repository:CustomerEntity(MongoDB)"`

### Using Custom Name Generator

```csharp
builder.Services
    .AddHealthChecks()
    .AddKistaRepositories(options => {
        options.Naming.NameGenerator = repoType => {
            var entityType = Kista.RepositoryRegistrationUtil.GetEntityType(repoType);
            var driverType = repoType.Name.Contains("Entity") ? "EntityFramework" 
                             : repoType.Name.Contains("Mongo") ? "MongoDB" 
                             : "InMemory";
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            return $"{entityType?.Name}_{driverType}_{env}";
        };
    });
```

**Example output:**
- `"ProductEntity_EntityFramework_Production"`
- `"CustomerEntity_MongoDB_Production"`

## Configuration from appsettings.json

```json
{
  "Kista": {
    "HealthChecks": {
      "Timeout": "00:00:10",
      "FailureStatus": "Degraded",
      "Tags": ["kista", "repository"],
      "StartupValidationMode": "Warning",
      "Naming": {
        "Template": "Kista:{Driver}:{EntityType}"
      },
      "EntityFramework": {
        "TestQuery": true,
        "Timeout": "00:00:15"
      },
      "MongoDB": {
        "PingTimeout": "00:00:03"
      },
      "PerRepository": {
        "ProductEntity": {
          "Timeout": "00:00:30",
          "Tags": ["critical", "data"]
        }
      }
    }
  }
}
```

```csharp
// Load from configuration
builder.Services
    .AddRepositoryContext()
    .UseEntityFramework<MyDbContext>(ef => ef.WithHealthChecks())
    .UseMongoDB<MyMongoContext>();

builder.Services
    .AddHealthChecks()
    .AddKistaRepositories(
        builder.Configuration.GetSection("Kista:HealthChecks"));
```

## Startup Validation

Configure health checks to run at application startup:

```csharp
builder.Services
    .AddHealthChecks()
    .AddKistaRepositories(options => {
        options.StartupValidationMode = StartupValidationMode.Warning;
    });
```

**StartupValidationMode values:**

| Value | Behavior |
|-------|----------|
| `None` | No startup validation (default) |
| `Warning` | Log warning if health checks fail, but continue startup |
| `FailFast` | Throw exception and stop startup if health checks fail |

**Example with FailFast:**

```csharp
builder.Services
    .AddHealthChecks()
    .AddKistaRepositories(options => {
        options.StartupValidationMode = StartupValidationMode.FailFast;
    });

var app = builder.Build();
// Application will not start if repository health checks fail
app.Run();
```

## Next Steps

- [Driver-Specific Guides](driver-specific.md) - Detailed driver documentation
- [Advanced Scenarios](advanced-scenarios.md) - Readiness/liveness probes, ASP.NET Core integration
