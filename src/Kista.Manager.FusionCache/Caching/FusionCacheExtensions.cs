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

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using ZiggyCreatures.Caching.Fusion;

namespace Kista.Caching {
	public class FusionCachingOptions {
		public TimeSpan? DefaultEntryDuration { get; set; }
		public bool? FailSafeEnabled { get; set; }
		public TimeSpan? FailSafeMaxDuration { get; set; }
		public TimeSpan? FactorySoftTimeout { get; set; }
		public TimeSpan? FactoryHardTimeout { get; set; }
		public CacheItemPriority? Priority { get; set; }
		public float? EagerRefreshThreshold { get; set; }
	}

	internal sealed class ConfiguredFusionCacheOptions<TEntity> : IConfigureOptions<EntityCacheOptions<TEntity>>
		where TEntity : class {

		private readonly FusionCachingOptions _fusionOptions;

		public ConfiguredFusionCacheOptions(FusionCachingOptions fusionOptions) {
			_fusionOptions = fusionOptions;
		}

		public void Configure(EntityCacheOptions<TEntity> options) {
			if (_fusionOptions.DefaultEntryDuration.HasValue)
				options.Expiration = _fusionOptions.DefaultEntryDuration;
		}
	}

	internal sealed class ConfiguredFusionCachingOptions : IConfigureOptions<FusionCachingOptions> {
		private readonly FusionCachingOptions _options;

		public ConfiguredFusionCachingOptions(FusionCachingOptions options) {
			_options = options;
		}

		public void Configure(FusionCachingOptions options) {
			if (_options.DefaultEntryDuration.HasValue)
				options.DefaultEntryDuration = _options.DefaultEntryDuration;
			if (_options.FailSafeEnabled.HasValue)
				options.FailSafeEnabled = _options.FailSafeEnabled;
			if (_options.FailSafeMaxDuration.HasValue)
				options.FailSafeMaxDuration = _options.FailSafeMaxDuration;
			if (_options.FactorySoftTimeout.HasValue)
				options.FactorySoftTimeout = _options.FactorySoftTimeout;
			if (_options.FactoryHardTimeout.HasValue)
				options.FactoryHardTimeout = _options.FactoryHardTimeout;
			if (_options.Priority.HasValue)
				options.Priority = _options.Priority;
			if (_options.EagerRefreshThreshold.HasValue)
				options.EagerRefreshThreshold = _options.EagerRefreshThreshold;
		}
	}

	public static class FusionCacheServiceExtensions {
		public static RepositoryContextBuilder WithFusionCaching(
			this RepositoryContextBuilder builder,
			Action<FusionCachingOptions>? configure = null,
			ServiceLifetime lifetime = ServiceLifetime.Singleton) {

			var fusionOptions = new FusionCachingOptions();
			configure?.Invoke(fusionOptions);

			builder.Services.AddSingleton<IConfigureOptions<FusionCachingOptions>>(
				new ConfiguredFusionCachingOptions(fusionOptions));

			foreach (var entityType in builder.RegisteredEntityTypes) {
				var cacheType = typeof(EntityFusionCache<>).MakeGenericType(entityType);
				var cacheInterface = typeof(IEntityCache<>).MakeGenericType(entityType);

				builder.Services.TryAdd(new ServiceDescriptor(cacheInterface, cacheType, lifetime));
				builder.Services.TryAdd(new ServiceDescriptor(cacheType, cacheType, lifetime));

				if (fusionOptions.DefaultEntryDuration.HasValue) {
					var entityOptionsType = typeof(EntityCacheOptions<>).MakeGenericType(entityType);
					builder.Services.AddSingleton(typeof(IConfigureOptions<>).MakeGenericType(entityOptionsType), sp => {
						return Activator.CreateInstance(
							typeof(ConfiguredFusionCacheOptions<>).MakeGenericType(entityType),
							fusionOptions)!;
					});
				}
			}

			return builder;
		}

		public static IServiceCollection AddEntityFusionCacheFor<TEntity>(
			this IServiceCollection services,
			Action<FusionCachingOptions>? configure = null,
			ServiceLifetime lifetime = ServiceLifetime.Singleton)
			where TEntity : class {

			if (configure != null) {
				var fusionOptions = new FusionCachingOptions();
				configure(fusionOptions);
				services.AddSingleton<IConfigureOptions<FusionCachingOptions>>(
					new ConfiguredFusionCachingOptions(fusionOptions));

				if (fusionOptions.DefaultEntryDuration.HasValue) {
					services.Configure<EntityCacheOptions<TEntity>>(options => {
						options.Expiration = fusionOptions.DefaultEntryDuration;
					});
				}
			}

			services.TryAdd(new ServiceDescriptor(typeof(IEntityCache<TEntity>), typeof(EntityFusionCache<TEntity>), lifetime));
			services.TryAdd(new ServiceDescriptor(typeof(EntityFusionCache<TEntity>), typeof(EntityFusionCache<TEntity>), lifetime));

			return services;
		}
	}
}
