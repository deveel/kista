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

using System.Collections;
using System.Linq.Expressions;

namespace Deveel.Data {
	/// <summary>
	/// An object that combines multiple <see cref="IQueryFilter"/> objects
	/// into a single one.
	/// </summary>
	public sealed class CombinedQueryFilter : IExpressionQueryFilter, IEnumerable<IQueryFilter> {
		private readonly IReadOnlyList<IQueryFilter> filters;

		/// <summary>
		/// Constructs the filter by combining the given list of filters.
		/// </summary>
		/// <param name="filters">
		/// The list of filters to combine.
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// If the given list of filters is <c>null</c>.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// Thrown if the given list of filters is empty.
		/// </exception>
		public CombinedQueryFilter(ICollection<IQueryFilter> filters) {
            ArgumentNullException.ThrowIfNull(filters);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(filters.Count, 0, nameof(filters));
            
			this.filters = filters.ToList().AsReadOnly();
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

			var filters = new List<IQueryFilter>(this.filters);
			if (filter is CombinedQueryFilter combined) {
				filters.AddRange(combined.filters);
			} else {
				filters.Add(filter);
			}
			
			return new CombinedQueryFilter(filters);
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
					var lambdaParam = lambda.Parameters[0];
					if (lambdaParam.Name != result.Parameters[0].Name)
						throw new InvalidOperationException("The parameters of the filters are not the same");

					var expr = Expression.AndAlso(result.Body, lambda.Body);
					result = Expression.Lambda<Func<TEntity, bool>>(expr, lambdaParam);
				}
			}

			return result ?? throw new InvalidOperationException("No filters were combined");
		}
	}
}
