using Microsoft.Extensions.DependencyInjection;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
[Trait("Feature", "UserIdentifier")]
public class HttpUserIdentifierStrategyBuilderTests
{
    [Fact]
    public void Should_AddHttpUserAccessor_WithDefaultStrategies()
    {
        var services = new ServiceCollection();

        services.AddHttpUserAccessor<string>();

        var provider = services.BuildServiceProvider();
        var accessor = provider.GetService<IUserAccessor<string>>();

        Assert.NotNull(accessor);
    }

    [Fact]
    public void Should_AddHttpUserAccessor_WithCustomConfiguration()
    {
        var services = new ServiceCollection();

        services.AddHttpUserAccessor<string>(builder =>
            builder.AddClaim("custom").AddStatic("fallback"));

        var provider = services.BuildServiceProvider();
        var accessor = provider.GetService<IUserAccessor<string>>();

        Assert.NotNull(accessor);
    }

    [Fact]
    public void Should_Throw_When_AddHttpUserAccessorWithNullConfigure()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() =>
            services.AddHttpUserAccessor<string>(null!));
    }

    [Fact]
    public void Should_AddUserAccessor_WithCustomConfigure()
    {
        var services = new ServiceCollection();

        services.AddUserAccessor<string>(builder =>
            builder.Add(new StaticUserIdentifierStrategy<string>("test")));

        var provider = services.BuildServiceProvider();
        var accessor = provider.GetService<IUserAccessor<string>>();

        Assert.NotNull(accessor);
    }

    [Fact]
    public void Should_Throw_When_AddUserAccessorWithNullConfigure()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() =>
            services.AddUserAccessor<string>(null!));
    }

    [Fact]
    public void Should_AddHttpUserAccessor_WithQueryString()
    {
        var services = new ServiceCollection();

        services.AddHttpUserAccessor<string>(builder =>
            builder.AddQueryString("custom_param"));

        var provider = services.BuildServiceProvider();
        var accessor = provider.GetService<IUserAccessor<string>>();

        Assert.NotNull(accessor);
    }

    [Fact]
    public void Should_AddHttpUserAccessor_WithRoute()
    {
        var services = new ServiceCollection();

        services.AddHttpUserAccessor<string>(builder =>
            builder.AddRoute("custom_key"));

        var provider = services.BuildServiceProvider();
        var accessor = provider.GetService<IUserAccessor<string>>();

        Assert.NotNull(accessor);
    }

    [Fact]
    public void Should_AddHttpUserAccessor_WithAllStrategies()
    {
        var services = new ServiceCollection();

        services.AddHttpUserAccessor<string>(builder =>
            builder.AddClaim().AddQueryString().AddRoute().AddStatic("default"));

        var provider = services.BuildServiceProvider();
        var accessor = provider.GetService<IUserAccessor<string>>();

        Assert.NotNull(accessor);
    }
}
