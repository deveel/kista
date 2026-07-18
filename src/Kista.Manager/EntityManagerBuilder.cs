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

using Kista.Caching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kista {
	/// <summary>
	/// A fluent builder for configuring entity-specific services
	/// (validators, cache key generators, error factories, caching)
	/// for a single entity type.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Created by <see cref="RepositoryBuilderExtensions.WithManagement(RepositoryBuilder, Action{EntityManagerBuilder}, ServiceLifetime)"/>
	/// and scoped to the entity and key types of the repository being configured.
	/// </para>
	/// </remarks>
	public class EntityManagerBuilder {
		private readonly RepositoryBuilder _repoBuilder;
		private readonly ServiceLifetime _lifetime;

		/// <summary>
		/// Constructs the builder with the given repository builder and lifetime.
		/// </summary>
		/// <param name="repoBuilder"></param>
		/// <param name="lifetime"></param>
		internal EntityManagerBuilder(RepositoryBuilder repoBuilder, ServiceLifetime lifetime) {
			_repoBuilder = repoBuilder;
			_lifetime = lifetime;
		}

		/// <summary>
		/// Gets the underlying service collection for direct registration.
		/// </summary>
		public IServiceCollection Services => _repoBuilder.Services;

		/// <summary>
		/// Gets the entity type managed by the repository.
		/// </summary>
		public Type EntityType => _repoBuilder.EntityType;

		/// <summary>
		/// Gets the entity key type managed by the repository.
		/// </summary>
		public Type EntityKeyType => _repoBuilder.EntityKeyType;

		/// <summary>
		/// Registers a validator type by scanning its implemented
		/// <see cref="IEntityValidator{TEntity}"/> and <see cref="IEntityValidator{TEntity, TKey}"/>
		/// interfaces, filtering for those matching the current entity and key types.
		/// </summary>
		/// <typeparam name="TValidator">The type of the validator to register.</typeparam>
		/// <returns>This builder for chaining.</returns>
		public EntityManagerBuilder WithValidator<TValidator>()
			where TValidator : class {
			var validatorType = typeof(TValidator);

			if (!validatorType.IsClass || validatorType.IsAbstract)
				throw new ArgumentException($"The type {validatorType} is not a concrete class");

			var interfaceTypes = validatorType.GetInterfaces().Where(x => x.IsGenericType);
			foreach (var interfaceType in interfaceTypes) {

				var genericDef = interfaceType.GetGenericTypeDefinition();

				if (genericDef == typeof(IEntityValidator<>)) {
					var entityType = interfaceType.GetGenericArguments()[0];
					if (entityType == EntityType) {
						var compareType = typeof(IEntityValidator<>).MakeGenericType(entityType);
						Services.TryAdd(new ServiceDescriptor(compareType, validatorType, _lifetime));
					}
				} else if (genericDef == typeof(IEntityValidator<,>)) {
					var args = interfaceType.GetGenericArguments();
					if (args[0] == EntityType) {
						var compareType = typeof(IEntityValidator<,>).MakeGenericType(args[0], args[1]);
						Services.TryAdd(new ServiceDescriptor(compareType, validatorType, _lifetime));
					}
				}
			}

			Services.Add(new ServiceDescriptor(validatorType, validatorType, _lifetime));
			return this;
		}

		/// <summary>
		/// Registers a cache key generator type by scanning its implemented
		/// <see cref="IEntityCacheKeyGenerator{TEntity}"/> interfaces,
		/// filtering for those matching the current entity type.
		/// </summary>
		/// <typeparam name="TGenerator">The type of the key generator to register.</typeparam>
		/// <returns>This builder for chaining.</returns>
		public EntityManagerBuilder WithCacheKeyGenerator<TGenerator>()
			where TGenerator : class {
			var generatorType = typeof(TGenerator);

			if (!generatorType.IsClass || generatorType.IsAbstract)
				throw new ArgumentException($"The type {generatorType} is not a concrete class");

			var entityTypes = generatorType.GetInterfaces()
				.Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEntityCacheKeyGenerator<>))
				.Select(x => x.GetGenericArguments()[0]);

			foreach (var entityType in entityTypes.Where(entityType => entityType == EntityType)) {
				var compareType = typeof(IEntityCacheKeyGenerator<>).MakeGenericType(entityType);
				Services.TryAdd(new ServiceDescriptor(compareType, generatorType, _lifetime));
			}

			Services.Add(new ServiceDescriptor(generatorType, generatorType, _lifetime));
			return this;
		}

		/// <summary>
		/// Registers an interceptor type by scanning its implemented
		/// <see cref="IEntityManagerInterceptor{TEntity}"/> and
		/// <see cref="IEntityManagerInterceptor{TEntity, TKey}"/>
		/// interfaces, filtering for those matching the current entity
		/// and key types.
		/// </summary>
		/// <typeparam name="TInterceptor">
		/// The type of the interceptor to register.
		/// </typeparam>
		/// <returns>This builder for chaining.</returns>
		public EntityManagerBuilder WithInterceptor<TInterceptor>()
			where TInterceptor : class {
			var interceptorType = typeof(TInterceptor);

			if (!interceptorType.IsClass || interceptorType.IsAbstract)
				throw new ArgumentException($"The type {interceptorType} is not a concrete class");

			var interfaceTypes = interceptorType.GetInterfaces().Where(x => x.IsGenericType);
			foreach (var interfaceType in interfaceTypes) {
				var genericDef = interfaceType.GetGenericTypeDefinition();

				if (genericDef == typeof(IEntityManagerInterceptor<>)) {
					var entityType = interfaceType.GetGenericArguments()[0];
					if (entityType == EntityType) {
						var compareType = typeof(IEntityManagerInterceptor<>).MakeGenericType(entityType);
						Services.TryAdd(new ServiceDescriptor(compareType, interceptorType, _lifetime));
					}
				} else if (genericDef == typeof(IEntityManagerInterceptor<,>)) {
					var args = interfaceType.GetGenericArguments();
					if (args[0] == EntityType && args[1] == EntityKeyType) {
						var compareType = typeof(IEntityManagerInterceptor<,>).MakeGenericType(args[0], args[1]);
						Services.TryAdd(new ServiceDescriptor(compareType, interceptorType, _lifetime));
					}
				}
			}

			Services.Add(new ServiceDescriptor(interceptorType, interceptorType, _lifetime));
			return this;
		}

		/// <summary>
		/// Registers a custom <see cref="EntityManager{TEntity}"/> or
		/// <see cref="EntityManager{TEntity, TKey}"/> subclass as the
		/// manager for the current entity (and key) type, replacing the
		/// default <see cref="EntityManager{TEntity}"/> registration.
		/// </summary>
		/// <typeparam name="TManager">
		/// The type of the custom manager to register. Must be a concrete
		/// class deriving from <see cref="EntityManager{TEntity}"/> or
		/// <see cref="EntityManager{TEntity, TKey}"/>.
		/// </typeparam>
		/// <returns>This builder for chaining.</returns>
		/// <exception cref="ArgumentException">
		/// Thrown when <typeparamref name="TManager"/> is not a concrete
		/// class or does not derive from a valid <see cref="EntityManager{TEntity}"/>.
		/// </exception>
		public EntityManagerBuilder UsingManager<TManager>()
			where TManager : class {
			var managerType = typeof(TManager);

			if (!managerType.IsClass || managerType.IsAbstract)
				throw new ArgumentException($"The type {managerType} is not a concrete class");

			var serviceTypes = CollectManagerServiceTypes(managerType);

			if (serviceTypes.Count == 0)
				throw new ArgumentException($"The type {managerType} is not a valid manager type for entity {EntityType}");

			if (!serviceTypes.Contains(managerType))
				serviceTypes.Add(managerType);

			RegisterServiceTypes(managerType, serviceTypes);

			return this;
		}

		private List<Type> CollectManagerServiceTypes(Type managerType) {
			var serviceTypes = new List<Type>();
			var baseType = managerType;
			while (baseType != null) {
				if (baseType.IsGenericType)
					CollectFromGenericType(baseType, serviceTypes);
				baseType = baseType.BaseType;
			}
			return serviceTypes;
		}

		private void CollectFromGenericType(Type baseType, List<Type> serviceTypes) {
			var genericType = baseType.GetGenericTypeDefinition();
			var genericArgs = baseType.GetGenericArguments();

			if (genericType == typeof(EntityManager<>) && genericArgs[0] == EntityType) {
				serviceTypes.Add(genericType.MakeGenericType(genericArgs[0]));
			} else if (genericType == typeof(EntityManager<,>)
				&& genericArgs[0] == EntityType && genericArgs[1] == EntityKeyType) {
				serviceTypes.Add(genericType.MakeGenericType(genericArgs[0], genericArgs[1]));
			}
		}

		private void RegisterServiceTypes(Type managerType, List<Type> serviceTypes) {
			foreach (var serviceType in serviceTypes) {
				if (serviceType == managerType) {
					Services.Add(new ServiceDescriptor(serviceType, managerType, _lifetime));
				} else {
					Services.Replace(new ServiceDescriptor(serviceType, managerType, _lifetime));
				}
			}
		}

		/// <summary>
		/// Registers an operation error factory for the current entity type.
		/// </summary>
		/// <typeparam name="TFactory">
		/// The type of the <see cref="IOperationErrorFactory"/> to register.
		/// </typeparam>
		/// <returns>This builder for chaining.</returns>
		public EntityManagerBuilder WithOperationErrorFactory<TFactory>()
			where TFactory : class, IOperationErrorFactory {
			Services.AddOperationErrorFactory(EntityType, typeof(TFactory));
			return this;
		}

		/// <summary>
		/// Registers soft-delete configuration for the entity manager.
		/// Soft-delete filtering activates automatically for any entity
		/// implementing <see cref="ISoftDeletable"/>: this call is
		/// reserved for future configuration knobs.
		/// </summary>
		/// <param name="configure">
		/// An optional delegate to configure the <see cref="SoftDeleteOptions"/>.
		/// </param>
		/// <returns>
		/// Returns the same builder for chaining.
		/// </returns>
		public EntityManagerBuilder WithSoftDelete(Action<SoftDeleteOptions>? configure = null) {
			var options = new SoftDeleteOptions();
			configure?.Invoke(options);
			Services.TryAddSingleton(options);
			return this;
		}
	}
}
