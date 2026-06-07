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
	/// Options for configuring entity management.
	/// </summary>
	public class ManagementOptions {
		/// <summary>
		/// Gets or sets whether to auto-register entity managers for all tracked entity types.
		/// </summary>
		public bool AutoRegisterManagers { get; set; } = true;
	}

	/// <summary>
	/// Extension methods for configuring entity management on a <see cref="RepositoryContextBuilder"/>.
	/// </summary>
	public static class RepositoryContextBuilderExtensions {
		/// <summary>
		/// Enables entity management for all tracked entity types.
		/// Auto-registers <see cref="EntityManager{TEntity}"/> for each entity type
		/// that has a repository registered.
		/// </summary>
		public static RepositoryContextBuilder WithManagement(
			this RepositoryContextBuilder builder,
			Action<ManagementOptions>? configure = null,
			ServiceLifetime lifetime = ServiceLifetime.Scoped) {
			var options = new ManagementOptions();
			configure?.Invoke(options);

			if (options.AutoRegisterManagers) {
				foreach (var entityType in builder.RegisteredEntityTypes) {
					var managerType = typeof(EntityManager<>).MakeGenericType(entityType);
					builder.Services.TryAdd(new ServiceDescriptor(managerType, managerType, lifetime));
				}
			}

			return builder;
		}
	}
}
