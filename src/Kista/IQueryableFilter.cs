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
	/// A filter that can be applied to a <see cref="IQueryable{T}"/>
	/// object to restrict the results of a query.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of entity to filter.
	/// </typeparam>
	public interface IQueryableFilter<TEntity> : IQueryFilter where TEntity : class {
		/// <summary>
		/// Applies the filter to the given queryable object.
		/// </summary>
		/// <param name="queryable">
		/// The queryable object to apply the filter to.
		/// </param>
		/// <returns>
		/// Returns an instance of <see cref="IQueryable{TEntity}"/> that
		/// is filtered by the conditions of this object.
		/// </returns>
		IQueryable<TEntity> Apply(IQueryable<TEntity> queryable);
	}
}
