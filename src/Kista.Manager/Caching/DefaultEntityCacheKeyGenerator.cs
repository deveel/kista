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

namespace Kista.Caching {
	/// <summary>
	/// A default implementation of <see cref="IEntityCacheKeyGenerator{TEntity}"/>
	/// that produces safe, normalized cache keys prefixed with the entity type name.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of the entity for which cache keys are generated.
	/// </typeparam>
	/// <remarks>
	/// <para>
	/// This implementation guards against cache-key injection by:
	/// </para>
	/// <list type="bullet">
	///   <item>Prefixing the key with the entity type name to avoid collisions across entity types.</item>
	///   <item>URL-encoding the key segment to neutralize special characters.</item>
	///   <item>Enforcing a maximum key length (256 characters) to prevent memory exhaustion.</item>
	/// </list>
	/// <para>
	/// Custom implementations of <see cref="IEntityCacheKeyGenerator{TEntity}"/>
	/// should follow the same normalization principles.
	/// </para>
	/// </remarks>
	public class DefaultEntityCacheKeyGenerator<TEntity> : IEntityCacheKeyGenerator<TEntity>
		where TEntity : class {

		private const int MaxKeyLength = 256;
		private readonly string _prefix;

		/// <summary>
		/// Initializes a new instance with the default prefix derived from
		/// the entity type name.
		/// </summary>
		public DefaultEntityCacheKeyGenerator() {
			_prefix = typeof(TEntity).Name + ":";
		}

		/// <inheritdoc/>
		public string GenerateKey(object key) {
			ArgumentNullException.ThrowIfNull(key);
			var raw = key.ToString() ?? string.Empty;
			return Normalize(_prefix + raw);
		}

		/// <inheritdoc/>
		public string[] GenerateAllKeys(TEntity entity) {
			ArgumentNullException.ThrowIfNull(entity);
			// By default, generate a single key from the entity's ToString().
			// Subclasses can override to produce additional keys (e.g. by
			// alternate unique fields).
			return new[] { Normalize(_prefix + entity.ToString()) };
		}

		private static string Normalize(string key) {
			if (key.Length > MaxKeyLength)
				key = key.Substring(0, MaxKeyLength);

			return Uri.EscapeDataString(key);
		}
	}
}