using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace Kista;

/// <summary>
/// A base class for the user identifier strategy test suites that resolve
/// the user identifier from an HTTP request value (query string or route),
/// providing the shared test cases that exercise the common behavior of
/// <see cref="QueryStringUserIdentifierStrategy{TKey}"/> and
/// <see cref="RouteUserIdentifierStrategy{TKey}"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
[Trait("Feature", "UserIdentifier")]
public abstract class HttpRequestUserIdentifierStrategyTestsBase {
	/// <summary>
	/// Gets the default parameter/key name used by the strategy under test
	/// when no explicit name is provided.
	/// </summary>
	protected abstract string DefaultParameterName { get; }

	/// <summary>
	/// Creates an instance of the strategy under test with the given
	/// parameter/key name.
	/// </summary>
	protected abstract IUserIdentifierStrategy<TKey> CreateStrategy<TKey>(string parameterName);

	/// <summary>
	/// Creates an instance of the strategy under test with the default
	/// parameter/key name.
	/// </summary>
	protected abstract IUserIdentifierStrategy<TKey> CreateDefaultStrategy<TKey>();

	/// <summary>
	/// Sets the value on the HTTP request that the strategy under test
	/// is expected to read (query string or route value).
	/// </summary>
	protected abstract void SetRequestValue(HttpRequest request, string parameterName, string value);

	/// <summary>
	/// Returns an empty request value container for the strategy under test
	/// (an empty <see cref="QueryCollection"/> or <see cref="RouteValueDictionary"/>).
	/// </summary>
	protected abstract void SetEmptyRequestValue(HttpRequest request);

	[Fact]
	public void Should_ReturnDefault_When_ServiceProviderIsNull() {
		var strategy = CreateDefaultStrategy<string>();
		var result = strategy.GetUserId(null);
		Assert.Null(result);
	}

	[Fact]
	public void Should_ReturnDefault_When_HttpContextIsNull() {
		var services = new ServiceCollection();
		services.AddHttpContextAccessor();
		var provider = services.BuildServiceProvider();
		var strategy = CreateDefaultStrategy<string>();

		var result = strategy.GetUserId(provider);

		Assert.Null(result);
	}

	[Fact]
	public void Should_ReturnDefault_When_ParameterNotFound() {
		var httpContext = Substitute.For<HttpContext>();
		var request = Substitute.For<HttpRequest>();
		SetEmptyRequestValue(request);
		httpContext.Request.Returns(request);
		var accessor = Substitute.For<IHttpContextAccessor>();
		accessor.HttpContext.Returns(httpContext);
		var services = new ServiceCollection();
		services.AddSingleton(accessor);
		var provider = services.BuildServiceProvider();
		var strategy = CreateStrategy<string>("custom_param");

		var result = strategy.GetUserId(provider);

		Assert.Null(result);
	}

	[Fact]
	public void Should_ReturnUserId_When_ParameterFound() {
		var httpContext = Substitute.For<HttpContext>();
		var request = Substitute.For<HttpRequest>();
		SetRequestValue(request, DefaultParameterName, "user456");
		httpContext.Request.Returns(request);
		var accessor = Substitute.For<IHttpContextAccessor>();
		accessor.HttpContext.Returns(httpContext);
		var services = new ServiceCollection();
		services.AddSingleton(accessor);
		var provider = services.BuildServiceProvider();
		var strategy = CreateDefaultStrategy<string>();

		var result = strategy.GetUserId(provider);

		Assert.Equal("user456", result);
	}

	[Fact]
	public void Should_ReturnDefault_When_ServiceProviderHasNoHttpContextAccessor() {
		var services = new ServiceCollection();
		var provider = services.BuildServiceProvider();
		var strategy = CreateDefaultStrategy<string>();

		var result = strategy.GetUserId(provider);

		Assert.Null(result);
	}

	[Fact]
	public void Should_ConvertToInt_When_TKeyIsInt() {
		var httpContext = Substitute.For<HttpContext>();
		var request = Substitute.For<HttpRequest>();
		SetRequestValue(request, DefaultParameterName, "99");
		httpContext.Request.Returns(request);
		var accessor = Substitute.For<IHttpContextAccessor>();
		accessor.HttpContext.Returns(httpContext);
		var services = new ServiceCollection();
		services.AddSingleton(accessor);
		var provider = services.BuildServiceProvider();
		var strategy = CreateStrategy<int>(DefaultParameterName);

		var result = strategy.GetUserId(provider);

		Assert.Equal(99, result);
	}

	[Fact]
	public void Should_ReturnDefault_When_ConvertFails() {
		var httpContext = Substitute.For<HttpContext>();
		var request = Substitute.For<HttpRequest>();
		SetRequestValue(request, DefaultParameterName, "invalid");
		httpContext.Request.Returns(request);
		var accessor = Substitute.For<IHttpContextAccessor>();
		accessor.HttpContext.Returns(httpContext);
		var services = new ServiceCollection();
		services.AddSingleton(accessor);
		var provider = services.BuildServiceProvider();
		var strategy = CreateStrategy<int>(DefaultParameterName);

		var result = strategy.GetUserId(provider);

		Assert.Equal(default, result);
	}

	[Fact]
	public void Should_Throw_When_CreatedWithEmptyParameter() {
		Assert.Throws<ArgumentException>(() => CreateStrategy<string>(""));
	}

	[Fact]
	public void Should_Throw_When_CreatedWithNullParameter() {
		Assert.Throws<ArgumentNullException>(() => CreateStrategy<string>(null!));
	}
}