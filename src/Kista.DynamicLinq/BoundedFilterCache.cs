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
	/// Provides configuration options for <see cref="BoundedFilterCache"/> and
	/// <see cref="BoundedExpressionCache"/> instances.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The primary configuration option is <see cref="MaxCapacity"/>, which determines
	/// the upper bound on the number of entries the cache can hold. When the cache
	/// reaches this limit, the least recently used entry is evicted to make room for
	/// new entries.
	/// </para>
	/// <para>
	/// Choosing an appropriate capacity depends on the diversity of filter expressions
	/// used by the application. A capacity that is too small will cause frequent evictions
	/// and reduce the cache hit rate, while a capacity that is too large wastes memory.
	/// Monitor <see cref="IFilterCacheStatistics.HitRate"/> to determine whether the
	/// current capacity is adequate.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// var options = new BoundedFilterCacheOptions { MaxCapacity = 4096 };
	/// var cache = new BoundedFilterCache(options);
	/// </code>
	/// </example>
	/// <seealso cref="BoundedFilterCache"/>
	/// <seealso cref="BoundedExpressionCache"/>
	public sealed class BoundedFilterCacheOptions {
		private int _maxCapacity = 1024;

		/// <summary>
		/// Gets or sets the maximum number of entries the cache can hold.
		/// </summary>
		/// <value>
		/// A positive integer. The default value is 1024.
		/// </value>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Thrown when setting a value less than 1.
		/// </exception>
		/// <remarks>
		/// When the cache reaches this capacity, the least recently used entry is
		/// evicted on the next insertion. For multi-tenant applications with a stable
		/// set of filter expressions, a capacity of 1024–4096 is typically sufficient.
		/// </remarks>
		public int MaxCapacity {
			get => _maxCapacity;
			set {
				if (value < 1)
					throw new ArgumentOutOfRangeException(nameof(value), "MaxCapacity must be at least 1.");
				_maxCapacity = value;
			}
		}
	}

	/// <summary>
	/// A bounded, thread-safe cache for compiled filter expression delegates
	/// with LRU (Least Recently Used) eviction policy.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <see cref="BoundedFilterCache"/> stores compiled <see cref="Delegate"/> instances
	/// produced by <see cref="FilterExpression.Compile(IFilterCache?, Type[], string[], string)"/> methods. It is designed for
	/// use in high-throughput scenarios where the same filter expression string is
	/// compiled repeatedly, such as multi-tenant SaaS applications processing thousands
	/// of queries per minute with identical filter shapes.
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
	/// var cache = new BoundedFilterCache();
	/// 
	/// // Custom capacity
	/// var cache = new BoundedFilterCache(4096);
	/// 
	/// // Using with FilterExpression.Compile
	/// var func = FilterExpression.Compile&lt;Person&gt;(cache, "p", "p.FirstName == \"John\"");
	/// </code>
	/// </example>
	/// <threadsafety>
	/// This type is thread-safe. All public members can be called concurrently from
	/// multiple threads.
	/// </threadsafety>
	/// <seealso cref="IFilterCache"/>
	/// <seealso cref="IFilterCacheStatistics"/>
	/// <seealso cref="BoundedFilterCacheOptions"/>
	/// <seealso cref="BoundedExpressionCache"/>
	public sealed class BoundedFilterCache : BoundedCache<Delegate>, IFilterCache {
		/// <summary>
		/// Initializes a new instance of the <see cref="BoundedFilterCache"/> class
		/// with the specified maximum capacity.
		/// </summary>
		/// <param name="maxCapacity">
		/// The maximum number of compiled delegates the cache can hold before
		/// LRU eviction begins. Must be at least 1.
		/// </param>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Thrown when <paramref name="maxCapacity"/> is less than 1.
		/// </exception>
		public BoundedFilterCache(int maxCapacity) : base(maxCapacity) {
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BoundedFilterCache"/> class
		/// with the default maximum capacity of 1024 entries.
		/// </summary>
		public BoundedFilterCache() : base() {
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BoundedFilterCache"/> class
		/// with the specified configuration options.
		/// </summary>
		/// <param name="options">
		/// The configuration options that determine the cache capacity.
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="options"/> is <c>null</c>.
		/// </exception>
		public BoundedFilterCache(BoundedFilterCacheOptions options) : base(options) {
		}

		/// <inheritdoc />
		public bool TryGet(string expression, out Delegate? labda) {
			return TryGetCore(expression, out labda);
		}

		/// <inheritdoc />
		public void Set(string expression, Delegate lambda) {
			SetCore(expression, lambda);
		}
	}
}
