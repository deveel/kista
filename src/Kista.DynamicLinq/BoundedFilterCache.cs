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
	/// The cache uses a combination of a <see cref="Dictionary{TKey,TValue}"/> for O(1)
	/// key lookup and a <see cref="LinkedList{T}"/> to track access order for LRU eviction.
	/// All operations are protected by a <see cref="SemaphoreSlim"/> (initialized as a
	/// binary semaphore) to ensure thread safety under concurrent access. The semaphore
	/// uses spin-waiting before falling back to kernel-level waiting, reducing
	/// context-switch overhead under moderate contention typical of high-throughput
	/// query paths.
	/// </para>
	/// <para>
	/// Statistics are tracked using <see cref="Volatile"/> reads and writes on the hit
	/// and miss counters, providing lock-free access to <see cref="IFilterCacheStatistics"/>
	/// properties without impacting cache throughput.
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
	public sealed class BoundedFilterCache : IFilterCache {
		private readonly SemaphoreSlim _semaphore = new(1, 1);
		private readonly Dictionary<string, LinkedListNode<CacheEntry>> _map;
		private readonly LinkedList<CacheEntry> _order;
		private readonly int _maxCapacity;
		private long _hits;
		private long _misses;

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
		public BoundedFilterCache(int maxCapacity) {
			if (maxCapacity < 1)
				throw new ArgumentOutOfRangeException(nameof(maxCapacity), "MaxCapacity must be at least 1.");

			_maxCapacity = maxCapacity;
			_map = new Dictionary<string, LinkedListNode<CacheEntry>>(maxCapacity, StringComparer.Ordinal);
			_order = new LinkedList<CacheEntry>();
			Statistics = new InternalStatistics(this);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BoundedFilterCache"/> class
		/// with the default maximum capacity of 1024 entries.
		/// </summary>
		public BoundedFilterCache()
			: this(new BoundedFilterCacheOptions().MaxCapacity) {
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
		public BoundedFilterCache(BoundedFilterCacheOptions options)
			: this(options?.MaxCapacity ?? throw new ArgumentNullException(nameof(options))) {
		}

		private sealed record CacheEntry(string Expression, Delegate Lambda);

		private sealed class InternalStatistics(BoundedFilterCache cache) : IFilterCacheStatistics {
			/// <inheritdoc/>
			public long Hits => Volatile.Read(ref cache._hits);
			/// <inheritdoc/>
			public long Misses => Volatile.Read(ref cache._misses);
			/// <inheritdoc/>
			public int CurrentSize => cache._map.Count;
			/// <inheritdoc/>
			public int MaxCapacity => cache._maxCapacity;

			/// <inheritdoc/>
			public double HitRate {
				get {
					var total = Hits + Misses;
					return total == 0 ? 0 : (double)Hits / total;
				}
			}

			/// <inheritdoc/>
			public void Reset() {
				Volatile.Write(ref cache._hits, 0);
				Volatile.Write(ref cache._misses, 0);
			}
		}

		/// <inheritdoc />
		public IFilterCacheStatistics Statistics { get; }

		/// <inheritdoc />
		/// <remarks>
		/// On a cache hit, the accessed entry is promoted to the most-recently-used
		/// position in the LRU ordering. This ensures that frequently used expressions
		/// are not evicted.
		/// </remarks>
		public bool TryGet(string expression, out Delegate? lambda) {
			ArgumentNullException.ThrowIfNull(expression);

			_semaphore.Wait();
			try {
				if (_map.TryGetValue(expression, out var node)) {
					_order.Remove(node);
					_order.AddFirst(node);
					Interlocked.Increment(ref _hits);
					lambda = node.Value.Lambda;
					return true;
				}

				Interlocked.Increment(ref _misses);
				lambda = null;
				return false;
			} finally {
				_semaphore.Release();
			}
		}

		/// <inheritdoc />
		/// <remarks>
		/// If the key already exists, the stored delegate is updated and the entry
		/// is promoted to the most-recently-used position. If the cache is at capacity
		/// and the key is new, the least recently used entry is evicted before insertion.
		/// </remarks>
		public void Set(string expression, Delegate lambda) {
			ArgumentNullException.ThrowIfNull(expression);
			ArgumentNullException.ThrowIfNull(lambda);

			_semaphore.Wait();
			try {
				if (_map.TryGetValue(expression, out var existingNode)) {
					_order.Remove(existingNode);
				} else if (_map.Count >= _maxCapacity) {
					var lruNode = _order.Last;
					if (lruNode != null) {
						_order.RemoveLast();
						_map.Remove(lruNode.Value.Expression);
					}
				}

				var entry = new CacheEntry(expression, lambda);
				var newNode = _order.AddFirst(entry);
				_map[expression] = newNode;
			} finally {
				_semaphore.Release();
			}
		}

		/// <inheritdoc />
		/// <remarks>
		/// This method removes all cached delegates but does not reset the
		/// <see cref="Statistics"/> counters. Call <see cref="IFilterCacheStatistics.Reset"/>
		/// separately if you need to clear the statistics as well.
		/// </remarks>
		public void Clear() {
			_semaphore.Wait();
			try {
				_map.Clear();
				_order.Clear();
			} finally {
				_semaphore.Release();
			}
		}
	}
}
