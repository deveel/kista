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

namespace Kista.Events {
	/// <summary>
	/// An event payload emitted when an entity is updated in the repository
	/// through the <see cref="EntityManager{TEntity, TKey}"/> operation
	/// pipeline.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of the entity updated.
	/// </typeparam>
	/// <remarks>
	/// Unlike <see cref="EntityCreatedData{TEntity}"/>, this payload carries
	/// the <see cref="Original"/> pre-image — the entity as loaded from the
	/// repository before the update was applied — so subscribers can compute
	/// diffs or audit the change.
	/// </remarks>
	public sealed class EntityUpdatedData<TEntity> : EntityEventData<TEntity>
		where TEntity : class {
		/// <summary>
		/// Constructs the event payload for an update operation.
		/// </summary>
		/// <param name="entity">
		/// The entity as persisted after the update.
		/// </param>
		/// <param name="original">
		/// The pre-image of the entity loaded from the repository before
		/// the update, or <c>null</c> if no pre-image was captured.
		/// </param>
		/// <param name="key">
		/// The key identifying the entity, or <c>null</c> if the entity
		/// does not have a valid key.
		/// </param>
		/// <param name="actor">
		/// The identifier of the actor that initiated the update,
		/// or <c>null</c> if no actor is available.
		/// </param>
		/// <param name="timestamp">
		/// The timestamp at which the update was started.
		/// </param>
		public EntityUpdatedData(
			TEntity entity,
			TEntity? original,
			object? key,
			string? actor,
			DateTimeOffset timestamp)
			: base(entity, EntityOperationKind.Update, key, actor, timestamp) {
			Original = original;
		}

		/// <summary>
		/// Gets the pre-image of the entity loaded from the repository
		/// before the update was applied, or <c>null</c> if no pre-image
		/// was captured.
		/// </summary>
		public TEntity? Original { get; }
	}
}