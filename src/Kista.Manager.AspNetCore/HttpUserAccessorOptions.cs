// Copyright 2023-2026 Antonello Provenzano
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Kista;

/// <summary>
/// Provides configuration options for the <see cref="HttpUserAccessor{TKey}"/>
/// service, defining the sources and parameters used to resolve
/// the current user identifier from an HTTP request.
/// </summary>
public class HttpUserAccessorOptions {
	/// <summary>
	/// Gets or sets the ordered list of sources to check when
	/// resolving the user identifier. The sources are checked
	/// in the order they appear in this list, and the first
	/// non-null value is returned.
	/// </summary>
	public IList<HttpUserIdentifierSource> Sources { get; set; } = new List<HttpUserIdentifierSource> {
		HttpUserIdentifierSource.Claim,
		HttpUserIdentifierSource.QueryString,
		HttpUserIdentifierSource.Route
	};

	/// <summary>
	/// Gets or sets the claim type to read when the source
	/// is <see cref="HttpUserIdentifierSource.Claim"/>.
	/// Defaults to <c>"sub"</c>.
	/// </summary>
	public string ClaimType { get; set; } = "sub";

	/// <summary>
	/// Gets or sets the query string parameter name to read
	/// when the source is <see cref="HttpUserIdentifierSource.QueryString"/>.
	/// Defaults to <c>"user_id"</c>.
	/// </summary>
	public string QueryStringParameter { get; set; } = "user_id";

	/// <summary>
	/// Gets or sets the route value key to read when the source
	/// is <see cref="HttpUserIdentifierSource.Route"/>.
	/// Defaults to <c>"userId"</c>.
	/// </summary>
	public string RouteParameter { get; set; } = "userId";
}
