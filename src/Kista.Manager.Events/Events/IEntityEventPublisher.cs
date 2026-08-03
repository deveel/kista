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
	/// An abstraction for publishing domain event payloads emitted by the
	/// <see cref="EntityManager{TEntity, TKey}"/> operation pipeline.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of the entity targeted by the events.
	/// </typeparam>
	/// <remarks>
	/// <para>
	/// The base <c>Kista.Manager.Events</c> package ships a default
	/// <see cref="InMemoryEntityEventPublisher{TEntity}"/> implementation
	/// that is usable and testable on its own. Teams can replace it with
	/// a custom implementation — such as the Hermodr CloudEvents adapter
	/// in <c>Kista.Manager.Hermodr</c> — by registering a different
	/// <see cref="IEntityEventPublisher{TEntity}"/> in the dependency
	/// injection container.
	/// </para>
	/// <para>
	/// The <see cref="EntityEventInterceptor{TEntity, TKey}"/> resolves
	/// this publisher from DI and calls <see cref="PublishAsync"/> in
	/// <c>PostWriteAsync</c> after a successful write.
	/// </para>
	/// </remarks>
	public interface IEntityEventPublisher<TEntity>
		where TEntity : class {
		/// <summary>
		/// Publishes the given event payload to downstream subscribers.
		/// </summary>
		/// <param name="data">
		/// The event payload to publish.
		/// </param>
		/// <param name="cancellationToken">
		/// A token to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns a <see cref="ValueTask"/> that completes when the
		/// event has been published.
		/// </returns>
		ValueTask PublishAsync(EntityEventData<TEntity> data, CancellationToken cancellationToken);
	}
}