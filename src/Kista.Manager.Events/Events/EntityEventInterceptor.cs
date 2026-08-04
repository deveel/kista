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

using Deveel;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kista.Events {
	/// <summary>
	/// A builtin interceptor that publishes domain events through
	/// <see cref="IEntityEventPublisher{TEntity}"/> in
	/// <see cref="PostWriteAsync"/> after a successful write, mapping the
	/// <see cref="EntityOperationKind"/> to the corresponding
	/// <see cref="EntityEventData{TEntity}"/> subclass.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of the entity managed by the pipeline.
	/// </typeparam>
	/// <typeparam name="TKey">
	/// The type of the key identifying the entity.
	/// </typeparam>
	/// <remarks>
	/// <para>
	/// <see cref="PreWriteAsync"/> never short-circuits the chain: event
	/// emission is a post-write concern that fires only after a successful
	/// write. <see cref="PostWriteAsync"/> constructs the event payload
	/// for the operation kind and publishes it through the resolved
	/// <see cref="IEntityEventPublisher{TEntity}"/>.
	/// </para>
	/// <para>
	/// The mapping is:
	/// <list type="table">
	/// <listheader><term>Operation kind</term><description>Event payload</description></listheader>
	/// <item><term><see cref="EntityOperationKind.Create"/></term><description><see cref="EntityCreatedData{TEntity}"/></description></item>
	/// <item><term><see cref="EntityOperationKind.Update"/></term><description><see cref="EntityUpdatedData{TEntity}"/> (carries <c>Original</c>)</description></item>
	/// <item><term><see cref="EntityOperationKind.Remove"/></term><description><see cref="EntityDeletedData{TEntity}"/> with <see cref="EntityDeleteKind.Soft"/> (when the entity implements <see cref="ISoftDeletable"/>) or <see cref="EntityDeleteKind.Hard"/></description></item>
	/// <item><term><see cref="EntityOperationKind.Restore"/></term><description><see cref="EntityRestoredData{TEntity}"/></description></item>
	/// <item><term><see cref="EntityOperationKind.HardDelete"/></term><description><see cref="EntityDeletedData{TEntity}"/> with <see cref="EntityDeleteKind.Hard"/></description></item>
	/// </list>
	/// </para>
	/// <para>
	/// Failures in the publisher are logged and swallowed, so an event
	/// outage never propagates to the caller of the write operation:
	/// this mirrors the resilience posture of the builtin
	/// <c>CacheInterceptor</c>.
	/// </para>
	/// </remarks>
	public class EntityEventInterceptor<TEntity, TKey> : IEntityManagerInterceptor<TEntity, TKey>
		where TEntity : class
		where TKey : notnull {
		private readonly IEntityEventPublisher<TEntity> _publisher;
		private readonly ILogger _logger;

		/// <summary>
		/// Constructs the interceptor with the event publisher and an
		/// optional logger used to report publish failures.
		/// </summary>
		/// <param name="publisher">
		/// The publisher used to dispatch the event payloads.
		/// </param>
		/// <param name="logger">
		/// The logger used to report publish failures.
		/// </param>
		public EntityEventInterceptor(
			IEntityEventPublisher<TEntity> publisher,
			ILogger? logger = null) {
			_publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
			_logger = logger ?? NullLogger.Instance;
		}

		/// <inheritdoc/>
		/// <remarks>
		/// Event emission is a post-write concern: this method never
		/// short-circuits the chain and returns <c>null</c> to let the
		/// write proceed.
		/// </remarks>
		public ValueTask<IOperationResult?> PreWriteAsync(IEntityOperationContext<TEntity, TKey> context)
			=> new((IOperationResult?)null);

		/// <inheritdoc/>
		/// <remarks>
		/// Constructs the event payload for the operation kind and
		/// publishes it through <see cref="IEntityEventPublisher{TEntity}"/>.
		/// The action is only taken when the operation succeeded: a
		/// not-changed or failed result leaves the event stream untouched.
		/// </remarks>
		public async ValueTask PostWriteAsync(IEntityOperationContext<TEntity, TKey> context, IOperationResult result) {
			if (!result.IsSuccess())
				return;

			var data = BuildEventData(context);
			if (data == null)
				return;

			try {
				await _publisher.PublishAsync(data, context.CancellationToken);
				_logger.LogEntityEventPublished(typeof(TEntity), context.Key, data.GetType());
			} catch (Exception ex) {
				_logger.LogEntityEventPublishFailed(ex, typeof(TEntity), context.Key, data.GetType());
			}
		}

		/// <summary>
		/// Builds the event payload for the given operation context, mapping
		/// the <see cref="EntityOperationKind"/> to the corresponding
		/// <see cref="EntityEventData{TEntity}"/> subclass.
		/// </summary>
		/// <param name="context">
		/// The operation context carrying the persisted entity, the
		/// optional pre-image, the actor, and the timestamp.
		/// </param>
		/// <returns>
		/// Returns the event payload, or <c>null</c> if the operation kind
		/// does not map to any event.
		/// </returns>
		protected virtual EntityEventData<TEntity>? BuildEventData(IEntityOperationContext<TEntity, TKey> context) {
			var entity = context.Entity;
			var key = context.Key;
			var actor = context.Actor;
			var timestamp = context.Timestamp;

			return context.Kind switch {
				EntityOperationKind.Create => new EntityCreatedData<TEntity>(entity, key, actor, timestamp),
				EntityOperationKind.Update => new EntityUpdatedData<TEntity>(entity, context.Original, key, actor, timestamp),
				EntityOperationKind.Restore => new EntityRestoredData<TEntity>(entity, key, actor, timestamp),
				EntityOperationKind.Remove => new EntityDeletedData<TEntity>(
					entity,
					entity is ISoftDeletable ? EntityDeleteKind.Soft : EntityDeleteKind.Hard,
					key, actor, timestamp),
				EntityOperationKind.HardDelete => new EntityDeletedData<TEntity>(
					entity, EntityDeleteKind.Hard, key, actor, timestamp),
				_ => null
			};
		}
	}
}