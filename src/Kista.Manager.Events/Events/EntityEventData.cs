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
	/// The abstract base class for all domain event data payloads emitted
	/// by the <see cref="EntityManager{TEntity, TKey}"/> operation pipeline
	/// through <see cref="IEntityEventPublisher{TEntity}"/>.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of the entity targeted by the event.
	/// </typeparam>
	/// <remarks>
	/// <para>
	/// Each per-operation subclass carries the entity affected by the
	/// lifecycle change, along with the actor and timestamp resolved
	/// from the <see cref="IEntityOperationContext{TEntity, TKey}"/>.
	/// </para>
	/// <para>
	/// The <see cref="EntityEventInterceptor{TEntity, TKey}"/> constructs
	/// these payloads in <c>PostWriteAsync</c> after a successful write
	/// and publishes them through <see cref="IEntityEventPublisher{TEntity}"/>.
	/// </para>
	/// </remarks>
	public abstract class EntityEventData<TEntity>
		where TEntity : class {
		/// <summary>
		/// Constructs the event payload with the given attributes.
		/// </summary>
		/// <param name="entity">
		/// The entity affected by the lifecycle change.
		/// </param>
		/// <param name="operationKind">
		/// The kind of operation that triggered the event.
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
		protected EntityEventData(
			TEntity entity,
			EntityOperationKind operationKind,
			object? key,
			string? actor,
			DateTimeOffset timestamp) {
			Entity = entity ?? throw new ArgumentNullException(nameof(entity));
			OperationKind = operationKind;
			Key = key;
			Actor = actor;
			Timestamp = timestamp;
		}

		/// <summary>
		/// Gets the entity affected by the lifecycle change.
		/// </summary>
		public TEntity Entity { get; }

		/// <summary>
		/// Gets the kind of operation that triggered the event.
		/// </summary>
		public EntityOperationKind OperationKind { get; }

		/// <summary>
		/// Gets the key identifying the entity targeted by the operation,
		/// or <c>null</c> if the entity does not have a valid key.
		/// </summary>
		public object? Key { get; }

		/// <summary>
		/// Gets the identifier of the actor that initiated the operation,
		/// or <c>null</c> if no actor is available.
		/// </summary>
		public string? Actor { get; }

		/// <summary>
		/// Gets the timestamp at which the operation was started.
		/// </summary>
		public DateTimeOffset Timestamp { get; }
	}
}