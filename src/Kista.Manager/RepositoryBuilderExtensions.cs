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
	/// Extension methods for configuring entity management on a <see cref="RepositoryBuilder"/>.
	/// </summary>
	public static class RepositoryBuilderExtensions {
		/// <summary>
		/// Registers an <see cref="EntityManager{TEntity}"/> for the entity type
		/// of this repository.
		/// </summary>
		/// <param name="builder">The repository builder.</param>
		/// <param name="lifetime">
		/// The service lifetime of the entity manager (default: Scoped).
		/// </param>
		/// <returns>The repository builder for chaining.</returns>
		public static RepositoryBuilder WithManagement(
			this RepositoryBuilder builder,
			ServiceLifetime lifetime = ServiceLifetime.Scoped) {
			RegisterEntityManager(builder, lifetime);
			return builder;
		}

		/// <summary>
		/// Registers an <see cref="EntityManager{TEntity}"/> for the entity type
		/// of this repository, and configures entity-specific services via the
		/// provided <paramref name="configure"/> callback.
		/// </summary>
		/// <param name="builder">The repository builder.</param>
		/// <param name="configure">
		/// A delegate that configures entity-specific services
		/// (validators, cache key generators, error factories, caching).
		/// </param>
		/// <param name="lifetime">
		/// The service lifetime of the entity manager (default: Scoped).
		/// </param>
		/// <returns>The repository builder for chaining.</returns>
		public static RepositoryBuilder WithManagement(
			this RepositoryBuilder builder,
			Action<EntityManagerBuilder> configure,
			ServiceLifetime lifetime = ServiceLifetime.Scoped) {
			var mgmtBuilder = new EntityManagerBuilder(builder, lifetime);
			configure(mgmtBuilder);
			RegisterEntityManager(builder, lifetime);
			return builder;
		}

		/// <summary>
		/// Registers the entity manager for the repository's entity type.
		/// </summary>
		private static void RegisterEntityManager(RepositoryBuilder builder, ServiceLifetime lifetime) {
			var entityType = builder.EntityType;
			var keyType = builder.EntityKeyType;

			if (keyType == typeof(object)) {
				var managerType = typeof(EntityManager<>).MakeGenericType(entityType);
				builder.Services.TryAdd(new ServiceDescriptor(managerType, managerType, lifetime));
			} else {
				var managerType = typeof(EntityManager<,>).MakeGenericType(entityType, keyType);
				builder.Services.TryAdd(new ServiceDescriptor(managerType, managerType, lifetime));
			}
		}
	}
}
