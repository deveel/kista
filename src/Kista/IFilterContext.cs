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

namespace Kista {
	/// <summary>
	/// Provides a context through which <see cref="IQueryFilter"/> implementations
	/// can resolve supporting infrastructure services during query execution.
	/// </summary>
	/// <remarks>
	/// <para>
	/// When a repository applies a filter to a query, it calls <see cref="IQueryFilter.Initialize"/>
	/// passing an <see cref="IFilterContext"/>. Filters can use this context to resolve
	/// services such as <see cref="IExpressionCache"/> or <see cref="IFilterCache"/>
	/// that enable optimizations like expression caching.
	/// </para>
	/// <para>
	/// The <see cref="Services"/> property exposes the repository's service provider,
	/// allowing filters to resolve any registered service. This enables a clean separation
	/// where filters declare their dependencies through the context rather than requiring
	/// callers to manually resolve and pass infrastructure instances.
	/// </para>
	/// </remarks>
	/// <example>
	/// A custom filter resolving a cache from the context:
	/// <code>
	/// public class CachedDynamicFilter : IQueryFilter {
	///     private IExpressionCache? _cache;
	/// 
	///     public void Initialize(IFilterContext context) {
	///         _cache = context.Services.GetService&lt;IExpressionCache&gt;();
	///     }
	/// }
	/// </code>
	/// </example>
	/// <seealso cref="IQueryFilter"/>
	/// <seealso cref="DefaultFilterContext"/>
	public interface IFilterContext {
		/// <summary>
		/// Gets the service provider used by the repository to resolve
		/// infrastructure services.
		/// </summary>
		/// <value>
		/// An <see cref="IServiceProvider"/> instance that can resolve services
		/// such as <see cref="IExpressionCache"/>, <see cref="IFilterCache"/>,
		/// or any other registered infrastructure service.
		/// </value>
		IServiceProvider Services { get; }
	}
}
