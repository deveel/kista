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
	/// An event payload emitted when an entity is created (added) in the
	/// repository through the <see cref="EntityManager{TEntity, TKey}"/>
	/// operation pipeline.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of the entity created.
	/// </typeparam>
	public sealed class EntityCreatedData<TEntity> : EntityEventData<TEntity>
		where TEntity : class {
		/// <summary>
		/// Constructs the event payload for a create operation.
		/// </summary>
		/// <param name="entity">
		/// The entity that was created.
		/// </param>
		/// <param name="key">
		/// The key identifying the entity, or <c>null</c> if the entity
		/// does not have a valid key.
		/// </param>
		/// <param name="actor">
		/// The identifier of the actor that initiated the creation,
		/// or <c>null</c> if no actor is available.
		/// </param>
		/// <param name="timestamp">
		/// The timestamp at which the creation was started.
		/// </param>
		public EntityCreatedData(
			TEntity entity,
			object? key,
			string? actor,
			DateTimeOffset timestamp)
			: base(entity, EntityOperationKind.Create, key, actor, timestamp) {
		}
	}
}