using System.Security.Claims;

namespace Kista.SampleApp.Owners.Middleware;

public class FakeUserMiddleware
{
    private readonly RequestDelegate _next;
    private const string UserIdHeader = "X-User-Id";

    public FakeUserMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(UserIdHeader, out var userId))
        {
            var identity = new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim("sub", userId.ToString())
            ], "FakeAuth");

            context.User = new ClaimsPrincipal(identity);
        }

        await _next(context);
    }
}
