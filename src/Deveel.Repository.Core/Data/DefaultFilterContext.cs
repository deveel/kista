// Copyright 2023-2025 Antonello Provenzano
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

namespace Deveel.Data {
	/// <summary>
	/// The default implementation of <see cref="IFilterContext"/> that wraps
	/// an <see cref="IServiceProvider"/>.
	/// </summary>
	/// <remarks>
	/// This class is used by repository implementations to provide filters
	/// with access to the dependency injection container. Filters can resolve
	/// services such as <see cref="IExpressionCache"/> through the context.
	/// </remarks>
	/// <seealso cref="IFilterContext"/>
	public sealed class DefaultFilterContext : IFilterContext {
		/// <summary>
		/// Initializes a new instance of the <see cref="DefaultFilterContext"/> class
		/// with the specified service provider.
		/// </summary>
		/// <param name="services">
		/// The service provider used to resolve infrastructure services.
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="services"/> is <c>null</c>.
		/// </exception>
		public DefaultFilterContext(IServiceProvider services) {
			Services = services ?? throw new ArgumentNullException(nameof(services));
		}

		/// <inheritdoc />
		public IServiceProvider Services { get; }
	}
}
