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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using MongoFramework;

namespace Kista
{
	/// <summary>
	/// Extends the <see cref="IServiceCollection"/> to provide methods
	/// to register a <see cref="IMongoDbContext"/> in service collections.
	/// </summary>
	public static class ServiceCollectionExtensions
	{
		/// <summary>
		/// Adds a <see cref="IMongoDbContext"/> to the service collection
		/// using the given connection string.
		/// </summary>
		/// <typeparam name="TContext">
		/// The type of the context to register.
		/// </typeparam>
		/// <param name="services">
		/// The service collection to add the context to.
		/// </param>
		/// <param name="connectionString">
		/// The connection string to use for the MongoDB connection.
		/// </param>
		/// <param name="lifetime">
		/// The lifetime of the context in the service collection.
		/// </param>
		/// <returns>
		/// Returns the service collection for chaining.
		/// </returns>
		[Obsolete("Use AddRepositoryContext().UseMongoDB<TContext>() instead.", false)]
		public static IServiceCollection AddMongoDbContext<TContext>(this IServiceCollection services, string connectionString, ServiceLifetime lifetime = ServiceLifetime.Singleton)
			where TContext : class, IMongoDbContext
		{
			return AddMongoDbContext<TContext>(services, builder => builder.UseConnection(connectionString), lifetime);
		}

		/// <summary>
		/// Adds a <see cref="IMongoDbContext"/> to the service collection.
		/// </summary>
		/// <typeparam name="TContext">
		/// The type of the context to register.
		/// </typeparam>
		/// <param name="services">
		/// The service collection to add the context to.
		/// </param>
		/// <param name="connectionBuilder">
		/// A delegate to a method that builds the connection string.
		/// </param>
		/// <param name="lifetime">
		/// The lifetime of the context in the service collection.
		/// </param>
		/// <returns>
		/// Returns the service collection for chaining.
		/// </returns>
		[Obsolete("Use AddRepositoryContext().UseMongoDB<TContext>() instead.", false)]
		public static IServiceCollection AddMongoDbContext<TContext>(this IServiceCollection services, Action<MongoConnectionBuilder> connectionBuilder, ServiceLifetime lifetime = ServiceLifetime.Scoped)
			where TContext : class, IMongoDbContext
		{

			ArgumentNullException.ThrowIfNull(connectionBuilder, nameof(connectionBuilder));

			var builder = new MongoConnectionBuilder(typeof(TContext), services,lifetime);
			connectionBuilder(builder);

			services.TryAdd(new ServiceDescriptor(typeof(TContext), typeof(TContext), lifetime));
			services.TryAdd(new ServiceDescriptor(typeof(IMongoDbContext), typeof(TContext), lifetime));

			if (typeof(IMongoDbTenantContext).IsAssignableFrom(typeof(TContext)))
				services.TryAdd(new ServiceDescriptor(typeof(IMongoDbTenantContext), typeof(TContext), lifetime));

			if (typeof(MongoDbContext).IsAssignableFrom(typeof(TContext)) &&
				typeof(MongoDbContext) != typeof(TContext))
				services.TryAdd(new ServiceDescriptor(typeof(MongoDbContext), typeof(TContext), lifetime));

			if (typeof(TContext) != typeof(MongoDbContext))
				services.TryAdd(new ServiceDescriptor(typeof(TContext), typeof(TContext), lifetime));

			if (typeof(IMongoDbTenantContext).IsAssignableFrom(typeof(TContext)))
				services.TryAdd(new ServiceDescriptor(typeof(IMongoDbTenantContext), provider => provider.GetRequiredService<TContext>(), lifetime));

			if (typeof(MongoDbTenantContext).IsAssignableFrom(typeof(TContext)) &&
				typeof(MongoDbTenantContext) != typeof(TContext))
				services.TryAdd(new ServiceDescriptor(typeof(MongoDbTenantContext), provider => provider.GetRequiredService<TContext>(), lifetime));

			return services;
		}
	}
}
