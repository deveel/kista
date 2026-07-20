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

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Kista
{
	/// <summary>
	/// A user identifier strategy that resolves the user identifier from a route value
	/// in the HTTP request.
	/// </summary>
	/// <typeparam name="TKey">The type of the user identifier key.</typeparam>
	public class RouteUserIdentifierStrategy<TKey> : IUserIdentifierStrategy<TKey>
	{
		private readonly string key;

		/// <summary>
		/// Initializes a new instance with the specified route value key.
		/// </summary>
		/// <param name="key">The route value key. Defaults to "userId".</param>
		public RouteUserIdentifierStrategy(string key = "userId")
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(key);
			this.key = key;
		}

		/// <inheritdoc/>
		public TKey? GetUserId(IServiceProvider? serviceProvider = null)
		{
			if (serviceProvider == null)
				return default;

			var httpContextAccessor = serviceProvider.GetService<IHttpContextAccessor>();
			if (httpContextAccessor?.HttpContext == null)
				return default;

			var value = httpContextAccessor.HttpContext.Request.RouteValues[key]?.ToString();
			if (value == null)
				return default;

			return UserIdentifierConverter.Convert<TKey>(value);
		}
	}
}
