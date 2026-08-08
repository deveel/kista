# Driver-Specific Health Checks

Each repository driver provides optimized health checks tailored to its underlying data store.

## Entity Framework Core

**Package:** `Kista.HealthChecks.EntityFramework`

### What It Checks

1. **Database Connectivity**: Calls `DbContext.Database.CanConnectAsync()` to verify the database is reachable
2. **Optional Query Test**: If enabled, runs `context.Set<TEntity>().AnyAsync()` to verify query execution

### Configuration

```csharp
builder.Services
    .AddRepositoryContext()
    .UseEntityFramework<MyDbContext>(ef => ef
        .WithHealthChecks(options => {
            options.Timeout = TimeSpan.FromSeconds(10);
            options.TestQuery = true;  // Run a test query
            options.Tags = ["data", "sql"];
        }));
```

### Diagnostic Data

When healthy:
```json
{
  "EntityType": "ProductEntity",
  "KeyType": "Int32",
  "DbContextType": "MyApp.Data.MyDbContext",
  "EntityExists": true,  // Only if TestQuery = true
  "ResponseType": "Healthy"
}
```

When unhealthy:
```json
{
  "EntityType": "ProductEntity",
  "KeyType": "Int32",
  "DbContextType": "MyApp.Data.MyDbContext",
  "ConnectionState": "Disconnected",  // or ExceptionType
  "ResponseType": "Unhealthy"
}
```

### Best Practices

✅ **Enable TestQuery for critical entities** - Ensures queries can execute, not just connect  
✅ **Use longer timeouts for cold databases** - Some databases take time to wake up  
✅ **Monitor DbContext type** - Helps identify which context is failing in multi-context scenarios  

⚠️ **Avoid heavy queries** - `TestQuery` runs `AnyAsync()`, which is lightweight  
⚠️ **Consider connection pooling** - Health checks can exhaust pools if timeout is too short  

---

## MongoDB

**Package:** `Kista.HealthChecks.MongoFramework`

### What It Checks

1. **Cluster Connectivity**: Runs a MongoDB `ping` command to verify the cluster is reachable
2. **Database Accessibility**: Confirms the configured database can be accessed

### Configuration

```csharp
builder.Services
    .AddRepositoryContext()
    .UseMongoDB<MyMongoContext>(mongo => mongo
        .WithHealthChecks(options => {
            options.Timeout = TimeSpan.FromSeconds(5);
            options.PingTimeout = TimeSpan.FromSeconds(3);
            options.Tags = ["data", "mongo"];
        }));
```

### Diagnostic Data

When healthy:
```json
{
  "EntityType": "CustomerEntity",
  "KeyType": "String",
  "ResponseType": "Healthy"
}
```

When unhealthy:
```json
{
  "EntityType": "CustomerEntity",
  "KeyType": "String",
  "ExceptionType": "MongoDB.Driver.MongoConnectionException",
  "ResponseType": "Unhealthy"
}
```

### Best Practices

✅ **Set PingTimeout shorter than Timeout** - Fail fast on MongoDB issues  
✅ **Monitor replica set status** - Health checks work with replica sets and sharded clusters  
✅ **Use tags for environment separation** - Differentiate prod vs. dev MongoDB instances  

⚠️ **Network latency** - MongoDB health checks are network-dependent  
⚠️ **Authentication failures** - Will show as connection failures  

---

## In-Memory

**Package:** `Kista.HealthChecks.InMemory`

### What It Checks

The In-Memory health check **always returns Healthy** if the repository is registered. Since there are no external dependencies, the check simply confirms the repository is available in the DI container.

### Configuration

```csharp
builder.Services
    .AddRepositoryContext()
    .UseInMemory(inMem => inMem
        .WithHealthChecks(options => {
            options.Tags = ["cache", "live"];
        }));
```

### Diagnostic Data

Always healthy:
```json
{
  "EntityType": "CacheEntity",
  "KeyType": "Int32",
  "ResponseType": "Healthy"
}
```

### Use Cases

✅ **Testing** - Perfect for integration tests that need health check infrastructure  
✅ **Development** - Run the full application stack without external dependencies  
✅ **Caching layers** - Monitor in-memory caches alongside persistent stores  

⚠️ **Not for production monitoring** - Doesn't verify actual data store health  
⚠️ **Always healthy** - Won't detect application-level issues  

---

## Driver Comparison

| Feature | Entity Framework | MongoDB | In-Memory |
|---------|-----------------|---------|-----------|
| **Connectivity Test** | ✅ `CanConnectAsync()` | ✅ Ping command | ❌ N/A |
| **Query Test** | ✅ Optional (`TestQuery`) | ❌ N/A | ❌ N/A |
| **External Dependency** | ✅ Database server | ✅ MongoDB cluster | ❌ None |
| **Typical Timeout** | 5-10 seconds | 3-5 seconds | 1 second |
| **Failure Modes** | Connection, auth, timeout | Connection, auth, timeout | None |
| **Best For** | Relational data | Document storage | Testing, caching |

---

## Multi-Driver Scenarios

When using multiple drivers in the same application:

```csharp
builder.Services
    .AddRepositoryContext()
    .UseEntityFramework<MyDbContext>(ef => ef
        .WithHealthChecks(options => {
            options.Tags = ["data", "sql", "ready"];
        }))
    .UseMongoDB<MyMongoContext>(mongo => mongo
        .WithHealthChecks(options => {
            options.Tags = ["data", "mongo", "ready"];
        }))
    .UseInMemory(inMem => inMem
        .WithHealthChecks(options => {
            options.Tags = ["cache", "live"];
        }));

builder.Services
    .AddHealthChecks()
    .AddKistaRepositories();
```

**Health check names will be:**
- `"Kista:EntityFramework:ProductEntity"`
- `"Kista:MongoDB:CustomerEntity"`
- `"Kista:InMemory:CacheEntity"`

---

## Next Steps

- [Advanced Scenarios](advanced-scenarios.md) - Readiness/liveness probes, custom naming
- [Configuration Guide](configuration.md) - All configuration options
