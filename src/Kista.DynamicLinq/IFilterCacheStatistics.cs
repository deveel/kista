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
	/// Exposes hit/miss counters and current size for monitoring
	/// compiled filter expression cache performance.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Implementations of this interface are exposed by <see cref="IFilterCache.Statistics"/>
	/// and <see cref="IExpressionCache.Statistics"/> to provide observability into cache
	/// effectiveness. Statistics are tracked using lock-free counters so they do not
	/// impact cache throughput under concurrent access.
	/// </para>
	/// <para>
	/// The <see cref="HitRate"/> property is useful for determining whether the cache
	/// capacity is sized appropriately. A low hit rate may indicate that the cache is
	/// too small for the working set of filter expressions, causing frequent evictions.
	/// </para>
	/// </remarks>
	/// <seealso cref="IFilterCache"/>
	/// <seealso cref="IExpressionCache"/>
	/// <seealso cref="BoundedFilterCache"/>
	/// <seealso cref="BoundedExpressionCache"/>
	public interface IFilterCacheStatistics {
		/// <summary>
		/// Gets the total number of cache hits since the cache was created
		/// or since <see cref="Reset"/> was last called.
		/// </summary>
		/// <value>
		/// A non-negative integer representing the number of times <c>TryGet</c>
		/// returned <c>true</c>.
		/// </value>
		long Hits { get; }

		/// <summary>
		/// Gets the total number of cache misses since the cache was created
		/// or since <see cref="Reset"/> was last called.
		/// </summary>
		/// <value>
		/// A non-negative integer representing the number of times <c>TryGet</c>
		/// returned <c>false</c>.
		/// </value>
		long Misses { get; }

		/// <summary>
		/// Gets the current number of entries stored in the cache.
		/// </summary>
		/// <value>
		/// An integer between zero and <see cref="MaxCapacity"/> inclusive.
		/// </value>
		int CurrentSize { get; }

		/// <summary>
		/// Gets the configured maximum number of entries the cache can hold
		/// before eviction begins.
		/// </summary>
		/// <value>
		/// A positive integer representing the cache capacity limit.
		/// </value>
		int MaxCapacity { get; }

		/// <summary>
		/// Gets the cache hit rate as a ratio between 0 and 1.
		/// </summary>
		/// <value>
		/// A double between 0.0 (no hits) and 1.0 (all hits).
		/// Returns 0.0 when no lookups have occurred (<see cref="Hits"/> + <see cref="Misses"/> == 0).
		/// </value>
		/// <remarks>
		/// This value is computed as <c>Hits / (Hits + Misses)</c> and can be used
		/// to assess whether the cache size is adequate for the application's
		/// filter expression working set.
		/// </remarks>
		double HitRate { get; }

		/// <summary>
		/// Resets all statistics counters (<see cref="Hits"/> and <see cref="Misses"/>) to zero.
		/// </summary>
		/// <remarks>
		/// This method does not clear the cache contents — only the counters are reset.
		/// The <see cref="CurrentSize"/> remains unchanged after calling this method.
		/// </remarks>
		void Reset();
	}
}
