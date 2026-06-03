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
	/// A service that provides the conversion of an 
	/// entity to and from a cached version of the entity.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of the entity to be cached.
	/// </typeparam>
	/// <typeparam name="TCached">
	/// The type of the cached entity.
	/// </typeparam>
	/// <remarks>
	/// This service is provided for cases in which
	/// the type of the entity to be cached is not
	/// serializable or cannot be easily cached,
	/// and therefore a conversion to and from another
	/// version is needed.
	/// </remarks>
	public interface IEntityEasyCacheConverter<TEntity, TCached> {
		/// <summary>
		/// Converts the given entity to an
		/// object that can be cached.
		/// </summary>
		/// <param name="entity">
		/// The entity instance to convert to a cached version.
		/// </param>
		/// <returns>
		/// Returns an object that can be cached.
		/// </returns>
		TCached ConvertToCached(TEntity entity);

		/// <summary>
		/// Converts back the given cached object
		/// to the original entity.
		/// </summary>
		/// <param name="cached">
		/// The cached object to be converted.
		/// </param>
		/// <returns>
		/// Returns the original entity.
		/// </returns>
		TEntity ConvertFromCached(TCached cached);
	}
}
