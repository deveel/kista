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
	/// A service that is used to generate the keys for caching
	/// a given entity.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of the entity to cache.
	/// </typeparam>
	public interface IEntityCacheKeyGenerator<in TEntity> where TEntity : class {
		/// <summary>
		/// Generates the given cache key using the given primary
		/// key of the entity.
		/// </summary>
		/// <param name="key">
		/// The instance of the primary key of the entity.
		/// </param>
		/// <returns>
		/// Returns a string that is the key to be used to cache
		/// or return the entity.
		/// </returns>
		string GenerateKey(object key);

		/// <summary>
		/// Generates the keys for the given entity.
		/// </summary>
		/// <param name="entity">
		/// The instance of the entity to generate the keys for.
		/// </param>
		/// <returns>
		/// Returns an array of strings that are the keys to be used
		/// to cache the entity.
		/// </returns>
		string[] GenerateAllKeys(TEntity entity);

		// We should provide a mechanism to generate a key
		//       form a filter expression
	}
}
