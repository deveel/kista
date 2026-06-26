using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
[Trait("Feature", "UserIdentifier")]
public class QueryStringUserIdentifierStrategyTests
{
    [Fact]
    public void Should_ReturnDefault_When_ServiceProviderIsNull()
    {
        var strategy = new QueryStringUserIdentifierStrategy<string>();
        var result = strategy.GetUserId(null);
        Assert.Null(result);
    }

    [Fact]
    public void Should_ReturnDefault_When_HttpContextIsNull()
    {
        var services = new ServiceCollection();
        services.AddHttpContextAccessor();
        var provider = services.BuildServiceProvider();
        var strategy = new QueryStringUserIdentifierStrategy<string>();

        var result = strategy.GetUserId(provider);

        Assert.Null(result);
    }

    [Fact]
    public void Should_ReturnDefault_When_ParameterNotFound()
    {
        var httpContext = Substitute.For<HttpContext>();
        var request = Substitute.For<HttpRequest>();
        request.Query.Returns(new QueryCollection());
        httpContext.Request.Returns(request);
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        var services = new ServiceCollection();
        services.AddSingleton(accessor);
        var provider = services.BuildServiceProvider();
        var strategy = new QueryStringUserIdentifierStrategy<string>("custom_param");

        var result = strategy.GetUserId(provider);

        Assert.Null(result);
    }

    [Fact]
    public void Should_ReturnUserId_When_ParameterFound()
    {
        var httpContext = Substitute.For<HttpContext>();
        var request = Substitute.For<HttpRequest>();
        request.Query.Returns(new QueryCollection(new Dictionary<string, StringValues>
        {
            { "user_id", "user456" }
        }));
        httpContext.Request.Returns(request);
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        var services = new ServiceCollection();
        services.AddSingleton(accessor);
        var provider = services.BuildServiceProvider();
        var strategy = new QueryStringUserIdentifierStrategy<string>();

        var result = strategy.GetUserId(provider);

        Assert.Equal("user456", result);
    }

    [Fact]
    public void Should_ReturnDefault_When_ServiceProviderHasNoHttpContextAccessor()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var strategy = new QueryStringUserIdentifierStrategy<string>();

        var result = strategy.GetUserId(provider);

        Assert.Null(result);
    }

    [Fact]
    public void Should_ConvertToInt_When_TKeyIsInt()
    {
        var httpContext = Substitute.For<HttpContext>();
        var request = Substitute.For<HttpRequest>();
        request.Query.Returns(new QueryCollection(new Dictionary<string, StringValues>
        {
            { "user_id", "99" }
        }));
        httpContext.Request.Returns(request);
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        var services = new ServiceCollection();
        services.AddSingleton(accessor);
        var provider = services.BuildServiceProvider();
        var strategy = new QueryStringUserIdentifierStrategy<int>();

        var result = strategy.GetUserId(provider);

        Assert.Equal(99, result);
    }

    [Fact]
    public void Should_ReturnDefault_When_ConvertFails()
    {
        var httpContext = Substitute.For<HttpContext>();
        var request = Substitute.For<HttpRequest>();
        request.Query.Returns(new QueryCollection(new Dictionary<string, StringValues>
        {
            { "user_id", "invalid" }
        }));
        httpContext.Request.Returns(request);
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        var services = new ServiceCollection();
        services.AddSingleton(accessor);
        var provider = services.BuildServiceProvider();
        var strategy = new QueryStringUserIdentifierStrategy<int>();

        var result = strategy.GetUserId(provider);

        Assert.Equal(default, result);
    }

    [Fact]
    public void Should_Throw_When_CreatedWithEmptyParameter()
    {
        Assert.Throws<ArgumentException>(() => new QueryStringUserIdentifierStrategy<string>(""));
    }

    [Fact]
    public void Should_Throw_When_CreatedWithNullParameter()
    {
        Assert.Throws<ArgumentNullException>(() => new QueryStringUserIdentifierStrategy<string>(null!));
    }
}
