# User Entities

The framework provides first-class support for _user-scoped_ entities — entities that belong to a specific user within the application, such as per-user preferences, configurations, or private records.

Within a tenant (or a single-tenant application), multiple users may exist, and each user may own their own set of entities.

## Defining User Entities

Any entity class can be used as a user entity. To make one user-scoped, implement the `IHaveOwner<TKey>` interface, where `TKey` is the type of the user identifier:

```csharp
public interface IHaveOwner<TKey>
{
    TKey Owner { get; }
    void SetOwner(TKey owner);
}
```

Example entity:

```csharp
public class UserConfiguration : IHaveOwner<string>
{
    public string Id { get; set; }
    public string UserId { get; set; }

    string IHaveOwner<string>.Owner => UserId;
    void IHaveOwner<string>.SetOwner(string owner) => UserId = owner;

    public string ConfigurationKey { get; set; }
    public string ConfigurationValue { get; set; }
}
```

> You can implement `IHaveOwner<TKey>` explicitly or as public members — both styles work equally well.

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
            opts.ThrowWhenUserNotSet = false;
        }), ServiceLifetime.Singleton);
```

| Option | Default | Description |
|--------|---------|-------------|
| `ThrowWhenUserNotSet` | `true` | Throw `InvalidOperationException` when no user context is available |
| `OwnerPropertyName` | `null` | Override automatic owner property discovery (use `[DataOwner]` instead) |

## User Identifier Resolution

The decorator relies on an `IUserAccessor<TKey>` service to resolve the current user's identity at runtime. See [User Identifier Resolution](user-identifier-resolution.md) for the full strategy-based resolution system.

## Complete Example

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
    public UserNoteRepository(IServiceProvider? services = null)
        : base(services) { }
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
    public async Task<ActionResult<IEnumerable<UserNote>>> GetNotes()
    {
        var results = await _notes.FindAllAsync();
        return Ok(results);
    }

    [HttpPost]
    public async Task<ActionResult<UserNote>> CreateNote(CreateNoteRequest request)
    {
        var note = new UserNote {
            Title = request.Title,
            Content = request.Content
        };

        await _notes.AddAsync(note);
        return CreatedAtAction(nameof(GetNotes), new { id = note.Id }, note);
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
        modelBuilder.Entity<UserConfiguration>()
            .HasOwnerFilter(_userAccessor);

        modelBuilder.Entity<Order>()
            .HasOwnerFilter("OwnerId", _userAccessor);
    }
}
```

This ensures that all queries automatically filter by the current user, providing data isolation at the database level.

> The `HasOwnerFilter()` method is specific to EF Core. For InMemory, the decorator handles owner filtering automatically. For MongoDB, use the decorator pattern as shown above.

## Automatic Timestamps

Entities can implement `IHaveTimeStamp` to have creation and update timestamps automatically set by the `EntityManager`. See [Automatic Timestamps](automatic-timestamps.md) for details.

<details>
<summary>Migration Guide (pre-Kista.Owners decorator)</summary>

If you were using the old user repository pattern, here's what changed:

### Before (old pattern)

```csharp
public class UserNoteRepository : EntityUserRepository<UserNote, Guid, Guid>
{
    public UserNoteRepository(DbContext ctx, IUserAccessor<Guid> accessor, ILogger? logger = null)
        : base(ctx, accessor, logger) { }
}

builder.Services.AddScoped<IUserNoteRepository, UserNoteRepository>();
```

### After (decorator pattern)

```csharp
public class UserNoteRepository : EntityRepository<UserNote>
{
    public UserNoteRepository(DbContext ctx) : base(ctx) { }
}

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

</details>
