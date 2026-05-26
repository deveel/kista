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

using System.Linq.Expressions;

namespace Kista {
	/// <summary>
	/// A utility class that provides methods to create
	/// sort rules to apply to a query.
	/// </summary>
	public static class QueryOrder {
		/// <summary>
		/// Creates a sort rule that will order the results
		/// of a query by the given field.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity of a collection that is the target
		/// of the sorting and that defines the field to select.
		/// </typeparam>
		/// <param name="field">
		/// The expression that selects the field to sort.
		/// </param>
		/// <param name="direction">
		/// The direction of the sorting (by default is ascending).
		/// </param>
		/// <returns>
		/// Returns an instance of <see cref="ExpressionSort{TEntity}"/>
		/// that represents the sorting rule.
		/// </returns>
		public static ExpressionSort<TEntity> OrderBy<TEntity>(Expression<Func<TEntity, object?>> field, SortDirection direction = SortDirection.Ascending)
			where TEntity : class {
			return new ExpressionSort<TEntity>(field, direction);
		}

		/// <summary>
		/// Creates a sort rule that will order the results
		/// of a query by the given field in descending order.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity of a collection that is the target
		/// of the sorting and that defines the field to select.
		/// </typeparam>
		/// <param name="field">
		/// The expression that selects the field to sort.
		/// </param>
		/// <returns>
		/// Returns an instance of <see cref="ExpressionSort{TEntity}"/>
		/// that represents the sorting rule.
		/// </returns>
		public static ExpressionSort<TEntity> OrderByDescending<TEntity>(Expression<Func<TEntity, object?>> field)
			where TEntity : class
			=> OrderBy(field, SortDirection.Descending);

		/// <summary>
		/// Creates a sort rule that will order the results
		/// by the given field name.
		/// </summary>
		/// <param name="fieldName">
		/// The name of the field to sort by.
		/// </param>
		/// <param name="direction">
		/// The direction of the sorting (by default is ascending).
		/// </param>
		/// <returns>
		/// Returns an instance of <see cref="FieldOrder"/> that
		/// represents the sorting rule.
		/// </returns>
		/// <seealso cref="FieldOrder(string,SortDirection)"/>
		public static FieldOrder OrderBy(string fieldName, SortDirection direction = SortDirection.Ascending)
			=> new FieldOrder(fieldName, direction);

		/// <summary>
		/// Creates a sort rule that will order the results
		/// of a query by the given field in descending order.
		/// </summary>
		/// <param name="fieldName">
		/// The name of the field to sort by.
		/// </param>
		/// <returns>
		/// Returns an instance of <see cref="FieldOrder"/> that
		/// represents the sorting rule.
		/// </returns>
		/// <seealso cref="FieldOrder(string,SortDirection)"/>
		public static FieldOrder OrderByDescending(string fieldName)
			=> OrderBy(fieldName, SortDirection.Descending);

		/// <summary>
		/// Combines the given sort rules into a single
		/// object that represents the combination of the rules.
		/// </summary>
		/// <param name="sort">
		/// The first sort rule to combine.
		/// </param>
		/// <param name="other">
		/// The second sort rule to combine.
		/// </param>
		/// <returns>
		/// Returns an instance of <see cref="CombinedOrder"/> that
		/// represents the combination of the given rules.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown if either of the given sort rules is <c>null</c>.
		/// </exception>
		public static CombinedOrder Combine(this IQueryOrder sort, IQueryOrder other) {
			ArgumentNullException.ThrowIfNull(sort, nameof(sort));
			ArgumentNullException.ThrowIfNull(other, nameof(other));

			if (sort is CombinedOrder combinedSort)
				return combinedSort.Combine(other);

			return new CombinedOrder(new[] {sort, other});
		}

		/// <summary>
		/// Determines if the given sort rule is ascending.
		/// </summary>
		/// <param name="sort">
		/// The sort rule to check.
		/// </param>
		/// <returns>
		/// Returns <c>true</c> if the given sort rule is ascending,
		/// or <c>false</c> if it is descending or not directional.
		/// </returns>
		public static bool IsAscending(this IQueryOrder sort) => (sort as IDirectionalOrder)?.Direction == SortDirection.Ascending;

		/// <summary>
		/// Determines if the given sort rule is descending.
		/// </summary>
		/// <param name="sort">
		/// The sort rule to check.
		/// </param>
		/// <returns>
		/// Returns <c>true</c> if the given sort rule is descending,
		/// or <c>false</c> if it is ascending or not directional.
		/// </returns>
		public static bool IsDescending(this IQueryOrder sort) => (sort as IDirectionalOrder)?.Direction == SortDirection.Descending;

		/// <summary>
		/// Applies the given sort rule to the given queryable
		/// object that represents a query.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity of a collection that is the target
		/// of the sorting.
		/// </typeparam>
		/// <param name="sort">
		/// The sort rule or combination of rules to apply.
		/// </param>
		/// <param name="queryable">
		/// The queryable object to sort.
		/// </param>
		/// <param name="fieldMapper">
		/// An optional field mapper to use to map the field names
		/// defined by <see cref="FieldOrder"/> instances to expressions
		/// that select the field to sort.
		/// </param>
		/// <returns>
		/// Returns the <see cref="IQueryable{TEntity}"/> that was
		/// sorted by the given rule.
		/// </returns>
		public static IQueryable<TEntity> Apply<TEntity>(this IQueryOrder sort, IQueryable<TEntity> queryable, Func<string, Expression<Func<TEntity, object?>>> fieldMapper)
			where TEntity : class
			=> Apply(sort, queryable, new DelegatedFieldMapper<TEntity>(fieldMapper));

		/// <summary>
		/// Applies the given sort rule to the given queryable
		/// object that represents a query.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity of a collection that is the target
		/// of the sorting.
		/// </typeparam>
		/// <param name="sort">
		/// The sort rule or combination of rules to apply.
		/// </param>
		/// <param name="queryable">
		/// The queryable object to sort.
		/// </param>
		/// <param name="fieldMapper">
		/// An optional field mapper to use to map the field names
		/// defined by <see cref="FieldOrder"/> instances to expressions
		/// that select the field to sort. When not specified, reflection 
		/// is used to map the field names from the members of the type.
		/// </param>
		/// <returns>
		/// Returns the <see cref="IQueryable{TEntity}"/> that was
		/// the result of the application of the given sort rule.
		/// </returns>
		public static IQueryable<TEntity> Apply<TEntity>(this IQueryOrder sort, IQueryable<TEntity> queryable, IFieldMapper<TEntity>? fieldMapper = null) 
			where TEntity : class {
			ArgumentNullException.ThrowIfNull(queryable, nameof(queryable));
			ArgumentNullException.ThrowIfNull(sort, nameof(sort));

			if (sort is CombinedOrder combinedSort) {
				foreach (var item in combinedSort) {
					queryable = item.Apply(queryable);
				}
			}

			if (sort is FieldOrder fieldSort)
				sort = fieldSort.Map(fieldMapper ?? new ReflectionFieldMapper<TEntity>());

			if (sort is IQueryableOrderBy<TEntity> queryableSort)
				return queryableSort.Apply(queryable);

			return queryable;
		} 
	}
}
