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

using System.ComponentModel;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Kista;

/// <summary>
/// An implementation of <see cref="IUserAccessor{TKey}"/> that resolves
/// the current user identifier from the HTTP context, using a configurable
/// chain of sources (claims, query string, route values).
/// </summary>
/// <typeparam name="TKey">
/// The type of the key used to identify the user. Must be convertible
/// from a string representation.
/// </typeparam>
public class HttpUserAccessor<TKey> : IUserAccessor<TKey> {
	private readonly IHttpContextAccessor httpContextAccessor;
	private readonly IOptions<HttpUserAccessorOptions> options;

	/// <summary>
	/// Constructs the accessor with the given HTTP context accessor
	/// and configuration options.
	/// </summary>
	/// <param name="httpContextAccessor">
	/// The accessor to the current HTTP context.
	/// </param>
	/// <param name="options">
	/// The configuration options specifying the sources and parameters.
	/// </param>
	public HttpUserAccessor(IHttpContextAccessor httpContextAccessor, IOptions<HttpUserAccessorOptions> options) {
		ArgumentNullException.ThrowIfNull(httpContextAccessor, nameof(httpContextAccessor));
		ArgumentNullException.ThrowIfNull(options, nameof(options));

		this.httpContextAccessor = httpContextAccessor;
		this.options = options;
	}

	/// <inheritdoc/>
	public TKey? GetUserId() {
		var httpContext = httpContextAccessor.HttpContext;
		if (httpContext == null)
			return default;

		var opts = options.Value;
		if (opts.Sources == null || opts.Sources.Count == 0)
			return default;

		foreach (var source in opts.Sources) {
			var value = ResolveFromSource(source, httpContext, opts);
			if (value != null) {
				var converted = ConvertValue(value);
				if (converted != null)
					return converted;
			}
		}

		return default;
	}

	private static string? ResolveFromSource(HttpUserIdentifierSource source, HttpContext httpContext, HttpUserAccessorOptions opts) {
		return source switch {
			HttpUserIdentifierSource.Claim => httpContext.User.FindFirst(opts.ClaimType)?.Value,
			HttpUserIdentifierSource.QueryString => httpContext.Request.Query[opts.QueryStringParameter].FirstOrDefault(),
			HttpUserIdentifierSource.Route => httpContext.Request.RouteValues[opts.RouteParameter]?.ToString(),
			_ => null
		};
	}

	private static TKey? ConvertValue(string value) {
		try {
			if (typeof(TKey) == typeof(string))
				return (TKey)(object)value;

			var converter = TypeDescriptor.GetConverter(typeof(TKey));
			return (TKey?)converter.ConvertFromInvariantString(value);
		} catch (NotSupportedException) {
			return default;
		} catch (Exception) {
			return default;
		}
	}
}
