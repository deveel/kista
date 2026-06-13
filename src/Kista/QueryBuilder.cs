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
	/// A fluent builder for composing and executing queries against
	/// a repository, without exposing the underlying
	/// <see cref="IQueryable{T}"/> hatch to consumer code.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of entity to build the query for.
	/// </typeparam>
	/// <remarks>
	/// <para>
	/// <see cref="QueryBuilder{TEntity}"/> is the public, top-level entry
	/// point for fluent query composition. It implements
	/// <see cref="IQueryBuilder{TEntity}"/> and can be used in two modes:
	/// </para>
	/// <list type="bullet">
	/// <item>
	/// <b>Standalone mode</b> — instantiated via the parameterless or
	/// <see cref="QueryBuilder(IQuery?)"/> constructor. The builder
	/// composes a query but throws <see cref="InvalidOperationException"/>
	/// on any terminal call. Standalone builders are useful for code paths
	/// that only need the built <see cref="IQuery"/> (for example, to
	/// hand off to a legacy entry point that accepts an
	/// <see cref="IQuery"/>).
	/// </item>
	/// <item>
	/// <b>Bound mode</b> — instantiated by
	/// <see cref="Repository{TEntity, TKey}.CreateQuery"/> through the
	/// private nested <c>Repository.QueryBuilder</c> class, which inherits
	/// from this type and overrides the terminal methods to dispatch
	/// through the protected <see cref="Repository{TEntity, TKey}"/>
	/// pipeline. External code cannot construct a bound instance
	/// directly; it must go through <c>CreateQuery()</c>.
	/// </item>
	/// </list>
	/// <para>
	/// <b>Mutability and thread-safety.</b> Every <c>Where</c> and
	/// <c>OrderBy</c> call mutates the builder. A builder instance is
	/// therefore <b>not thread-safe</b>: build a new instance per logical
	/// operation and do not share it across threads or concurrent
	/// executions.
	/// </para>
	/// <para>
	/// The accumulated <see cref="IQueryFilter"/> and <see cref="IQueryOrder"/>
	/// are exposed through the <see cref="IQuery"/> contract (via
	/// <see cref="Query"/>, <see cref="IQuery.Filter"/>, and
	/// <see cref="IQuery.Order"/>) so that the built state can be reused
	/// against a different repository of the same engine or handed to a
	/// legacy entry point.
	/// </para>
	/// </remarks>
	public class QueryBuilder<TEntity> : IQuery, IQueryBuilder<TEntity> where TEntity : class {
		private IQuery _query;

		/// <summary>
		/// Creates a new standalone (unbound) query builder that
		/// composes a query without a target repository.
		/// </summary>
		/// <remarks>
		/// The terminal methods of a standalone builder throw
		/// <see cref="InvalidOperationException"/>. Use this constructor
		/// when you only need to build an <see cref="IQuery"/> to hand
		/// off to a repository's
		/// <c>FindAsync(IQuery, ...)</c> or
		/// <c>GetPageAsync(PageRequest, ...)</c> entry point.
		/// </remarks>
		public QueryBuilder() {
			_query = global::Kista.Query.Empty;
		}

		/// <summary>
		/// Creates a new standalone query builder that starts from the
		/// given query.
		/// </summary>
		/// <param name="query">
		/// The query to use as the starting point, or <c>null</c> for
		/// an empty query.
		/// </param>
		public QueryBuilder(IQuery? query) {
			_query = query ?? global::Kista.Query.Empty;
		}

		/// <summary>
		/// Gets the immutable <see cref="IQuery"/> that represents the
		/// accumulated filter and ordering of this builder.
		/// </summary>
		/// <remarks>
		/// The returned value is a snapshot of the current state of the
		/// builder. Subsequent <c>Where</c> or <c>OrderBy</c> calls on the
		/// builder do not mutate the previously returned <see cref="IQuery"/>.
		/// </remarks>
		public IQuery Query => _query;

		IQueryFilter? IQuery.Filter => _query.Filter;

		IQueryOrder? IQuery.Order => _query.Order;

		/// <summary>
		/// Thrown by the default terminal-method implementation when the
		/// builder is not bound to a repository.
		/// </summary>
		private static InvalidOperationException NotBoundException() =>
			new("This query builder is not bound to a repository. Use Repository.CreateQuery() to obtain a bound builder.");

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
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="filter"/> is <c>null</c>.
		/// </exception>
		public virtual QueryBuilder<TEntity> Where(Expression<Func<TEntity, bool>> filter) {
			ArgumentNullException.ThrowIfNull(filter);

			return Where(QueryFilter.Where<TEntity>(filter));
		}

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
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="filter"/> is <c>null</c>.
		/// </exception>
		public virtual QueryBuilder<TEntity> Where(IQueryFilter filter) {
			ArgumentNullException.ThrowIfNull(filter);

			_query = _query.HasFilter()
				? new Query(QueryFilter.Combine(_query.Filter ?? QueryFilter.Empty, filter), _query.Order)
				: new Query(filter, _query.Order);

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
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="field"/> is <c>null</c>.
		/// </exception>
		public virtual QueryBuilder<TEntity> OrderBy(Expression<Func<TEntity, object?>> field, SortDirection direction = SortDirection.Ascending) {
			ArgumentNullException.ThrowIfNull(field);

			return OrderBy(QueryOrder.OrderBy(field, direction));
		}

		/// <summary>
		/// Orders in a descending order the results of the
		/// query by the given field.
		/// </summary>
		/// <param name="field">
		/// The expression used to select the field to sort by.
		/// </param>
		/// <returns>
		/// Returns this query builder with the new sort
		/// for chaining calls.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="field"/> is <c>null</c>.
		/// </exception>
		public virtual QueryBuilder<TEntity> OrderByDescending(Expression<Func<TEntity, object?>> field) {
			ArgumentNullException.ThrowIfNull(field);

			return OrderBy(field, SortDirection.Descending);
		}

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
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="sort"/> is <c>null</c>.
		/// </exception>
		public virtual QueryBuilder<TEntity> OrderBy(IQueryOrder sort) {
			ArgumentNullException.ThrowIfNull(sort);

			var combinedOrder = _query.Order?.Combine(sort) ?? sort;
			_query = new Query(_query.Filter ?? QueryFilter.Empty, combinedOrder);

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
		/// <exception cref="ArgumentException">
		/// Thrown when <paramref name="fieldName"/> is <c>null</c>,
		/// empty, or whitespace.
		/// </exception>
		public virtual QueryBuilder<TEntity> OrderBy(string fieldName, SortDirection direction = SortDirection.Ascending) {
			ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

			return OrderBy(QueryOrder.OrderBy(fieldName, direction));
		}

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
		/// <exception cref="ArgumentException">
		/// Thrown when <paramref name="fieldName"/> is <c>null</c>,
		/// empty, or whitespace.
		/// </exception>
		public virtual QueryBuilder<TEntity> OrderByDescending(string fieldName) {
			ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

			return OrderBy(fieldName, SortDirection.Descending);
		}

		/// <inheritdoc />
		IQueryBuilder<TEntity> IQueryBuilder<TEntity>.Where(Expression<Func<TEntity, bool>> filter) => Where(filter);

		/// <inheritdoc />
		IQueryBuilder<TEntity> IQueryBuilder<TEntity>.Where(IQueryFilter filter) => Where(filter);

		/// <inheritdoc />
		IQueryBuilder<TEntity> IQueryBuilder<TEntity>.OrderBy(Expression<Func<TEntity, object?>> field, SortDirection direction) => OrderBy(field, direction);

		/// <inheritdoc />
		IQueryBuilder<TEntity> IQueryBuilder<TEntity>.OrderByDescending(Expression<Func<TEntity, object?>> field) => OrderByDescending(field);

		/// <inheritdoc />
		IQueryBuilder<TEntity> IQueryBuilder<TEntity>.OrderBy(IQueryOrder sort) => OrderBy(sort);

		/// <inheritdoc />
		IQueryBuilder<TEntity> IQueryBuilder<TEntity>.OrderBy(string fieldName, SortDirection direction) => OrderBy(fieldName, direction);

		/// <inheritdoc />
		IQueryBuilder<TEntity> IQueryBuilder<TEntity>.OrderByDescending(string fieldName) => OrderByDescending(fieldName);

		/// <summary>
		/// Executes the query and returns the first entity that matches
		/// the filter, or <c>null</c> if none found.
		/// </summary>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns the first entity matching the query, or <c>null</c>
		/// if no entity matches.
		/// </returns>
		/// <exception cref="InvalidOperationException">
		/// Thrown by the default implementation when the builder is
		/// not bound to a repository. The private nested
		/// <c>Repository.QueryBuilder</c> overrides this to dispatch
		/// through the protected repository pipeline.
		/// </exception>
		public virtual ValueTask<TEntity?> FirstOrDefaultAsync(CancellationToken cancellationToken = default) {
			throw NotBoundException();
		}

		/// <summary>
		/// Executes the query and returns all matching entities as a
		/// read-only list.
		/// </summary>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns a read-only list of all entities matching the query.
		/// </returns>
		/// <exception cref="InvalidOperationException">
		/// Thrown by the default implementation when the builder is
		/// not bound to a repository.
		/// </exception>
		public virtual ValueTask<IReadOnlyList<TEntity>> ToListAsync(CancellationToken cancellationToken = default) {
			throw NotBoundException();
		}

		/// <summary>
		/// Counts the number of entities that match the filter.
		/// </summary>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns the number of entities matching the filter.
		/// </returns>
		/// <exception cref="InvalidOperationException">
		/// Thrown by the default implementation when the builder is
		/// not bound to a repository.
		/// </exception>
		public virtual ValueTask<long> CountAsync(CancellationToken cancellationToken = default) {
			throw NotBoundException();
		}

		/// <summary>
		/// Determines if at least one entity matches the filter.
		/// </summary>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns <c>true</c> if at least one entity matches the filter,
		/// otherwise <c>false</c>.
		/// </returns>
		/// <exception cref="InvalidOperationException">
		/// Thrown by the default implementation when the builder is
		/// not bound to a repository.
		/// </exception>
		public virtual ValueTask<bool> AnyAsync(CancellationToken cancellationToken = default) {
			throw NotBoundException();
		}

		/// <summary>
		/// Executes the query and returns a page of results.
		/// </summary>
		/// <param name="page">
		/// The number of the page to return (1-based).
		/// </param>
		/// <param name="size">
		/// The maximum size of the page.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns a <see cref="PageResult{TEntity}"/> containing the
		/// page items and pagination metadata.
		/// </returns>
		/// <exception cref="InvalidOperationException">
		/// Thrown by the default implementation when the builder is
		/// not bound to a repository.
		/// </exception>
		public virtual ValueTask<PageResult<TEntity>> GetPageAsync(int page, int size, CancellationToken cancellationToken = default) {
			throw NotBoundException();
		}
	}
}
