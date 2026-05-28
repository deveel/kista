using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
[Trait("Feature", "UserAccessor")]
public class UserAccessorTests {
	#region StaticUserIdentifierStrategy

	[Fact]
	public void StaticUserIdentifierStrategy_Should_ReturnFixedUserId() {
		var strategy = new StaticUserIdentifierStrategy<string>("user-123");

		var userId = strategy.GetUserId();

		Assert.Equal("user-123", userId);
	}

	[Fact]
	public void StaticUserIdentifierStrategy_Should_ReturnUserId_RegardlessOfServiceProvider() {
		var strategy = new StaticUserIdentifierStrategy<Guid>(Guid.NewGuid());

		var userId = strategy.GetUserId(null);

		Assert.NotEqual(Guid.Empty, userId);
	}

	#endregion

	#region CompositeUserIdentifierStrategy

	[Fact]
	public void CompositeUserIdentifierStrategy_Should_ReturnFirstSuccessfulResult() {
		var composite = new CompositeUserIdentifierStrategy<string>();
		composite.Add(new StaticUserIdentifierStrategy<string>("first"));
		composite.Add(new StaticUserIdentifierStrategy<string>("second"));

		var userId = composite.GetUserId();

		Assert.Equal("first", userId);
	}

	[Fact]
	public void CompositeUserIdentifierStrategy_Should_FallbackToNext_WhenFirstReturnsNull() {
		var composite = new CompositeUserIdentifierStrategy<string>();
		composite.Add(new NullReturningStrategy<string>());
		composite.Add(new StaticUserIdentifierStrategy<string>("fallback"));

		var userId = composite.GetUserId();

		Assert.Equal("fallback", userId);
	}

	[Fact]
	public void CompositeUserIdentifierStrategy_Should_ReturnDefault_WhenAllStrategiesReturnNull() {
		var composite = new CompositeUserIdentifierStrategy<string>();
		composite.Add(new NullReturningStrategy<string>());
		composite.Add(new NullReturningStrategy<string>());

		var userId = composite.GetUserId();

		Assert.Null(userId);
	}

	[Fact]
	public void CompositeUserIdentifierStrategy_Should_Throw_WhenAddingNullStrategy() {
		var composite = new CompositeUserIdentifierStrategy<string>();

		Assert.Throws<ArgumentNullException>(() => composite.Add(null!));
	}

	[Fact]
	public void CompositeUserIdentifierStrategy_Should_ExposeStrategiesAsReadOnly() {
		var composite = new CompositeUserIdentifierStrategy<string>();
		composite.Add(new StaticUserIdentifierStrategy<string>("test"));

		Assert.Single(composite.Strategies);
	}

	#endregion

	#region StrategyBasedUserAccessor

	[Fact]
	public void StrategyBasedUserAccessor_Should_DelegateToComposite() {
		var composite = new CompositeUserIdentifierStrategy<string>();
		composite.Add(new StaticUserIdentifierStrategy<string>("delegated-user"));

		var accessor = new StrategyBasedUserAccessor<string>(composite);

		Assert.Equal("delegated-user", accessor.GetUserId());
	}

	[Fact]
	public void StrategyBasedUserAccessor_Should_Throw_WhenCompositeIsNull() {
		Assert.Throws<ArgumentNullException>(() => new StrategyBasedUserAccessor<string>(null!));
	}

	#endregion

	#region ClaimUserIdentifierStrategy

	[Fact]
	public void ClaimUserIdentifierStrategy_Should_ReturnUserId_WhenClaimExists() {
		var services = CreateServicesWithUserClaim("sub", "user-456");
		var strategy = new ClaimUserIdentifierStrategy<string>();

		var userId = strategy.GetUserId(services);

		Assert.Equal("user-456", userId);
	}

	[Fact]
	public void ClaimUserIdentifierStrategy_Should_ReturnDefault_WhenClaimMissing() {
		var services = CreateServicesWithClaims(new Claim("name", "test"));
		var strategy = new ClaimUserIdentifierStrategy<string>();

		var userId = strategy.GetUserId(services);

		Assert.Null(userId);
	}

	[Fact]
	public void ClaimUserIdentifierStrategy_Should_ReturnDefault_WhenNoServiceProvider() {
		var strategy = new ClaimUserIdentifierStrategy<string>();

		var userId = strategy.GetUserId(null);

		Assert.Null(userId);
	}

	[Fact]
	public void ClaimUserIdentifierStrategy_Should_UseCustomClaimType() {
		var services = CreateServicesWithUserClaim("email", "user@example.com");
		var strategy = new ClaimUserIdentifierStrategy<string>("email");

		var userId = strategy.GetUserId(services);

		Assert.Equal("user@example.com", userId);
	}

	[Fact]
	public void ClaimUserIdentifierStrategy_Should_ConvertToGuid() {
		var guid = Guid.NewGuid();
		var services = CreateServicesWithUserClaim("sub", guid.ToString());
		var strategy = new ClaimUserIdentifierStrategy<Guid>();

		var userId = strategy.GetUserId(services);

		Assert.Equal(guid, userId);
	}

	[Fact]
	public void ClaimUserIdentifierStrategy_Should_ConvertToInt() {
		var services = CreateServicesWithUserClaim("sub", "12345");
		var strategy = new ClaimUserIdentifierStrategy<int>();

		var userId = strategy.GetUserId(services);

		Assert.Equal(12345, userId);
	}

	[Fact]
	public void ClaimUserIdentifierStrategy_Should_ReturnDefault_WhenConversionFails() {
		var services = CreateServicesWithUserClaim("sub", "not-a-guid");
		var strategy = new ClaimUserIdentifierStrategy<Guid>();

		var userId = strategy.GetUserId(services);

		Assert.Equal(Guid.Empty, userId);
	}

	#endregion

	#region QueryStringUserIdentifierStrategy

	[Fact]
	public void QueryStringUserIdentifierStrategy_Should_ReturnUserId_WhenParameterExists() {
		var services = CreateServicesWithQueryString("user_id", "user-789");
		var strategy = new QueryStringUserIdentifierStrategy<string>();

		var userId = strategy.GetUserId(services);

		Assert.Equal("user-789", userId);
	}

	[Fact]
	public void QueryStringUserIdentifierStrategy_Should_ReturnDefault_WhenParameterMissing() {
		var services = CreateServicesWithQueryString("other_param", "value");
		var strategy = new QueryStringUserIdentifierStrategy<string>();

		var userId = strategy.GetUserId(services);

		Assert.Null(userId);
	}

	[Fact]
	public void QueryStringUserIdentifierStrategy_Should_UseCustomParameter() {
		var services = CreateServicesWithQueryString("uid", "custom-user");
		var strategy = new QueryStringUserIdentifierStrategy<string>("uid");

		var userId = strategy.GetUserId(services);

		Assert.Equal("custom-user", userId);
	}

	[Fact]
	public void QueryStringUserIdentifierStrategy_Should_ConvertToInt() {
		var services = CreateServicesWithQueryString("user_id", "42");
		var strategy = new QueryStringUserIdentifierStrategy<int>();

		var userId = strategy.GetUserId(services);

		Assert.Equal(42, userId);
	}

	#endregion

	#region RouteUserIdentifierStrategy

	[Fact]
	public void RouteUserIdentifierStrategy_Should_ReturnUserId_WhenRouteValueExists() {
		var services = CreateServicesWithRouteValue("userId", "route-user");
		var strategy = new RouteUserIdentifierStrategy<string>();

		var userId = strategy.GetUserId(services);

		Assert.Equal("route-user", userId);
	}

	[Fact]
	public void RouteUserIdentifierStrategy_Should_ReturnDefault_WhenRouteValueMissing() {
		var services = CreateServicesWithRouteValue("otherId", "value");
		var strategy = new RouteUserIdentifierStrategy<string>();

		var userId = strategy.GetUserId(services);

		Assert.Null(userId);
	}

	[Fact]
	public void RouteUserIdentifierStrategy_Should_UseCustomKey() {
		var services = CreateServicesWithRouteValue("uid", "custom-route");
		var strategy = new RouteUserIdentifierStrategy<string>("uid");

		var userId = strategy.GetUserId(services);

		Assert.Equal("custom-route", userId);
	}

	[Fact]
	public void RouteUserIdentifierStrategy_Should_ConvertToGuid() {
		var guid = Guid.NewGuid();
		var services = CreateServicesWithRouteValue("userId", guid.ToString());
		var strategy = new RouteUserIdentifierStrategy<Guid>();

		var userId = strategy.GetUserId(services);

		Assert.Equal(guid, userId);
	}

	#endregion

	#region Fallback Chain Tests

	[Fact]
	public void Should_FallbackFromClaimToQueryString_WhenClaimMissing() {
		var services = CreateServicesWithQueryString("user_id", "qs-user");
		var composite = new CompositeUserIdentifierStrategy<string>();
		composite.Add(new ClaimUserIdentifierStrategy<string>());
		composite.Add(new QueryStringUserIdentifierStrategy<string>());

		var userId = composite.GetUserId(services);

		Assert.Equal("qs-user", userId);
	}

	[Fact]
	public void Should_FallbackFromQueryStringToRoute_WhenQueryStringMissing() {
		var services = CreateServicesWithRouteValue("userId", "route-user");
		var composite = new CompositeUserIdentifierStrategy<string>();
		composite.Add(new QueryStringUserIdentifierStrategy<string>());
		composite.Add(new RouteUserIdentifierStrategy<string>());

		var userId = composite.GetUserId(services);

		Assert.Equal("route-user", userId);
	}

	[Fact]
	public void Should_FallbackToStatic_WhenAllHttpSourcesFail() {
		var services = CreateServicesWithEmptyContext();
		var composite = new CompositeUserIdentifierStrategy<string>();
		composite.Add(new ClaimUserIdentifierStrategy<string>());
		composite.Add(new QueryStringUserIdentifierStrategy<string>());
		composite.Add(new StaticUserIdentifierStrategy<string>("system-user"));

		var userId = composite.GetUserId(services);

		Assert.Equal("system-user", userId);
	}

	[Fact]
	public void Should_UseFirstSuccessful_WhenMultipleStrategiesSucceed() {
		var services = CreateServicesWithUserClaim("sub", "claim-user");
		var composite = new CompositeUserIdentifierStrategy<string>();
		composite.Add(new ClaimUserIdentifierStrategy<string>());
		composite.Add(new StaticUserIdentifierStrategy<string>("static-user"));

		var userId = composite.GetUserId(services);

		Assert.Equal("claim-user", userId);
	}

	#endregion

	#region DI Registration Tests

	[Fact]
	public void AddUserAccessor_Should_RegisterService() {
		var services = new ServiceCollection();
		services.AddUserAccessor<string>(builder => {
			builder.AddStatic("test-user");
		});

		var provider = services.BuildServiceProvider();
		var accessor = provider.GetRequiredService<IUserAccessor<string>>();

		Assert.NotNull(accessor);
		Assert.Equal("test-user", accessor.GetUserId());
	}

	[Fact]
	public void AddUserAccessor_Should_RegisterHttpContextAccessor() {
		var services = new ServiceCollection();
		services.AddUserAccessor<string>(builder => {
			builder.AddStatic("test");
		});

		var provider = services.BuildServiceProvider();
		var httpContextAccessor = provider.GetService<IHttpContextAccessor>();

		Assert.NotNull(httpContextAccessor);
	}

	[Fact]
	public void AddHttpUserAccessor_WithoutConfigure_Should_RegisterDefaultChain() {
		var services = new ServiceCollection();
		services.AddHttpUserAccessor<string>();

		var provider = services.BuildServiceProvider();
		var accessor = provider.GetRequiredService<IUserAccessor<string>>();

		Assert.NotNull(accessor);
	}

	[Fact]
	public void AddHttpUserAccessor_WithConfigure_Should_ApplyCustomStrategies() {
		var services = new ServiceCollection();
		services.AddHttpUserAccessor<string>(builder => {
			builder.AddClaim("email");
			builder.AddStatic("fallback");
		});

		var provider = services.BuildServiceProvider();
		var accessor = provider.GetRequiredService<IUserAccessor<string>>();

		Assert.NotNull(accessor);
	}

	[Fact]
	public void AddUserAccessor_Should_NotConflict_When_MultipleKeyTypesRegistered() {
		var services = new ServiceCollection();
		services.AddUserAccessor<string>(b => b.AddStatic("string-user"));
		services.AddUserAccessor<Guid>(b => b.AddStatic(Guid.NewGuid()));

		var provider = services.BuildServiceProvider();
		var stringAccessor = provider.GetRequiredService<IUserAccessor<string>>();
		var guidAccessor = provider.GetRequiredService<IUserAccessor<Guid>>();

		Assert.NotNull(stringAccessor);
		Assert.NotNull(guidAccessor);
		Assert.Equal("string-user", stringAccessor.GetUserId());
	}

	[Fact]
	public void WithHttpUserAccessor_Should_RegisterService() {
		var services = new ServiceCollection();
		var builder = new RepositoryContextBuilder(services);
		builder.WithHttpUserAccessor<string>();

		var provider = services.BuildServiceProvider();
		var accessor = provider.GetRequiredService<IUserAccessor<string>>();

		Assert.NotNull(accessor);
	}

	[Fact]
	public void WithHttpUserAccessor_WithConfigure_Should_RegisterService() {
		var services = new ServiceCollection();
		var builder = new RepositoryContextBuilder(services);
		builder.WithHttpUserAccessor<string>(b => {
			b.AddClaim("sub");
			b.AddStatic("anonymous");
		});

		var provider = services.BuildServiceProvider();
		var accessor = provider.GetRequiredService<IUserAccessor<string>>();

		Assert.NotNull(accessor);
	}

	#endregion

	#region Helper Methods

	private static IServiceProvider CreateServicesWithUserClaim(string claimType, string value) {
		return CreateServicesWithClaims(new Claim(claimType, value));
	}

	private static IServiceProvider CreateServicesWithClaims(params Claim[] claims) {
		var httpContext = new DefaultHttpContext();
		httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims));

		var services = new ServiceCollection();
		services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = httpContext });
		return services.BuildServiceProvider();
	}

	private static IServiceProvider CreateServicesWithQueryString(string parameter, string value) {
		var httpContext = new DefaultHttpContext();
		httpContext.Request.QueryString = new QueryString($"?{parameter}={value}");

		var services = new ServiceCollection();
		services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = httpContext });
		return services.BuildServiceProvider();
	}

	private static IServiceProvider CreateServicesWithRouteValue(string key, string value) {
		var httpContext = new DefaultHttpContext();
		httpContext.Request.RouteValues.Add(key, value);

		var services = new ServiceCollection();
		services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = httpContext });
		return services.BuildServiceProvider();
	}

	private static IServiceProvider CreateServicesWithEmptyContext() {
		var httpContext = new DefaultHttpContext();

		var services = new ServiceCollection();
		services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = httpContext });
		return services.BuildServiceProvider();
	}

	private sealed class NullReturningStrategy<TKey> : IUserIdentifierStrategy<TKey> {
		public TKey? GetUserId(IServiceProvider? serviceProvider = null) => default;
	}

	#endregion
}
