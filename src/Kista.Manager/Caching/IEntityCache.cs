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

namespace Kista.Caching {
	/// <summary>
	/// Provides a contract for caching entities of a given type.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of the entity to cache.
	/// </typeparam>
	/// <remarks>
	/// <para>
	/// Implementations of this interface are responsible for storing,
	/// retrieving, and removing entities from the cache. The key
	/// generation is handled separately by
	/// <see cref="IEntityCacheKeyGenerator{TEntity}"/>.
	/// </para>
	/// </remarks>
	public interface IEntityCache<TEntity> where TEntity : class {
		/// <summary>
		/// Gets the given entity from the cache, if available,
		/// and uses the given factory to create the entity if to
		/// then cache it.
		/// </summary>
		/// <param name="cacheKey">
		/// The unique key used to identify the entity in the cache.
		/// </param>
		/// <param name="valueFactory">
		/// A function that is used to create the entity to cache,
		/// if this was not found in the cache.
		/// </param>
		/// <param name="cancellationToken">
		/// A cancellation token that can be used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns the entity from the cache, if available, or
		/// <c>null</c> if the entity was not found in the cache
		/// and the factory was not able to create it.
		/// </returns>
		ValueTask<TEntity?> GetOrSetAsync(string cacheKey, Func<ValueTask<TEntity?>> valueFactory, CancellationToken cancellationToken = default);

		/// <summary>
		/// Sets the given entity in the cache with the given key.
		/// </summary>
		/// <param name="cacheKeys">
		/// An array of keys used to identify the entity in the cache.
		/// </param>
		/// <param name="entity">
		/// The instance of the entity to cache.
		/// </param>
		/// <param name="cancellationToken">
		/// A cancellation token that can be used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns a task that when completed will set the entity
		/// in the cache.
		/// </returns>
		ValueTask SetAsync(string[] cacheKeys, TEntity entity, CancellationToken cancellationToken = default);

		/// <summary>
		/// Removes a given entity from the cache with the given key.
		/// </summary>
		/// <param name="cacheKeys">
		/// A set of keys used to identify the entity in the cache.
		/// </param>
		/// <param name="cancellationToken">
		/// A cancellation token that can be used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns a task that when completed will remove the entity
		/// from the cache.
		/// </returns>
		ValueTask RemoveAsync(string[] cacheKeys, CancellationToken cancellationToken = default);
	}
}
