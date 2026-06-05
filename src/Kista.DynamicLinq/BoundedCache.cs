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
	/// A bounded, thread-safe LRU cache base class used by <see cref="BoundedExpressionCache"/>
	/// and <see cref="BoundedFilterCache"/>.
	/// </summary>
	/// <typeparam name="TValue">The type of value stored in the cache.</typeparam>
	public abstract class BoundedCache<TValue> {
		private readonly SemaphoreSlim _semaphore = new(1, 1);
		private readonly Dictionary<string, LinkedListNode<CacheEntry>> _map;
		private readonly LinkedList<CacheEntry> _order;
		private readonly int _maxCapacity;
		private long _hits;
		private long _misses;

		/// <summary>
	/// Initializes a new instance with the specified maximum capacity.
	/// </summary>
	/// <param name="maxCapacity">The maximum number of entries before LRU eviction begins.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxCapacity"/> is less than 1.</exception>
	protected BoundedCache(int maxCapacity) {
			if (maxCapacity < 1)
				throw new ArgumentOutOfRangeException(nameof(maxCapacity), "MaxCapacity must be at least 1.");

			_maxCapacity = maxCapacity;
			_map = new Dictionary<string, LinkedListNode<CacheEntry>>(maxCapacity, StringComparer.Ordinal);
			_order = new LinkedList<CacheEntry>();
			Statistics = new InternalStatistics(this);
		}

	/// <summary>
	/// Initializes a new instance with the default maximum capacity of 1024 entries.
	/// </summary>
	protected BoundedCache()
		: this(1024) {
	}

	/// <summary>
	/// Initializes a new instance with the specified configuration options.
	/// </summary>
	/// <param name="options">The configuration options that determine the cache capacity.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is <c>null</c>.</exception>
	protected BoundedCache(BoundedFilterCacheOptions options)
		: this(options?.MaxCapacity ?? throw new ArgumentNullException(nameof(options))) {
	}

	/// <summary>
	/// Represents an entry in the cache with a string key and a value.
	/// </summary>
	protected record CacheEntry(string Key, TValue Value);

		private sealed class InternalStatistics(BoundedCache<TValue> cache) : IFilterCacheStatistics {
			public long Hits => Volatile.Read(ref cache._hits);
			public long Misses => Volatile.Read(ref cache._misses);
			public int CurrentSize => cache._map.Count;
			public int MaxCapacity => cache._maxCapacity;

			public double HitRate {
				get {
					var total = Hits + Misses;
					return total == 0 ? 0 : (double)Hits / total;
				}
			}

			public void Reset() {
				Volatile.Write(ref cache._hits, 0);
				Volatile.Write(ref cache._misses, 0);
			}
		}

		/// <summary>
		/// Gets the statistics for this cache instance.
		/// </summary>
		public IFilterCacheStatistics Statistics { get; }

		/// <summary>
		/// Attempts to retrieve a value from the cache by key, promoting it to the
		/// most-recently-used position on a hit.
		/// </summary>
		/// <param name="key">The cache key.</param>
		/// <param name="value">When returned, the cached value if found; otherwise <c>default</c>.</param>
		/// <returns><c>true</c> if the key was found; otherwise <c>false</c>.</returns>
		protected bool TryGetCore(string key, out TValue? value) {
			ArgumentNullException.ThrowIfNull(key);

			_semaphore.Wait();
			try {
				if (_map.TryGetValue(key, out var node)) {
					_order.Remove(node);
					_order.AddFirst(node);
					Interlocked.Increment(ref _hits);
					value = node.Value.Value;
					return true;
				}

				Interlocked.Increment(ref _misses);
				value = default;
				return false;
			} finally {
				_semaphore.Release();
			}
		}

		/// <summary>
		/// Stores a value in the cache. If the key already exists, the value is updated
		/// and promoted to the most-recently-used position. If the cache is at capacity,
		/// the least recently used entry is evicted.
		/// </summary>
		/// <param name="key">The cache key.</param>
		/// <param name="value">The value to store.</param>
		protected void SetCore(string key, TValue value) {
			ArgumentNullException.ThrowIfNull(key);
			ArgumentNullException.ThrowIfNull(value);

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

				var entry = new CacheEntry(key, value);
				var newNode = _order.AddFirst(entry);
				_map[key] = newNode;
			} finally {
				_semaphore.Release();
			}
		}

		/// <summary>
		/// Removes all entries from the cache. Does not reset the <see cref="Statistics"/> counters.
		/// </summary>
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
