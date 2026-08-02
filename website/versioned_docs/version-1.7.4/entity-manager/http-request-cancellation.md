# HTTP Request Cancellation
The `Kista.Manager.AspNetCore` package extends the Entity Manager model with an `IOperationCancellationSource` implementation that forwards the ASP.NET Core **request-aborted** cancellation token to every manager operation.

When the HTTP client disconnects or the network layer aborts the request, the `HttpContext.RequestAborted` token transitions to a cancelled state. With this integration in place, all in-flight repository operations are automatically cancelled — **without any extra code in your manager or your controllers**.

## How It Works

```
HTTP client disconnect
        │
        ▼
HttpContext.RequestAborted  ──►  HttpRequestCancellationSource.Token
                                            │
                                            ▼
                              EntityManager<TEntity> (uses IOperationCancellationSource)
                                            │
                                            ▼
                              IRepository<TEntity> operation  ──►  cancelled
```

1. `HttpRequestCancellationSource` implements `IOperationCancellationSource`.
2. Its `Token` property delegates to `IHttpContextAccessor.HttpContext?.RequestAborted`, returning `CancellationToken.None` when there is no active HTTP context (e.g. during background processing or unit tests).
3. When an `EntityManager<TEntity>` method is called **without** an explicit `CancellationToken`, the manager resolves the registered `IOperationCancellationSource` from the DI container and uses its token automatically.

## Installation

Add the NuGet package to your ASP.NET Core project:

```bash
dotnet add package Kista.Manager.AspNetCore
```

## Registration

Call `AddHttpRequestTokenSource()` in your `Program.cs` (or `Startup.ConfigureServices`):

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Register the HTTP request cancellation source for the Entity Manager.
// This also registers IHttpContextAccessor if it has not been registered yet.
builder.Services.AddHttpRequestTokenSource();

// Register the manager as usual
builder.Services.AddRepositoryContext()
    .AddRepository<MyEntityRepository>(repo => repo
        .WithManagement())
    .UseInMemory();
```

> **Note** `AddHttpRequestTokenSource` calls `services.AddHttpContextAccessor()` internally, so you do not need to call it separately.

## Usage in a Controller

Because the token is resolved automatically from the DI container, you can call manager methods without passing a `CancellationToken` explicitly:

```csharp
[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly EntityManager<Order> _manager;

    public OrdersController(EntityManager<Order> manager)
    {
        _manager = manager;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAsync(string id)
    {
        // No CancellationToken argument — the manager picks up
        // HttpContext.RequestAborted automatically.
        var order = await _manager.FindAsync(id);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] Order order)
    {
        var result = await _manager.AddAsync(order);
        return result.IsSuccess ? CreatedAtAction(nameof(GetAsync), new { id = order.Id }, order)
                                : BadRequest(result.Error);
    }
}
```

You can still pass an explicit token when needed — the explicit token always takes precedence over the one supplied by the `IOperationCancellationSource`:

```csharp
// Explicit token overrides the HTTP source
var order = await _manager.FindAsync(id, HttpContext.RequestAborted);
```

## Custom Cancellation Sources

`AddHttpRequestTokenSource` is a convenience wrapper around the generic `AddOperationTokenSource<TSource>` method available in the base `Kista.Manager` package:

```csharp
// Equivalent manual registration
builder.Services.AddHttpContextAccessor();
builder.Services.AddOperationTokenSource<HttpRequestCancellationSource>(ServiceLifetime.Singleton);
```

You can follow the same pattern to register your own `IOperationCancellationSource` implementation for non-ASP.NET Core hosts:

```csharp
public class MyCustomCancellationSource : IOperationCancellationSource
{
    public CancellationToken Token { get; } = /* ... */;
}

builder.Services.AddOperationTokenSource<MyCustomCancellationSource>();
```

## API Reference

### `HttpRequestCancellationSource`

| Member | Description |
| ------ | ----------- |
| `HttpRequestCancellationSource(IHttpContextAccessor)` | Constructor. Receives the `IHttpContextAccessor` from the DI container. |
| `CancellationToken Token` | Returns `HttpContext.RequestAborted` for the current request, or `CancellationToken.None` when no HTTP context is active. |

### `ServiceCollectionExtensions.AddHttpRequestTokenSource`

```csharp
public static IServiceCollection AddHttpRequestTokenSource(
    this IServiceCollection services)
```

Registers `HttpRequestCancellationSource` as a **singleton** `IOperationCancellationSource` and ensures `IHttpContextAccessor` is also registered.

| Parameter | Description |
| --------- | ----------- |
| `services` | The `IServiceCollection` to add the services to. |

Returns the same `IServiceCollection` instance to allow method chaining.

