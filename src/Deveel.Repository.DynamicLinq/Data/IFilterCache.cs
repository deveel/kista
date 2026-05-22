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

namespace Deveel.Data {
	/// <summary>
	/// Defines a cache for compiled filter expression <see cref="Delegate"/> instances
	/// to avoid the overhead of repeated compilation.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This interface addresses the compilation stage of the Dynamic LINQ pipeline.
	/// When <see cref="FilterExpression.Compile(IFilterCache?, Type[], string[], string)"/> is called with a cache, the compiled
	/// delegate is stored so that subsequent calls with the same expression string can
	/// retrieve it directly without re-parsing or re-compiling.
	/// </para>
	/// <para>
	/// Implementations must be thread-safe, as filter compilation may occur concurrently
	/// across multiple request threads. The built-in <see cref="BoundedFilterCache"/>
	/// provides a thread-safe LRU implementation with configurable capacity and statistics.
	/// </para>
	/// <para>
	/// For caching at the parsing stage (before compilation), see <see cref="IExpressionCache"/>.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// var cache = new BoundedFilterCache(2048);
	/// var func = FilterExpression.Compile(cache, typeof(Person), "p", "p.FirstName == \"John\"");
	/// </code>
	/// </example>
	/// <seealso cref="IFilterCacheStatistics"/>
	/// <seealso cref="IExpressionCache"/>
	/// <seealso cref="BoundedFilterCache"/>
	/// <seealso cref="ServiceCollectionExtensions"/>
	public interface IFilterCache {
		/// <summary>
		/// Attempts to retrieve a previously compiled <see cref="Delegate"/> from the cache.
		/// </summary>
		/// <param name="expression">
		/// The expression string used as the cache key. This should match the string
		/// that was originally passed to <see cref="Set"/>.
		/// </param>
		/// <param name="labda">
		/// When this method returns, contains the cached <see cref="Delegate"/> if the
		/// key was found; otherwise, <c>null</c>.
		/// </param>
		/// <returns>
		/// <c>true</c> if the cache contained an entry for the specified expression;
		/// otherwise, <c>false</c>.
		/// </returns>
		/// <remarks>
		/// Implementations that support LRU eviction should update the access order
		/// of the entry when a hit occurs, promoting it to the most-recently-used position.
		/// </remarks>
		bool TryGet(string expression, out Delegate? labda);

		/// <summary>
		/// Stores a compiled <see cref="Delegate"/> in the cache.
		/// </summary>
		/// <param name="expression">
		/// The expression string to use as the cache key.
		/// </param>
		/// <param name="lambda">
		/// The compiled delegate to store.
		/// </param>
		/// <remarks>
		/// If the key already exists, the implementation should update the stored value
		/// and refresh its access order. If the cache is at capacity, bounded implementations
		/// should evict the least recently used entry before inserting the new one.
		/// </remarks>
		void Set(string expression, Delegate lambda);

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
		/// such as <see cref="BoundedFilterCache"/> always provide statistics.
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
