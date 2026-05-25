# Sample Application
> **Renamed:** This project was renamed from **Deveel.Repository** to **Kista** on **May 26, 2025**. The name *Kista* is Old Norse for "chest" or "repository", better reflecting the project purpose as a data access framework.

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
