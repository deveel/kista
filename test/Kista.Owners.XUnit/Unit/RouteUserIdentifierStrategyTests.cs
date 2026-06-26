using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
[Trait("Feature", "UserIdentifier")]
public class RouteUserIdentifierStrategyTests
{
    [Fact]
    public void Should_ReturnDefault_When_ServiceProviderIsNull()
    {
        var strategy = new RouteUserIdentifierStrategy<string>();
        var result = strategy.GetUserId(null);
        Assert.Null(result);
    }

    [Fact]
    public void Should_ReturnDefault_When_HttpContextIsNull()
    {
        var services = new ServiceCollection();
        services.AddHttpContextAccessor();
        var provider = services.BuildServiceProvider();
        var strategy = new RouteUserIdentifierStrategy<string>();

        var result = strategy.GetUserId(provider);

        Assert.Null(result);
    }

    [Fact]
    public void Should_ReturnDefault_When_RouteKeyNotFound()
    {
        var httpContext = Substitute.For<HttpContext>();
        var request = Substitute.For<HttpRequest>();
        request.RouteValues.Returns(new Microsoft.AspNetCore.Routing.RouteValueDictionary());
        httpContext.Request.Returns(request);
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        var services = new ServiceCollection();
        services.AddSingleton(accessor);
        var provider = services.BuildServiceProvider();
        var strategy = new RouteUserIdentifierStrategy<string>("custom_key");

        var result = strategy.GetUserId(provider);

        Assert.Null(result);
    }

    [Fact]
    public void Should_ReturnUserId_When_RouteKeyFound()
    {
        var httpContext = Substitute.For<HttpContext>();
        var request = Substitute.For<HttpRequest>();
        request.RouteValues.Returns(new Microsoft.AspNetCore.Routing.RouteValueDictionary
        {
            { "userId", "user789" }
        });
        httpContext.Request.Returns(request);
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        var services = new ServiceCollection();
        services.AddSingleton(accessor);
        var provider = services.BuildServiceProvider();
        var strategy = new RouteUserIdentifierStrategy<string>();

        var result = strategy.GetUserId(provider);

        Assert.Equal("user789", result);
    }

    [Fact]
    public void Should_ReturnDefault_When_ServiceProviderHasNoHttpContextAccessor()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var strategy = new RouteUserIdentifierStrategy<string>();

        var result = strategy.GetUserId(provider);

        Assert.Null(result);
    }

    [Fact]
    public void Should_ConvertToInt_When_TKeyIsInt()
    {
        var httpContext = Substitute.For<HttpContext>();
        var request = Substitute.For<HttpRequest>();
        request.RouteValues.Returns(new Microsoft.AspNetCore.Routing.RouteValueDictionary
        {
            { "userId", "42" }
        });
        httpContext.Request.Returns(request);
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        var services = new ServiceCollection();
        services.AddSingleton(accessor);
        var provider = services.BuildServiceProvider();
        var strategy = new RouteUserIdentifierStrategy<int>();

        var result = strategy.GetUserId(provider);

        Assert.Equal(42, result);
    }

    [Fact]
    public void Should_ReturnDefault_When_ConvertFails()
    {
        var httpContext = Substitute.For<HttpContext>();
        var request = Substitute.For<HttpRequest>();
        request.RouteValues.Returns(new Microsoft.AspNetCore.Routing.RouteValueDictionary
        {
            { "userId", "invalid" }
        });
        httpContext.Request.Returns(request);
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        var services = new ServiceCollection();
        services.AddSingleton(accessor);
        var provider = services.BuildServiceProvider();
        var strategy = new RouteUserIdentifierStrategy<int>();

        var result = strategy.GetUserId(provider);

        Assert.Equal(default, result);
    }

    [Fact]
    public void Should_Throw_When_CreatedWithEmptyKey()
    {
        Assert.Throws<ArgumentException>(() => new RouteUserIdentifierStrategy<string>(""));
    }

    [Fact]
    public void Should_Throw_When_CreatedWithNullKey()
    {
        Assert.Throws<ArgumentNullException>(() => new RouteUserIdentifierStrategy<string>(null!));
    }
}
