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


namespace Kista {
	/// <summary>
	/// Extends the <see cref="IRepository{T}"/> interface with methods
	/// that allow to perform filtering and paging of the data.
	/// </summary>
	public static class PageRequestExtensions {
		/// <summary>
		/// Adds a dynamic filter to the request.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of the entity to filter.
		/// </typeparam>
		/// <param name="request">
		/// The request to add the filter to.
		/// </param>
		/// <param name="paramName">
		/// The name of the parameter to use in the filter expression.
		/// </param>
		/// <param name="expression">
		/// The dynamic LINQ expression to use to filter the data.
		/// </param>
		/// <returns>
		/// Returns the instance of the <see cref="PageQuery{TEntity}"/>
		/// with the filter appended.
		/// </returns>
		public static PageQuery<TEntity> Where<TEntity>(this PageQuery<TEntity> request, string paramName, string expression)
			where TEntity : class
			=> request.Where(FilterExpression.AsLambda<TEntity>(paramName, expression));

		/// <summary>
		/// Adds a dynamic filter to the request.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of the entity to filter.
		/// </typeparam>
		/// <param name="request">
		/// The request to add the filter to.
		/// </param>
		/// <param name="expression">
		/// The dynamic LINQ expression to use to filter the data.
		/// </param>
		/// <remarks>
		/// This overload uses the <see cref="DynamicLinqFilter.DefaultParameterName">default
		/// parameter name</see> for the filter expression.
		/// </remarks>
		/// <returns>
		/// Returns the instance of the <see cref="PageQuery{TEntity}"/>
		/// with the filter appended.
		/// </returns>
		public static PageQuery<TEntity> Where<TEntity>(this PageQuery<TEntity> request, string expression)
			where TEntity : class
			=> request.Where(DynamicLinqFilter.DefaultParameterName, expression);

	}
}
