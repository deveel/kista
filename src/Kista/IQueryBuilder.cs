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
	/// <see cref="IQueryBuilder{TEntity}"/> is the consumer-facing surface that
	/// replaces the obsolete <c>AsQueryable()</c> hatch. It lets callers compose
	/// a query (filters and ordering) and execute it through the terminal
	/// methods (<see cref="FirstOrDefaultAsync"/>, <see cref="ToListAsync"/>,
	/// <see cref="CountAsync"/>, <see cref="AnyAsync"/>,
	/// <see cref="GetPageAsync(int, int, CancellationToken)"/>) without ever
	/// receiving an <see cref="IQueryable{T}"/>.
	/// </para>
	/// <para>
	/// <b>Thread-safety and mutability.</b> Implementations of this interface
	/// are stateful: every <c>Where</c> or <c>OrderBy</c> call mutates the
	/// internal query. A builder instance is therefore <b>not thread-safe</b>
	/// and must not be shared across threads or executed concurrently from
	/// different call sites. Build a new builder per logical operation.
	/// </para>
	/// <para>
	/// The built query is exposed through the <see cref="Query"/> property as
	/// an <see cref="IQuery"/>, which can be reused by a different repository
	/// of the same entity type or handed to a legacy code path that still
	/// accepts an <see cref="IQuery"/>.
	/// </para>
	/// </remarks>
	public interface IQueryBuilder<TEntity> : IQuery where TEntity : class {
		/// <summary>
		/// Gets the immutable <see cref="IQuery"/> that represents the
		/// accumulated filter and ordering of this builder.
		/// </summary>
		/// <remarks>
		/// The returned value is a snapshot of the current state of the
		/// builder. Subsequent <c>Where</c> or <c>OrderBy</c> calls on the
		/// builder do not mutate the previously returned <see cref="IQuery"/>.
		/// </remarks>
		IQuery Query { get; }

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
		IQueryBuilder<TEntity> Where(Expression<Func<TEntity, bool>> filter);

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
		IQueryBuilder<TEntity> Where(IQueryFilter filter);

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
		IQueryBuilder<TEntity> OrderBy(Expression<Func<TEntity, object?>> field, SortDirection direction = SortDirection.Ascending);

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
		IQueryBuilder<TEntity> OrderByDescending(Expression<Func<TEntity, object?>> field);

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
		IQueryBuilder<TEntity> OrderBy(IQueryOrder sort);

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
		IQueryBuilder<TEntity> OrderBy(string fieldName, SortDirection direction = SortDirection.Ascending);

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
		IQueryBuilder<TEntity> OrderByDescending(string fieldName);

		/// <summary>
		/// Executes the query and returns the first entity
		/// that matches the filter, or <c>null</c> if none found.
		/// </summary>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns the first entity matching the query,
		/// or <c>null</c> if no entity matches.
		/// </returns>
		ValueTask<TEntity?> FirstOrDefaultAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Executes the query and returns all matching entities
		/// as a read-only list.
		/// </summary>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns a read-only list of all entities matching the query.
		/// </returns>
		ValueTask<IReadOnlyList<TEntity>> ToListAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Counts the number of entities that match the filter.
		/// </summary>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns the number of entities matching the filter.
		/// </returns>
		ValueTask<long> CountAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Determines if at least one entity matches the filter.
		/// </summary>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns <c>true</c> if at least one entity matches
		/// the filter, otherwise <c>false</c>.
		/// </returns>
		ValueTask<bool> AnyAsync(CancellationToken cancellationToken = default);

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
		/// Returns a <see cref="PageResult{TEntity}"/> containing
		/// the page items and pagination metadata.
		/// </returns>
		ValueTask<PageResult<TEntity>> GetPageAsync(int page, int size, CancellationToken cancellationToken = default);
	}
}
