# Sample Application

The Kista repository ships several reference ASP.NET Core applications under `samples/`:

| Sample | Path | Demonstrates |
|--------|------|-------------|
| `Kista.SampleApp` | `samples/Kista.SampleApp/` | Lifecycle management, custom repositories, minimal API CRUD |
| `Kista.SampleApp.OperationPipeline` | `samples/Kista.SampleApp.OperationPipeline/` | The extensible operation pipeline — audit logging and short-circuit interceptors |
| `Kista.SampleApp.Owners` | `samples/Kista.SampleApp.Owners/` | User-scoped entities with owner filtering |
| `Kista.SampleApp.SoftDelete` | `samples/Kista.SampleApp.SoftDelete/` | Soft-delete with restore and hard-delete |

The first sample (`Kista.SampleApp`) is described in detail below; the **Operation Pipeline** sample is covered in the [Operation Pipeline Sample](#operation-pipeline-sample) section at the end of this page.

## `Kista.SampleApp` — Lifecycle & CRUD

The `Kista.SampleApp` is a reference ASP.NET Core application that demonstrates how to use the Kista framework with lifecycle management, custom repositories, and minimal API endpoints.

## Project Structure

```
samples/Kista.SampleApp/
└── src/Kista.SampleApp/
    ├── Models/
    │   └── Contact.cs                 # Entity with Guid key
    ├── DTOs/
    │   └── ContactDTOs.cs             # Request/Response DTOs
    ├── Repositories/
    │   └── ContactRepository.cs       # Custom repository interface
    ├── Lifecycle/
    │   ├── ContactLifecycleHandler.cs # Lifecycle handler for Contact
    │   └── SampleLifecycleProfile.cs  # Environment-aware seed profile
    ├── SeedData/
    │   └── DefaultContactSeedData.cs  # Seed data provider
    ├── Endpoints/
    │   ├── ContactEndpoints.cs        # CRUD endpoints
    │   └── LifecycleEndpoints.cs      # Lifecycle management endpoints
    ├── Extensions/
    │   └── RepositoryServiceCollectionExtensions.cs  # DI registration
    ├── Program.cs                     # Application entry point
    └── appsettings.json               # Configuration
```

## Running the Sample

```bash
cd samples/Kista.SampleApp/src/Kista.SampleApp
dotnet run
```

The application starts with:
- **In-Memory** driver by default
- **OpenAPI** documentation at `/openapi/v1.json`
- **Swagger UI** available in development mode

## Endpoints

### Lifecycle Management

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/api/lifecycle/create` | Create the Contact repository |
| `POST` | `/api/lifecycle/drop` | Drop the Contact repository |
| `POST` | `/api/lifecycle/seed` | Seed the Contact repository with default data |
| `POST` | `/api/lifecycle/initialize` | Drop, create, and seed in one call |

### Contact CRUD

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/contacts` | List all contacts |
| `GET` | `/api/contacts/{id}` | Get a contact by ID |
| `POST` | `/api/contacts` | Create a new contact |
| `PUT` | `/api/contacts/{id}` | Update an existing contact |
| `DELETE` | `/api/contacts/{id}` | Delete a contact |

## Key Concepts Demonstrated

### 1. Custom Repository Interface

```csharp
public interface IContactRepository : IRepository<Contact, Guid> {
    // Custom query methods can be added here
}
```

### 2. Lifecycle Handler

```csharp
public class ContactLifecycleHandler : IRepositoryLifecycleHandler<Contact> {
    public ValueTask<bool> ExistsAsync(CancellationToken ct) { ... }
    public ValueTask CreateAsync(CancellationToken ct) { ... }
    public ValueTask DropAsync(CancellationToken ct) { ... }
    public ValueTask SeedAsync(object? seedData, CancellationToken ct) { ... }
}
```

### 3. Seed Data Provider

```csharp
public class DefaultContactSeedData : IRepositorySeedDataProvider<Contact> {
    public IEnumerable<Contact> GetSeedData() {
        yield return new Contact { FirstName = "John", LastName = "Doe", Email = "john@example.com" };
        yield return new Contact { FirstName = "Jane", LastName = "Doe", Email = "jane@example.com" };
    }
}
```

### 4. Lifecycle Profile

```csharp
public class SampleLifecycleProfile : IRepositoryLifecycleProfile {
    public SeedStrategy GetSeedStrategy(string? environmentName) {
        return environmentName switch {
            "Development" => SeedStrategy.Always,
            "Staging" => SeedStrategy.IfMissing,
            _ => SeedStrategy.Never
        };
    }
}
```

### 5. Fluent Registration

```csharp
public static void AddContactRepository(this IServiceCollection services, IConfiguration config) {
    services.AddRepositoryContext()
        .UseInMemory(b => b.WithLifecycle())
        .ConfigureLifecycle(options => {
            options.SeedStrategy = SeedStrategy.ByEnvironment;
        })
        .WithLifecycleHandler<Contact, ContactLifecycleHandler>()
        .WithLifecycleProfile<SampleLifecycleProfile>();
}
```

### 6. Consuming the Lifecycle Service

```csharp
app.MapPost("/api/lifecycle/initialize", async (
    IRepositoryLifecycleService service,
    CancellationToken ct) => {
    await service.DropRepositoryAsync<Contact, Guid>(ct);
    await service.CreateRepositoryAsync<Contact, Guid>(ct);
    await service.SeedRepositoryAsync<Contact, Guid>(null, ct);
    return Results.Ok(new { Message = "Initialized" });
});
```

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."
  },
  "Repository": {
    "Driver": "InMemory",
    "Lifecycle": {
      "DeleteIfExists": true,
      "SeedStrategy": "ByEnvironment"
    }
  }
}
```

### appsettings.Development.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Deveel": "Debug"
    }
  }
}
```

## Switching Drivers

The sample is designed to be easily switched between drivers. Modify `RepositoryServiceCollectionExtensions.cs`:

```csharp
// In-Memory (default)
.UseInMemory(b => b.WithLifecycle())

// Entity Framework Core
.UseEntityFramework<AppDbContext>(b => b
    .ConfigureDbContext(opts => opts.UseSqlServer(config.GetConnectionString("DefaultConnection"))))

// MongoDB
.UseMongoDB<MongoContext>(b => b
    .WithConnectionString(config.GetConnectionString("MongoDb")))
```

## Operation Pipeline Sample

The `Kista.SampleApp.OperationPipeline` reference app demonstrates the [Operation Pipeline](entity-manager/operation-pipeline.md) on `EntityManager` — every write to `TaskItem` flows through two interceptors registered via `WithInterceptor<T>()`:

1. **`BusinessHoursInterceptor`** — short-circuits writes attempted outside 09:00–18:00 UTC by returning a failed `IOperationResult` from `PreWriteAsync`. The repository write is skipped, downstream interceptors are skipped, and the caller receives the error result — no exception thrown.
2. **`AuditInterceptor`** — logs every successful write in `PostWriteAsync`, demonstrating the after-write slot (the gap the pipeline closes that the legacy `On*Async` hooks could not fill).

### Project Structure

```
samples/Kista.SampleApp.OperationPipeline/
└── src/Kista.SampleApp.OperationPipeline/
    ├── Models/
    │   └── TaskItem.cs                        # Entity with Guid key, IHaveTimeStamp
    ├── DTOs/
    │   └── TaskDTOs.cs                        # Request/Response DTOs
    ├── Data/
    │   └── SampleDbContext.cs                 # EF Core SQLite DbContext
    ├── Repositories/
    │   └── TaskRepository.cs                  # Custom repository interface
    ├── Interceptors/
    │   ├── AuditInterceptor.cs                # PostWriteAsync audit log
    │   └── BusinessHoursInterceptor.cs        # PreWriteAsync short-circuit
    ├── Endpoints/
    │   └── TaskEndpoints.cs                   # CRUD endpoints
    ├── SeedData/
    │   └── DefaultSeedData.cs                 # Seed data provider
    ├── Extensions/
    │   └── RepositoryServiceCollectionExtensions.cs   # DI registration
    ├── Program.cs                             # Application entry point
    └── appsettings.json                       # Configuration
```

### Running the Sample

```bash
cd samples/Kista.SampleApp.OperationPipeline/src/Kista.SampleApp.OperationPipeline
dotnet run
```

The application starts with:

- **SQLite** driver (`tasks.db` created next to the project)
- **OpenAPI** documentation at `/openapi/v1.json` (Development only)
- Seed data loaded on startup

### Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/tasks` | List all tasks |
| `GET` | `/api/tasks/{id}` | Get a task by ID |
| `POST` | `/api/tasks` | Create a new task (rejected outside business hours) |
| `PUT` | `/api/tasks/{id}` | Update a task (rejected outside business hours) |
| `DELETE` | `/api/tasks/{id}` | Delete a task (rejected outside business hours) |

### Registration

```csharp
services.AddRepositoryContext()
    .UseEntityFramework<SampleDbContext>(builder => builder
        .ConfigureDbContext(options => options.UseSqlite("Data Source=tasks.db"))
        .WithLifecycle())
    .AddRepository<TaskRepository>(repo => repo
        .WithManagement(mgmt => mgmt
            .WithInterceptor<BusinessHoursInterceptor<TaskItem, Guid>>()
            .WithInterceptor<AuditInterceptor<TaskItem, Guid>>()));
```

Interceptors run in **registration order**: `BusinessHoursInterceptor` runs first (can short-circuit), `AuditInterceptor` runs second (only if the write proceeds and succeeds).

### What it demonstrates

- **Short-circuit** — `BusinessHoursInterceptor.PreWriteAsync` returns `OperationResult.Fail(new OperationError("OUTSIDE_BUSINESS_HOURS", ...))`; the repository write is skipped and the caller receives a structured error result instead of an exception.
- **After-write slot** — `AuditInterceptor.PostWriteAsync` is invoked only on successful writes, with access to `context.Kind`, `context.Key`, `context.Actor`, and `context.Timestamp`. This is the slot the legacy `On*Async` hooks lacked.
- **Composability** — the two concerns stack cleanly in registration order, with no decorator or subclass ceremony.

→ See [Operation Pipeline](entity-manager/operation-pipeline.md) for the full interceptor contract, the operation context, and more examples.
