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
	/// The implementation uses a <see cref="Dictionary{TKey,TValue}"/> for O(1) key lookup
	/// and a <see cref="LinkedList{T}"/> to maintain access order for LRU eviction.
	/// All public operations are protected by a <see cref="SemaphoreSlim"/> (initialized
	/// as a binary semaphore) to ensure thread safety under concurrent access. The semaphore
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
	public sealed class BoundedExpressionCache : IExpressionCache {
		private readonly SemaphoreSlim _semaphore = new(1, 1);
		private readonly Dictionary<string, LinkedListNode<CacheEntry>> _map;
		private readonly LinkedList<CacheEntry> _order;
		private readonly int _maxCapacity;
		private long _hits;
		private long _misses;

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
		public BoundedExpressionCache(int maxCapacity) {
			if (maxCapacity < 1)
				throw new ArgumentOutOfRangeException(nameof(maxCapacity), "MaxCapacity must be at least 1.");

			_maxCapacity = maxCapacity;
			_map = new Dictionary<string, LinkedListNode<CacheEntry>>(maxCapacity, StringComparer.Ordinal);
			_order = new LinkedList<CacheEntry>();
			Statistics = new InternalStatistics(this);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BoundedExpressionCache"/> class
		/// with the default maximum capacity of 1024 entries.
		/// </summary>
		public BoundedExpressionCache()
			: this(1024) {
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
		public BoundedExpressionCache(BoundedFilterCacheOptions options)
			: this(options?.MaxCapacity ?? throw new ArgumentNullException(nameof(options))) {
		}

		private sealed record CacheEntry(string Key, LambdaExpression Expression);

		private sealed class InternalStatistics(BoundedExpressionCache cache) : IFilterCacheStatistics {
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
		public bool TryGet(string key, out LambdaExpression? expression) {
			ArgumentNullException.ThrowIfNull(key, nameof(key));

			_semaphore.Wait();
			try {
				if (_map.TryGetValue(key, out var node)) {
					_order.Remove(node);
					_order.AddFirst(node);
					Interlocked.Increment(ref _hits);
					expression = node.Value.Expression;
					return true;
				}

				Interlocked.Increment(ref _misses);
				expression = null;
				return false;
			} finally {
				_semaphore.Release();
			}
		}

		/// <inheritdoc />
		/// <remarks>
		/// If the key already exists, the stored expression is updated and the entry
		/// is promoted to the most-recently-used position. If the cache is at capacity
		/// and the key is new, the least recently used entry is evicted before insertion.
		/// </remarks>
		public void Set(string key, LambdaExpression expression) {
			ArgumentNullException.ThrowIfNull(key, nameof(key));
			ArgumentNullException.ThrowIfNull(expression, nameof(expression));

			_semaphore.Wait();
			try {
				if (_map.TryGetValue(key, out var existingNode)) {
					_order.Remove(existingNode);
				} else if (_map.Count >= _maxCapacity) {
					var lruNode = _order.Last;
					if (lruNode != null) {
						_order.RemoveLast();
						_map.Remove(lruNode.Value.Key);
					}
				}

				var entry = new CacheEntry(key, expression);
				var newNode = _order.AddFirst(entry);
				_map[key] = newNode;
			} finally {
				_semaphore.Release();
			}
		}

		/// <inheritdoc />
		/// <remarks>
		/// This method removes all cached expressions but does not reset the
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
