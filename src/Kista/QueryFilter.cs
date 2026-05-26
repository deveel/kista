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

using System;
using System.Linq.Expressions;

namespace Kista {
	/// <summary>
	/// A utility class that provides a set of static methods to create
	/// default types of query filters.
	/// </summary>
	public static class QueryFilter {
		/// <summary>
		/// Identifies an empty query filter, that implementations
		/// of the <see cref="IFilterableRepository{TEntity}"/> can use to
		/// convert to a default query.
		/// </summary>
		public static readonly IQueryFilter Empty = new EmptyQueryFilter();

		/// <summary>
		/// Determines if the given filter is the empty one.
		/// </summary>
		/// <param name="filter">
		/// The filter to check if it is the empty one.
		/// </param>
		/// <remarks>
		/// The method verifies if the reference of the given filter
		/// if the same of the <see cref="Empty"/> one.
		/// </remarks>
		/// <returns>
		/// Returns <c>true</c> if the given filter is the empty one,
		/// or <c>false</c> otherwise.
		/// </returns>
		public static bool IsEmpty(this IQueryFilter filter) => Equals(filter, Empty);

		/// <summary>
		/// Converts the given filter to a LINQ expression that can be
		/// used to filter a <see cref="IQueryable{TEntity}"/> storage
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity to filter.
		/// </typeparam>
		/// <param name="filter">
		/// The instance of the filter to convert to a LINQ expression.
		/// </param>
		/// <remarks>
		/// If the given filter is the empty one, the method returns
		/// a lambda expression that always returns <c>true</c>.
		/// </remarks>
		/// <returns>
		/// Returns an instance of <see cref="Expression{TDelegate}"/> that
		/// is obtained from the conversion of the given filter.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown if the given filter is <c>null</c>.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// Thrown when the given filter is not an instance of <see cref="ExpressionQueryFilter{TEntity}"/>.
		/// </exception>
		public static Expression<Func<TEntity, bool>> AsLambda<TEntity>(this IQueryFilter filter)
			where TEntity : class {
			ArgumentNullException.ThrowIfNull(filter, nameof(filter));

			if (filter.IsEmpty())
				return x => true;

			if (!(filter is IExpressionQueryFilter filterExpr))
				throw new ArgumentException("Only expression query filters are supported");

			return filterExpr.AsLambda<TEntity>();
		}

		/// <summary>
		/// Constructs a new query filter that is built from the given
		/// LINQ expression.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of the entity that is the target of the filter.
		/// </typeparam>
		/// <param name="exp">
		/// The lambda expression that defines the filter.
		/// </param>
		/// <remarks>
		/// Various implementations of <see cref="IFilterableRepository{TEntity}"/> can support
		/// LINQ expressions to define the filter to apply to the query, and this
		/// method provides a factory to create a default implementation of
		/// this kind of filter.
		/// </remarks>
		/// <returns>
		/// Returns a new instance of <see cref="ExpressionQueryFilter{TEntity}"/>
		/// wrapping the given expression.
		/// </returns>
		public static ExpressionQueryFilter<TEntity> Where<TEntity>(Expression<Func<TEntity, bool>> exp)
			where TEntity : class
			=> new ExpressionQueryFilter<TEntity>(exp);

		/// <summary>
		/// Applies the filter to the given queryable object, producing
		/// a result that is the filtered query.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of the entity that is the target of the filter.
		/// </typeparam>
		/// <param name="filter">
		/// The filter to apply to the query.
		/// </param>
		/// <param name="queryable">
		/// The queryable object to filter.
		/// </param>
		/// <returns>
		/// Returns an instance of <see cref="IQueryable{TEntity}"/> that is
		/// the result of the application of the given filter to the queryable.
		/// </returns>
		public static IQueryable<TEntity> Apply<TEntity>(this IQueryFilter filter, IQueryable<TEntity> queryable) where TEntity : class {
			ArgumentNullException.ThrowIfNull(filter);

			if (filter.IsEmpty())
				return queryable;

			if (filter is CombinedQueryFilter combined) {
				foreach (var f in combined) {
					queryable = f.Apply(queryable);
				}

				return queryable;
			}

			if (filter is IQueryableFilter<TEntity> filterQueryable)
				return filterQueryable.Apply(queryable);
			if (filter is IExpressionQueryFilter filterExpr)
				return queryable.Where(filterExpr.AsLambda<TEntity>());

			throw new ArgumentException($"The given filter is not supported: {filter.GetType()}");
		}

		/// <summary>
		/// Combines the list of filters into a single one.
		/// </summary>
		/// <param name="filters">
		/// The list of filters to combine.
		/// </param>
		/// <returns>
		/// Returns a <see cref="IQueryFilter"/> that is the result of the
		/// combination of the given filters.
		/// </returns>
		public static IQueryFilter Combine(IEnumerable<IQueryFilter> filters) {
			IQueryFilter? result = null;

			foreach (var filter in filters) {
				if (result  == null)
					result = filter;
				else
					result = Combine(result, filter);
			}

			return result == null ? Empty : result;
		}

		/// <summary>
		/// Combines the list of filters into a single one.
		/// </summary>
		/// <param name="filters">
		/// The list of filters to combine.
		/// </param>
		/// <returns>
		/// Returns a <see cref="IQueryFilter"/> that is the result of the
		/// combination of the given filters.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown if the given list of filters is <c>null</c>.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// Thrown if the given list of filters is empty.
		/// </exception>
		public static IQueryFilter Combine(params IQueryFilter[] filters) {
            ArgumentNullException.ThrowIfNull(filters);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(filters.Length, 0);

			return Combine((IEnumerable<IQueryFilter>)filters);
		}

		/// <summary>
		/// Combines the two filters into a single one.
		/// </summary>
		/// <param name="filter1">
		/// The first filter to combine.
		/// </param>
		/// <param name="filter2">
		/// The second filter to combine.
		/// </param>
		/// <returns>
		/// Returns an <see cref="IQueryFilter"/> that represents the combined filter.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown if either of the given filters is <c>null</c>.
		/// </exception>
		public static CombinedQueryFilter Combine(IQueryFilter filter1, IQueryFilter filter2) {
            ArgumentNullException.ThrowIfNull(filter1);
            ArgumentNullException.ThrowIfNull(filter2);

			if (filter1.IsEmpty() && filter2.IsEmpty())
				throw new ArgumentException("Cannot combine two empty filters");

			var filters = new List<IQueryFilter>();

			if (filter1 is CombinedQueryFilter combined1) {
				filters.AddRange(combined1);
			} else if (!filter1.IsEmpty()) {
				filters.Add(filter1);
			}

			if (filter2 is CombinedQueryFilter combined2) {
				filters.AddRange(combined2);
			} else if (!filter2.IsEmpty()) {
				filters.Add(filter2);
			}

			return new CombinedQueryFilter(filters);
		}

		readonly struct EmptyQueryFilter : IQueryFilter {
		}
	}
}