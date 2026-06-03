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

using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Kista {
	/// <summary>
	/// Describes the request to obtain a page of a given size
	/// from a repository
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of entity to page.
	/// </typeparam>
	/// <seealso cref="RepositoryBase{TEntity, TKey}.QueryPageAsync(PageQuery{TEntity}, CancellationToken)"/>
	public class PageQuery<TEntity> : PageRequest, IQuery where TEntity : class {
		private QueryBuilder<TEntity> queryBuilder;

		/// <summary>
		/// Constructs a new page request with the given page number and size
		/// </summary>
		/// <param name="page">
		/// The number of the page to request
		/// </param>
		/// <param name="size">
		/// The maximum size of the page to return.
		/// </param>
		/// <exception cref="ArgumentOutOfRangeException">
		/// If either the page number or the page size are smaller than 1.
		/// </exception>
		public PageQuery(int page, int size) : base(page, size) {
			queryBuilder = new QueryBuilder<TEntity>();
		}


		/// <summary>
		/// The query that is applied to the request
		/// </summary>
		public IQuery? Query { 
			get => queryBuilder.Query;
			set => queryBuilder = new QueryBuilder<TEntity>(value);
		}

		[ExcludeFromCodeCoverage]
		IQueryFilter? IQuery.Filter => Query?.Filter;

		[ExcludeFromCodeCoverage]
		IQueryOrder? IQuery.Order => Query?.Order;

		/// <summary>
		/// Sets or appends a new filter
		/// </summary>
		/// <param name="expression">The filter expression to add</param>
		/// <returns>
		/// Returns this page request with the new filter
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown if the <paramref name="expression"/> is <c>null</c>.
		/// </exception>
		public PageQuery<TEntity> Where(Expression<Func<TEntity, bool>> expression) {
            ArgumentNullException.ThrowIfNull(expression);

			queryBuilder.Where(expression);

			return this;
		}

		/// <summary>
		/// Appends an ascending sort rule to the page request
		/// </summary>
		/// <param name="field">
		/// The expression that selects the field to sort by.
		/// </param>
		/// <returns>
		/// Returns this instance of the page request with the
		/// appended sort rule.
		/// </returns>
		public PageQuery<TEntity> OrderBy(Expression<Func<TEntity, object?>> field) {
            ArgumentNullException.ThrowIfNull(field);

			return OrderBy(new ExpressionSort<TEntity>(field));
		}

		/// <summary>
		/// Appends a descending sort rule to the page request
		/// </summary>
		/// <param name="field">
		/// The expression that selects the field to sort by.
		/// </param>
		/// <returns>
		/// Returns this instance of the page request with the
		/// appended sort rule.
		/// </returns>
		public PageQuery<TEntity> OrderByDescending(Expression<Func<TEntity, object?>> field) {
            ArgumentNullException.ThrowIfNull(field);

			return OrderBy(new ExpressionSort<TEntity>(field, SortDirection.Descending));
		}

		/// <summary>
		/// Appends the given sort order to the request
		/// </summary>
		/// <param name="order">
		/// The sort order to apply to the page query.
		/// </param>
		/// <returns>
		/// Returns this instance of <see cref="PageQuery{TEntity}"/> for chaining.
		/// </returns>
		public PageQuery<TEntity> OrderBy(IQueryOrder order) {
            ArgumentNullException.ThrowIfNull(order);

			queryBuilder.OrderBy(order);

			return this;
		}

		/// <summary>
		/// Appends an order by the given field name
		/// </summary>
		/// <param name="fieldName">
		/// The name of the field to sort by
		/// </param>
		/// <param name="direction">
		/// The sort direction to order by.
		/// </param>
		/// <returns>
		/// Returns this instance of the page request with the
		/// appended sort rule.
		/// </returns>
		public PageQuery<TEntity> OrderBy(string fieldName, SortDirection direction = SortDirection.Ascending) {
			ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

			return OrderBy(new FieldOrder(fieldName, direction));
		}

		/// <summary>
		/// Appends a descending sort rule to the page request.
		/// </summary>
		/// <param name="fieldName">
		/// The name of the field to sort by.
		/// </param>
		/// <returns>
		/// Returns this instance of the page request with the
		/// appended sort rule.
		/// </returns>
		public PageQuery<TEntity> OrderByDescending(string fieldName) {
            ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

			return OrderBy(fieldName, SortDirection.Descending);
		}

		/// <summary>
		/// Applies the query to the given <see cref="IQueryable{TEntity}"/>,
		/// if this page request has a query defined.
		/// </summary>
		/// <param name="queryable">
		/// The queryable to apply the query to.
		/// </param>
		/// <returns>
		/// Returns a <see cref="IQueryable{TEntity}"/> that is the result
		/// of the application of the query to the given queryable.
		/// </returns>
		public IQueryable<TEntity> ApplyQuery(IQueryable<TEntity> queryable) {
			return queryBuilder.Apply(queryable);
		}
	}
}