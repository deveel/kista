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

using System.Reflection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Linq;

namespace Deveel.Data {
	/// <summary>
	/// Extensions for the <see cref="IServiceCollection"/> to register
	/// repositories and providers.
	/// </summary>
	public static partial class ServiceCollectionExtensions {
		/// <summary>
		/// Registers a repository of the given type in the service collection.
		/// </summary>
		/// <typeparam name="TRepository">
		/// The type of the repository to register.
		/// </typeparam>
		/// <param name="services">
		/// The service collection to register the repository.
		/// </param>
		/// <param name="lifetime">
		/// The lifetime of the repository in the service collection.
		/// </param>
		/// <returns>
		/// Returns the same <see cref="IServiceCollection"/> to allow chaining.
		/// </returns>
		/// <seealso cref="AddRepository(IServiceCollection, Type, ServiceLifetime)"/>
		[Obsolete("Use AddRepositoryContext().AddRepository<TRepository>() instead.", false)]
		public static IServiceCollection AddRepository<TRepository>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Scoped)
			=> services.AddRepository(typeof(TRepository), lifetime);

		/// <summary>
		/// Registers a repository of the given type in the service collection.
		/// </summary>
		/// <param name="services">
		/// The service collection to register the repository.
		/// </param>
		/// <param name="repositoryType">
		/// The type of the repository to register.
		/// </param>
		/// <param name="lifetime">
		/// the lifetime of the repository in the service collection.
		/// </param>
		/// <returns>
		/// Returns the same <see cref="IServiceCollection"/> to allow chaining.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when the given <paramref name="repositoryType"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// Thrown when the given <paramref name="repositoryType"/> is not
		/// a class or is abstract.
		/// </exception>
		/// <exception cref="RepositoryException">
		/// Thrown when the given <paramref name="repositoryType"/> is not a valid
		/// repository type.
		/// </exception>
		[Obsolete("Use AddRepositoryContext().AddRepository() instead.", false)]
		public static IServiceCollection AddRepository(this IServiceCollection services, Type repositoryType, ServiceLifetime lifetime = ServiceLifetime.Scoped) {
			ArgumentNullException.ThrowIfNull(repositoryType, nameof(repositoryType));

			if (!repositoryType.IsClass || repositoryType.IsAbstract)
				throw new ArgumentException($"The type '{repositoryType}' is not a valid repository type", nameof(repositoryType));

			if (!RepositoryRegistrationUtil.IsValidRepositoryType(repositoryType))
				throw new ArgumentException($"The type '{repositoryType}' is not a valid repository type", nameof(repositoryType));
				
			var serviceTypes = RepositoryRegistrationUtil.GetRepositoryServiceTypes(repositoryType);

			foreach (var serviceType in serviceTypes) {
				services.TryAdd(new ServiceDescriptor(serviceType, repositoryType, lifetime));
			}

			services.Add(new ServiceDescriptor(repositoryType, repositoryType, lifetime));

			return services;
		}

        /// <summary>
        /// Registers a repository controller of the given type in the service collection.
        /// </summary>
        /// <typeparam name="TController">
        /// The type of the repository controller to register.
        /// </typeparam>
        /// <param name="services">
        /// The service collection to register the controller.
        /// </param>
        /// <param name="configure">
        /// An optional action to configure the repository controller options.
        /// </param>
        /// <returns>
        /// Returns the same <see cref="IServiceCollection"/> to allow chaining.
        /// </returns>
        [Obsolete("Use AddRepositoryContext().ConfigureLifecycle() instead.", false)]
        public static IServiceCollection AddRepositoryController<TController>(this IServiceCollection services, Action<RepositoryControllerOptions>? configure = null)
            where TController : class, IRepositoryController {

            var options = services.AddOptions<RepositoryControllerOptions>();

            if (configure != null)
                options.Configure(configure);

            services.AddSingleton<IRepositoryController, TController>();
            services.AddSingleton<TController>();

            return services;
        }

        /// <summary>
        /// Registers the default repository controller in the service collection.
        /// </summary>
        /// <param name="services">
        /// The service collection to register the controller.
        /// </param>
        /// <param name="configure">
        /// An optional action to configure the repository controller options.
        /// </param>
        /// <returns>
        /// Returns the same <see cref="IServiceCollection"/> to allow chaining.
        /// </returns>
        [Obsolete("Use AddRepositoryContext().ConfigureLifecycle() instead.", false)]
        public static IServiceCollection AddRepositoryController(this IServiceCollection services, Action<RepositoryControllerOptions>? configure = null)
            => services.AddRepositoryController<DefaultRepositoryController>(configure);

		/// <summary>
		/// Registers a singleton <see cref="ISystemTime"/> service of the
		/// given <typeparamref name="TTime"/> type.
		/// </summary>
		/// <typeparam name="TTime">
		/// The type of the <see cref="ISystemTime"/> implementation.
		/// </typeparam>
		/// <param name="services">
		/// The <see cref="IServiceCollection"/> to add the service to.
		/// </param>
		/// <returns>
		/// Returns the <see cref="IServiceCollection"/> so that additional calls can be chained.
		/// </returns>
		public static IServiceCollection AddSystemTime<TTime>(this IServiceCollection services)
			where TTime : class, ISystemTime {
			services.TryAddSingleton<ISystemTime, TTime>();
			services.AddSingleton<TTime>();
			return services;
		}

		/// <summary>
		/// Registers a singleton instance of <see cref="ISystemTime"/> of the
		/// given <typeparamref name="TTime"/> type.
		/// </summary>
		/// <typeparam name="TTime">
		/// The type of the <see cref="ISystemTime"/> implementation.
		/// </typeparam>
		/// <param name="services">
		/// The <see cref="IServiceCollection"/> to add the service to.
		/// </param>
		/// <param name="time">
		/// The instance of <typeparamref name="TTime"/> to register.
		/// </param>
		/// <returns>
		/// Returns the <see cref="IServiceCollection"/> so that additional calls
		/// can be chained.
		/// </returns>
		public static IServiceCollection AddSystemTime<TTime>(this IServiceCollection services, TTime time)
			where TTime : class, ISystemTime {
			services.TryAddSingleton<ISystemTime>(time);
			services.AddSingleton(time);
			return services;
		}

		/// <summary>
		/// Registers the default <see cref="ISystemTime"/> service implementation
		/// </summary>
		/// <param name="services">
		/// The <see cref="IServiceCollection"/> to add the service to.
		/// </param>
		/// <returns>
		/// Returns the <see cref="IServiceCollection"/> so that additional calls can be chained.
		/// </returns>
		public static IServiceCollection AddSystemTime(this IServiceCollection services)
			=> services.AddSystemTime<SystemTime>();
    }
}