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

using System.Text.Json;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Kista.Caching {
	/// <summary>
	/// An implementation of <see cref="IEntityCache{TEntity}"/> that
	/// uses <see cref="IDistributedCache"/> as the backing cache store.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of the entity to cache.
	/// </typeparam>
	/// <remarks>
	/// <para>
	/// This implementation serializes entities to JSON using
	/// <see cref="System.Text.Json"/> for storage in a distributed cache
	/// (e.g. Redis via <c>Microsoft.Extensions.Caching.StackExchangeRedis</c>,
	/// SQL Server via <c>Microsoft.Extensions.Caching.SqlServer</c>).
	/// </para>
	/// <para>
	/// Expiration is configured via <see cref="EntityCacheOptions{TEntity}"/>.
	/// When no expiration is configured, entities are cached with a default
	/// of 5 minutes.
	/// </para>
	/// </remarks>
	public class EntityDistributedCache<TEntity> : IEntityCache<TEntity>
		where TEntity : class {

		private static readonly JsonSerializerOptions DefaultJsonOptions = new() {
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			PropertyNameCaseInsensitive = true,
		};

		private readonly IDistributedCache _cache;
		private readonly EntityCacheOptions<TEntity>? _options;

		/// <summary>
		/// Constructs the cache with the given distributed cache and options.
		/// </summary>
		/// <param name="cache">
		/// The <see cref="IDistributedCache"/> instance used to store entities.
		/// </param>
		/// <param name="options">
		/// An optional set of options to configure the entity cache.
		/// </param>
		public EntityDistributedCache(
			IDistributedCache cache,
			IOptions<EntityCacheOptions<TEntity>>? options = null) {
			_cache = cache;
			_options = options?.Value;
		}

		/// <summary>
		/// Gets the expiration time for cached entities.
		/// </summary>
		protected virtual TimeSpan Expiration => _options?.Expiration ?? TimeSpan.FromMinutes(5);

		/// <summary>
		/// Gets the <see cref="JsonSerializerOptions"/> used for serialization.
		/// Override to customize serialization behavior.
		/// </summary>
		protected virtual JsonSerializerOptions JsonOptions => DefaultJsonOptions;

		/// <inheritdoc/>
		public async ValueTask<TEntity?> GetOrSetAsync(string cacheKey, Func<ValueTask<TEntity?>> valueFactory, CancellationToken cancellationToken = default) {
			var bytes = await _cache.GetAsync(cacheKey, cancellationToken);
			if (bytes != null) {
				return Deserialize(bytes);
			}

			var entity = await valueFactory();
			if (entity == null)
				return null;

			var entryOptions = GetDistributedCacheEntryOptions();
			var serialized = Serialize(entity);
			await _cache.SetAsync(cacheKey, serialized, entryOptions, cancellationToken);

			return entity;
		}

		/// <inheritdoc/>
		public async ValueTask SetAsync(string[] cacheKeys, TEntity entity, CancellationToken cancellationToken = default) {
			var entryOptions = GetDistributedCacheEntryOptions();
			var serialized = Serialize(entity);

			foreach (var key in cacheKeys) {
				await _cache.SetAsync(key, serialized, entryOptions, cancellationToken);
			}
		}

		/// <inheritdoc/>
		public async ValueTask RemoveAsync(string[] cacheKeys, CancellationToken cancellationToken = default) {
			foreach (var key in cacheKeys) {
				await _cache.RemoveAsync(key, cancellationToken);
			}
		}

		/// <summary>
		/// Creates the <see cref="DistributedCacheEntryOptions"/> from the
		/// configured <see cref="EntityCacheOptions{TEntity}"/>.
		/// </summary>
		/// <returns>
		/// Returns an instance of <see cref="DistributedCacheEntryOptions"/>.
		/// </returns>
		protected virtual DistributedCacheEntryOptions GetDistributedCacheEntryOptions() {
			return new DistributedCacheEntryOptions {
				AbsoluteExpirationRelativeToNow = Expiration,
			};
		}

		/// <summary>
		/// Serializes the given entity to a byte array.
		/// </summary>
		/// <param name="entity">The entity to serialize.</param>
		/// <returns>The serialized bytes.</returns>
		protected virtual byte[] Serialize(TEntity entity) {
			return JsonSerializer.SerializeToUtf8Bytes(entity, JsonOptions);
		}

		/// <summary>
		/// Deserializes the given byte array back to an entity.
		/// </summary>
		/// <param name="bytes">The serialized bytes.</param>
		/// <returns>The deserialized entity, or <c>null</c> if deserialization failed.</returns>
		protected virtual TEntity? Deserialize(byte[] bytes) {
			return JsonSerializer.Deserialize<TEntity>(bytes, JsonOptions);
		}
	}
}
