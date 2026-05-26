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
	/// Extends instances of <see cref="IQueryable{T}"/> to 
	/// provide filtering capabilities.
	/// </summary>
	public static class QueryableExtensions {
		/// <summary>
		/// Counts the number of entities that match the filter.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of the entity to count.
		/// </typeparam>
		/// <param name="queriable">
		/// The queryable object to count the entities from.
		/// </param>
		/// <param name="filter">
		/// The filter to apply to the query.
		/// </param>
		/// <returns>
		/// Returns the number of entities that match the given filter.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown if either the queryable or the filter are <c>null</c>.
		/// </exception>
		public static long LongCount<TEntity>(this IQueryable<TEntity> queriable, IQueryFilter filter) where TEntity : class {
			ArgumentNullException.ThrowIfNull(queriable, nameof(queriable));
			ArgumentNullException.ThrowIfNull(filter, nameof(filter));

			if (filter.IsEmpty())
				return queriable.LongCount();

			return filter.Apply(queriable).LongCount();
		}

		/// <summary>
		/// Lists all the entities that match the given filter.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of the entity to list.
		/// </typeparam>
		/// <param name="queriable">
		/// The queryable object to list the entities from.
		/// </param>
		/// <param name="filter">
		/// The filter to apply to the query.
		/// </param>
		/// <returns>
		/// Returns a list of entities that match the given filter.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown if either the queryable or the filter are <c>null</c>.
		/// </exception>
		public static IList<TEntity> ToList<TEntity>(this IQueryable<TEntity> queriable, IQueryFilter filter)
			where TEntity : class {
			ArgumentNullException.ThrowIfNull(queriable, nameof(queriable));
			ArgumentNullException.ThrowIfNull(filter, nameof(filter));

			if (filter.IsEmpty())
				return queriable.ToList();

			return filter.Apply(queriable).ToList();
		}

		/// <summary>
		/// Returns the first entity that matches the given filter.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of the entity to find.
		/// </typeparam>
		/// <param name="queryable">
		/// The queryable object to find the entity from.
		/// </param>
		/// <param name="filter">
		/// The filter to apply to the query.
		/// </param>
		/// <returns>
		/// Returns an instance of <typeparamref name="TEntity"/> that
		/// matched the given filter, or <c>null</c> if no entity is found.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown if either the queryable or the filter are <c>null</c>.
		/// </exception>
		public static TEntity? FirstOrDefault<TEntity>(this IQueryable<TEntity> queryable, IQueryFilter filter)
			where TEntity : class {
			ArgumentNullException.ThrowIfNull(queryable, nameof(queryable));
			ArgumentNullException.ThrowIfNull(filter, nameof(filter));

			if (filter.IsEmpty())
				return queryable.FirstOrDefault();

			return filter.Apply(queryable).FirstOrDefault();
		}

		/// <summary>
		/// Checks if there are any entities that match the given filter.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of the entity to check.
		/// </typeparam>
		/// <param name="queryable">
		/// The queryable object to check the entities from.
		/// </param>
		/// <param name="filter">
		/// The filter to apply to the query.
		/// </param>
		/// <returns>
		/// Returns <c>true</c> if there are entities that match the given filter,
		/// otherwise <c>false</c>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown if either the queryable or the filter are <c>null</c>.
		/// </exception>
		public static bool Any<TEntity>(this IQueryable<TEntity> queryable, IQueryFilter filter)
			where TEntity : class {
			ArgumentNullException.ThrowIfNull(queryable, nameof(queryable));
			ArgumentNullException.ThrowIfNull(filter, nameof(filter));

			if (filter.IsEmpty())
				return queryable.Any();

			return filter.Apply(queryable).Any();
		}
	}
}
