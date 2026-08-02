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

using Deveel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kista.Caching {
	/// <summary>
	/// A builtin interceptor that aligns the entity cache to the
	/// <see cref="EntityManager{TEntity, TKey}"/> operation pipeline,
	/// replacing the former inline <c>SetToCacheAsync</c> / <c>EvictAsync</c>
	/// glue that was duplicated across the write methods of the manager.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of the entity managed by the pipeline.
	/// </typeparam>
	/// <typeparam name="TKey">
	/// The type of the key identifying the entity.
	/// </typeparam>
	/// <remarks>
	/// <para>
	/// The interceptor is only appended to the pipeline when an
	/// <see cref="IEntityCache{TEntity}"/> is registered in the dependency
	/// injection container: this makes the cache concern removable (no
	/// cache registered, no cache side effects) and reorderable (user-
	/// registered interceptors run before it), while preserving the
	/// default cache behavior out of the box.
	/// </para>
	/// <para>
	/// <see cref="PreWriteAsync"/> never short-circuits the chain: the
	/// cache is a write-path concern that fires only after a successful
	/// write. <see cref="PostWriteAsync"/> re-caches the written entity
	/// for <see cref="EntityOperationKind.Create"/>,
	/// <see cref="EntityOperationKind.Update"/> and
	/// <see cref="EntityOperationKind.Restore"/> operations, and for
	/// <see cref="EntityOperationKind.Remove"/> operations when the
	/// entity implements <see cref="ISoftDeletable"/> (soft-delete
	/// branch); it evicts the entity for
	/// <see cref="EntityOperationKind.Remove"/> operations when the
	/// entity does not implement <see cref="ISoftDeletable"/> (hard
	/// branch), and for all
	/// <see cref="EntityOperationKind.HardDelete"/> operations.
	/// </para>
	/// <para>
	/// Failures in the cache are logged and swallowed, so a cache
	/// outage never propagates to the caller of the write operation:
	/// this preserves the behavior of the former inline helpers.
	/// </para>
	/// </remarks>
	internal sealed class CacheInterceptor<TEntity, TKey> : IEntityManagerInterceptor<TEntity, TKey>
		where TEntity : class
		where TKey : notnull {
		private readonly IEntityCache<TEntity> _cache;
		private readonly IEntityCacheKeyGenerator<TEntity>? _keyGenerator;
		private readonly Func<TEntity, TKey?> _getEntityKey;
		private readonly ILogger _logger;

		/// <summary>
		/// Constructs the interceptor with the cache, the optional key
		/// generator, a delegate to extract the primary key of an entity,
		/// and a logger used to report cache failures.
		/// </summary>
		/// <param name="cache">
		/// The cache where the entities are stored.
		/// </param>
		/// <param name="keyGenerator">
		/// An optional service used to generate the cache keys for an
		/// entity. When <c>null</c>, or when it returns an empty array of
		/// keys, the entity is not cached.
		/// </param>
		/// <param name="getEntityKey">
		/// A delegate returning the primary key of an entity, used for
		/// diagnostic logging when the cache interaction fails.
		/// </param>
		/// <param name="logger">
		/// The logger used to report cache failures.
		/// </param>
		public CacheInterceptor(
			IEntityCache<TEntity> cache,
			IEntityCacheKeyGenerator<TEntity>? keyGenerator,
			Func<TEntity, TKey?> getEntityKey,
			ILogger? logger = null) {
			_cache = cache ?? throw new ArgumentNullException(nameof(cache));
			_keyGenerator = keyGenerator;
			_getEntityKey = getEntityKey ?? throw new ArgumentNullException(nameof(getEntityKey));
			_logger = logger ?? NullLogger.Instance;
		}

		/// <summary>
		/// Computes the cache keys for the given entity, using the
		/// <see cref="IEntityCacheKeyGenerator{TEntity}"/> when one is
		/// available, or returning an empty array (meaning the entity
		/// will not be cached).
		/// </summary>
		/// <param name="entity">
		/// The entity to generate the keys for.
		/// </param>
		/// <returns>
		/// Returns an array of strings that identify the entity in the
		/// cache, or an empty array when no key generator is registered.
		/// </returns>
		private string[] GenerateCacheKeys(TEntity entity) {
			if (_keyGenerator == null)
				return Array.Empty<string>();

			return _keyGenerator.GenerateAllKeys(entity);
		}

		/// <summary>
		/// Sets the given entity in the cache, swallowing and logging
		/// any failure so it never propagates to the caller.
		/// </summary>
		/// <param name="entity">
		/// The entity to set in the cache.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		private async ValueTask SetToCacheAsync(TEntity entity, CancellationToken cancellationToken) {
			try {
				var keys = GenerateCacheKeys(entity);
				if (keys.Length == 0)
					return;

				await _cache.SetAsync(keys, entity, cancellationToken);
			} catch (Exception ex) {
				_logger.LogEntityNotCached(ex, typeof(TEntity), _getEntityKey(entity));
			}
		}

		/// <summary>
		/// Evicts the given entity from the cache, swallowing and logging
		/// any failure so it never propagates to the caller.
		/// </summary>
		/// <param name="entity">
		/// The entity to evict from the cache.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		private async ValueTask EvictAsync(TEntity entity, CancellationToken cancellationToken) {
			try {
				var keys = GenerateCacheKeys(entity);
				if (keys.Length == 0)
					return;

				await _cache.RemoveAsync(keys, cancellationToken);
			} catch (Exception ex) {
				_logger.LogEntityNotEvicted(ex, typeof(TEntity), _getEntityKey(entity));
			}
		}

		/// <inheritdoc/>
		/// <remarks>
		/// The cache is a write-path concern: this method never
		/// short-circuits the chain and returns <c>null</c> to let the
		/// write proceed.
		/// </remarks>
		public ValueTask<IOperationResult?> PreWriteAsync(IEntityOperationContext<TEntity, TKey> context)
			=> new((IOperationResult?)null);

		/// <inheritdoc/>
		/// <remarks>
		/// Re-caches or evicts the written entity depending on the
		/// operation kind. The action is only taken when the operation
		/// succeeded: a not-changed or failed result leaves the cache
		/// untouched, matching the former inline behavior that fired
		/// only after a successful repository write.
		/// </remarks>
		public async ValueTask PostWriteAsync(IEntityOperationContext<TEntity, TKey> context, IOperationResult result) {
			if (!result.IsSuccess())
				return;

			var entity = context.Entity;
			var token = context.CancellationToken;

			switch (context.Kind) {
				case EntityOperationKind.Create:
				case EntityOperationKind.Update:
				case EntityOperationKind.Restore:
					await SetToCacheAsync(entity, token);
					break;
				case EntityOperationKind.Remove:
					if (entity is ISoftDeletable)
						await SetToCacheAsync(entity, token);
					else
						await EvictAsync(entity, token);
					break;
				case EntityOperationKind.HardDelete:
					await EvictAsync(entity, token);
					break;
				default:
					break;
			}
		}
	}
}