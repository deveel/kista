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

namespace Kista.Caching {
	/// <summary>
	/// An implementation of <see cref="IEntityCache{TEntity}"/> that
	/// uses the <see cref="IMemoryCache"/> to store entities in-process.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of the entity to cache.
	/// </typeparam>
	/// <remarks>
	/// <para>
	/// This implementation provides a default in-process caching strategy
	/// that requires no external dependencies beyond the standard
	/// <c>Microsoft.Extensions.Caching.Memory</c> package.
	/// </para>
	/// <para>
	/// Expiration is configured via <see cref="EntityCacheOptions{TEntity}"/>.
	/// When no expiration is configured, entities are cached indefinitely
	/// (subject to memory pressure eviction by <see cref="IMemoryCache"/>).
	/// </para>
	/// </remarks>
	public class EntityMemoryCache<TEntity> : IEntityCache<TEntity>
		where TEntity : class {

		private readonly IMemoryCache _cache;
		private readonly EntityCacheOptions<TEntity>? _options;

		/// <summary>
		/// Constructs the cache with the given memory cache and options.
		/// </summary>
		/// <param name="cache">
		/// The <see cref="IMemoryCache"/> instance used to store entities.
		/// </param>
		/// <param name="options">
		/// An optional set of options to configure the entity cache.
		/// </param>
		public EntityMemoryCache(
			IMemoryCache cache,
			IOptions<EntityCacheOptions<TEntity>>? options = null) {
			_cache = cache;
			_options = options?.Value;
		}

		/// <summary>
		/// Gets the expiration time for cached entities.
		/// </summary>
		protected virtual TimeSpan? Expiration => _options?.Expiration;

		/// <inheritdoc/>
		public async ValueTask<TEntity?> GetOrSetAsync(string cacheKey, Func<ValueTask<TEntity?>> valueFactory, CancellationToken cancellationToken = default) {
			if (_cache.TryGetValue(cacheKey, out TEntity? cached))
				return cached;

			var entity = await valueFactory();
			if (entity == null)
				return null;

			SetCacheEntry(cacheKey, entity);

			return entity;
		}

		/// <inheritdoc/>
		public ValueTask SetAsync(string[] cacheKeys, TEntity entity, CancellationToken cancellationToken = default) {
			foreach (var key in cacheKeys) {
				SetCacheEntry(key, entity);
			}

			return ValueTask.CompletedTask;
		}

		/// <inheritdoc/>
		public ValueTask RemoveAsync(string[] cacheKeys, CancellationToken cancellationToken = default) {
			foreach (var key in cacheKeys) {
				_cache.Remove(key);
			}

			return ValueTask.CompletedTask;
		}

		private void SetCacheEntry(string key, TEntity entity) {
			var expiration = Expiration;
			if (expiration.HasValue) {
				_cache.Set(key, entity, expiration.Value);
			} else {
				_cache.Set(key, entity);
			}
		}
	}
}
