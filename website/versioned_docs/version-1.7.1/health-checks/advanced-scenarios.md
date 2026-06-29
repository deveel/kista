# Advanced Health Check Scenarios

This guide covers advanced usage patterns for Kista repository health checks.

## Separate Readiness and Liveness Probes

In containerized environments (Kubernetes, Docker Swarm), you may need separate endpoints for readiness and liveness:

- **Readiness**: Is the application ready to accept traffic? (checks dependencies)
- **Liveness**: Is the application still alive? (basic process check)

### Configuration

```csharp
builder.Services
    .AddRepositoryContext()
    .UseEntityFramework<MyDbContext>(ef => ef
        .WithHealthChecks(options => {
            options.Tags = ["ready", "data"];
        }))
    .UseMongoDB<MyMongoContext>(mongo => mongo
        .WithHealthChecks(options => {
            options.Tags = ["ready", "data"];
        }))
    .UseInMemory(inMem => inMem
        .WithHealthChecks(options => {
            options.Tags = ["live", "cache"];
        }));

builder.Services
    .AddHealthChecks()
    .AddKistaRepositories();

var app = builder.Build();

// Readiness probe - checks all data dependencies
app.MapHealthChecks("/health/ready", new HealthCheckOptions {
    Predicate = check => check.Tags.Contains("ready")
});

// Liveness probe - basic process check
app.MapHealthChecks("/health/live", new HealthCheckOptions {
    Predicate = check => check.Tags.Contains("live")
});

app.Run();
```

### Kubernetes Example

```yaml
apiVersion: v1
kind: Pod
metadata:
  name: my-app
spec:
  containers:
  - name: my-app
    image: my-app:latest
    readinessProbe:
      httpGet:
        path: /health/ready
        port: 8080
      initialDelaySeconds: 30
      periodSeconds: 10
      timeoutSeconds: 5
    livenessProbe:
      httpGet:
        path: /health/live
        port: 8080
      initialDelaySeconds: 60
      periodSeconds: 30
      timeoutSeconds: 5
```

---

## Custom Health Check Endpoint with ASP.NET Core Integration

Use the optional `Kista.Manager.AspNetCore.HealthChecks` package for simplified endpoint configuration:

### Installation

```bash
dotnet add package Kista.Manager.AspNetCore.HealthChecks
```

### Usage

```csharp
builder.Services
    .AddRepositoryContext()
    .UseEntityFramework<MyDbContext>()
    .UseMongoDB<MyMongoContext>();

builder.Services
    .AddHealthChecks()
    .AddKistaRepositories();

var app = builder.Build();

// Use Kista's simplified endpoint mapping
app.MapRepositoryHealthChecks("/health", options => {
    options.ResponseType = HealthCheckResponseFormat.Json;
    options.SuccessStatusCode = 200;
    options.DegradedStatusCode = 200;
    options.UnhealthyStatusCode = 503;
    options.AllowCaching = false;
    options.TagFilter = tags => tags.Contains("production");
});

app.Run();
```

### Endpoint Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ResponseType` | `HealthCheckResponseFormat` | `Json` | Response format (Json or Text) |
| `SuccessStatusCode` | `int` | 200 | HTTP status for Healthy |
| `DegradedStatusCode` | `int` | 200 | HTTP status for Degraded |
| `UnhealthyStatusCode` | `int` | 503 | HTTP status for Unhealthy |
| `AllowCaching` | `bool` | `false` | Whether to allow response caching |
| `TagFilter` | `Func<IEnumerable<string>, bool>` | `null` | Filter which health checks to include |

---

## Authorization on Health Endpoints

Protect health check endpoints with authorization:

```csharp
var app = builder.Build();

app.MapHealthChecks("/health", new HealthCheckOptions {
    Predicate = check => check.Tags.Contains("production")
})
.RequireAuthorization()  // Requires authentication
.RequireAuthorization("HealthCheckPolicy");  // Requires specific policy

app.Run();
```

### Authorization Policy Example

```csharp
builder.Services.AddAuthorization(options => {
    options.AddPolicy("HealthCheckPolicy", policy =>
        policy.RequireClaim("role", "monitoring"));
});
```

---

## Host Filtering

Restrict health check endpoints to specific hosts:

```csharp
var app = builder.Build();

app.MapHealthChecks("/health")
    .RequireHost("*:5001");  // Only respond on port 5001

app.Run();
```

---

## Custom Response Writer

Implement a custom response writer for monitoring system integration:

```csharp
var app = builder.Build();

app.MapHealthChecks("/health", new HealthCheckOptions {
    ResponseWriter = async (context, report) => {
        context.Response.ContentType = "application/json";
        
        var response = new {
            Status = report.Status.ToString(),
            Duration = report.TotalDuration,
            Checks = report.Entries.Select(e => new {
                Name = e.Key,
                Status = e.Value.Status.ToString(),
                Description = e.Value.Description,
                Duration = e.Value.Duration
            })
        };
        
        await JsonSerializer.SerializeAsync(context.Response.Body, response);
    }
});

app.Run();
```

---

## Health Check Publishers

Push health check results to monitoring systems:

```csharp
builder.Services.AddSingleton<IHealthCheckPublisher, ApplicationInsightsHealthCheckPublisher>();

builder.Services.Configure<HealthCheckPublisherOptions>(options => {
    options.Delay = TimeSpan.FromSeconds(5);
    options.Period = TimeSpan.FromSeconds(30);
    options.Timeout = TimeSpan.FromSeconds(10);
});
```

### Custom Publisher Example

```csharp
public class ApplicationInsightsHealthCheckPublisher : IHealthCheckPublisher {
    private readonly TelemetryClient _telemetry;
    
    public ApplicationInsightsHealthCheckPublisher(TelemetryClient telemetry) {
        _telemetry = telemetry;
    }
    
    public Task PublishAsync(HealthReport report, CancellationToken cancellationToken) {
        _telemetry.TrackMetric("HealthCheck.Status", 
            report.Status == HealthStatus.Healthy ? 1 : 0);
        
        foreach (var entry in report.Entries) {
            _telemetry.TrackMetric($"HealthCheck.{entry.Key}.Status",
                entry.Value.Status == HealthStatus.Healthy ? 1 : 0);
        }
        
        return Task.CompletedTask;
    }
}
```

---

## Performance Considerations

### Timeout Configuration

Set appropriate timeouts based on your environment:

```csharp
builder.Services
    .AddHealthChecks()
    .AddKistaRepositories(options => {
        // Production - longer timeouts
        options.Timeout = TimeSpan.FromSeconds(10);
        
        // Development - shorter timeouts
        if (builder.Environment.IsDevelopment()) {
            options.Timeout = TimeSpan.FromSeconds(3);
        }
    });
```

### Caching

Health check responses are not cached by default. For high-traffic scenarios:

```csharp
app.MapHealthChecks("/health", new HealthCheckOptions {
    AllowCachingResponses = true
});
```

⚠️ **Warning**: Caching can delay detection of actual failures. Use with caution.

### Rate Limiting

Apply rate limiting to health check endpoints:

```csharp
builder.Services.AddRateLimiter(options => {
    options.AddPolicy("HealthCheckLimit", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1)
            }));
});

var app = builder.Build();

app.MapHealthChecks("/health")
    .RequireRateLimiting("HealthCheckLimit");
```

---

## Troubleshooting

### Health Check Fails Intermittently

**Symptoms**: Health check occasionally returns Unhealthy

**Solutions**:
1. Increase timeout: `options.Timeout = TimeSpan.FromSeconds(15);`
2. Check network connectivity
3. Verify database connection pool settings
4. Enable detailed logging:

```csharp
builder.Logging.AddFilter("Microsoft.Extensions.Diagnostics.HealthChecks", LogLevel.Debug);
```

### Health Check Never Passes at Startup

**Symptoms**: Application fails to start with `FailFast` mode

**Solutions**:
1. Check connection strings in configuration
2. Verify database server is running
3. Check firewall rules
4. Temporarily disable `FailFast` to see detailed errors:

```csharp
options.StartupValidationMode = StartupValidationMode.Warning;
```

### Health Check Endpoint Returns 404

**Symptoms**: `/health` endpoint not found

**Solutions**:
1. Ensure `MapHealthChecks()` is called after `UseRouting()`
2. Verify endpoint path is correct
3. Check for conflicting routes

---

## Next Steps

- [Overview](overview.md) - Introduction to health checks
- [Configuration Guide](configuration.md) - All configuration options
- [Driver-Specific Guides](driver-specific.md) - EF Core, MongoDB, In-Memory details
