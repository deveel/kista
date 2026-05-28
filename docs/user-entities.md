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

Mark the owner property with the `[DataOwner]` attribute so the framework can automatically discover it:

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

If no `[DataOwner]` attribute is found, the framework falls back to a property named `Owner`.

## Owner Scoping via Decorator Pattern

Kista uses a **decorator pattern** for user scoping. Any `IRepository<TEntity, TKey>` can be wrapped with `UserScopedRepositoryDecorator`, which automatically:

- **Sets the owner** on new entities when added (via `IHaveOwner<TUserKey>.SetOwner()`)
- **Filters all queries** by the current user's ID (via `IUserAccessor<TUserKey>`)
- **Throws** when no user context is available (configurable)

### Installation

```bash
dotnet add package Kista.Owners
```

### Registration

Use `.WithOwnerScoping()` on the repository builder to enable owner scoping:

```csharp
builder.Services.AddRepositoryContext()
    .UseEntityFramework<AppDbContext>(b => b
        .ConfigureDbContext(opts => opts.UseSqlServer("...")))
    .AddRepository<UserConfigurationRepository>(repo => repo
        .WithOwnerScoping(), ServiceLifetime.Scoped);
```

The decorator is registered via [Scrutor](https://github.com/khellang/Scrutor) — it wraps the underlying repository transparently. Consumers continue to resolve `IRepository<TEntity, TKey>` as usual; the decorator intercepts all operations.

### Options Configuration

```csharp
builder.Services.AddRepositoryContext()
    .UseInMemory()
    .AddRepository<UserConfigurationRepository>(repo => repo
        .WithOwnerScoping(opts => {
            opts.ThrowWhenUserNotSet = false;  // Don't throw when no user (returns empty)
        }), ServiceLifetime.Singleton);
```

| Option | Default | Description |
|--------|---------|-------------|
| `ThrowWhenUserNotSet` | `true` | Throw `InvalidOperationException` when no user context is available |
| `OwnerPropertyName` | `null` | Override automatic owner property discovery (use `[DataOwner]` instead) |

## User Identifier Resolution

The decorator relies on an `IUserAccessor<TKey>` service to resolve the current user's identity at runtime:

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
| `StaticUserIdentifierStrategy<TKey>` | `Kista.Owners` | Returns a fixed user ID (ideal for background jobs, system users, disconnected scenarios) |
| `ClaimUserIdentifierStrategy<TKey>` | `Kista.Owners` | Resolves from JWT/auth claims |
| `QueryStringUserIdentifierStrategy<TKey>` | `Kista.Owners` | Resolves from HTTP query string parameters |
| `RouteUserIdentifierStrategy<TKey>` | `Kista.Owners` | Resolves from HTTP route values |

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

**Chained on RepositoryContextBuilder (requires `Kista.Manager.AspNetCore`):**

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

        return (TKey)Convert.ChangeType(value, typeof(TKey));
    }
}

// Register custom strategy
builder.Services.AddUserAccessor<string>(b => {
    b.Add(new HeaderUserIdentifierStrategy<string>("X-User-Id"));
    b.AddStatic("anonymous");
});
```

## Complete Example: User-Scoped Entity with Owner Scoping

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
public interface IUserNoteRepository : IRepository<UserNote, Guid>
{
}

public class UserNoteRepository : InMemoryRepository<UserNote, Guid>, IUserNoteRepository
{
    public UserNoteRepository(IServiceProvider sp) : base(null, null, sp) { }
}

// Registration (ASP.NET Core)
builder.Services.AddRepositoryContext()
    .UseInMemory(b => b.WithLifecycle())
    .AddRepository<UserNoteRepository>(repo => repo
        .WithOwnerScoping(), ServiceLifetime.Singleton)
    .AddHttpUserAccessor<Guid>(b => {
        b.AddClaim("sub");
        b.AddQueryString("user_id");
    });

// Usage in controller
[ApiController]
[Route("api/notes")]
public class NotesController : ControllerBase
{
    private readonly IRepository<UserNote, Guid> _notes;

    public NotesController(IRepository<UserNote, Guid> notes)
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

        // OwnerId is set automatically by the decorator
        return await _notes.AddAsync(note);
    }
}
```

## EF Core Query Filter Configuration

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

> **Note:** The `HasOwnerFilter()` method is specific to EF Core. For InMemory, the decorator handles owner filtering automatically. For MongoDB, use the decorator pattern as shown above.

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

## Migration Guide

If you were using the old user repository pattern (pre-Kista.Owners decorator), here's what changed:

### Before (old pattern)

```csharp
// Old: separate user repository classes
public class UserNoteRepository : EntityUserRepository<UserNote, Guid, Guid>
{
    public UserNoteRepository(DbContext ctx, IUserAccessor<Guid> accessor, ILogger? logger = null)
        : base(ctx, accessor, logger) { }
}

// Old: user repositories registered directly
builder.Services.AddScoped<IUserNoteRepository, UserNoteRepository>();
```

### After (decorator pattern)

```csharp
// New: regular repository, decorated with owner scoping
public class UserNoteRepository : EntityRepository<UserNote>
{
    public UserNoteRepository(DbContext ctx) : base(ctx) { }
}

// New: decorator registered via WithOwnerScoping
builder.Services.AddRepositoryContext()
    .UseEntityFramework<AppDbContext>(...)
    .AddRepository<UserNoteRepository>(repo => repo
        .WithOwnerScoping(), ServiceLifetime.Scoped);
```

### Key Changes

| Before | After |
|--------|-------|
| `EntityUserRepository<TEntity, TKey, TOwnerKey>` base class | `.WithOwnerScoping()` on any repository |
| `InMemoryUserRepository<TEntity, TKey, TOwnerKey>` | Decorator wraps `InMemoryRepository<TEntity, TKey>` |
| `MongoUserRepository<TEntity, TKey, TOwnerKey>` | Decorator wraps `MongoRepository<TEntity>` |
| `IUserRepository<TEntity, TKey, TOwnerKey>` interface | No longer needed; use `IRepository<TEntity, TKey>` |
| User accessor passed to repository constructor | Resolved from DI by the decorator |

### Breaking Changes

- `IUserRepository<,,>`, `InMemoryUserRepository<,,>`, `EntityUserRepository<,,>`, and `MongoUserRepository<,,>` have been **removed**.
- Driver packages no longer depend on `Kista.Owners`; you must add it explicitly to use owner scoping.
- `Kista.Owners` types now live in the `Kista` namespace (no `using Kista.Owners;` needed).
