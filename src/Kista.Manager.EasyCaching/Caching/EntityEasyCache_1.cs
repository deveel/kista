// Copyright 2023-2025 Antonello Provenzano
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
	/// is based on an <see cref="IEasyCachingProvider"/>.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of the entity to cache.
	/// </typeparam>
	/// <remarks>
	/// This implementation of the Entity Cache assumes that the
	/// entity of type <typeparamref name="TEntity"/> is the same 
	/// type of the cached object.
	/// </remarks>
	/// <seealso cref="EntityEasyCache{TEntity, TCached}"/>
	public class EntityEasyCache<TEntity> : EntityEasyCache<TEntity, TEntity> where TEntity : class {
		/// <summary>
		/// Constructs the cache with the given provider and options.
		/// </summary>
		/// <param name="cacheProvider">
		/// The caching provider to use to cache the entities.
		/// </param>
		/// <param name="options">
		/// A set of options to configure the cache.
		/// </param>
		/// <param name="keyGenerator">
		/// A generator to create the keys to use to identify the
		/// entities in the cache.
		/// </param>
		/// <param name="converter">
		/// A converter to convert the entity to and from the cached
		/// instance of the entity.
		/// </param>
		public EntityEasyCache(
			IEasyCachingProvider cacheProvider, 
			IOptions<EntityCacheOptions<TEntity>>? options = null, 
			IEntityCacheKeyGenerator<TEntity>? keyGenerator = null, 
			IEntityEasyCacheConverter<TEntity, TEntity>? converter = null) 
			: base(cacheProvider, options, keyGenerator, converter) {
		}
	}
}
