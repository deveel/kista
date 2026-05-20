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
using Microsoft.Extensions.Options;

namespace Deveel.Data.Caching {
	/// <summary>
	/// Options for configuring EasyCaching-based entity caching.
	/// </summary>
	public class EasyCachingOptions {
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

	internal sealed class ConfiguredEntityCacheOptions<TEntity> : IConfigureOptions<EntityCacheOptions<TEntity>>
		where TEntity : class {
		private readonly TimeSpan? _expiration;
		private readonly string? _prefix;

		public ConfiguredEntityCacheOptions(TimeSpan? expiration, string? prefix) {
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
	/// Extension methods for configuring EasyCaching on a <see cref="RepositoryContextBuilder"/>.
	/// </summary>
	public static class RepositoryContextBuilderExtensions {
		/// <summary>
		/// Enables EasyCaching-based entity caching for all tracked entity types.
		/// Registers <see cref="EntityEasyCache{TEntity}"/> for each entity type
		/// that has a repository registered.
		/// </summary>
		public static RepositoryContextBuilder WithEasyCaching(
			this RepositoryContextBuilder builder,
			Action<EasyCachingOptions>? configure = null,
			ServiceLifetime lifetime = ServiceLifetime.Singleton) {
			var options = new EasyCachingOptions();
			configure?.Invoke(options);

			foreach (var entityType in builder.RegisteredEntityTypes) {
				var cacheType = typeof(EntityEasyCache<>).MakeGenericType(entityType);
				var cacheInterface = typeof(IEntityCache<>).MakeGenericType(entityType);

				builder.Services.TryAdd(new ServiceDescriptor(cacheInterface, cacheType, lifetime));
				builder.Services.TryAdd(new ServiceDescriptor(cacheType, cacheType, lifetime));

				if (options.DefaultExpiration.HasValue || !string.IsNullOrEmpty(options.CacheKeyPrefix)) {
					var entityOptionsType = typeof(EntityCacheOptions<>).MakeGenericType(entityType);
					var expiration = options.DefaultExpiration;
					var prefix = options.CacheKeyPrefix;

					builder.Services.AddSingleton(typeof(IConfigureOptions<>).MakeGenericType(entityOptionsType), sp => {
						return Activator.CreateInstance(
							typeof(ConfiguredEntityCacheOptions<>).MakeGenericType(entityType),
							expiration, prefix)!;
					});
				}
			}

			return builder;
		}
	}
}
