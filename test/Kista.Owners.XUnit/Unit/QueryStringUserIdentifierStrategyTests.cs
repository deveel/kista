using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
[Trait("Feature", "UserIdentifier")]
public class QueryStringUserIdentifierStrategyTests : HttpRequestUserIdentifierStrategyTestsBase {
	protected override string DefaultParameterName => "user_id";

	protected override IUserIdentifierStrategy<TKey> CreateStrategy<TKey>(string parameterName)
		=> new QueryStringUserIdentifierStrategy<TKey>(parameterName);

	protected override IUserIdentifierStrategy<TKey> CreateDefaultStrategy<TKey>()
		=> new QueryStringUserIdentifierStrategy<TKey>();

	protected override void SetRequestValue(HttpRequest request, string parameterName, string value) {
		request.Query.Returns(new QueryCollection(new Dictionary<string, StringValues> {
			{ parameterName, value }
		}));
	}

	protected override void SetEmptyRequestValue(HttpRequest request)
		=> request.Query.Returns(new QueryCollection());
}