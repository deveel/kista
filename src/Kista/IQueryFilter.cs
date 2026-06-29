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
	/// A marker interface that is implemented by objects
	/// representing filters of a query to a repository
	/// </summary>
	/// <remarks>
	/// <para>
	/// Implementations can override <see cref="Initialize"/> to receive a
	/// <see cref="IFilterContext"/> before the filter is applied to a query.
	/// This allows filters to resolve supporting infrastructure services
	/// such as expression caches from the repository's service provider.
	/// </para>
	/// <para>
	/// The default implementation of <see cref="Initialize"/> is a no-op,
	/// so existing filter implementations continue to work without modification.
	/// </para>
	/// </remarks>
	/// <seealso cref="IFilterContext"/>
	/// <seealso cref="IExpressionQueryFilter"/>
	/// <seealso cref="IQueryableFilter{TEntity}"/>
	public interface IQueryFilter {
		/// <summary>
		/// Initializes the filter with the given context before it is applied
		/// to a query.
		/// </summary>
		/// <param name="context">
		/// The filter context providing access to the repository's service
		/// provider and other infrastructure services.
		/// </param>
		/// <remarks>
		/// <para>
		/// This method is called by repository implementations immediately
		/// before applying the filter to a query. Filters can use this opportunity
		/// to resolve services such as expression caches or filter cached
		/// that enable optimizations like expression caching.
		/// </para>
		/// <para>
		/// The default implementation does nothing. Override this method only
		/// when the filter needs to resolve services from the context.
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>
		/// public class MyFilter : IQueryFilter {
		///     private IExpressionCache? _cache;
		/// 
		///     public void Initialize(IFilterContext context) {
		///         _cache = context.Services.GetService&lt;IExpressionCache&gt;();
		///     }
		/// }
		/// </code>
		/// </example>
		void Initialize(IFilterContext context) { }
	}
}