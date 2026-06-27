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
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kista
{
	/// <summary>
	/// Provides extension methods for the <see cref="IServiceCollection"/> interface
	/// to register services for handling HTTP request cancellation.
	/// </summary>
	public static class ServiceCollectionExtensions
	{
		/// <summary>
		/// Registers a singleton instance of the <see cref="HttpRequestCancellationSource"/> 
		/// in the collection of services.
		/// </summary>
		/// <param name="services">
		/// The collection of services to register the source.
		/// </param>
		/// <remarks>
		/// This method also tries to register the <see cref="IHttpContextAccessor"/>
		/// into the collection of services, if not already registered.
		/// </remarks>
		/// <returns>
		/// Returns the given collection of services for chaining calls.
		/// </returns>
		public static IServiceCollection AddHttpRequestTokenSource(this IServiceCollection services) {
			services.AddHttpContextAccessor();
			services.AddOperationTokenSource<HttpRequestCancellationSource>(ServiceLifetime.Singleton);

			return services;
		}
	}
}