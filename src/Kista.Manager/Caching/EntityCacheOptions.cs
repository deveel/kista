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

namespace Kista.Caching {
	/// <summary>
	/// Provides a set of options for the caching of entities.
	/// </summary>
	public class EntityCacheOptions {
		/// <summary>
		/// Gets or sets the maximum expiration
		/// time for the cached entities.
		/// </summary>
		public TimeSpan? Expiration { get; set; }

		/// <summary>
		/// Gets or sets a prefix to prepend to all cache keys.
		/// Useful for isolating caches across environments or applications.
		/// </summary>
		public string? CacheKeyPrefix { get; set; }
	}
}
