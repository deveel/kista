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
using Microsoft.Extensions.Options;

using ZiggyCreatures.Caching.Fusion;

namespace Kista.Caching {
	public class EntityFusionCache<TEntity> : IEntityCache<TEntity>
		where TEntity : class {

		private readonly IFusionCache _cache;
		private readonly EntityCacheOptions<TEntity>? _entityOptions;
		private readonly FusionCachingOptions? _fusionOptions;

		public EntityFusionCache(
			IFusionCache cache,
			IOptions<EntityCacheOptions<TEntity>>? entityOptions = null,
			IOptions<FusionCachingOptions>? fusionOptions = null) {
			_cache = cache;
			_entityOptions = entityOptions?.Value;
			_fusionOptions = fusionOptions?.Value;
		}

		protected virtual FusionCacheEntryOptions GetEntryOptions() {
			if (_fusionOptions != null) {
				var options = new FusionCacheEntryOptions {
					Duration = _fusionOptions.DefaultEntryDuration ?? _entityOptions?.Expiration ?? TimeSpan.FromMinutes(5),
					IsFailSafeEnabled = _fusionOptions.FailSafeEnabled ?? false,
					Priority = _fusionOptions.Priority ?? CacheItemPriority.Normal,
				};

				if (_fusionOptions.FailSafeMaxDuration.HasValue)
					options.FailSafeMaxDuration = _fusionOptions.FailSafeMaxDuration.Value;
				if (_fusionOptions.FactorySoftTimeout.HasValue)
					options.FactorySoftTimeout = _fusionOptions.FactorySoftTimeout.Value;
				if (_fusionOptions.FactoryHardTimeout.HasValue)
					options.FactoryHardTimeout = _fusionOptions.FactoryHardTimeout.Value;
				if (_fusionOptions.EagerRefreshThreshold.HasValue)
					options.EagerRefreshThreshold = _fusionOptions.EagerRefreshThreshold.Value;

				return options;
			}

			return new FusionCacheEntryOptions {
				Duration = _entityOptions?.Expiration ?? TimeSpan.FromMinutes(5),
			};
		}

		public async ValueTask<TEntity?> GetOrSetAsync(string cacheKey, Func<ValueTask<TEntity?>> valueFactory, CancellationToken cancellationToken = default) {
			var entryOptions = GetEntryOptions();

			var result = await _cache.GetOrSetAsync<TEntity?>(cacheKey, async (ct) => {
				return await valueFactory();
			}, entryOptions, cancellationToken);

			return result;
		}

		public async ValueTask SetAsync(string[] cacheKeys, TEntity entity, CancellationToken cancellationToken = default) {
			if (cacheKeys.Length == 0)
				return;

			var entryOptions = GetEntryOptions();

			foreach (var key in cacheKeys) {
				await _cache.SetAsync(key, entity, entryOptions, cancellationToken);
			}
		}

		public async ValueTask RemoveAsync(string[] cacheKeys, CancellationToken cancellationToken = default) {
			if (cacheKeys.Length == 0)
				return;

			foreach (var key in cacheKeys) {
				await _cache.RemoveAsync(key, default(Action<FusionCacheEntryOptions>?), cancellationToken);
			}
		}
	}
}
