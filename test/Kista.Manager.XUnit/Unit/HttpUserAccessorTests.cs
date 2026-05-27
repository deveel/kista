using System.ComponentModel;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
[Trait("Feature", "HttpUserAccessor")]
public class HttpUserAccessorTests {
	#region Constructor

	[Fact]
	public void Should_ThrowArgumentNullException_When_HttpContextAccessorIsNull() {
		var options = Options.Create(new HttpUserAccessorOptions());

		var ex = Assert.Throws<ArgumentNullException>(() => new HttpUserAccessor<string>(null!, options));
		Assert.Equal("httpContextAccessor", ex.ParamName);
	}

	[Fact]
	public void Should_ThrowArgumentNullException_When_OptionsIsNull() {
		var accessor = new HttpContextAccessor();

		var ex = Assert.Throws<ArgumentNullException>(() => new HttpUserAccessor<string>(accessor, null!));
		Assert.Equal("options", ex.ParamName);
	}

	#endregion

	#region Null / Empty Context

	[Fact]
	public void Should_ReturnDefault_When_HttpContextIsNull() {
		var accessor = new HttpContextAccessor { HttpContext = null };
		var options = Options.Create(new HttpUserAccessorOptions());
		var userAccessor = new HttpUserAccessor<string>(accessor, options);

		var userId = userAccessor.GetUserId();

		Assert.Null(userId);
	}

	[Fact]
	public void Should_ReturnDefault_When_NoSourcesConfigured() {
		var httpContext = new DefaultHttpContext();
		var accessor = new HttpContextAccessor { HttpContext = httpContext };
		var options = Options.Create(new HttpUserAccessorOptions {
			Sources = new List<HttpUserIdentifierSource>()
		});
		var userAccessor = new HttpUserAccessor<string>(accessor, options);

		var userId = userAccessor.GetUserId();

		Assert.Null(userId);
	}

	[Fact]
	public void Should_ReturnDefault_When_NoUserIdentityAndNoFallback() {
		var httpContext = new DefaultHttpContext();
		var accessor = new HttpContextAccessor { HttpContext = httpContext };
		var options = Options.Create(new HttpUserAccessorOptions {
			Sources = new List<HttpUserIdentifierSource> {
				HttpUserIdentifierSource.Claim
			}
		});
		var userAccessor = new HttpUserAccessor<string>(accessor, options);

		var userId = userAccessor.GetUserId();

		Assert.Null(userId);
	}

	#endregion

	#region Claim Source

	[Fact]
	public void Should_ResolveUserId_FromClaim() {
		var httpContext = new DefaultHttpContext();
		var identity = new ClaimsIdentity(new[] { new Claim("sub", "user-abc") });
		httpContext.User = new ClaimsPrincipal(identity);
		var accessor = new HttpContextAccessor { HttpContext = httpContext };
		var options = Options.Create(new HttpUserAccessorOptions {
			Sources = new List<HttpUserIdentifierSource> {
				HttpUserIdentifierSource.Claim
			}
		});
		var userAccessor = new HttpUserAccessor<string>(accessor, options);

		var userId = userAccessor.GetUserId();

		Assert.Equal("user-abc", userId);
	}

	[Fact]
	public void Should_ResolveUserId_FromCustomClaimType() {
		var httpContext = new DefaultHttpContext();
		var identity = new ClaimsIdentity(new[] { new Claim("client_id", "client-42") });
		httpContext.User = new ClaimsPrincipal(identity);
		var accessor = new HttpContextAccessor { HttpContext = httpContext };
		var options = Options.Create(new HttpUserAccessorOptions {
			Sources = new List<HttpUserIdentifierSource> {
				HttpUserIdentifierSource.Claim
			},
			ClaimType = "client_id"
		});
		var userAccessor = new HttpUserAccessor<string>(accessor, options);

		var userId = userAccessor.GetUserId();

		Assert.Equal("client-42", userId);
	}

	[Fact]
	public void Should_ReturnDefault_When_ClaimNotFound() {
		var httpContext = new DefaultHttpContext();
		var identity = new ClaimsIdentity(new[] { new Claim("email", "test@example.com") });
		httpContext.User = new ClaimsPrincipal(identity);
		var accessor = new HttpContextAccessor { HttpContext = httpContext };
		var options = Options.Create(new HttpUserAccessorOptions {
			Sources = new List<HttpUserIdentifierSource> {
				HttpUserIdentifierSource.Claim
			}
		});
		var userAccessor = new HttpUserAccessor<string>(accessor, options);

		var userId = userAccessor.GetUserId();

		Assert.Null(userId);
	}

	#endregion

	#region QueryString Source

	[Fact]
	public void Should_ResolveUserId_FromQueryString() {
		var httpContext = new DefaultHttpContext();
		httpContext.Request.QueryString = new QueryString("?user_id=qry-456");
		var accessor = new HttpContextAccessor { HttpContext = httpContext };
		var options = Options.Create(new HttpUserAccessorOptions {
			Sources = new List<HttpUserIdentifierSource> {
				HttpUserIdentifierSource.QueryString
			}
		});
		var userAccessor = new HttpUserAccessor<string>(accessor, options);

		var userId = userAccessor.GetUserId();

		Assert.Equal("qry-456", userId);
	}

	[Fact]
	public void Should_ResolveUserId_FromCustomQueryStringParameter() {
		var httpContext = new DefaultHttpContext();
		httpContext.Request.QueryString = new QueryString("?uid=custom-qry");
		var accessor = new HttpContextAccessor { HttpContext = httpContext };
		var options = Options.Create(new HttpUserAccessorOptions {
			Sources = new List<HttpUserIdentifierSource> {
				HttpUserIdentifierSource.QueryString
			},
			QueryStringParameter = "uid"
		});
		var userAccessor = new HttpUserAccessor<string>(accessor, options);

		var userId = userAccessor.GetUserId();

		Assert.Equal("custom-qry", userId);
	}

	[Fact]
	public void Should_ReturnDefault_When_QueryStringParameterMissing() {
		var httpContext = new DefaultHttpContext();
		httpContext.Request.QueryString = new QueryString("?other=value");
		var accessor = new HttpContextAccessor { HttpContext = httpContext };
		var options = Options.Create(new HttpUserAccessorOptions {
			Sources = new List<HttpUserIdentifierSource> {
				HttpUserIdentifierSource.QueryString
			}
		});
		var userAccessor = new HttpUserAccessor<string>(accessor, options);

		var userId = userAccessor.GetUserId();

		Assert.Null(userId);
	}

	#endregion

	#region Route Source

	[Fact]
	public void Should_ResolveUserId_FromRoute() {
		var httpContext = new DefaultHttpContext();
		httpContext.Request.RouteValues["userId"] = "route-789";
		var accessor = new HttpContextAccessor { HttpContext = httpContext };
		var options = Options.Create(new HttpUserAccessorOptions {
			Sources = new List<HttpUserIdentifierSource> {
				HttpUserIdentifierSource.Route
			}
		});
		var userAccessor = new HttpUserAccessor<string>(accessor, options);

		var userId = userAccessor.GetUserId();

		Assert.Equal("route-789", userId);
	}

	[Fact]
	public void Should_ResolveUserId_FromCustomRouteParameter() {
		var httpContext = new DefaultHttpContext();
		httpContext.Request.RouteValues["tenant"] = "tenant-abc";
		var accessor = new HttpContextAccessor { HttpContext = httpContext };
		var options = Options.Create(new HttpUserAccessorOptions {
			Sources = new List<HttpUserIdentifierSource> {
				HttpUserIdentifierSource.Route
			},
			RouteParameter = "tenant"
		});
		var userAccessor = new HttpUserAccessor<string>(accessor, options);

		var userId = userAccessor.GetUserId();

		Assert.Equal("tenant-abc", userId);
	}

	[Fact]
	public void Should_ReturnDefault_When_RouteParameterMissing() {
		var httpContext = new DefaultHttpContext();
		httpContext.Request.RouteValues["other"] = "value";
		var accessor = new HttpContextAccessor { HttpContext = httpContext };
		var options = Options.Create(new HttpUserAccessorOptions {
			Sources = new List<HttpUserIdentifierSource> {
				HttpUserIdentifierSource.Route
			}
		});
		var userAccessor = new HttpUserAccessor<string>(accessor, options);

		var userId = userAccessor.GetUserId();

		Assert.Null(userId);
	}

	#endregion

	#region Priority / Fallback

	[Fact]
	public void Should_ReturnClaimValue_When_AllSourcesPresent() {
		var httpContext = new DefaultHttpContext();
		var identity = new ClaimsIdentity(new[] { new Claim("sub", "claim-user") });
		httpContext.User = new ClaimsPrincipal(identity);
		httpContext.Request.QueryString = new QueryString("?user_id=qry-user");
		httpContext.Request.RouteValues["userId"] = "route-user";
		var accessor = new HttpContextAccessor { HttpContext = httpContext };
		var options = Options.Create(new HttpUserAccessorOptions());
		var userAccessor = new HttpUserAccessor<string>(accessor, options);

		var userId = userAccessor.GetUserId();

		Assert.Equal("claim-user", userId);
	}

	[Fact]
	public void Should_FallBackToQueryString_When_ClaimMissing() {
		var httpContext = new DefaultHttpContext();
		httpContext.Request.QueryString = new QueryString("?user_id=qry-fallback");
		httpContext.Request.RouteValues["userId"] = "route-fallback";
		var accessor = new HttpContextAccessor { HttpContext = httpContext };
		var options = Options.Create(new HttpUserAccessorOptions());
		var userAccessor = new HttpUserAccessor<string>(accessor, options);

		var userId = userAccessor.GetUserId();

		Assert.Equal("qry-fallback", userId);
	}

	[Fact]
	public void Should_FallBackToRoute_When_ClaimAndQueryStringMissing() {
		var httpContext = new DefaultHttpContext();
		httpContext.Request.RouteValues["userId"] = "route-only";
		var accessor = new HttpContextAccessor { HttpContext = httpContext };
		var options = Options.Create(new HttpUserAccessorOptions());
		var userAccessor = new HttpUserAccessor<string>(accessor, options);

		var userId = userAccessor.GetUserId();

		Assert.Equal("route-only", userId);
	}

	[Fact]
	public void Should_ReturnDefault_When_AllSourcesEmpty() {
		var httpContext = new DefaultHttpContext();
		var accessor = new HttpContextAccessor { HttpContext = httpContext };
		var options = Options.Create(new HttpUserAccessorOptions());
		var userAccessor = new HttpUserAccessor<string>(accessor, options);

		var userId = userAccessor.GetUserId();

		Assert.Null(userId);
	}

	[Fact]
	public void Should_FollowCustomSourceOrder() {
		var httpContext = new DefaultHttpContext();
		httpContext.Request.QueryString = new QueryString("?user_id=qry-user");
		httpContext.Request.RouteValues["userId"] = "route-user";
		var identity = new ClaimsIdentity(new[] { new Claim("sub", "claim-user") });
		httpContext.User = new ClaimsPrincipal(identity);
		var accessor = new HttpContextAccessor { HttpContext = httpContext };
		// Reverse order: route, query, claim
		var options = Options.Create(new HttpUserAccessorOptions {
			Sources = new List<HttpUserIdentifierSource> {
				HttpUserIdentifierSource.Route,
				HttpUserIdentifierSource.QueryString,
				HttpUserIdentifierSource.Claim
			}
		});
		var userAccessor = new HttpUserAccessor<string>(accessor, options);

		var userId = userAccessor.GetUserId();

		// Route is first in the custom order
		Assert.Equal("route-user", userId);
	}

	#endregion

	#region TKey Conversion

	[Fact]
	public void Should_ResolveGuidKey_FromClaim() {
		var guid = Guid.NewGuid();
		var httpContext = new DefaultHttpContext();
		var identity = new ClaimsIdentity(new[] { new Claim("sub", guid.ToString()) });
		httpContext.User = new ClaimsPrincipal(identity);
		var accessor = new HttpContextAccessor { HttpContext = httpContext };
		var options = Options.Create(new HttpUserAccessorOptions {
			Sources = new List<HttpUserIdentifierSource> {
				HttpUserIdentifierSource.Claim
			}
		});
		var userAccessor = new HttpUserAccessor<Guid>(accessor, options);

		var userId = userAccessor.GetUserId();

		Assert.Equal(guid, userId);
	}

	[Fact]
	public void Should_ResolveIntKey_FromQueryString() {
		var httpContext = new DefaultHttpContext();
		httpContext.Request.QueryString = new QueryString("?user_id=42");
		var accessor = new HttpContextAccessor { HttpContext = httpContext };
		var options = Options.Create(new HttpUserAccessorOptions {
			Sources = new List<HttpUserIdentifierSource> {
				HttpUserIdentifierSource.QueryString
			}
		});
		var userAccessor = new HttpUserAccessor<int>(accessor, options);

		var userId = userAccessor.GetUserId();

		Assert.Equal(42, userId);
	}

	[Fact]
	public void Should_ReturnDefault_When_ConversionFails() {
		var httpContext = new DefaultHttpContext();
		var identity = new ClaimsIdentity(new[] { new Claim("sub", "not-a-guid") });
		httpContext.User = new ClaimsPrincipal(identity);
		var accessor = new HttpContextAccessor { HttpContext = httpContext };
		var options = Options.Create(new HttpUserAccessorOptions {
			Sources = new List<HttpUserIdentifierSource> {
				HttpUserIdentifierSource.Claim
			}
		});
		var userAccessor = new HttpUserAccessor<Guid>(accessor, options);

		var userId = userAccessor.GetUserId();

		Assert.Equal(Guid.Empty, userId);
	}

	[Fact]
	public void Should_ReturnDefault_When_IntConversionFails() {
		var httpContext = new DefaultHttpContext();
		var identity = new ClaimsIdentity(new[] { new Claim("sub", "abc") });
		httpContext.User = new ClaimsPrincipal(identity);
		var accessor = new HttpContextAccessor { HttpContext = httpContext };
		var options = Options.Create(new HttpUserAccessorOptions {
			Sources = new List<HttpUserIdentifierSource> {
				HttpUserIdentifierSource.Claim
			}
		});
		var userAccessor = new HttpUserAccessor<int>(accessor, options);

		var userId = userAccessor.GetUserId();

		Assert.Equal(0, userId);
	}

	#endregion

	#region DI Registration

	[Fact]
	public void AddHttpUserAccessor_Should_RegisterService() {
		var services = new ServiceCollection();

		services.AddHttpUserAccessor<string>();
		var provider = services.BuildServiceProvider();
		var accessor = provider.GetService<IUserAccessor<string>>();

		Assert.NotNull(accessor);
		Assert.IsType<HttpUserAccessor<string>>(accessor);
	}

	[Fact]
	public void AddHttpUserAccessor_Should_RegisterHttpContextAccessor() {
		var services = new ServiceCollection();

		services.AddHttpUserAccessor<string>();
		var provider = services.BuildServiceProvider();
		var httpContextAccessor = provider.GetService<IHttpContextAccessor>();

		Assert.NotNull(httpContextAccessor);
	}

	[Fact]
	public void AddHttpUserAccessor_WithConfigure_Should_ApplyOptions() {
		var services = new ServiceCollection();

		services.AddHttpUserAccessor<string>(options => {
			options.ClaimType = "custom_claim";
			options.Sources = new List<HttpUserIdentifierSource> {
				HttpUserIdentifierSource.Claim
			};
		});
		var provider = services.BuildServiceProvider();
		var accessor = provider.GetRequiredService<IUserAccessor<string>>();
		var optionsAccessor = provider.GetRequiredService<IOptions<HttpUserAccessorOptions>>();

		Assert.NotNull(accessor);
		Assert.Equal("custom_claim", optionsAccessor.Value.ClaimType);
		Assert.Single(optionsAccessor.Value.Sources);
		Assert.Equal(HttpUserIdentifierSource.Claim, optionsAccessor.Value.Sources[0]);
	}

	[Fact]
	public void AddHttpUserAccessor_Should_NotConflict_When_MultipleKeyTypesRegistered() {
		var services = new ServiceCollection();

		services.AddHttpUserAccessor<string>();
		services.AddHttpUserAccessor<Guid>();
		var provider = services.BuildServiceProvider();

		var stringAccessor = provider.GetRequiredService<IUserAccessor<string>>();
		var guidAccessor = provider.GetRequiredService<IUserAccessor<Guid>>();

		Assert.NotNull(stringAccessor);
		Assert.NotNull(guidAccessor);
		Assert.IsType<HttpUserAccessor<string>>(stringAccessor);
		Assert.IsType<HttpUserAccessor<Guid>>(guidAccessor);
	}

	#endregion

	#region Builder Extension

	[Fact]
	public void WithHttpUserAccessor_Should_RegisterService() {
		var services = new ServiceCollection();
		var builder = new RepositoryContextBuilder(services);

		builder.WithHttpUserAccessor<string>();
		var provider = services.BuildServiceProvider();
		var accessor = provider.GetService<IUserAccessor<string>>();

		Assert.NotNull(accessor);
		Assert.IsType<HttpUserAccessor<string>>(accessor);
	}

	[Fact]
	public void WithHttpUserAccessor_Should_ReturnSameBuilder() {
		var services = new ServiceCollection();
		var builder = new RepositoryContextBuilder(services);

		var result = builder.WithHttpUserAccessor<string>();

		Assert.Same(builder, result);
	}

	[Fact]
	public void WithHttpUserAccessor_WithConfigure_Should_ApplyOptions() {
		var services = new ServiceCollection();
		var builder = new RepositoryContextBuilder(services);

		builder.WithHttpUserAccessor<string>(options => {
			options.RouteParameter = "tenantId";
			options.Sources = new List<HttpUserIdentifierSource> {
				HttpUserIdentifierSource.Route
			};
		});
		var provider = services.BuildServiceProvider();
		var optionsAccessor = provider.GetRequiredService<IOptions<HttpUserAccessorOptions>>();

		Assert.Equal("tenantId", optionsAccessor.Value.RouteParameter);
		Assert.Single(optionsAccessor.Value.Sources);
		Assert.Equal(HttpUserIdentifierSource.Route, optionsAccessor.Value.Sources[0]);
	}

	#endregion
}
