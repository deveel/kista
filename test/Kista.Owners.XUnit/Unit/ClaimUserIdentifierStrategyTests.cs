using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
[Trait("Feature", "UserIdentifier")]
public class ClaimUserIdentifierStrategyTests
{
    const string TestUserId = "user123";
    [Fact]
    public void Should_ReturnDefault_When_ServiceProviderIsNull()
    {
        var strategy = new ClaimUserIdentifierStrategy<string>();
        var result = strategy.GetUserId(null);
        Assert.Null(result);
    }

    [Fact]
    public void Should_ReturnDefault_When_HttpContextIsNull()
    {
        var services = new ServiceCollection();
        services.AddHttpContextAccessor();
        var provider = services.BuildServiceProvider();
        var strategy = new ClaimUserIdentifierStrategy<string>();

        var result = strategy.GetUserId(provider);

        Assert.Null(result);
    }

    [Fact]
    public void Should_ReturnDefault_When_ClaimNotFound()
    {
        var httpContext = Substitute.For<HttpContext>();
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        httpContext.User.Returns(user);
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        var services = new ServiceCollection();
        services.AddSingleton(accessor);
        var provider = services.BuildServiceProvider();
        var strategy = new ClaimUserIdentifierStrategy<string>("custom_claim");

        var result = strategy.GetUserId(provider);

        Assert.Null(result);
    }

    [Fact]
    public void Should_ReturnUserId_When_ClaimFound()
    {
        var httpContext = Substitute.For<HttpContext>();
        var identity = new ClaimsIdentity(new[] { new Claim("sub", TestUserId) });
        var user = new ClaimsPrincipal(identity);
        httpContext.User.Returns(user);
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        var services = new ServiceCollection();
        services.AddSingleton(accessor);
        var provider = services.BuildServiceProvider();
        var strategy = new ClaimUserIdentifierStrategy<string>();

        var result = strategy.GetUserId(provider);

        Assert.Equal(TestUserId, result);
    }

    [Fact]
    public void Should_ReturnDefault_When_ServiceProviderHasNoHttpContextAccessor()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var strategy = new ClaimUserIdentifierStrategy<string>();

        var result = strategy.GetUserId(provider);

        Assert.Null(result);
    }

    [Fact]
    public void Should_ConvertToString_When_TKeyIsString()
    {
        var httpContext = Substitute.For<HttpContext>();
        var identity = new ClaimsIdentity(new[] { new Claim("sub", TestUserId) });
        httpContext.User.Returns(new ClaimsPrincipal(identity));
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        var services = new ServiceCollection();
        services.AddSingleton(accessor);
        var provider = services.BuildServiceProvider();
        var strategy = new ClaimUserIdentifierStrategy<string>();

        var result = strategy.GetUserId(provider);

        Assert.Equal(TestUserId, result);
    }

    [Fact]
    public void Should_ConvertToInt_When_TKeyIsInt()
    {
        var httpContext = Substitute.For<HttpContext>();
        var identity = new ClaimsIdentity(new[] { new Claim("sub", "42") });
        httpContext.User.Returns(new ClaimsPrincipal(identity));
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        var services = new ServiceCollection();
        services.AddSingleton(accessor);
        var provider = services.BuildServiceProvider();
        var strategy = new ClaimUserIdentifierStrategy<int>();

        var result = strategy.GetUserId(provider);

        Assert.Equal(42, result);
    }

    [Fact]
    public void Should_ReturnDefault_When_ConvertFailsForInt()
    {
        var httpContext = Substitute.For<HttpContext>();
        var identity = new ClaimsIdentity(new[] { new Claim("sub", "not-a-number") });
        httpContext.User.Returns(new ClaimsPrincipal(identity));
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        var services = new ServiceCollection();
        services.AddSingleton(accessor);
        var provider = services.BuildServiceProvider();
        var strategy = new ClaimUserIdentifierStrategy<int>();

        var result = strategy.GetUserId(provider);

        Assert.Equal(default, result);
    }

    [Fact]
    public void Should_Throw_When_CreatedWithEmptyClaimType()
    {
        Assert.Throws<ArgumentException>(() => new ClaimUserIdentifierStrategy<string>(""));
    }

    [Fact]
    public void Should_Throw_When_CreatedWithNullClaimType()
    {
        Assert.Throws<ArgumentNullException>(() => new ClaimUserIdentifierStrategy<string>(null!));
    }
}
