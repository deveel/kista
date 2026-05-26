# Dynamic LINQ Filter Cache
> **Renamed:** This project was renamed from **Deveel.Repository** to **Kista** on **May 26, 2025**. The name *Kista* is Old Norse for "chest" or "repository", better reflecting the project purpose as a data access framework.

The `Kista.DynamicLinq` package includes a bounded, thread-safe expression cache that eliminates redundant parsing and compilation of Dynamic LINQ filter expressions. This feature is designed for production workloads where the same filter shapes are executed thousands of times per minute.

## The Problem

When you use Dynamic LINQ to filter repository queries, every call goes through these steps:

1. **Parse** the expression string into an expression tree (`DynamicExpressionParser.ParseLambda`)
2. **Compile** the expression tree into a delegate (`Expression.Compile()`)
3. **Execute** the compiled delegate against the data

Steps 1 and 2 are expensive. Parsing involves tokenization, syntax analysis, and expression tree construction. Compilation involves IL generation. Doing this on every query invocation creates avoidable CPU overhead that surfaces as elevated p95 latencies during traffic spikes — especially in multi-tenant applications where the same filter shape runs repeatedly.

## The Solution

The filter cache intercepts the parsing stage and stores parsed expression trees, keyed on a composite of the entity type, parameter name, and expression text. When the same filter is requested again, the cached expression tree is returned directly — no parsing, no compilation.

### What You Get

| Benefit | Description |
|---------|-------------|
| **Lower CPU utilization** | Repetitive query patterns skip parsing and compilation entirely |
| **Reduced p95 latency** | No parsing spikes during traffic surges — cache hits are O(1) dictionary lookups |
| **Bounded memory** | LRU eviction ensures the cache never grows beyond your configured limit |
| **Observable** | Hit/miss counters and hit rate are exposed for monitoring dashboards |
| **Replaceable** | The cache is a DI service — swap in your own implementation for custom eviction logic |
| **Zero behavioral change** | Only compilation is cached, not result sets. Query results are identical. |
| **Automatic resolution** | Filters receive the cache automatically from the repository's service provider — no manual passing required |

## Quick Start

### 1. Register the Cache

Add the filter cache to your DI container in `Program.cs`:

```csharp
// Default capacity: 1024 entries
builder.Services.AddFilterCache();

// Or configure a custom capacity
builder.Services.AddFilterCache(options => options.MaxCapacity = 4096);

// Or set capacity directly
builder.Services.AddFilterCache(maxCapacity: 2048);
```

### 2. Use It — Automatic Resolution (Recommended)

Once the cache is registered in DI and the repository is constructed with an `IServiceProvider`, `DynamicLinqFilter` resolves the cache automatically. You don't need to pass it manually:

```csharp
public class OrderService {
    private readonly IRepository<Order> _repository;

    public OrderService(IRepository<Order> repository) {
        _repository = repository;
    }

    public async Task<IReadOnlyList<Order>> FindActiveOrdersAsync() {
        // No cache parameter needed — it's resolved automatically
        var filter = new DynamicLinqFilter("x.Status == \"Active\"");
        return await _repository.FindAllAsync(filter);
    }
}
```

The repository calls `Initialize` on the filter before applying it, and `DynamicLinqFilter` resolves `IExpressionCache` from the service provider behind the scenes.

### 3. Use It — Manual Override (Optional)

If you need to use a specific cache instance for a particular query (overriding the DI-registered one), pass it to the constructor. A constructor-provided cache takes precedence over the context-resolved one:

```csharp
var dedicatedCache = new BoundedExpressionCache(256);
var filter = new DynamicLinqFilter("x.Status == \"Active\"", dedicatedCache);
return await _repository.FindAllAsync(filter);
```

### 4. Direct Use with `FilterExpression`

You can also use the cache with the `FilterExpression` static class directly:

```csharp
var cache = serviceProvider.GetRequiredService<IExpressionCache>();
var lambda = FilterExpression.AsLambda<Order>(cache, "x", "x.Status == \"Active\"");
```

## How Automatic Cache Resolution Works

The framework uses a two-part pattern to connect filters with infrastructure services:

### IFilterContext

`IFilterContext` provides filters access to the repository's service provider:

```csharp
public interface IFilterContext {
    IServiceProvider Services { get; }
}
```

### IQueryFilter.Initialize

`IQueryFilter` defines an `Initialize` method with a default no-op implementation:

```csharp
public interface IQueryFilter {
    void Initialize(IFilterContext context) { }
}
```

When a repository method like `FindAllAsync`, `CountAsync`, `ExistsAsync`, or `GetPageAsync` receives a filter, it:

1. Creates a `DefaultFilterContext` wrapping its `IServiceProvider`
2. Calls `filter.Initialize(context)` before applying the filter to the query
3. The filter resolves any services it needs (e.g., `IExpressionCache`)

### DynamicLinqFilter Implementation

`DynamicLinqFilter` implements `Initialize` to auto-resolve the cache:

```csharp
public void Initialize(IFilterContext context) {
    if (Cache != null)
        return;  // Constructor-provided cache takes precedence

    Cache = context.Services.GetService(typeof(IExpressionCache)) as IExpressionCache;
}
```

### CombinedQueryFilter Propagation

`CombinedQueryFilter` propagates `Initialize` to all child filters, so nested filters also receive the context:

```csharp
public void Initialize(IFilterContext context) {
    foreach (var filter in filters)
        filter.Initialize(context);
}
```

### Repository Support

All built-in repository implementations (`InMemoryRepository`, `EntityRepository`, `MongoRepository`) accept an optional `IServiceProvider` in their constructors and expose it via the `Services` property:

```csharp
// InMemoryRepository
var repo = new InMemoryRepository<Order>(services: serviceProvider);

// EntityRepository
var repo = new EntityRepository<Order, Guid>(dbContext, services: serviceProvider);

// MongoRepository
var repo = new MongoRepository<Order, string>(mongoContext, services: serviceProvider);
```

When `Services` is non-null, the repository initializes all filters before applying them.

## Architecture

### Two Cache Layers

The framework provides two separate cache interfaces for the two stages of the Dynamic LINQ pipeline:

| Interface | Caches | Used By |
|-----------|--------|---------|
| `IExpressionCache` | Parsed `LambdaExpression` trees | `DynamicLinqFilter`, `FilterExpression.AsLambda<T>()` |
| `IFilterCache` | Compiled `Delegate` instances | `FilterExpression.Compile()` overloads |

For most use cases, `IExpressionCache` is the one you want. It caches the parsed expression tree, which avoids both parsing and (when used with `IQueryable`) allows the LINQ provider to compile the expression in its own optimized way.

### How `AddFilterCache()` Works

The `AddFilterCache()` extension registers **both** cache types as singletons with the same capacity:

```csharp
// This registers both IExpressionCache and IFilterCache
builder.Services.AddFilterCache(maxCapacity: 2048);
```

If you need different capacities or custom implementations, register them individually:

```csharp
builder.Services.AddExpressionCache<MyExpressionCache>();
builder.Services.AddFilterCache<MyFilterCache>();
```

## Configuration

### BoundedFilterCacheOptions

| Property | Default | Description |
|----------|---------|-------------|
| `MaxCapacity` | 1024 | Maximum number of entries before LRU eviction begins |

### Choosing the Right Capacity

The optimal cache size depends on the diversity of filter expressions in your application:

| Scenario | Recommended Capacity |
|----------|---------------------|
| Small application with a handful of fixed filters | 256–512 |
| Multi-tenant SaaS with per-tenant filter variations | 1024–4096 |
| Large application with many dynamic search combinations | 4096–8192 |

Monitor the cache hit rate to determine whether your capacity is adequate. A hit rate below 70% typically indicates the cache is too small for your workload.

## Monitoring

### IFilterCacheStatistics

Both `BoundedFilterCache` and `BoundedExpressionCache` expose statistics through the `Statistics` property:

```csharp
var cache = serviceProvider.GetRequiredService<IExpressionCache>();
var stats = cache.Statistics;

Console.WriteLine($"Hits: {stats.Hits}");
Console.WriteLine($"Misses: {stats.Misses}");
Console.WriteLine($"Hit Rate: {stats.HitRate:P1}");
Console.WriteLine($"Current Size: {stats.CurrentSize}");
Console.WriteLine($"Max Capacity: {stats.MaxCapacity}");
```

| Property | Type | Description |
|----------|------|-------------|
| `Hits` | `long` | Total cache hits since creation or last reset |
| `Misses` | `long` | Total cache misses since creation or last reset |
| `HitRate` | `double` | Ratio between 0.0 and 1.0 (0%–100%) |
| `CurrentSize` | `int` | Number of entries currently in the cache |
| `MaxCapacity` | `int` | Configured maximum capacity |

### Integrating with Health Checks

You can expose cache statistics through ASP.NET Core Health Checks:

```csharp
builder.Services.AddHealthChecks()
    .AddCheck("filter-cache", () => {
        var cache = serviceProvider.GetRequiredService<IExpressionCache>();
        var stats = cache.Statistics;
        return stats.HitRate > 0.5
            ? HealthCheckResult.Healthy($"Hit rate: {stats.HitRate:P1}")
            : HealthCheckResult.Degraded($"Low hit rate: {stats.HitRate:P1}");
    });
```

### Resetting Statistics

Call `stats.Reset()` to clear counters without clearing the cache contents:

```csharp
// Reset at the start of each monitoring window
cache.Statistics.Reset();
```

## Thread Safety

Both `BoundedFilterCache` and `BoundedExpressionCache` are fully thread-safe. All public methods can be called concurrently from multiple request threads. The implementation uses `SemaphoreSlim` for cache operations, leveraging spin-waiting under moderate contention for better throughput than a traditional `lock`. Statistics counters use `Volatile` reads/writes, ensuring that monitoring access does not contend with cache throughput.

## Custom Cache Implementations

The cache interfaces are designed to be replaceable. Implement `IExpressionCache` or `IFilterCache` to provide custom behavior:

```csharp
public class DistributedExpressionCache : IExpressionCache {
    private readonly IDistributedCache _distributedCache;
    // ... implementation
}

builder.Services.AddSingleton<IExpressionCache, DistributedExpressionCache>();
```

### Interface Contract

When implementing a custom cache, follow these rules:

| Method | Requirement |
|--------|-------------|
| `TryGet` | Must return `true` and set the output parameter when the key exists; `false` otherwise |
| `Set` | Must store the expression; if the key exists, update the value |
| `Clear` | Must remove all entries; must not reset statistics |
| `Statistics` | Return an `IFilterCacheStatistics` instance, or `null` if not tracking |

## Custom Filter Implementations

If you build your own `IQueryFilter` that needs infrastructure services, implement `Initialize`:

```csharp
public class CachedDynamicFilter : IQueryFilter {
    private readonly string _expression;
    private IExpressionCache? _cache;

    public CachedDynamicFilter(string expression) {
        _expression = expression;
    }

    public void Initialize(IFilterContext context) {
        _cache = context.Services.GetService<IExpressionCache>();
    }

    public Expression<Func<TEntity, bool>> AsLambda<TEntity>() where TEntity : class {
        return FilterExpression.AsLambda<TEntity>(_cache, "x", _expression);
    }
}
```

The `Initialize` method is called by the repository before the filter is applied, so by the time `AsLambda<TEntity>()` runs, `_cache` is already resolved.

## Benchmarks

The repository includes BenchmarkDotNet baselines for the filter cache. Run them with:

```bash
dotnet run -c Release --framework net8.0 \
  --project benchmarks/repobench/repobench.csproj \
  -- --driver dynamic-linq
```

The benchmark suite compares:

| Benchmark | What It Measures |
|-----------|-----------------|
| `ColdCache_ParseAndCompile` | Baseline: parsing + compilation without cache |
| `WarmCache_CacheHit` | Cache hit: dictionary lookup only |
| `WarmCache_MixedExpressions` | Multiple distinct expressions with cache |
| `ColdCache_MultipleDistinctExpressions` | Multiple distinct expressions without cache |

## Cache Key Format

Expression cache keys are composite strings that prevent collisions between different entity types and parameter names:

```
{EntityFullName}|{ParameterName}|{Expression}
```

For example:

```
MyApp.Models.Order|x|x.Status == "Active"
MyApp.Models.Customer|c|c.Region == "US"
```

This ensures that the same expression string applied to different entity types produces separate cache entries.

## Migration Notes

### From v1.5.x (Automatic Cache Resolution)

The `IQueryFilter` interface now includes an `Initialize(IFilterContext context)` method with a default no-op body. Existing filter implementations continue to work without modification.

`DynamicLinqFilter` now auto-resolves `IExpressionCache` from the repository's service provider when one is available. If you previously passed the cache manually, that still works — the constructor-provided cache takes precedence.

Repositories now accept an optional `IServiceProvider` parameter. To enable automatic cache resolution, pass the service provider when constructing the repository:

```csharp
// Before: cache had to be passed manually on every filter
var filter = new DynamicLinqFilter("x.Status == \"Active\"", cache);

// After: cache is resolved automatically (if repository has Services)
var filter = new DynamicLinqFilter("x.Status == \"Active\"");
```

### From v1.4.x

The `DynamicLinqFilter` constructors accept an optional `IExpressionCache` parameter. Existing code continues to work without modification — the cache parameter defaults to `null`, which preserves the previous behavior of parsing on every call.

```csharp
// Before (still works)
var filter = new DynamicLinqFilter("x.Status == \"Active\"");

// After (with caching — manual)
var filter = new DynamicLinqFilter("x.Status == \"Active\"", cache);

// After (with caching — automatic, when repository has IServiceProvider)
var filter = new DynamicLinqFilter("x.Status == \"Active\"");
```

The `IFilterCache` interface gained two new members:

| Member | Type | Default |
|--------|------|---------|
| `Statistics` | `IFilterCacheStatistics?` | `null` |
| `Clear()` | `void` | — |

Existing `IFilterCache` implementations need to add these members. The `Clear()` method should remove all entries, and `Statistics` can return `null` if statistics tracking is not needed.
