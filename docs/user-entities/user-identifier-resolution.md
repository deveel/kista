# User Identifier Resolution

The `UserScopedRepositoryDecorator` relies on an `IUserAccessor<TKey>` service to resolve the current user's identity at runtime:

```csharp
public interface IUserAccessor<TKey>
{
    TKey? GetUserId();
}
```

## Strategy-Based Resolution

Kista uses a composable strategy pattern for user resolution. Multiple strategies can be chained together, and they are evaluated in registration order — the first strategy that successfully resolves a user ID wins (fallback chain pattern).

### Available Strategies

| Strategy | Package | Description |
|----------|---------|-------------|
| `StaticUserIdentifierStrategy<TKey>` | `Kista.Owners` | Returns a fixed user ID (ideal for background jobs, system users, disconnected scenarios) |
| `ClaimUserIdentifierStrategy<TKey>` | `Kista.Owners` | Resolves from JWT/auth claims |
| `QueryStringUserIdentifierStrategy<TKey>` | `Kista.Owners` | Resolves from HTTP query string parameters |
| `RouteUserIdentifierStrategy<TKey>` | `Kista.Owners` | Resolves from HTTP route values |

### Registration Examples

**Background job with static user:**

```csharp
builder.Services.AddUserAccessor<string>(b => {
    b.AddStatic("system-worker");
});
```

**HTTP application (secure default — claim only):**

```csharp
builder.Services.AddHttpUserAccessor<string>();
// Resolves exclusively from the "sub" claim of the authenticated ClaimsPrincipal.
// Unauthenticated requests yield no user identity (fail-closed).
```

**HTTP application with explicit fallback chain (opt-in, advanced):**

> **Security warning:** The query-string and route fallbacks are <b>client-controlled</b>.
> Enabling them allows any caller to impersonate another user by appending
> <c>?user_id=&lt;victim&gt;</c> or crafting a route. Only enable these strategies behind a
> trusted gateway or an authorization layer that validates the resolved identity.

```csharp
builder.Services.AddHttpUserAccessor<string>(b => {
    b.AddClaim("sub");              // Try JWT "sub" claim first
    b.AddQueryString("user_id");    // Fallback to query string (UNSAFE without gateway)
    b.AddRoute("userId");           // Fallback to route value   (UNSAFE without gateway)
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

### Creating Custom Strategies

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
