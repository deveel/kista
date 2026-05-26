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

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kista {
	/// <summary>
	/// Extends a <see cref="IServiceCollection"/> to provide
	/// the registration of a <see cref="EntityRepository{TEntity}"/>
	/// </summary>
	public static class ServiceCollectionExtensions {
		/// <summary>
		/// Registers an Entity Framework repository for the given 
		/// type of entity in the service collection.
		/// </summary>
		/// <param name="services">
		/// The service collection to register the repository into.
		/// </param>
		/// <param name="entityType">
		/// The type of entity to register the repository for.
		/// </param>
		/// <param name="lifetime">
		/// The lifetime of the repository in the service collection.
		/// </param>
		/// <remarks>
		/// This method will register a <see cref="EntityRepository{TEntity}"/>
		/// built with the given <paramref name="entityType"/> as generic argument.
		/// </remarks>
		/// <returns>
		/// Returns the service collection with the repository registered.
		/// </returns>
		[Obsolete("Use AddRepositoryContext().UseEntityFramework<TDbContext>() instead.", false)]
		public static IServiceCollection AddEntityRepository(this IServiceCollection services, Type entityType, ServiceLifetime lifetime = ServiceLifetime.Scoped) {
			var repositoryType = typeof(EntityRepository<>).MakeGenericType(entityType);
			return services.AddRepository(repositoryType, lifetime);
		}

		/// <summary>
		/// Registers an Entity Framework repository for the given
		/// type of entity in the service collection.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity to register the repository for.
		/// </typeparam>
		/// <param name="services">
		/// The service collection to register the repository into.
		/// </param>
		/// <param name="lifetime">
		/// The lifetime of the repository in the service collection.
		/// </param>
		/// <returns>
		/// Returns the service collection with the repository registered.
		/// </returns>
		[Obsolete("Use AddRepositoryContext().UseEntityFramework<TDbContext>() instead.", false)]
		public static IServiceCollection AddEntityRepository<TEntity>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Scoped) where TEntity : class
			=> services.AddEntityRepository(typeof(TEntity), lifetime);
    }
}