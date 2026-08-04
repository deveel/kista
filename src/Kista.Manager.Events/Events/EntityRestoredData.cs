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
	/// An event payload emitted when an entity is restored after a
	/// previous soft-delete through the <see cref="EntityManager{TEntity, TKey}"/>
	/// operation pipeline.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of the entity restored.
	/// </typeparam>
	public sealed class EntityRestoredData<TEntity> : EntityEventData<TEntity>
		where TEntity : class {
		/// <summary>
		/// Constructs the event payload for a restore operation.
		/// </summary>
		/// <param name="entity">
		/// The entity that was restored.
		/// </param>
		/// <param name="key">
		/// The key identifying the entity, or <c>null</c> if the entity
		/// does not have a valid key.
		/// </param>
		/// <param name="actor">
		/// The identifier of the actor that initiated the restoration,
		/// or <c>null</c> if no actor is available.
		/// </param>
		/// <param name="timestamp">
		/// The timestamp at which the restoration was started.
		/// </param>
		public EntityRestoredData(
			TEntity entity,
			object? key,
			string? actor,
			DateTimeOffset timestamp)
			: base(entity, EntityOperationKind.Restore, key, actor, timestamp) {
		}
	}
}