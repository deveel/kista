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

namespace Deveel.Data {
	/// <summary>
	/// Defines a cache for parsed <see cref="LambdaExpression"/> objects used in
	/// dynamic LINQ filter evaluation.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This interface addresses the parsing stage of the Dynamic LINQ pipeline.
	/// When the same filter expression string is used repeatedly (common in multi-tenant
	/// applications where the same query shape runs thousands of times per minute),
	/// caching the parsed expression avoids the cost of re-invoking
	/// <c>DynamicExpressionParser.ParseLambda</c> on each call.
	/// </para>
	/// <para>
	/// The cache key is typically a composite of the entity type full name, the
	/// parameter name, and the expression string, ensuring that expressions for
	/// different entity types or parameters do not collide.
	/// </para>
	/// <para>
	/// Implementations must be thread-safe, as filter expressions may be compiled
	/// concurrently across multiple request threads. The built-in
	/// <see cref="BoundedExpressionCache"/> provides a thread-safe LRU implementation.
	/// </para>
	/// </remarks>
	/// <example>
	/// Register a bounded expression cache in the DI container:
	/// <code>
	/// services.AddFilterCache(maxCapacity: 2048);
	/// </code>
	/// Then resolve it and pass it to <see cref="DynamicLinqFilter"/>:
	/// <code>
	/// var cache = serviceProvider.GetRequiredService&lt;IExpressionCache&gt;();
	/// var filter = new DynamicLinqFilter("x.Status == \"Active\"", cache);
	/// </code>
	/// </example>
	/// <seealso cref="IFilterCache"/>
	/// <seealso cref="IFilterCacheStatistics"/>
	/// <seealso cref="BoundedExpressionCache"/>
	/// <seealso cref="ServiceCollectionExtensions"/>
	public interface IExpressionCache {
		/// <summary>
		/// Attempts to retrieve a previously parsed <see cref="LambdaExpression"/>
		/// from the cache.
		/// </summary>
		/// <param name="key">
		/// The unique cache key identifying the expression. This is typically a
		/// composite string that includes the entity type, parameter name, and
		/// expression text.
		/// </param>
		/// <param name="expression">
		/// When this method returns, contains the cached <see cref="LambdaExpression"/>
		/// if the key was found; otherwise, <c>null</c>.
		/// </param>
		/// <returns>
		/// <c>true</c> if the cache contained an entry for the specified key;
		/// otherwise, <c>false</c>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="key"/> is <c>null</c>.
		/// </exception>
		/// <remarks>
		/// Implementations that support LRU eviction should update the access order
		/// of the entry when a hit occurs, promoting it to the most-recently-used position.
		/// </remarks>
		bool TryGet(string key, out LambdaExpression? expression);

		/// <summary>
		/// Stores a parsed <see cref="LambdaExpression"/> in the cache.
		/// </summary>
		/// <param name="key">
		/// The unique cache key identifying the expression.
		/// </param>
		/// <param name="expression">
		/// The parsed lambda expression to store.
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="key"/> or <paramref name="expression"/> is <c>null</c>.
		/// </exception>
		/// <remarks>
		/// If the key already exists, the implementation should update the stored value
		/// and refresh its access order. If the cache is at capacity, bounded implementations
		/// should evict the least recently used entry before inserting the new one.
		/// </remarks>
		void Set(string key, LambdaExpression expression);

		/// <summary>
		/// Gets the statistics for this cache instance, if the implementation
		/// tracks hit/miss counters.
		/// </summary>
		/// <value>
		/// An <see cref="IFilterCacheStatistics"/> instance when statistics tracking
		/// is supported; otherwise, <c>null</c>.
		/// </value>
		/// <remarks>
		/// The default implementation returns <c>null</c>. Bounded cache implementations
		/// such as <see cref="BoundedExpressionCache"/> always provide statistics.
		/// </remarks>
		IFilterCacheStatistics? Statistics => null;

		/// <summary>
		/// Removes all entries from the cache.
		/// </summary>
		/// <remarks>
		/// This method does not reset the <see cref="Statistics"/> counters.
		/// Use <see cref="IFilterCacheStatistics.Reset"/> to clear statistics separately.
		/// </remarks>
		void Clear();
	}
}
