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
	/// A bounded, thread-safe cache for parsed <see cref="LambdaExpression"/> objects
	/// with LRU (Least Recently Used) eviction policy.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <see cref="BoundedExpressionCache"/> stores parsed expression trees produced by
	/// <c>DynamicExpressionParser.ParseLambda</c>. Caching at the parsing stage avoids
	/// the overhead of re-parsing the same expression string, which involves tokenization,
	/// syntax analysis, and expression tree construction.
	/// </para>
	/// <para>
	/// This cache is used by <see cref="FilterExpression.AsLambda{T}(IExpressionCache?, string, string)"/>
	/// and <see cref="DynamicLinqFilter"/> when an <see cref="IExpressionCache"/> is provided.
	/// The cached expressions can then be compiled into delegates on demand, or the
	/// expression tree can be used directly with <see cref="System.Linq.Queryable"/> filtering methods.
	/// </para>
	/// <para>
	/// The implementation is backed by <see cref="BoundedCache{TValue}"/> which provides the
	/// LRU eviction mechanism and thread safety using a <see cref="Dictionary{TKey,TValue}"/>
	/// for O(1) key lookup and a <see cref="LinkedList{T}"/> to maintain access order.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// // Default capacity (1024 entries)
	/// var cache = new BoundedExpressionCache();
	/// 
	/// // Custom capacity
	/// var cache = new BoundedExpressionCache(4096);
	/// 
	/// // Using with FilterExpression.AsLambda
	/// var lambda = FilterExpression.AsLambda&lt;Person&gt;(cache, "p", "p.FirstName == \"John\"");
	/// </code>
	/// </example>
	/// <threadsafety>
	/// This type is thread-safe. All public members can be called concurrently from
	/// multiple threads.
	/// </threadsafety>
	/// <seealso cref="IExpressionCache"/>
	/// <seealso cref="IFilterCacheStatistics"/>
	/// <seealso cref="BoundedFilterCacheOptions"/>
	/// <seealso cref="BoundedFilterCache"/>
	public sealed class BoundedExpressionCache : BoundedCache<LambdaExpression>, IExpressionCache {
		/// <summary>
		/// Initializes a new instance of the <see cref="BoundedExpressionCache"/> class
		/// with the specified maximum capacity.
		/// </summary>
		/// <param name="maxCapacity">
		/// The maximum number of parsed expressions the cache can hold before
		/// LRU eviction begins. Must be at least 1.
		/// </param>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Thrown when <paramref name="maxCapacity"/> is less than 1.
		/// </exception>
		public BoundedExpressionCache(int maxCapacity) : base(maxCapacity) {
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BoundedExpressionCache"/> class
		/// with the default maximum capacity of 1024 entries.
		/// </summary>
		public BoundedExpressionCache() : base() {
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BoundedExpressionCache"/> class
		/// with the specified configuration options.
		/// </summary>
		/// <param name="options">
		/// The configuration options that determine the cache capacity.
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="options"/> is <c>null</c>.
		/// </exception>
		public BoundedExpressionCache(BoundedFilterCacheOptions options) : base(options) {
		}

		/// <inheritdoc />
		public bool TryGet(string key, out LambdaExpression? expression) {
			return TryGetCore(key, out expression);
		}

		/// <inheritdoc />
		public void Set(string key, LambdaExpression expression) {
			SetCore(key, expression);
		}
	}
}
