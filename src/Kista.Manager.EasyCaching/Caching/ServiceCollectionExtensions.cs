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

namespace Kista.Caching {
	/// <summary>
	/// Extends the <see cref="IServiceCollection"/> to provide methods
	/// to register the <see cref="EntityEasyCache{TEntity}"/> service.
	/// </summary>
	public static class ServiceCollectionExtensions {
		/// <summary>
		/// Registers a <see cref="EntityEasyCache{TEntity, TCached}"/> or
		/// <see cref="EntityEasyCache{TEntity}"/> service to the given 
		/// collection of services.
		/// </summary>
		/// <param name="services">
		/// The collection of services to register the cache.
		/// </param>
		/// <param name="cacheType">
		/// The type of the cache to register, that must be
		/// inherited from <see cref="EntityEasyCache{TEntity, TCached}"/> or
		/// <see cref="EntityEasyCache{TEntity}"/>.
		/// </param>
		/// <param name="lifetime">
		/// The desired lifetime of the cache.
		/// </param>
		/// <returns>
		/// Returns the given collection of services for chaining calls.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown when the given <paramref name="cacheType"/> is not a valid
		/// type for an EasyCaching cache.
		/// </exception>
		[Obsolete("Use AddRepositoryContext().WithEasyCaching() instead.", false)]
		public static IServiceCollection AddEntityEasyCache(this IServiceCollection services, Type cacheType, ServiceLifetime lifetime = ServiceLifetime.Singleton) {
			var baseClass = cacheType;
			bool isCache = false;

			while(baseClass != null) {
				if (baseClass.GetGenericArguments().Length == 2 &&
					baseClass.GetGenericTypeDefinition() == typeof(EntityEasyCache<,>)) {
					var entityType = baseClass.GetGenericArguments()[0];
					var entityCacheType = typeof(IEntityCache<>).MakeGenericType(entityType);

					services.TryAdd(new ServiceDescriptor(entityCacheType, cacheType, lifetime));
					services.TryAdd(new ServiceDescriptor(baseClass, cacheType, lifetime));
					isCache = true;
				} else if (
					baseClass.GetGenericArguments().Length == 1 &&
					baseClass.GetGenericTypeDefinition() == typeof(EntityEasyCache<>)) {
					var entityType = baseClass.GetGenericArguments()[0];
					var entityCacheType = typeof(IEntityCache<>).MakeGenericType(entityType);
					services.TryAdd(new ServiceDescriptor(baseClass, cacheType, lifetime));
					isCache = true;
				}

				if (baseClass == typeof(object) && !isCache)
					throw new ArgumentException($"The type {cacheType} is not a valid cache type");

				baseClass = baseClass.BaseType;
			}

			services.Add(new ServiceDescriptor(cacheType, cacheType, lifetime));
			return services;
		}

		/// <summary>
		/// Registers a <see cref="EntityEasyCache{TEntity, TCached}"/> or
		/// <see cref="EntityEasyCache{TEntity}"/> service to the given 
		/// collection of services.
		/// </summary>
		/// <typeparam name="TCache">
		/// The type of the cache to register, that must be
		/// inherited from <see cref="EntityEasyCache{TEntity, TCached}"/> or
		/// <see cref="EntityEasyCache{TEntity}"/>.
		/// </typeparam>
		/// <param name="services">
		/// The collection of services to register the cache.
		/// </param>
		/// <param name="lifetime">
		/// The desired lifetime of the cache.
		/// </param>
		/// <returns>
		/// Returns the given collection of services for chaining calls.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown when the given <typeparamref name="TCache"/> is not a valid
		/// type for an EasyCaching cache.
		/// </exception>
		[Obsolete("Use AddRepositoryContext().WithEasyCaching() instead.", false)]
		public static IServiceCollection AddEntityEasyCache<TCache>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Singleton)
			where TCache : class {
			return AddEntityEasyCache(services, typeof(TCache), lifetime);
		}

		/// <summary>
		/// Registers the default <see cref="EntityEasyCache{TEntity}"/>
		/// for the given entity type.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of the entity to cache.
		/// </typeparam>
		/// <param name="services">
		/// The collection of services to register the cache.
		/// </param>
		/// <param name="lifetime">
		/// The desired lifetime of the cache.
		/// </param>
		/// <returns>
		/// Returns the given collection of services for chaining calls.
		/// </returns>
		[Obsolete("Use AddRepositoryContext().WithEasyCaching() instead.", false)]
		public static IServiceCollection AddEntityEasyCacheFor<TEntity>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Singleton)
			where TEntity : class {
			return AddEntityEasyCache(services, typeof(EntityEasyCache<TEntity>), lifetime);
		}

		/// <summary>
		/// Registers the default <see cref="EntityEasyCache{TEntity}"/>
		/// for the given entity type.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of the entity to cache.
		/// </typeparam>
		/// <param name="services">
		/// The collection of services to register the cache.
		/// </param>
		/// <param name="configure">
		/// A function that configures the options of the cache.
		/// </param>
		/// <param name="lifetime">
		/// The desired lifetime of the cache.
		/// </param>
		/// <returns>
		/// Returns the given collection of services for chaining calls.
		/// </returns>
		[Obsolete("Use AddRepositoryContext().WithEasyCaching() instead.", false)]
		public static IServiceCollection AddEntityEasyCacheFor<TEntity>(this IServiceCollection services, Action<EntityCacheOptions> configure, ServiceLifetime lifetime = ServiceLifetime.Singleton)
			where TEntity : class {
			services.AddEntityCacheOptions<TEntity>(configure);
			return services.AddEntityEasyCacheFor<TEntity>(lifetime);
		}

		/// <summary>
		/// Registers the default <see cref="EntityEasyCache{TEntity}"/>
		/// for the given entity type.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of the entity to cache.
		/// </typeparam>
		/// <param name="services">
		/// The collection of services to register the cache.
		/// </param>
		/// <param name="configSectionPath">
		/// The path of the configuration section that contains the options
		/// to configure the cache.
		/// </param>
		/// <param name="lifetime">
		/// The desired lifetime of the cache.
		/// </param>
		/// <returns>
		/// Returns the given collection of services for chaining calls.
		/// </returns>
		[Obsolete("Use AddRepositoryContext().WithEasyCaching() instead.", false)]
		public static IServiceCollection AddEntityEasyCacheFor<TEntity>(this IServiceCollection services, string configSectionPath, ServiceLifetime lifetime = ServiceLifetime.Singleton)
			where TEntity : class {
			services.AddEntityCacheOptions<TEntity>(configSectionPath);
			return services.AddEntityEasyCacheFor<TEntity>(lifetime);
		}

		/// <summary>
		/// Registers a service that is used to convert an
		/// entity to a cached object and vice-versa.
		/// </summary>
		/// <param name="services">
		/// The collection of services to register the converter.
		/// </param>
		/// <param name="converterType">
		/// The type of the converter to register, that must be
		/// an implementation of <see cref="IEntityEasyCacheConverter{TEntity, TCached}"/>.
		/// </param>
		/// <param name="lifetime">
		/// The desired lifetime of the converter.
		/// </param>
		/// <returns>
		/// Returns the given collection of services for chaining calls.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown when the given <paramref name="converterType"/> is not a valid
		/// instance of <see cref="IEntityEasyCacheConverter{TEntity, TCached}"/>.
		/// </exception>
		[Obsolete("Use AddRepositoryContext().WithEasyCaching() instead.", false)]
		public static IServiceCollection AddEntityEasyCacheConverter(this IServiceCollection services, Type converterType,  ServiceLifetime lifetime = ServiceLifetime.Singleton) {
			if (!converterType.IsClass || converterType.IsAbstract)
				throw new ArgumentException($"The type {converterType} is not a valid converter type");

			var inheritance = converterType.GetInterfaces()
				.Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEntityEasyCacheConverter<,>));

			foreach (var @interface in inheritance) {
				var genericArguments = @interface.GetGenericArguments();
				var entityType = genericArguments[0];
				var cachedType = genericArguments[1];

				var contract = typeof(IEntityEasyCacheConverter<,>).MakeGenericType(entityType, cachedType);
				services.TryAdd(new ServiceDescriptor(contract, converterType, lifetime));
			}

			services.Add(new ServiceDescriptor(converterType, converterType, lifetime));

			return services;
		}

		/// <summary>
		/// Registers a service that is used to convert an
		/// entity to a cached object and vice-versa.
		/// </summary>
		/// <typeparam name="TConverter">
		/// The type of the converter to register, that must be
		/// an implementation of <see cref="IEntityEasyCacheConverter{TEntity, TCached}"/>.
		/// </typeparam>
		/// <param name="services">
		/// The collection of services to register the converter.
		/// </param>
		/// <param name="lifetime">
		/// The desired lifetime of the converter.
		/// </param>
		/// <returns>
		/// Returns the given collection of services for chaining calls.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown when the given <typeparamref name="TConverter"/> is not a valid
		/// instance of <see cref="IEntityEasyCacheConverter{TEntity, TCached}"/>.
		/// </exception>
		[Obsolete("Use AddRepositoryContext().WithEasyCaching() instead.", false)]
		public static IServiceCollection AddEntityEasyCacheConverter<TConverter>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Singleton)
			where TConverter : class {
			return AddEntityEasyCacheConverter(services, typeof(TConverter), lifetime);
		}
	}
}
