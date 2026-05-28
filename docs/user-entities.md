# User Entities
> **Renamed:** This project was renamed from **Deveel.Repository** to **Kista** on **May 26, 2025**. The name *Kista* is Old Norse for "chest" or "repository", better reflecting the project purpose as a data access framework.

The framework provides first-class support for _user-scoped_ entities — entities that belong to a specific user within the application, such as per-user preferences, configurations, or private records.

Within a tenant (or a single-tenant application), multiple users may exist, and each user may own their own set of entities.

## Defining User Entities

Any entity class can be used as a user entity. To make one user-scoped, implement the `IHaveOwner<TKey>` interface, where `TKey` is the type of the user identifier:

```csharp
public interface IHaveOwner<TKey>
{
    // The identifier of the owner
    TKey Owner { get; }

    // Assigns (or re-assigns) an owner
    void SetOwner(TKey owner);
}
```

Example entity:

```csharp
public class UserConfiguration : IHaveOwner<string>
{
    public string Id { get; set; }

    // The UserId field stores the owner identifier
    public string UserId { get; set; }

    // Explicit implementation
    string IHaveOwner<string>.Owner => UserId;

    void IHaveOwner<string>.SetOwner(string owner) => UserId = owner;

    public string ConfigurationKey { get; set; }
    public string ConfigurationValue { get; set; }
}
```

> **Note:** You can implement `IHaveOwner<TKey>` explicitly (as above) or as public members — both styles work equally well.

### Automatic Owner Detection with `[DataOwner]` Attribute

All repository drivers (EF Core, InMemory, MongoDB) support the `[DataOwner]` attribute to mark the owner property:

```csharp
public class UserConfiguration : IHaveOwner<string>
{
    public string Id { get; set; }

    [DataOwner]
    public string UserId { get; set; }

    string IHaveOwner<string>.Owner => UserId;
    void IHaveOwner<string>.SetOwner(string owner) => UserId = owner;
}
```

This allows the framework to:
- **Automatically set the owner** when adding new entities via `SetOwner()`
- **Filter queries** by the current user (EF Core query filters, InMemory/MongoDB user repositories)
- **Detect the owner property** without explicit configuration

## The User Repository Interface

The framework provides `IUserRepository<TEntity, TKey, TOwnerKey>` to represent a repository scoped to the current user:

```csharp
public interface IUserRepository<TEntity, TKey, TOwnerKey>
    : IRepository<TEntity, TKey>
    where TEntity : class, IHaveOwner<TOwnerKey>
```

You can extend this interface to add domain-specific operations:

```csharp
public interface IUserConfigurationRepository
    : IUserRepository<UserConfiguration, string, string>
{
    Task<UserConfiguration?> FindByKeyAsync(
        string userId, string configurationKey,
        CancellationToken cancellationToken = default);
}
```

## User Identifier Resolution

The user repository implementations rely on an `IUserAccessor<TKey>` service to resolve the current user's identity at runtime:

```csharp
public interface IUserAccessor<TKey>
{
    TKey? GetUserId();
}
```

### Strategy-Based Resolution

Kista uses a composable strategy pattern for user resolution. Multiple strategies can be chained together, and they are evaluated in registration order — the first strategy that successfully resolves a user ID wins (fallback chain pattern).

#### Available Strategies

| Strategy | Package | Description |
|----------|---------|-------------|
| `StaticUserIdentifierStrategy<TKey>` | `Kista` | Returns a fixed user ID (ideal for background jobs, system users, disconnected scenarios) |
| `ClaimUserIdentifierStrategy<TKey>` | `Kista.Manager.AspNetCore` | Resolves from JWT/auth claims |
| `QueryStringUserIdentifierStrategy<TKey>` | `Kista.Manager.AspNetCore` | Resolves from HTTP query string parameters |
| `RouteUserIdentifierStrategy<TKey>` | `Kista.Manager.AspNetCore` | Resolves from HTTP route values |

#### Registration Examples

**Background job with static user:**

```csharp
builder.Services.AddUserAccessor<string>(b => {
    b.AddStatic("system-worker");
});
```

**HTTP application with fallback chain:**

```csharp
builder.Services.AddHttpUserAccessor<string>(b => {
    b.AddClaim("sub");              // Try JWT "sub" claim first
    b.AddQueryString("user_id");    // Fallback to query string
    b.AddRoute("userId");           // Fallback to route value
});
```

**HTTP with static fallback (anonymous user):**

```csharp
builder.Services.AddHttpUserAccessor<string>(b => {
    b.AddClaim("sub");
    b.AddStatic("anonymous");       // Fallback for unauthenticated requests
});
```

**Custom strategy chain via repository builder:**

```csharp
builder.Services.AddRepositoryContext()
    .UseEntityFramework<MyDbContext>(...)
    .WithHttpUserAccessor<string>(b => {
        b.AddClaim("sub");
        b.AddStatic("background-user");
    });
```

#### Creating Custom Strategies

Implement `IUserIdentifierStrategy<TKey>` to create your own resolution logic:

```csharp
public class HeaderUserIdentifierStrategy<TKey> : IUserIdentifierStrategy<TKey>
{
    private readonly string headerName;

    public HeaderUserIdentifierStrategy(string headerName = "X-User-Id")
    {
        this.headerName = headerName;
    }

    public TKey? GetUserId(IServiceProvider? serviceProvider = null)
    {
        if (serviceProvider == null)
            return default;

        var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
        var value = httpContextAccessor.HttpContext?.Request.Headers[headerName].FirstOrDefault();
        
        if (value == null)
            return default;

        // Convert string to TKey (handles string, Guid, int, etc.)
        return (TKey)Convert.ChangeType(value, typeof(TKey));
    }
}

// Register custom strategy
builder.Services.AddUserAccessor<string>(b => {
    b.Add(new HeaderUserIdentifierStrategy<string>("X-User-Id"));
    b.AddStatic("anonymous");
});
```

## Automatic Timestamps

Entities can implement `IHaveTimeStamp` to have creation and update timestamps automatically set by the `EntityManager`:

```csharp
public interface IHaveTimeStamp
{
    DateTimeOffset? CreatedAtUtc { get; set; }
    DateTimeOffset? UpdatedAtUtc { get; set; }
}
```

Example entity with timestamps:

```csharp
public class Article : IHaveOwner<string>, IHaveTimeStamp
{
    public string Id { get; set; }
    
    [DataOwner]
    public string OwnerId { get; set; }

    public DateTimeOffset? CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public string Title { get; set; }
    public string Content { get; set; }

    string IHaveOwner<string>.Owner => OwnerId;
    void IHaveOwner<string>.SetOwner(string owner) => OwnerId = owner;
}
```

### How It Works

When using `EntityManager<TEntity, TKey>`:

- **On Add:** `CreatedAtUtc` is automatically set to `Time.UtcNow` when an entity implementing `IHaveTimeStamp` is added
- **On Update:** `UpdatedAtUtc` is automatically set to `Time.UtcNow` when the entity is updated

```csharp
// Adding - CreatedAtUtc is set automatically
var article = await manager.AddAsync(new Article {
    Title = "My Article",
    Content = "Content here"
});

Console.WriteLine(article.CreatedAtUtc); // 2025-01-15T10:30:00Z

// Updating - UpdatedAtUtc is set automatically
article.Title = "Updated Title";
article = await manager.UpdateAsync(article);

Console.WriteLine(article.UpdatedAtUtc); // 2025-01-15T11:45:00Z
```

> **Note:** Timestamps are only set if the `Time` service (ITimeProvider) is registered in the DI container. This is typically done automatically in ASP.NET Core applications.

## Entity Framework Core User Repository

For EF Core, the package `Kista.EntityFramework` provides `EntityUserRepository<TEntity, TKey, TOwnerKey>` as a base class for user-scoped repositories:

```csharp
public class UserConfigurationRepository
    : EntityUserRepository<UserConfiguration, string, string>,
      IUserConfigurationRepository
{
    public UserConfigurationRepository(
        MyDbContext context,
        IUserAccessor<string> userAccessor,
        ILogger<UserConfigurationRepository>? logger = null)
        : base(context, userAccessor, logger) { }

    public async Task<UserConfiguration?> FindByKeyAsync(
        string userId, string configurationKey,
        CancellationToken cancellationToken = default)
    {
        return await AsQueryable()
            .FirstOrDefaultAsync(
                x => x.UserId == userId && x.ConfigurationKey == configurationKey,
                cancellationToken);
    }
}
```

The base class automatically:
- Filters all queries by the owner key from `IUserAccessor<TKey>`
- Sets the owner on new entities via `SetOwner()` when adding

## InMemory User Repository

The `Kista.InMemory` package provides `InMemoryUserRepository<TEntity, TKey, TUserKey>` for testing and lightweight scenarios:

```csharp
public class InMemoryUserConfigurationRepository
    : InMemoryUserRepository<UserConfiguration, string, string>,
      IUserConfigurationRepository
{
    public InMemoryUserConfigurationRepository(
        IUserAccessor<string> userAccessor,
        IEnumerable<UserConfiguration>? initialData = null)
        : base(userAccessor, initialData) { }
}
```

## MongoDB User Repository

The `Kista.MongoFramework` package provides `MongoUserRepository<TEntity, TKey, TUserKey>` for MongoDB-backed user repositories:

```csharp
public class MongoUserConfigurationRepository
    : MongoUserRepository<UserConfiguration, string, string>,
      IUserConfigurationRepository
{
    public MongoUserConfigurationRepository(
        IMongoDbContext context,
        IUserAccessor<string> userAccessor,
        ILogger<MongoUserConfigurationRepository>? logger = null)
        : base(context, userAccessor, logger) { }
}
```

All user repository implementations across drivers:
- **Automatically set the owner** when adding entities (via `IHaveOwner<TUserKey>.SetOwner()`)
- **Filter by current user** on `FindAsync()` operations
- **Support the `[DataOwner]` attribute** for automatic owner property detection

### EF Core Query Filter Configuration

You can also configure owner-based query filters directly in `OnModelCreating` (EF Core only):

```csharp
public class MyDbContext : DbContext
{
    private readonly IUserAccessor<string> _userAccessor;

    public MyDbContext(DbContextOptions options, IUserAccessor<string> userAccessor)
        : base(options)
    {
        _userAccessor = userAccessor;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Auto-detects [DataOwner] attribute
        modelBuilder.Entity<UserConfiguration>()
            .HasOwnerFilter(_userAccessor);

        // Or specify the property name explicitly
        modelBuilder.Entity<Order>()
            .HasOwnerFilter("OwnerId", _userAccessor);
    }
}
```

This ensures that all queries automatically filter by the current user, providing data isolation at the database level.

> **Note:** The `HasOwnerFilter()` method is specific to EF Core. For InMemory and MongoDB, use the respective `InMemoryUserRepository` and `MongoUserRepository` classes which handle owner filtering automatically.

## Complete Example: User-Scoped Entity with Timestamps

```csharp
// Entity definition
public class UserNote : IHaveOwner<Guid>, IHaveTimeStamp
{
    public Guid Id { get; set; }
    
    [DataOwner]
    public Guid OwnerId { get; set; }

    public DateTimeOffset? CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public string Title { get; set; }
    public string Content { get; set; }

    Guid IHaveOwner<Guid>.Owner => OwnerId;
    void IHaveOwner<Guid>.SetOwner(Guid owner) => OwnerId = owner;
}

// Repository
public interface IUserNoteRepository : IUserRepository<UserNote, Guid, Guid>
{
}

public class UserNoteRepository 
    : EntityUserRepository<UserNote, Guid, Guid>, IUserNoteRepository
{
    public UserNoteRepository(
        AppDbContext context,
        IUserAccessor<Guid> userAccessor,
        ILogger<UserNoteRepository>? logger = null)
        : base(context, userAccessor, logger) { }
}

// Registration (ASP.NET Core)
builder.Services.AddRepositoryContext()
    .UseEntityFramework<AppDbContext>(b => b
        .ConfigureDbContext(opts => opts.UseSqlServer("...")))
    .AddRepository<UserNoteRepository>()
    .WithHttpUserAccessor<Guid>(b => {
        b.AddClaim("sub");
        b.AddQueryString("user_id");
    });

// Usage in controller
[ApiController]
[Route("api/notes")]
public class NotesController : ControllerBase
{
    private readonly IUserNoteRepository _notes;

    public NotesController(IUserNoteRepository notes)
    {
        _notes = notes;
    }

    [HttpGet]
    public async Task<IEnumerable<UserNote>> GetNotes()
    {
        // Automatically filtered by current user
        return await _notes.FindAllAsync();
    }

    [HttpPost]
    public async Task<UserNote> CreateNote(CreateNoteRequest request)
    {
        var note = new UserNote {
            Title = request.Title,
            Content = request.Content
        };

        // OwnerId is set automatically by EntityUserRepository
        // CreatedAtUtc is set automatically by EntityManager
        return await _notes.AddAsync(note);
    }
}
```
