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

namespace Kista {
	/// <summary>
	/// A sorting rule that can be applied to a queryable
	/// object to sort the results.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of entity to sort.
	/// </typeparam>
	public interface IQueryableOrderBy<TEntity> : IQueryOrder {
		/// <summary>
		/// Applies the sorting rule to the given <see cref="IQueryable{TEntity}"/>
		/// </summary>
		/// <param name="queryable">
		/// The queryable object to sort.
		/// </param>
		/// <returns>
		/// Returns a <see cref="IQueryable{TEntity}"/> that
		/// is sorted by the rule.
		/// </returns>
		IQueryable<TEntity> Apply(IQueryable<TEntity> queryable);
	}
}
