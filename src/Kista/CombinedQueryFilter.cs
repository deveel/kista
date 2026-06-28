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

using System.Collections;
using System.Linq.Expressions;

namespace Kista {
	/// <summary>
	/// An object that combines multiple <see cref="IQueryFilter"/> objects
	/// into a single one.
	/// </summary>
	public sealed class CombinedQueryFilter : IExpressionQueryFilter, IEnumerable<IQueryFilter> {
		private readonly IReadOnlyList<IQueryFilter> filters;
		private readonly FilterLogicalOperator logicalOperator;

		/// <summary>
		/// Gets the logical operator used to combine the filters.
		/// </summary>
		public FilterLogicalOperator LogicalOperator => logicalOperator;

		/// <summary>
		/// Constructs the filter by combining the given list of filters
		/// using the specified logical operator.
		/// </summary>
		/// <param name="filters">
		/// The list of filters to combine.
		/// </param>
		/// <param name="logicalOperator">
		/// The logical operator to use when combining the filters.
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// If the given list of filters is <c>null</c>.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// Thrown if the given list of filters is empty.
		/// </exception>
		public CombinedQueryFilter(ICollection<IQueryFilter> filters, FilterLogicalOperator logicalOperator = FilterLogicalOperator.And) {
            ArgumentNullException.ThrowIfNull(filters);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(filters.Count, 0, nameof(filters));
            
			this.filters = filters.ToList().AsReadOnly();
			this.logicalOperator = logicalOperator;
		}

		IEnumerator<IQueryFilter> IEnumerable<IQueryFilter>.GetEnumerator() => filters.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => (this as IEnumerable<IQueryFilter>).GetEnumerator();

		/// <summary>
		/// Creates a new combination between the filters
		/// of this object and the given one.
		/// </summary>
		/// <param name="filter">
		/// The filter to combine with this object.
		/// </param>
		/// <returns>
		/// Returns a new <see cref="CombinedQueryFilter"/> that combines
		/// the filters of this object and the given one.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown if the given filter is <c>null</c>.
		/// </exception>
	public CombinedQueryFilter Combine(IQueryFilter filter) {
		ArgumentNullException.ThrowIfNull(filter);

		var combinedFilters = new List<IQueryFilter>(this.filters);
		if (filter is CombinedQueryFilter combined) {
			combinedFilters.AddRange(combined.filters);
		} else {
			combinedFilters.Add(filter);
		}

		return new CombinedQueryFilter(combinedFilters, logicalOperator);
	}

		/// <inheritdoc/>
		public void Initialize(IFilterContext context) {
			foreach (var filter in filters)
				filter.Initialize(context);
		}

		/// <inheritdoc/>
		public Expression<Func<TEntity, bool>> AsLambda<TEntity>()
			where TEntity : class {

			if (filters.Count == 0)
				throw new InvalidOperationException("No filters were combined");

			if (filters.Count == 1)
				return filters[0].AsLambda<TEntity>();

			Expression<Func<TEntity, bool>>? result = null;

			foreach (var filter in filters) {
				if (filter == null || filter.IsEmpty())
					continue;

				var lambda = filter.AsLambda<TEntity>();
				if (result == null) {
					result = lambda;
				} else {
					var resultParam = result.Parameters[0];
					var lambdaBody = ParameterRebinder.ReplaceParameter(lambda.Body, lambda.Parameters[0], resultParam);

					var expr = logicalOperator == FilterLogicalOperator.Or
						? Expression.OrElse(result.Body, lambdaBody)
						: Expression.AndAlso(result.Body, lambdaBody);
					result = Expression.Lambda<Func<TEntity, bool>>(expr, resultParam);
				}
			}

			return result ?? throw new InvalidOperationException("No filters were combined");
		}
	}

	sealed class ParameterRebinder : ExpressionVisitor {
		private readonly ParameterExpression source;
		private readonly ParameterExpression target;

		private ParameterRebinder(ParameterExpression source, ParameterExpression target) {
			this.source = source;
			this.target = target;
		}

		public static Expression ReplaceParameter(Expression expression, ParameterExpression source, ParameterExpression target) {
			return new ParameterRebinder(source, target).Visit(expression);
		}

		protected override Expression VisitParameter(ParameterExpression node) {
			return node == source ? target : base.VisitParameter(node);
		}
	}
}
