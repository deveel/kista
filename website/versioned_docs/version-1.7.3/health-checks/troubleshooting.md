# Troubleshooting Health Checks

Common issues and solutions for Kista repository health checks.

## Health Check Fails with Timeout

### Symptoms
```json
{
  "status": "Unhealthy",
  "results": {
    "Kista:EntityFramework:ProductEntity": {
      "status": "Unhealthy",
      "description": "Health check timed out",
      "data": {
        "ErrorType": "Timeout"
      }
    }
  }
}
```

### Causes
1. Database server is slow to respond
2. Network latency
3. Connection pool exhaustion
4. Database is starting up (cold start)

### Solutions

**1. Increase Timeout**
```csharp
builder.Services
    .AddHealthChecks()
    .AddKistaRepositories(options => {
        options.Timeout = TimeSpan.FromSeconds(15);
        
        // Or per-repository
        options.ConfigureRepository<ProductEntity>(repoOptions => {
            repoOptions.Timeout = TimeSpan.FromSeconds(30);
        });
    });
```

**2. Check Connection Pool Settings**
```csharp
// Entity Framework
optionsBuilder.UseSqlServer(connectionString, opts => {
    opts.MaxBatchSize(1000);
    opts.CommandTimeout(30);
});
```

**3. Enable Connection Resiliency**
```csharp
optionsBuilder.UseSqlServer(connectionString, opts => {
    opts.EnableRetryOnFailure(
        maxRetryCount: 5,
        maxRetryDelay: TimeSpan.FromSeconds(30),
        errorNumbersToAdd: null);
});
```

**4. Monitor Connection Pool**
```csharp
// Add logging for connection pool
builder.Logging.AddFilter(
    "Microsoft.EntityFrameworkCore.Database.Connection",
    LogLevel.Debug);
```

---

## Health Check Fails with Connection Error

### Symptoms
```json
{
  "status": "Unhealthy",
  "results": {
    "Kista:EntityFramework:ProductEntity": {
      "status": "Unhealthy",
      "description": "Database connection failed: Login timeout expired",
      "data": {
        "DbContextType": "MyApp.Data.MyDbContext",
        "ExceptionType": "System.Data.SqlClient.SqlException"
      }
    }
  }
}
```

### Causes
1. Invalid connection string
2. Database server is down
3. Network connectivity issues
4. Authentication failure
5. Firewall blocking connection

### Solutions

**1. Verify Connection String**
```csharp
// Check configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
Console.WriteLine($"Connection string: {connectionString}");
```

**2. Test Database Connectivity Manually**
```csharp
// Add a test endpoint
app.MapGet("/test-db", async (MyDbContext context) => {
    try {
        var canConnect = await context.Database.CanConnectAsync();
        return Results.Ok(new { CanConnect = canConnect });
    }
    catch (Exception ex) {
        return Results.Problem(ex.Message);
    }
});
```

**3. Check Network Connectivity**
```bash
# From application host
telnet db-server 1433  # SQL Server
telnet mongo-server 27017  # MongoDB
```

**4. Verify Authentication**
```csharp
// For SQL Server
// Use Windows Authentication
"Server=.;Database=MyDb;Trusted_Connection=True;"

// Or SQL Authentication
"Server=.;Database=MyDb;User Id=user;Password=pass;"
```

**5. Check Firewall Rules**
- Ensure database port is open
- Verify application host IP is allowed
- Check cloud security groups (AWS, Azure, GCP)

---

## Health Check Endpoint Returns 404

### Symptoms
- Request to `/health` returns 404 Not Found
- No health check endpoint is accessible

### Causes
1. `MapHealthChecks()` not called
2. Called before `UseRouting()`
3. Path mismatch
4. Endpoint routing not enabled

### Solutions

**1. Ensure Correct Order**
```csharp
var app = builder.Build();

app.UseRouting();  // Must come before MapHealthChecks
app.MapHealthChecks("/health");  // Then map health checks
app.UseEndpoints();  // If using UseEndpoints
```

**2. Verify Path**
```csharp
// Check the configured path
app.MapHealthChecks("/health");  // Must match request URL

// Test with curl
curl http://localhost:5000/health
```

**3. Enable Endpoint Routing**
```csharp
// In Program.cs or Startup.cs
builder.Services.AddEndpointsApiExplorer();
```

---

## Health Check Always Returns Degraded

### Symptoms
- Health check consistently returns `Degraded` status
- Application functions normally

### Causes
1. `FailureStatus` set to `Degraded` (default)
2. Minor dependency issue
3. Configuration issue

### Solutions

**1. Check Failure Status Configuration**
```csharp
builder.Services
    .AddHealthChecks()
    .AddKistaRepositories(options => {
        // Change to Unhealthy for critical failures
        options.FailureStatus = HealthStatus.Unhealthy;
    });
```

**2. Review Health Check Logs**
```csharp
// Enable detailed logging
builder.Logging.AddFilter(
    "Microsoft.Extensions.Diagnostics.HealthChecks",
    LogLevel.Information);
```

**3. Check Individual Health Checks**
```csharp
// Filter to specific health check
app.MapHealthChecks("/health/ef", new HealthCheckOptions {
    Predicate = check => check.Name.Contains("EntityFramework")
});
```

---

## Startup Validation Fails

### Symptoms
- Application fails to start
- Exception: "Repository health checks failed at startup"

### Causes
1. Database not ready at startup
2. Connection string missing
3. Dependency not registered

### Solutions

**1. Use Warning Mode**
```csharp
builder.Services
    .AddHealthChecks()
    .AddKistaRepositories(options => {
        // Log warning but continue
        options.StartupValidationMode = StartupValidationMode.Warning;
    });
```

**2. Add Startup Delay**
```csharp
// In Program.cs
var app = builder.Build();

// Wait for dependencies
await Task.Delay(TimeSpan.FromSeconds(5));

app.Run();
```

**3. Check Service Registration**
```csharp
// Verify DbContext is registered
var contextType = builder.Services.FirstOrDefault(
    s => s.ServiceType == typeof(MyDbContext));
    
if (contextType == null)
    throw new Exception("DbContext not registered");
```

---

## Performance Issues

### Symptoms
- Health checks slow down application
- High CPU or memory usage during health checks

### Causes
1. Too many health checks
2. Health checks running too frequently
3. Heavy test queries

### Solutions

**1. Reduce Health Check Frequency**
```csharp
// For health check publishers
builder.Services.Configure<HealthCheckPublisherOptions>(options => {
    options.Period = TimeSpan.FromMinutes(1);  // Default is 30 seconds
});
```

**2. Disable Test Queries**
```csharp
builder.Services
    .AddRepositoryContext()
    .UseEntityFramework<MyDbContext>(ef => ef
        .WithHealthChecks(options => {
            options.TestQuery = false;  // Only test connection
        }));
```

**3. Exclude Non-Critical Repositories**
```csharp
builder.Services
    .AddHealthChecks()
    .AddKistaRepositories(options => {
        options.ExcludedRepositoryTypes.Add(typeof(LogEntity));
        options.ExcludedRepositoryTypes.Add(typeof(AuditEntity));
    });
```

**4. Cache Health Check Results**
```csharp
app.MapHealthChecks("/health", new HealthCheckOptions {
    AllowCachingResponses = true
});
```

---

## Logging and Diagnostics

### Enable Detailed Logging

```csharp
builder.Logging
    .AddFilter("Microsoft.Extensions.Diagnostics.HealthChecks", LogLevel.Debug)
    .AddFilter("Kista.HealthChecks", LogLevel.Debug)
    .AddFilter("Microsoft.EntityFrameworkCore.Database", LogLevel.Information);
```

### Log Health Check Results

```csharp
builder.Services.AddSingleton<IHealthCheckPublisher, LoggingHealthCheckPublisher>();

public class LoggingHealthCheckPublisher : IHealthCheckPublisher {
    private readonly ILogger<LoggingHealthCheckPublisher> _logger;
    
    public LoggingHealthCheckPublisher(ILogger<LoggingHealthCheckPublisher> logger) {
        _logger = logger;
    }
    
    public Task PublishAsync(HealthReport report, CancellationToken cancellationToken) {
        _logger.LogInformation(
            "Health check completed: {Status} in {Duration}ms",
            report.Status,
            report.TotalDuration.TotalMilliseconds);
        
        foreach (var entry in report.Entries) {
            _logger.LogDebug(
                "  {Name}: {Status} - {Description}",
                entry.Key,
                entry.Value.Status,
                entry.Value.Description);
        }
        
        return Task.CompletedTask;
    }
}
```

---

## Common Error Messages

### "Cannot connect to database"
- **Cause**: Database server unreachable
- **Solution**: Check connection string, network, firewall

### "Login timeout expired"
- **Cause**: Authentication taking too long
- **Solution**: Increase timeout, check credentials

### "Health check timed out"
- **Cause**: Health check exceeded timeout
- **Solution**: Increase timeout, optimize query

### "The given key was not present in the dictionary"
- **Cause**: Configuration missing
- **Solution**: Check appsettings.json or environment variables

### "No health checks registered"
- **Cause**: `AddKistaRepositories()` not called
- **Solution**: Call `AddKistaRepositories()` after configuring repositories

---

## Getting Help

If you're still experiencing issues:

1. **Check Logs**: Enable detailed logging as shown above
2. **Test Manually**: Use curl or browser to test health endpoint
3. **Review Configuration**: Double-check all configuration options
4. **Check Dependencies**: Ensure all required packages are installed
5. **Open Issue**: Report the issue on the Kista GitHub repository

### Diagnostic Information to Collect

When reporting an issue, include:
- Kista version
- .NET version
- Driver packages installed
- Health check configuration
- Full error message and stack trace
- Health check endpoint response (sanitized)
