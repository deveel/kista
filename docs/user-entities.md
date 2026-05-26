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

## Accessing the Current User at Runtime

The user repository implementations rely on an `IUserAccessor<TKey>` service to resolve the current user's identity at runtime:

```csharp
public interface IUserAccessor<TKey>
{
    TKey? GetUserId();
}
```

Register your own implementation of `IUserAccessor<TKey>` in the DI container. For example, in an ASP.NET Core application you might read the user identity from `IHttpContextAccessor`:

```csharp
public class HttpUserAccessor : IUserAccessor<string>
{
    private readonly IHttpContextAccessor _httpContext;

    public HttpUserAccessor(IHttpContextAccessor httpContext)
        => _httpContext = httpContext;

    public string? GetUserId()
        => _httpContext.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
}

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IUserAccessor<string>, HttpUserAccessor>();
```

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

The base class automatically filters all queries by the owner key returned by `IUserAccessor<TKey>`.
