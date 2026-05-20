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

using Deveel.Data;
using Deveel.Data.Caching;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Deveel.Data {
	/// <summary>
	/// Extends the <see cref="IServiceCollection"/> to provide methods
	/// to register the <see cref="EntityManager{TEntity}"/> service.
	/// </summary>
	public static class ServiceCollectionExtensions {
		/// <summary>
		/// Registers a <see cref="EntityManager{TEntity}"/> service
		/// in the given collection of services.
		/// </summary>
		/// <typeparam name="TManager">
		/// The type of the <see cref="EntityManager{TEntity}"/> to register.
		/// </typeparam>
		/// <param name="services">
		/// The collection of services to register the manager.
		/// </param>
		/// <param name="lifetime">
		/// The desired lifetime of the manager.
		/// </param>
		/// <returns>
		/// Returns the given collection of services for chaining calls.
		/// </returns>
		/// <seealso cref="AddEntityManager(IServiceCollection, Type, ServiceLifetime)"/>
        [Obsolete("Use AddRepositoryContext().WithManagement() instead.", false)]
        public static IServiceCollection AddEntityManager<TManager>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where TManager : class
            => AddEntityManager(services, typeof(TManager), lifetime);

		/// <summary>
		/// Adds and instance of <see cref="EntityManager{TEntity}"/> for the given
		/// entity type in the collection of services.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of the entity to manage.
		/// </typeparam>
		/// <param name="services">
		/// The collection of services to register the manager.
		/// </param>
		/// <param name="lifetime">
		/// The desired lifetime of the manager.
		/// </param>
		/// <returns>
		/// Returns the given collection of services for chaining calls.
		/// </returns>
		[Obsolete("Use AddRepositoryContext().WithManagement() instead.", false)]
		public static IServiceCollection AddManagerFor<TEntity>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Scoped)
			where TEntity : class
			=> AddEntityManager(services, typeof(EntityManager<TEntity>), lifetime);

		/// <summary>
		/// Registers a <see cref="EntityManager{TEntity}"/> service
		/// in the given collection of services.
		/// </summary>
		/// <param name="services">
		/// The collection of services to register the manager.
		/// </param>
		/// <param name="managerType">
		/// The type of the <see cref="EntityManager{TEntity}"/> to register.
		/// </param>
		/// <param name="lifetime">
		/// The desired lifetime of the manager.
		/// </param>
		/// <remarks>
		/// This method iterates through the base types of the given <paramref name="managerType"/>
		/// to find the <see cref="EntityManager{TEntity}"/> instances from
		/// which the service derives, and registers the service for each of them.
		/// </remarks>
		/// <returns>
		/// Returns the given collection of services for chaining calls.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown when the given <paramref name="managerType"/> is not a concrete class,
		/// or if the type is not a valid <see cref="EntityManager{TEntity}"/>.
		/// </exception>
        [Obsolete("Use AddRepositoryContext().WithManagement() instead.", false)]
        public static IServiceCollection AddEntityManager(this IServiceCollection services, Type managerType, ServiceLifetime lifetime = ServiceLifetime.Scoped) {
            ArgumentNullException.ThrowIfNull(managerType, nameof(managerType));

			if (!managerType.IsClass || managerType.IsAbstract)
				throw new ArgumentException($"The type {managerType} is not a concrete class", nameof(managerType));

			var serviceTypes = new List<Type>();

			var baseType = managerType;
			while(baseType != null) {
				if (baseType.IsGenericType) {
					var genericType = baseType.GetGenericTypeDefinition();
					var genericArgs = baseType.GetGenericArguments();
					var entityType = genericArgs[0];

					if (genericType == typeof(EntityManager<>)) {
						var compareType = genericType.MakeGenericType(entityType);

						serviceTypes.Add(compareType);
					} else if (genericType == typeof(EntityManager<,>)) {
						var keyType = genericArgs[1];

						var compareType = genericType.MakeGenericType(entityType, keyType);

						serviceTypes.Add(compareType);
					}
				}

				baseType = baseType.BaseType;
			}

			if (serviceTypes.Count == 0)
				throw new ArgumentException($"The type {managerType} is not a valid manager type", nameof(managerType));

			if (!serviceTypes.Contains(managerType))
				serviceTypes.Add(managerType);

			foreach (var serviceType in serviceTypes) {
				if (serviceType == managerType) {
					services.Add(ServiceDescriptor.Describe(serviceType, managerType, lifetime));
				} else {
					services.TryAdd(ServiceDescriptor.Describe(serviceType, managerType, lifetime));
				}
			}

			return services;
        }

		/// <summary>
		/// Adds a set of configuration options for a
		/// cache of entities of the given type.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of the entity to cache.
		/// </typeparam>
		/// <param name="services">
		/// The collection of services to register the options.
		/// </param>
		/// <param name="configure">
		/// A delegate to a method that configures the options.
		/// </param>
		/// <returns>
		/// Returns the given collection of services for chaining calls.
		/// </returns>
		[Obsolete("Use AddRepositoryContext().WithManagement() instead.", false)]
		public static IServiceCollection AddEntityCacheOptions<TEntity>(this IServiceCollection services, Action<EntityCacheOptions> configure) where TEntity : class {
			ArgumentNullException.ThrowIfNull(configure, nameof(configure));

			services.AddOptions<EntityCacheOptions<TEntity>>()
				.Configure(options => {
					configure(options);
				});

			return services;
		}

		/// <summary>
		/// Adds a set of configuration options for a
		/// cache of entities of the given type.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of the entity to cache.
		/// </typeparam>
		/// <param name="services">
		/// The collection of services to register the options.
		/// </param>
		/// <param name="configSectionPath">
		/// The path to the configuration section that contains
		/// the options to bind.
		/// </param>
		/// <returns>
		/// Returns the given collection of services for chaining calls.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when the given <paramref name="configSectionPath"/> is <c>null</c>.
		/// </exception>
		[Obsolete("Use AddRepositoryContext().WithManagement() instead.", false)]
		public static IServiceCollection AddEntityCacheOptions<TEntity>(this IServiceCollection services, string configSectionPath) where TEntity : class {
			ArgumentNullException.ThrowIfNull(configSectionPath, nameof(configSectionPath));

			services.AddOptions<EntityCacheOptions<TEntity>>()
				.BindConfiguration(configSectionPath);

			return services;
		}

		/// <summary>
		/// Registers a service to generate cache keys for entities
		/// of a certain type.
		/// </summary>
		/// <param name="services">
		/// The collection of services to register the generator.
		/// </param>
		/// <param name="generatorType">
		/// The type of the generator to register.
		/// </param>
		/// <param name="lifetime">
		/// The desired lifetime of the generator.
		/// </param>
		/// <returns>
		/// Returns the given collection of services for chaining calls.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown when the given <paramref name="generatorType"/> is not a valid
		/// implementation of <see cref="IEntityCacheKeyGenerator{TEntity}"/>.
		/// </exception>
		[Obsolete("Use AddRepositoryContext().WithManagement() instead.", false)]
		public static IServiceCollection AddEntityCacheKeyGenerator(this IServiceCollection services, Type generatorType, ServiceLifetime lifetime = ServiceLifetime.Singleton) {
			if (!generatorType.IsClass || generatorType.IsAbstract)
				throw new ArgumentException($"The type {generatorType} is not a valid generator type");

			var inheritance = generatorType.GetInterfaces()
				.Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEntityCacheKeyGenerator<>));

			foreach (var interfaceType in inheritance) {
				var entityType = interfaceType.GetGenericArguments()[0];
				var compareType = typeof(IEntityCacheKeyGenerator<>).MakeGenericType(entityType);

				services.TryAdd(new ServiceDescriptor(compareType, generatorType, lifetime));
			}

			services.Add(new ServiceDescriptor(generatorType, generatorType, lifetime));

			return services;
		}

		/// <summary>
		/// Registers a service to generate cache keys for entities
		/// of a certain type.
		/// </summary>
		/// <typeparam name="TGenerator">
		/// The type of the generator to register.
		/// </typeparam>
		/// <param name="services">
		/// The collection of services to register the generator.
		/// </param>
		/// <param name="lifetime">
		/// The desired lifetime of the generator.
		/// </param>
		/// <returns>
		/// Returns the given collection of services for chaining calls.
		/// </returns>
		[Obsolete("Use AddRepositoryContext().WithManagement() instead.", false)]
		public static IServiceCollection AddEntityCacheKeyGenerator<TGenerator>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Singleton)
			where TGenerator : class {
			return AddEntityCacheKeyGenerator(services, typeof(TGenerator), lifetime);
		}

		/// <summary>
		/// Registers an entity validation service in the collection of services.
		/// </summary>
		/// <typeparam name="TValidator">
		/// The type of the entity validator to register.
		/// </typeparam>
		/// <param name="services">
		/// The collection of services to register the validator.
		/// </param>
		/// <param name="lifetime">
		/// The desired lifetime of the validator.
		/// </param>
		/// <returns>
		/// Returns the given collection of services for chaining calls.
		/// </returns>
		/// <seealso cref="AddEntityValidator(IServiceCollection, Type, ServiceLifetime)"/>
		[Obsolete("Use AddRepositoryContext().WithManagement() instead.", false)]
		public static IServiceCollection AddEntityValidator<TValidator>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Transient)
			where TValidator : class 
			=> AddEntityValidator(services, typeof(TValidator), lifetime);

		/// <summary>
		/// Adds an entity validator service in the collection of services.
		/// </summary>
		/// <param name="services">
		/// The collection of services to register the validator.
		/// </param>
		/// <param name="validatorType">
		/// The type of the validator to register.
		/// </param>
		/// <param name="lifetime">
		/// The desired lifetime of the validator service.
		/// </param>
		/// <returns>
		/// Returns the given collection of services for chaining calls.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown when the given type is not a concrete class.
		/// </exception>
		[Obsolete("Use AddRepositoryContext().WithManagement() instead.", false)]
		public static IServiceCollection AddEntityValidator(this IServiceCollection services, Type validatorType, ServiceLifetime lifetime = ServiceLifetime.Transient) {
			ArgumentNullException.ThrowIfNull(validatorType, nameof(validatorType));

			if (!validatorType.IsClass || validatorType.IsAbstract)
				throw new ArgumentException($"The type {validatorType} is not a concrete class", nameof(validatorType));

			var interfaceTypes = validatorType.GetInterfaces();
			foreach (var interfaceType in interfaceTypes) {
				if (interfaceType.GetGenericTypeDefinition() == typeof(IEntityValidator<>)) {
					var compareType = typeof(IEntityValidator<>).MakeGenericType(interfaceType.GetGenericArguments()[0]);

					services.TryAdd(new ServiceDescriptor(compareType, validatorType, lifetime));
				} else if (interfaceType.GetGenericTypeDefinition() == typeof(IEntityValidator<,>)) {
					var argTypes = interfaceType.GetGenericArguments();
					var compareType = typeof(IEntityValidator<,>).MakeGenericType(argTypes[0], argTypes[1]);

					services.TryAdd(new ServiceDescriptor(compareType, validatorType, lifetime));
				}
			}

			services.Add(new ServiceDescriptor(validatorType, validatorType, lifetime));

			return services;
		}
    }
}