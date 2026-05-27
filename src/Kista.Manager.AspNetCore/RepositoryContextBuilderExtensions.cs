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


using Microsoft.Extensions.DependencyInjection;

namespace Kista {
	/// <summary>
	/// Extension methods for configuring ASP.NET Core features on a <see cref="RepositoryContextBuilder"/>.
	/// </summary>
	public static class RepositoryContextBuilderExtensions {
		/// <summary>
		/// Enables HTTP request-based operation cancellation.
		/// Registers <see cref="HttpRequestCancellationSource"/> and <see cref="IHttpContextAccessor"/>.
		/// </summary>
		public static RepositoryContextBuilder WithHttpRequestCancellation(this RepositoryContextBuilder builder) {
			builder.Services.AddHttpContextAccessor();
			builder.Services.AddOperationTokenSource<HttpRequestCancellationSource>(ServiceLifetime.Singleton);
			return builder;
		}

		/// <summary>
		/// Registers an HTTP-based user accessor that resolves the current
		/// user identifier from the request (claims, query string, or route).
		/// </summary>
		/// <typeparam name="TKey">
		/// The type of the user identifier key.
		/// </typeparam>
		/// <param name="builder">
		/// The repository context builder to configure.
		/// </param>
		/// <param name="configure">
		/// An optional delegate to configure <see cref="HttpUserAccessorOptions"/>.
		/// </param>
		/// <returns>
		/// Returns the same builder instance for chaining.
		/// </returns>
		public static RepositoryContextBuilder WithHttpUserAccessor<TKey>(this RepositoryContextBuilder builder, Action<HttpUserAccessorOptions>? configure = null) {
			builder.Services.AddHttpUserAccessor<TKey>(configure);
			return builder;
		}
	}
}
