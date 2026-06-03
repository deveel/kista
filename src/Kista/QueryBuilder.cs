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

using System.Linq.Expressions;

namespace Kista {
	/// <summary>
	/// A fluent builder that can be used to create a query
	/// that can be applied to a repository.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of entity to build the query for.
	/// </typeparam>
	public sealed class QueryBuilder<TEntity> : IQuery where TEntity : class {
		/// <summary>
		/// Creates a new query builder that wraps the 
		/// given query.
		/// </summary>
		/// <param name="query">
		/// The query object that is wrapped by this builder.
		/// </param>
		/// <remarks>
		/// When the provided query is <c>null</c> an empty query
		/// is created.
		/// </remarks>
		public QueryBuilder(IQuery? query = null) {
			Query = query ?? global::Kista.Query.Empty;
		}

		/// <summary>
		/// Gets the query that is built by this builder.
		/// </summary>
		public IQuery Query { get; private set; }

		IQueryFilter? IQuery.Filter => Query.Filter;

		IQueryOrder? IQuery.Order => Query.Order;

		private static IQueryOrder CombineOrder(IQueryOrder? order, IQueryOrder other) {
			if (order == null)
				return other;

			return order.Combine(other);
		}

		/// <summary>
		/// Combines the filter of the query with the given
		/// filter expression.
		/// </summary>
		/// <param name="filter">
		/// The filter expression to combine with the query.
		/// </param>
		/// <returns>
		/// Returns this query builder with the new filter
		/// for chaining calls.
		/// </returns>
		public QueryBuilder<TEntity> Where(Expression<Func<TEntity, bool>> filter)
			=> Where(QueryFilter.Where<TEntity>(filter));

		/// <summary>
		/// Combines the filter of the query with the given
		/// filter object.
		/// </summary>
		/// <param name="filter">
		/// The filter object to combine with the query.
		/// </param>
		/// <returns>
		/// Returns this query builder with the new filter
		/// for chaining calls.
		/// </returns>
		public QueryBuilder<TEntity> Where(IQueryFilter filter) {
			if (Query.HasFilter()) {
				Query = new Query(QueryFilter.Combine(Query.Filter ?? QueryFilter.Empty, filter), Query.Order);
			} else {
				Query = new Query(filter, Query.Order);
			}

			return this;
		}

		/// <summary>
		/// Orders the results of the query by the given field.
		/// </summary>
		/// <param name="field">
		/// The expression used to select the field to sort by.
		/// </param>
		/// <param name="direction">
		/// The direction of the sort.
		/// </param>
		/// <returns>
		/// Returns this query builder with the new sort
		/// for chaining calls.
		/// </returns>
		public QueryBuilder<TEntity> OrderBy(Expression<Func<TEntity, object?>> field, SortDirection direction = SortDirection.Ascending)
			=> OrderBy(QueryOrder.OrderBy(field, direction));

		/// <summary>
		/// Orders in a descending order the results of the 
		/// query by the given field.
		/// </summary>
		/// <param name="field">
		/// The expression used to select the field to sort by.
		/// </param>
		/// <returns>
		/// Returns the built <see cref="IQuery"/> instance.
		/// </returns>
		public QueryBuilder<TEntity> OrderByDescending(Expression<Func<TEntity, object?>> field)
			=> OrderBy(field, SortDirection.Descending);

		/// <summary>
		/// Orders the results of the query by the given sorting rule.
		/// </summary>
		/// <param name="sort">
		/// The sorting rule to apply to the query.
		/// </param>
		/// <returns>
		/// Returns this query builder with the new sort
		/// for chaining calls.
		/// </returns>
		public QueryBuilder<TEntity> OrderBy(IQueryOrder sort) {
			Query = new Query(Query.Filter ?? QueryFilter.Empty, CombineOrder(Query.Order, sort));

			return this;
		}

		/// <summary>
		/// Orders the results of the query by the given field.
		/// </summary>
		/// <param name="fieldName">
		/// The name of the field to sort by.
		/// </param>
		/// <param name="direction">
		/// The direction of the sort.
		/// </param>
		/// <returns>
		/// Returns this query builder with the new sort
		/// for chaining calls.
		/// </returns>
		public QueryBuilder<TEntity> OrderBy(string fieldName, SortDirection direction = SortDirection.Ascending)
			=> OrderBy(QueryOrder.OrderBy(fieldName, direction));

		/// <summary>
		/// Orders in a descending order the results of the
		/// query by the given field.
		/// </summary>
		/// <param name="fieldName">
		/// The name of the field to sort by.
		/// </param>
		/// <returns>
		/// Returns this query builder with the new sort
		/// for chaining calls.
		/// </returns>
		public QueryBuilder<TEntity> OrderByDescending(string fieldName)
			=> OrderBy(fieldName, SortDirection.Descending);
	}
}
