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
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kista {
	/// <summary>
	/// Base class for repository driver builders that provides common functionality
	/// for lifecycle management and service registration.
	/// </summary>
	/// <remarks>
	/// This base class extracts the common patterns shared by
	/// <see cref="EntityFrameworkRepositoryBuilder"/>, <see cref="MongoRepositoryBuilder"/>,
	/// and <see cref="InMemoryRepositoryBuilder"/> to reduce code duplication.
	/// </remarks>
	public abstract class RepositoryDriverBuilder {
		/// <summary>
		/// Gets the parent <see cref="RepositoryContextBuilder"/> that this builder is configured through.
		/// </summary>
		protected RepositoryContextBuilder Parent { get; }

		/// <summary>
		/// Gets the underlying service collection.
		/// </summary>
		public IServiceCollection Services => Parent.Services;

		/// <summary>
		/// Gets or sets whether lifecycle support is enabled.
		/// </summary>
		protected bool EnableLifecycle { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="RepositoryDriverBuilder"/> class.
		/// </summary>
		/// <param name="parent">The parent repository context builder.</param>
		/// <param name="enableLifecycle">Whether lifecycle support is enabled by default.</param>
		protected RepositoryDriverBuilder(RepositoryContextBuilder parent, bool enableLifecycle = true) {
			Parent = parent ?? throw new ArgumentNullException(nameof(parent));
			EnableLifecycle = enableLifecycle;
		}

		/// <summary>
		/// Enables lifecycle support for the repository driver.
		/// </summary>
		/// <returns>The current builder instance for method chaining.</returns>
		public virtual TBuilder WithLifecycle<TBuilder>() where TBuilder : RepositoryDriverBuilder {
			EnableLifecycle = true;
			return (TBuilder)this;
		}

		/// <summary>
		/// Disables lifecycle support for the repository driver.
		/// </summary>
		/// <returns>The current builder instance for method chaining.</returns>
		public virtual TBuilder WithoutLifecycle<TBuilder>() where TBuilder : RepositoryDriverBuilder {
			EnableLifecycle = false;
			return (TBuilder)this;
		}

		/// <summary>
		/// Registers the lifecycle handler for the repository driver.
		/// </summary>
		/// <param name="lifecycleHandlerType">The open generic lifecycle handler type.</param>
		protected void RegisterLifecycleHandler(Type lifecycleHandlerType) {
			if (EnableLifecycle) {
				Parent.Services.TryAddTransient(typeof(IRepositoryLifecycleHandler<>), lifecycleHandlerType);
			}
		}
	}
}