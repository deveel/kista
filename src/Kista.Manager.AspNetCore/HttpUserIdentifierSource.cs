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
/// Defines the possible sources from which the user identifier
/// can be resolved in an HTTP request.
/// </summary>
public enum HttpUserIdentifierSource {
	/// <summary>
	/// The user identifier is resolved from a claim in the
	/// current <see cref="System.Security.Claims.ClaimsPrincipal"/>.
	/// </summary>
	Claim,

	/// <summary>
	/// The user identifier is resolved from a query string
	/// parameter in the HTTP request.
	/// </summary>
	QueryString,

	/// <summary>
	/// The user identifier is resolved from a route value
	/// in the HTTP request.
	/// </summary>
	Route
}
