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

using EasyCaching.Core;

using Microsoft.Extensions.Options;

namespace Kista.Caching {
	/// <summary>
	/// An implementation of <see cref="IEntityCache{TEntity}"/> that
	/// uses the EasyCaching library to store the entities.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of the entity to cache.
	/// </typeparam>
	/// <typeparam name="TCached">
	/// The type of the cached version of the entity.
	/// </typeparam>
	public class EntityEasyCache<TEntity, TCached> : IEntityCache<TEntity> 
		where TEntity : class 
		where TCached : class {
		private readonly IEasyCachingProvider cacheProvider;
		private readonly EntityCacheOptions<TEntity>? options;
		/// <summary>
		/// A generator to create the keys for the entities.
		/// </summary>
		/// <remarks>
		/// Reserved for use by derived classes that wish to customize key generation.
		/// </remarks>
		protected readonly IEntityCacheKeyGenerator<TEntity>? keyGenerator;
		private readonly IEntityEasyCacheConverter<TEntity, TCached>? converter;

		/// <summary>
		/// Constructs the cache with the given provider and options.
		/// </summary>
		/// <param name="cacheProvider">
		/// The EasyCaching provider to use to store the entities.
		/// </param>
		/// <param name="options">
		/// A set of options to configure the entity cache.
		/// </param>
		/// <param name="keyGenerator">
		/// A generator to create the keys for the entities.
		/// </param>
		/// <param name="converter">
		/// A service to convert the entity to and from the cached version.
		/// </param>
		public EntityEasyCache(IEasyCachingProvider cacheProvider, 
			IOptions<EntityCacheOptions<TEntity>>? options = null, 
			IEntityCacheKeyGenerator<TEntity>? keyGenerator = null,
			IEntityEasyCacheConverter<TEntity, TCached>? converter = null) {
			this.cacheProvider = cacheProvider;
			this.options = options?.Value;
			this.keyGenerator = keyGenerator;
			this.converter = converter;
		}

		/// <summary>
		/// Gets the expiration time for the cached entities.
		/// </summary>
		protected virtual TimeSpan Expiration => options?.Expiration ?? TimeSpan.FromMinutes(5);

		/// <summary>
		/// Attempts to convert the given entity to a version
		/// that can be cached.
		/// </summary>
		/// <param name="entity">
		/// The entity instance to convert.
		/// </param>
		/// <returns>
		/// Returns an instance of <typeparamref name="TCached"/>
		/// that is the result of the conversion.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown if the conversion is not supported by the cache.
		/// </exception>
		protected virtual TCached ConvertToCached(TEntity entity) {
			if (converter != null)
				return converter.ConvertToCached(entity);

			if (entity is TCached cached)
				return cached;

			throw new NotSupportedException("The converter is not defined for this cache");
		}

		/// <summary>
		/// Attempts to convert back the given cached version of the entity
		/// to the original entity.
		/// </summary>
		/// <param name="cached">
		/// The instance of the cached version of the entity.
		/// </param>
		/// <returns>
		/// Returns an instance of <typeparamref name="TEntity"/>
		/// that is the result of the conversion.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown if the conversion is not supported by the cache.
		/// </exception>
		protected virtual TEntity ConvertFromCached(TCached cached) {
			if (converter != null)
				return converter.ConvertFromCached(cached);

			if (cached is TEntity entity)
				return entity;

			throw new NotSupportedException("The converter is not defined for this cache");
		}

	private Func<Task<TCached>> Factory(Func<ValueTask<TEntity?>> valueFactory) {
		return async () => {
			var entity = await valueFactory();
			if (entity == null)
#pragma warning disable CS8603 // Possible null reference return.
				// EasyCaching does not support nullable explicitly
				return null;
#pragma warning restore CS8603 // Possible null reference return

			return ConvertToCached(entity);
		};
	}

		/// <inheritdoc/>
		public async ValueTask<TEntity?> GetOrSetAsync(string cacheKey, Func<ValueTask<TEntity?>> valueFactory, CancellationToken cancellationToken = default) {
			try {
				var factory = Factory(valueFactory);

				var result = await cacheProvider.GetAsync<TCached>(cacheKey, factory, Expiration, cancellationToken);

				if (!result.HasValue)
					return null;

				if (result.IsNull)
					return null;

				return ConvertFromCached(result.Value);
			} catch (Exception ex) {

				throw new RepositoryException("Could not get or set the entity in the cache", ex);
			}
		}

		/// <inheritdoc/>
		public async ValueTask RemoveAsync(string[] cacheKeys, CancellationToken cancellationToken = default) {
			try {
				await cacheProvider.RemoveAllAsync(cacheKeys, cancellationToken);
			} catch (Exception ex) {
				throw new RepositoryException("Unable to remove the entity from the cache", ex);
			}
		}

		/// <inheritdoc/>
		public async ValueTask SetAsync(string[] cacheKeys, TEntity entity, CancellationToken cancellationToken = default) {
			try {
				var cached = ConvertToCached(entity);
				var pairs = cacheKeys.ToDictionary(x => x, y => cached);
				await cacheProvider.SetAllAsync(pairs, Expiration, cancellationToken);
			} catch (Exception ex) {
				throw new RepositoryException("Unable to set the entity in the cache", ex);
			}
		}
	}
}