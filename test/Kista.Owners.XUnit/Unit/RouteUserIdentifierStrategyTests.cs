using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
[Trait("Feature", "UserIdentifier")]
public class RouteUserIdentifierStrategyTests : HttpRequestUserIdentifierStrategyTestsBase {
	protected override string DefaultParameterName => "userId";

	protected override IUserIdentifierStrategy<TKey> CreateStrategy<TKey>(string parameterName)
		=> new RouteUserIdentifierStrategy<TKey>(parameterName);

	protected override IUserIdentifierStrategy<TKey> CreateDefaultStrategy<TKey>()
		=> new RouteUserIdentifierStrategy<TKey>();

	protected override void SetRequestValue(HttpRequest request, string parameterName, string value) {
		request.RouteValues.Returns(new RouteValueDictionary {
			{ parameterName, value }
		});
	}

	protected override void SetEmptyRequestValue(HttpRequest request)
		=> request.RouteValues.Returns(new RouteValueDictionary());
}