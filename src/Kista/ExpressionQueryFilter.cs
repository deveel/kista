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
    /// An implementation of a query filter that uses a lambda expression
    /// </summary>
    /// <typeparam name="TEntity">The type of entity to construct
    /// the field</typeparam>
    public sealed class ExpressionQueryFilter<TEntity> : IExpressionQueryFilter, IQueryableFilter<TEntity> where TEntity : class {
		/// <summary>
		/// Constructs the filter with the given expression
		/// </summary>
		/// <param name="expr">
		/// The expression that is used to filter the entities
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// Thrown if the expression is <c>null</c>.
		/// </exception>
		public ExpressionQueryFilter(Expression<Func<TEntity, bool>> expr) {
			Expression = expr ?? throw new ArgumentNullException(nameof(expr));
		}

		/// <summary>
		/// Gets the lambda filter expression
		/// </summary>
		public Expression<Func<TEntity, bool>> Expression { get; }

		IQueryable<TEntity> IQueryableFilter<TEntity>.Apply(IQueryable<TEntity> queryable) {
			return queryable.Where(Expression);
		}

		Expression<Func<T, bool>> IExpressionQueryFilter.AsLambda<T>()
			=> Expression.As<T>();
	}
}