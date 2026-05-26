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
	/// A sorting rule that references a field by
	/// using an expression.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of entity of a collection that is the target
	/// of the sorting and that defines the field to select.
	/// </typeparam>
	public sealed class ExpressionSort<TEntity> : IDirectionalOrder, IQueryableOrderBy<TEntity> where TEntity : class {
		/// <summary>
		/// Constructs the sorting rule using the given expression
		/// that selects the field to sort.
		/// </summary>
		/// <param name="field">
		/// The expression that selects the field to sort.
		/// </param>
		/// <param name="direction">
		/// The direction of the sorting.
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// Thrown if the given <paramref name="field"/> is <c>null</c>.
		/// </exception>
		public ExpressionSort(Expression<Func<TEntity, object?>> field, SortDirection direction = SortDirection.Ascending) {
			ArgumentNullException.ThrowIfNull(field, nameof(field));

			Field = field;
			Direction = direction;
		}

		/// <summary>
		/// Gets the expression that selects the field to sort.
		/// </summary>
		public Expression<Func<TEntity, object?>> Field { get; }

		/// <summary>
		/// Gets the direction of the sorting.
		/// </summary>
		public SortDirection Direction { get; }

		/// <inheritdoc/>
		IQueryable<TEntity> IQueryableOrderBy<TEntity>.Apply(IQueryable<TEntity> queryable) {
			if (queryable.Expression.Type == typeof(IOrderedQueryable<TEntity>)) {
				var orderedEnumerable = (IOrderedQueryable<TEntity>) queryable;
				if (Direction == SortDirection.Ascending) {
					queryable = orderedEnumerable.ThenBy(Field);
				} else {
					queryable = orderedEnumerable.ThenByDescending(Field);
				}
			} else {
				if (Direction == SortDirection.Ascending) {
					queryable = queryable.OrderBy(Field);
				} else {
					queryable = queryable.OrderByDescending(Field);
				}
			}

			return queryable;
		}
	}
}
