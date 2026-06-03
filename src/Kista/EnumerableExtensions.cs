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

namespace Kista {
	/// <summary>
	/// Extensions for the <see cref="IEnumerable{T}"/> to provide
	/// the capability to wrap the collection into a <see cref="IRepository{TEntity}"/>.
	/// </summary>
	public static class EnumerableExtensions {
		/// <summary>
		/// Makes the given collection of entities a <see cref="IRepository{TEntity}"/>.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of the entities in the collection.
		/// </typeparam>
		/// <param name="entities">
		/// The collection of entities to wrap.
		/// </param>
		/// <returns>
		/// Returns an instance of <see cref="IRepository{TEntity}"/> that wraps
		/// the given collection.
		/// </returns>
		public static IRepository<TEntity> AsRepository<TEntity>(this IEnumerable<TEntity> entities) where TEntity : class {
			return new RepositoryWrapper<TEntity>(entities);
		}
	}
}
