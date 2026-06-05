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

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Kista.Caching {
	/// <summary>
	/// Options for configuring <see cref="IDistributedCache"/>-based entity caching.
	/// </summary>
	public class DistributedCachingOptions {
		/// <summary>
		/// Gets or sets the default expiration time for cached entities.
		/// When set, all registered entity caches will use this expiration.
		/// </summary>
		public TimeSpan? DefaultExpiration { get; set; }

		/// <summary>
		/// Gets or sets a prefix to prepend to all cache keys.
		/// Useful for isolating caches across environments or applications.
		/// </summary>
		public string? CacheKeyPrefix { get; set; }
	}

	internal sealed class ConfiguredDistributedCacheOptions<TEntity> : IConfigureOptions<EntityCacheOptions<TEntity>>
		where TEntity : class {

		private readonly TimeSpan? _expiration;
		private readonly string? _prefix;

		public ConfiguredDistributedCacheOptions(TimeSpan? expiration, string? prefix) {
			_expiration = expiration;
			_prefix = prefix;
		}

		public void Configure(EntityCacheOptions<TEntity> options) {
			if (_expiration.HasValue)
				options.Expiration = _expiration;
			if (!string.IsNullOrEmpty(_prefix))
				options.CacheKeyPrefix = _prefix;
		}
	}

	/// <summary>
	/// Extension methods for configuring <see cref="IDistributedCache"/>-based caching
	/// on a <see cref="RepositoryContextBuilder"/> or <see cref="IServiceCollection"/>.
	/// </summary>
	public static class DistributedCacheServiceExtensions {
		/// <summary>
		/// Enables distributed caching for all tracked entity types.
		/// Registers <see cref="EntityDistributedCache{TEntity}"/> for each entity type
		/// that has a repository registered.
		/// </summary>
		/// <param name="builder">
		/// The repository context builder to configure.
		/// </param>
		/// <param name="configure">
		/// An optional delegate to configure the distributed caching options.
		/// </param>
		/// <param name="lifetime">
		/// The service lifetime for the cache registrations (default: Singleton).
		/// </param>
		/// <returns>
		/// Returns the builder for chaining.
		/// </returns>
		public static RepositoryContextBuilder WithDistributedCaching(
			this RepositoryContextBuilder builder,
			Action<DistributedCachingOptions>? configure = null,
			ServiceLifetime lifetime = ServiceLifetime.Singleton) {

			var options = new DistributedCachingOptions();
			configure?.Invoke(options);

			foreach (var entityType in builder.RegisteredEntityTypes) {
				var cacheType = typeof(EntityDistributedCache<>).MakeGenericType(entityType);
				var cacheInterface = typeof(IEntityCache<>).MakeGenericType(entityType);

				builder.Services.TryAdd(new ServiceDescriptor(cacheInterface, cacheType, lifetime));
				builder.Services.TryAdd(new ServiceDescriptor(cacheType, cacheType, lifetime));

				if (options.DefaultExpiration.HasValue || !string.IsNullOrEmpty(options.CacheKeyPrefix)) {
					var entityOptionsType = typeof(EntityCacheOptions<>).MakeGenericType(entityType);
					var expiration = options.DefaultExpiration;
					var prefix = options.CacheKeyPrefix;

					builder.Services.AddSingleton(typeof(IConfigureOptions<>).MakeGenericType(entityOptionsType), sp => {
						return Activator.CreateInstance(
							typeof(ConfiguredDistributedCacheOptions<>).MakeGenericType(entityType),
							expiration, prefix)!;
					});
				}
			}

			return builder;
		}

		/// <summary>
		/// Registers an <see cref="EntityDistributedCache{TEntity}"/> for the given entity type.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of the entity to cache.
		/// </typeparam>
		/// <param name="services">
		/// The service collection to register the cache.
		/// </param>
		/// <param name="configure">
		/// An optional delegate to configure the cache options.
		/// </param>
		/// <param name="lifetime">
		/// The service lifetime (default: Singleton).
		/// </param>
		/// <returns>
		/// Returns the service collection for chaining.
		/// </returns>
		public static IServiceCollection AddEntityDistributedCacheFor<TEntity>(
			this IServiceCollection services,
			Action<EntityCacheOptions>? configure = null,
			ServiceLifetime lifetime = ServiceLifetime.Singleton)
			where TEntity : class {

			if (configure != null) {
				services.AddOptions<EntityCacheOptions<TEntity>>()
					.Configure(options => configure(options));
			}

			services.TryAdd(new ServiceDescriptor(typeof(IEntityCache<TEntity>), typeof(EntityDistributedCache<TEntity>), lifetime));
			services.TryAdd(new ServiceDescriptor(typeof(EntityDistributedCache<TEntity>), typeof(EntityDistributedCache<TEntity>), lifetime));

			return services;
		}
	}
}
