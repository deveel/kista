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
	/// A filter that negates the expression of another <see cref="IQueryFilter"/>.
	/// </summary>
	public readonly struct NotQueryFilter : IExpressionQueryFilter {
		private readonly IQueryFilter innerFilter;

		/// <summary>
		/// Constructs the filter that negates the given inner filter.
		/// </summary>
		/// <param name="innerFilter">
		/// The filter to negate.
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// Thrown if the given filter is <c>null</c>.
		/// </exception>
		public NotQueryFilter(IQueryFilter innerFilter) {
			ArgumentNullException.ThrowIfNull(innerFilter);
			this.innerFilter = innerFilter;
		}

		/// <summary>
		/// Gets the inner filter that is negated by this filter.
		/// </summary>
		public IQueryFilter InnerFilter => innerFilter;

		/// <inheritdoc/>
		public void Initialize(IFilterContext context) {
			innerFilter.Initialize(context);
		}

		/// <inheritdoc/>
		public Expression<Func<TEntity, bool>> AsLambda<TEntity>()
			where TEntity : class {

			var innerLambda = innerFilter.AsLambda<TEntity>();
			var param = innerLambda.Parameters[0];
			var body = Expression.Not(innerLambda.Body);
			return Expression.Lambda<Func<TEntity, bool>>(body, param);
		}
	}
}
