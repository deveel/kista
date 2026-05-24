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

using System.ComponentModel.DataAnnotations;

using Deveel.Data;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Deveel {
	/// <summary>
	/// Provides methods to register entity management services
	/// in a collection of services.
	/// </summary>
	public static class ServiceCollectionExtensions {
		/// <summary>
		/// Registers a <see cref="IOperationErrorFactory"/> service in the
		/// collection of services.
		/// </summary>
		/// <param name="services">
		/// The collection of services to register the factory.
		/// </param>
		/// <param name="entityType">
		/// The type of the entity for which the factory is registered.
		/// </param>
		/// <param name="factoryType">
		/// The type of the operation error factory to register.
		/// </param>
		/// <returns>
		/// Returns the given collection of services for chaining calls.
		/// </returns>
		public static IServiceCollection AddOperationErrorFactory(this IServiceCollection services, Type entityType, Type factoryType) {
			if (!typeof(IOperationErrorFactory).IsAssignableFrom(factoryType))
				throw new ArgumentException($"The given type '{factoryType}' is not assignable to '{typeof(IOperationErrorFactory)}'");
			if (!factoryType.IsClass || factoryType.IsAbstract)
				throw new ArgumentException($"The given type '{factoryType}' is not a concrete class");

			var serviceType = typeof(IOperationErrorFactory<>).MakeGenericType(entityType);

			services.TryAdd(ServiceDescriptor.Singleton(serviceType, sp => { 
				var decoratorType = typeof(OperationErrorFactoryDecorator<>).MakeGenericType(entityType);
				var instance = sp.GetRequiredService(factoryType); 
				return Activator.CreateInstance(decoratorType, instance)!;
			}));
			services.Add(ServiceDescriptor.Singleton(factoryType, factoryType));
			return services;
		}

		/// <summary>
		/// Registers a <see cref="IOperationErrorFactory"/> service in the
		/// collection of services.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of the entity for which the factory is registered.
		/// </typeparam>
		/// <typeparam name="TFactory">
		/// The type of the operation error factory to register.
		/// </typeparam>
		/// <param name="services">
		/// The collection of services to register the factory.
		/// </param>
		/// <returns>
		/// Returns the given collection of services for chaining calls.
		/// </returns>
		public static IServiceCollection AddOperationErrorFactory<TEntity, TFactory>(this IServiceCollection services)
			where TEntity : class
			where TFactory : class, IOperationErrorFactory {
			return services.AddOperationErrorFactory(typeof(TEntity), typeof(TFactory));
		}

		/// <summary>
		/// Registers the operation cancellation source in the 
		/// collection of services.
		/// </summary>
		/// <typeparam name="TSource">
		/// The type of the operation cancellation source to register.
		/// </typeparam>
		/// <param name="services">
		/// The collection of services to register the source.
		/// </param>
		/// <param name="lifetime">
		/// The desired lifetime of the cancellation source.
		/// </param>
		/// <returns>
		/// Returns the given collection of services for chaining calls.
		/// </returns>
		public static IServiceCollection AddOperationTokenSource<TSource>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Singleton)
			where TSource : class, IOperationCancellationSource {
			services.TryAdd(new ServiceDescriptor(typeof(IOperationCancellationSource), typeof(TSource), lifetime));
			services.Add(new ServiceDescriptor(typeof(TSource), typeof(TSource), lifetime));

			return services;
		}
        
		class OperationErrorFactoryDecorator<TEntity> : IOperationErrorFactory<TEntity> where TEntity : class {
			private readonly OperationErrorFactory errorFactory;

			public OperationErrorFactoryDecorator(OperationErrorFactory errorFactory) {
				this.errorFactory = errorFactory;
			}

			/// <inheritdoc/>
			public IOperationError CreateError(string errorCode, string domain, string? message = null) 
				=> errorFactory.CreateError(errorCode, domain, message);

			/// <inheritdoc/>
			public IOperationError CreateError(Exception exception) 
				=> errorFactory.CreateError(exception);

			/// <inheritdoc/>
			public IValidationError CreateValidationError(string errorCode, string domain, IReadOnlyList<ValidationResult> validationResults) 
				=> errorFactory.CreateValidationError(errorCode, domain, validationResults);
		}
	}
}
