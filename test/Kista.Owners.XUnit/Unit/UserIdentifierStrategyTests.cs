using Microsoft.Extensions.DependencyInjection;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
[Trait("Feature", "UserIdentifier")]
public class UserIdentifierStrategyTests
{
    [Fact]
    public void Should_ReturnDefault_When_ClaimStrategyServiceProviderIsNull()
    {
        var strategy = new ClaimUserIdentifierStrategy<string>();
        var result = strategy.GetUserId(null);

        Assert.Null(result);
    }

    [Fact]
    public void Should_ReturnDefault_When_RouteStrategyServiceProviderIsNull()
    {
        var strategy = new RouteUserIdentifierStrategy<string>();
        var result = strategy.GetUserId(null);

        Assert.Null(result);
    }

    [Fact]
    public void Should_ReturnDefault_When_QueryStringStrategyServiceProviderIsNull()
    {
        var strategy = new QueryStringUserIdentifierStrategy<string>();
        var result = strategy.GetUserId(null);

        Assert.Null(result);
    }

    [Fact]
    public void Should_ReturnDefault_When_ClaimStrategyServiceProviderHasNoHttpContext()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var strategy = new ClaimUserIdentifierStrategy<string>();

        var result = strategy.GetUserId(provider);

        Assert.Null(result);
    }

    [Fact]
    public void Should_ReturnDefault_When_RouteStrategyServiceProviderHasNoHttpContext()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var strategy = new RouteUserIdentifierStrategy<string>();

        var result = strategy.GetUserId(provider);

        Assert.Null(result);
    }

    [Fact]
    public void Should_ReturnDefault_When_QueryStringStrategyServiceProviderHasNoHttpContext()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var strategy = new QueryStringUserIdentifierStrategy<string>();

        var result = strategy.GetUserId(provider);

        Assert.Null(result);
    }

    [Fact]
    public void Should_Throw_When_ClaimStrategyCreatedWithEmptyClaimType()
    {
        Assert.Throws<ArgumentException>(() => new ClaimUserIdentifierStrategy<string>(""));
    }

    [Fact]
    public void Should_Throw_When_ClaimStrategyCreatedWithNullClaimType()
    {
        Assert.Throws<ArgumentNullException>(() => new ClaimUserIdentifierStrategy<string>(null!));
    }

    [Fact]
    public void Should_Throw_When_RouteStrategyCreatedWithEmptyKey()
    {
        Assert.Throws<ArgumentException>(() => new RouteUserIdentifierStrategy<string>(""));
    }

    [Fact]
    public void Should_Throw_When_RouteStrategyCreatedWithNullKey()
    {
        Assert.Throws<ArgumentNullException>(() => new RouteUserIdentifierStrategy<string>(null!));
    }

    [Fact]
    public void Should_Throw_When_QueryStringStrategyCreatedWithEmptyParameter()
    {
        Assert.Throws<ArgumentException>(() => new QueryStringUserIdentifierStrategy<string>(""));
    }

    [Fact]
    public void Should_Throw_When_QueryStringStrategyCreatedWithNullParameter()
    {
        Assert.Throws<ArgumentNullException>(() => new QueryStringUserIdentifierStrategy<string>(null!));
    }

    [Fact]
    public void Should_ReturnDefaultInt_When_ClaimStrategyWithIntKey()
    {
        var strategy = new ClaimUserIdentifierStrategy<int>();
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        var result = strategy.GetUserId(provider);

        Assert.Equal(default, result);
    }
}
