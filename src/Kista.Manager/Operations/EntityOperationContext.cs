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
	/// The default implementation of the
	/// <see cref="IEntityOperationContext{TEntity, TKey}"/> interface,
	/// carrying the contextual information of a single write operation
	/// through the <see cref="EntityManager{TEntity, TKey}"/> operation
	/// pipeline.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of the entity targeted by the operation.
	/// </typeparam>
	/// <typeparam name="TKey">
	/// The type of the key identifying the entity.
	/// </typeparam>
	public sealed class EntityOperationContext<TEntity, TKey> : IEntityOperationContext<TEntity, TKey>
		where TEntity : class
		where TKey : notnull {
		/// <summary>
		/// Constructs a new operation context with the given attributes.
		/// </summary>
		/// <param name="kind">
		/// The kind of the operation being performed.
		/// </param>
		/// <param name="entity">
		/// The entity targeted by the operation.
		/// </param>
		/// <param name="original">
		/// The pre-image of the entity loaded from the repository, or
		/// <c>null</c> for create operations.
		/// </param>
		/// <param name="key">
		/// The key identifying the entity, or <c>null</c> if the entity
		/// does not have a valid key.
		/// </param>
		/// <param name="actor">
		/// The identifier of the actor that initiated the operation,
		/// or <c>null</c> if no actor is available.
		/// </param>
		/// <param name="timestamp">
		/// The timestamp at which the operation was started.
		/// </param>
		/// <param name="cancellationToken">
		/// The cancellation token associated with the operation.
		/// </param>
		public EntityOperationContext(
			EntityOperationKind kind,
			TEntity entity,
			TEntity? original,
			TKey? key,
			string? actor,
			DateTimeOffset timestamp,
			CancellationToken cancellationToken) {
			Kind = kind;
			Entity = entity;
			Original = original;
			Key = key;
			Actor = actor;
			Timestamp = timestamp;
			CancellationToken = cancellationToken;
			Items = new Dictionary<string, object?>(StringComparer.Ordinal);
		}

		/// <inheritdoc/>
		public EntityOperationKind Kind { get; }

		/// <inheritdoc/>
		public TEntity Entity { get; set; }

		/// <inheritdoc/>
		public TEntity? Original { get; }

		/// <inheritdoc/>
		public TKey? Key { get; }

		/// <inheritdoc/>
		public string? Actor { get; }

		/// <inheritdoc/>
		public DateTimeOffset Timestamp { get; }

		/// <inheritdoc/>
		public CancellationToken CancellationToken { get; }

		/// <inheritdoc/>
		public IDictionary<string, object?> Items { get; }
	}
}