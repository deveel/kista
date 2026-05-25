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
	/// A type of query filter that is convertible
	/// to a LINQ lambda expression for filtering.
	/// </summary>
	public interface IExpressionQueryFilter : IQueryFilter {
		/// <summary>
		/// Converts the filter to a LINQ expression that can be
		/// used to filter a <see cref="IQueryable{TEntity}"/> storage
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of the entity to be filtered
		/// </typeparam>
		/// <returns>
		/// Returns an instance of <see cref="LambdaExpression"/> that
		/// can be used to filter a <see cref="IQueryable{TEntity}"/>.
		/// </returns>
		Expression<Func<TEntity, bool>> AsLambda<TEntity>()
			where TEntity : class;
	}
}
