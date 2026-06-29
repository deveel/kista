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

namespace Kista
{
	/// <summary>
	/// Extension methods for <see cref="RepositoryBuilder"/> to configure
	/// seed data providers scoped to a specific repository registration.
	/// </summary>
	public static class RepositoryBuilderSeedExtensions
	{
		/// <summary>
		/// Registers a seed data provider for the entity type associated with this builder.
		/// </summary>
		/// <typeparam name="TProvider">
		/// The type implementing <see cref="IRepositorySeedDataProvider{TEntity}"/>.
		/// </typeparam>
		/// <param name="builder">The repository builder to configure.</param>
		/// <param name="lifetime">The service lifetime (default: <see cref="ServiceLifetime.Singleton"/>).</param>
		/// <returns>The same builder for chaining.</returns>
		public static RepositoryBuilder WithSeedData<TProvider>(this RepositoryBuilder builder, ServiceLifetime lifetime = ServiceLifetime.Singleton)
			where TProvider : class
		{
			var providerType = typeof(IRepositorySeedDataProvider<>).MakeGenericType(builder.EntityType);
			builder.Services.Add(ServiceDescriptor.Describe(providerType, typeof(TProvider), lifetime));
			return builder;
		}

		/// <summary>
		/// Registers inline seed data for the entity type associated with this builder.
		/// </summary>
		/// <typeparam name="TEntity">The type of the entity to seed.</typeparam>
		/// <param name="builder">The repository builder to configure.</param>
		/// <param name="data">The seed data to register.</param>
		/// <returns>The same builder for chaining.</returns>
		public static RepositoryBuilder WithSeedData<TEntity>(this RepositoryBuilder builder, IEnumerable<TEntity> data)
			where TEntity : class
		{
			builder.Services.AddSingleton<IRepositorySeedDataProvider<TEntity>>(
				new RepositoryContextBuilder.CollectionSeedDataProvider<TEntity>(data));
			return builder;
		}
	}
}
