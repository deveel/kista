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
	/// Extends the <see cref="IQuery"/> to provide
	/// functions for filtering and sorting queryable
	/// objects.
	/// </summary>
	public static class QueryExtensions {
		/// <summary>
		/// Applies the query to the given <see cref="IQueryable{TEntity}"/>
		/// to filter and sort the results.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity to apply the query to.
		/// </typeparam>
		/// <param name="query">
		/// The query to be applied to the queryable.
		/// </param>
		/// <param name="queryable">
		/// The queryable to apply the query to.
		/// </param>
		/// <returns>
		/// Returns the <see cref="IQueryable{TEntity}"/> that was
		/// filtered and sorted.
		/// </returns>
		public static IQueryable<TEntity> Apply<TEntity>(this IQuery query, IQueryable<TEntity> queryable)
			where TEntity : class {
			ArgumentNullException.ThrowIfNull(queryable, nameof(queryable));

			if (query.Filter != null)
				queryable = query.Filter.Apply<TEntity>(queryable);

			if (query.Order != null)
				queryable = query.Order.Apply<TEntity>(queryable);

			return queryable;
		}

		/// <summary>
		/// Gets a value indicating if the query has a filter.
		/// </summary>
		public static bool HasFilter(this IQuery query) => !(query.Filter?.IsEmpty() ?? true);

	}
}
