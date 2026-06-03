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
	/// Provides extension methods for registering filter cache services
	/// in an <see cref="IServiceCollection"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// These extensions simplify the registration of <see cref="IExpressionCache"/> and
	/// <see cref="IFilterCache"/> implementations in the dependency injection container.
	/// Caches are registered as singletons because they are designed to be shared across
	/// all repository operations and request lifetimes.
	/// </para>
	/// <para>
	/// The <see cref="AddFilterCache(IServiceCollection, int)"/> overload registers both
	/// the expression cache (for parsed <see cref="System.Linq.Expressions.LambdaExpression"/>
	/// objects) and the filter cache (for compiled <see cref="Delegate"/> instances) with
	/// the same capacity. For scenarios where different capacities are needed, register
	/// each cache type individually.
	/// </para>
	/// </remarks>
	/// <example>
	/// Register caches with default capacity (1024):
	/// <code>
	/// services.AddFilterCache();
	/// </code>
	/// Register caches with a custom capacity:
	/// <code>
	/// services.AddFilterCache(maxCapacity: 4096);
	/// </code>
	/// Register using a configuration action:
	/// <code>
	/// services.AddFilterCache(options => options.MaxCapacity = 8192);
	/// </code>
	/// Register a custom cache implementation:
	/// <code>
	/// services.AddFilterCache&lt;MyCustomFilterCache&gt;();
	/// services.AddExpressionCache&lt;MyCustomExpressionCache&gt;();
	/// </code>
	/// </example>
	/// <seealso cref="IExpressionCache"/>
	/// <seealso cref="IFilterCache"/>
	/// <seealso cref="BoundedExpressionCache"/>
	/// <seealso cref="BoundedFilterCache"/>
	public static class ServiceCollectionExtensions {
		/// <summary>
		/// Registers bounded LRU caches for both parsed expressions and compiled delegates
		/// as singleton services with the specified maximum capacity.
		/// </summary>
		/// <param name="services">
		/// The <see cref="IServiceCollection"/> to add the cache services to.
		/// </param>
		/// <param name="maxCapacity">
		/// The maximum number of entries each cache can hold. The default is 1024.
		/// Must be at least 1.
		/// </param>
		/// <returns>
		/// The same <see cref="IServiceCollection"/> instance so that additional calls can be chained.
		/// </returns>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Thrown when <paramref name="maxCapacity"/> is less than 1.
		/// </exception>
		/// <remarks>
		/// <para>
		/// This method registers both <see cref="IExpressionCache"/> and <see cref="IFilterCache"/>
		/// using <see cref="BoundedExpressionCache"/> and <see cref="BoundedFilterCache"/> respectively,
		/// each with the same <paramref name="maxCapacity"/>.
		/// </para>
		/// <para>
		/// Uses <c>TryAddSingleton</c> so that existing registrations are not overwritten, allowing consumers to
		/// register custom implementations before calling this method.
		/// </para>
		/// </remarks>
		public static IServiceCollection AddFilterCache(this IServiceCollection services, int maxCapacity = 1024) {
			services.TryAddSingleton<IExpressionCache>(new BoundedExpressionCache(maxCapacity));
			services.TryAddSingleton<IFilterCache>(new BoundedFilterCache(maxCapacity));
			return services;
		}

		/// <summary>
		/// Registers bounded LRU caches for both parsed expressions and compiled delegates
		/// as singleton services using the provided configuration action.
		/// </summary>
		/// <param name="services">
		/// The <see cref="IServiceCollection"/> to add the cache services to.
		/// </param>
		/// <param name="configure">
		/// An action that configures the <see cref="BoundedFilterCacheOptions"/> used to
		/// construct both cache instances.
		/// </param>
		/// <returns>
		/// The same <see cref="IServiceCollection"/> instance so that additional calls can be chained.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="configure"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Thrown when <paramref name="configure"/> sets <see cref="BoundedFilterCacheOptions.MaxCapacity"/>
		/// to a value less than 1.
		/// </exception>
		/// <remarks>
		/// Both <see cref="IExpressionCache"/> and <see cref="IFilterCache"/> are constructed
		/// from the same <see cref="BoundedFilterCacheOptions"/> instance, so they share
		/// the same capacity setting.
		/// </remarks>
		public static IServiceCollection AddFilterCache(this IServiceCollection services, Action<BoundedFilterCacheOptions> configure) {
			var options = new BoundedFilterCacheOptions();
			configure(options);
			services.TryAddSingleton<IExpressionCache>(new BoundedExpressionCache(options));
			services.TryAddSingleton<IFilterCache>(new BoundedFilterCache(options));
			return services;
		}

		/// <summary>
		/// Registers a custom <see cref="IExpressionCache"/> implementation as a singleton service.
		/// </summary>
		/// <typeparam name="TCache">
		/// The type of the expression cache implementation. Must implement <see cref="IExpressionCache"/>
		/// and be a reference type.
		/// </typeparam>
		/// <param name="services">
		/// The <see cref="IServiceCollection"/> to add the cache service to.
		/// </param>
		/// <returns>
		/// The same <see cref="IServiceCollection"/> instance so that additional calls can be chained.
		/// </returns>
		/// <remarks>
		/// Use this method when you need a custom eviction policy, distributed caching,
		/// or other specialized behavior not provided by <see cref="BoundedExpressionCache"/>.
		/// The implementation will be instantiated by the DI container as a singleton.
		/// </remarks>
		public static IServiceCollection AddExpressionCache<TCache>(this IServiceCollection services)
			where TCache : class, IExpressionCache {
			services.TryAddSingleton<IExpressionCache, TCache>();
			return services;
		}

		/// <summary>
		/// Registers a custom <see cref="IFilterCache"/> implementation as a singleton service.
		/// </summary>
		/// <typeparam name="TCache">
		/// The type of the filter cache implementation. Must implement <see cref="IFilterCache"/>
		/// and be a reference type.
		/// </typeparam>
		/// <param name="services">
		/// The <see cref="IServiceCollection"/> to add the cache service to.
		/// </param>
		/// <returns>
		/// The same <see cref="IServiceCollection"/> instance so that additional calls can be chained.
		/// </returns>
		/// <remarks>
		/// Use this method when you need a custom eviction policy, distributed caching,
		/// or other specialized behavior not provided by <see cref="BoundedFilterCache"/>.
		/// The implementation will be instantiated by the DI container as a singleton.
		/// </remarks>
		public static IServiceCollection AddFilterCache<TCache>(this IServiceCollection services)
			where TCache : class, IFilterCache {
			services.TryAddSingleton<IFilterCache, TCache>();
			return services;
		}
	}
}
