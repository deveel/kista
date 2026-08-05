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

using CloudNative.CloudEvents;

using Hermodr;

using Microsoft.Extensions.Options;

namespace Kista.Events {
	/// <summary>
	/// An <see cref="IEntityEventPublisher{TEntity}"/> that bridges the
	/// base <c>Kista.Manager.Events</c> event model with the
	/// <see href="https://hermodr.deveel.org">Hermodr</see> CloudEvents
	/// framework, mapping each <see cref="EntityEventData{TEntity}"/>
	/// subclass to a canonical CNCF CloudEvent and dispatching it through
	/// Hermodr's <see cref="IEventPublisher"/>.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of the entity targeted by the events.
	/// </typeparam>
	/// <remarks>
	/// <para>
	/// Each event payload is mapped to a CloudEvent with a canonical
	/// <c>type</c> string:
	/// <list type="table">
	/// <listheader><term>Payload</term><description>CloudEvent <c>type</c></description></listheader>
	/// <item><term><see cref="EntityCreatedData{TEntity}"/></term><description><c>kista.entity.created</c></description></item>
	/// <item><term><see cref="EntityUpdatedData{TEntity}"/></term><description><c>kista.entity.updated</c></description></item>
	/// <item><term><see cref="EntityDeletedData{TEntity}"/></term><description><c>kista.entity.deleted</c></description></item>
	/// <item><term><see cref="EntityRestoredData{TEntity}"/></term><description><c>kista.entity.restored</c></description></item>
	/// </list>
	/// </para>
	/// <para>
	/// The <c>source</c> is built as <c>&lt;SourceUriScheme&gt;://&lt;entity-type-name&gt;</c>,
	/// the <c>subject</c> is the stringified entity key, the
	/// <c>datacontenttype</c> is <c>application/json</c>, and the
	/// <c>data</c> is the original POCO payload. For
	/// <see cref="EntityDeletedData{TEntity}"/>, the <see cref="EntityDeleteKind"/>
	/// is carried as a CloudEvent extension attribute named
	/// <c>kistadeletekind</c>.
	/// </para>
	/// <para>
	/// The CloudEvent is dispatched through Hermodr's
	/// <see cref="IEventPublisher.PublishEventAsync"/>, which runs the
	/// full middleware pipeline (enrichment, outbox, dead-letter capture,
	/// fan-out) before delivering to the registered channels. Transports
	/// are pluggable through Hermodr channel packages (Azure Service Bus,
	/// RabbitMQ, MassTransit, Webhook) with zero application code change.
	/// </para>
	/// <para>
	/// <b>Performance note.</b> Unlike <c>InMemoryEntityEventPublisher</c>,
	/// which enqueues events into a channel and returns immediately, this
	/// publisher awaits the full Hermodr middleware chain inline on the
	/// caller's request thread. If any middleware performs I/O (outbox DB
	/// write, webhook HTTP), the user-facing write latency includes event
	/// publication. To decouple publication from the request thread, wrap
	/// the publisher in a bounded-channel-based buffer with a background
	/// consumer.
	/// </para>
	/// </remarks>
	public class HermodrEventPublisher<TEntity> : IEntityEventPublisher<TEntity>
		where TEntity : class {
		/// <summary>
		/// The CloudEvent <c>type</c> string emitted for
		/// <see cref="EntityCreatedData{TEntity}"/> payloads.
		/// </summary>
		public const string CreatedEventType = "kista.entity.created";

		/// <summary>
		/// The CloudEvent <c>type</c> string emitted for
		/// <see cref="EntityUpdatedData{TEntity}"/> payloads.
		/// </summary>
		public const string UpdatedEventType = "kista.entity.updated";

		/// <summary>
		/// The CloudEvent <c>type</c> string emitted for
		/// <see cref="EntityDeletedData{TEntity}"/> payloads.
		/// </summary>
		public const string DeletedEventType = "kista.entity.deleted";

		/// <summary>
		/// The CloudEvent <c>type</c> string emitted for
		/// <see cref="EntityRestoredData{TEntity}"/> payloads.
		/// </summary>
		public const string RestoredEventType = "kista.entity.restored";

		/// <summary>
		/// The name of the CloudEvent extension attribute carrying the
		/// <see cref="EntityDeleteKind"/> discriminator on
		/// <see cref="EntityDeletedData{TEntity}"/> payloads.
		/// </summary>
		public const string DeleteKindAttributeName = "kistadeletekind";

		private static readonly CloudEventAttribute DeleteKindAttribute =
			CloudEventAttribute.CreateExtension(DeleteKindAttributeName, CloudEventAttributeType.String);

	private readonly IEventPublisher _publisher;
	private readonly HermodrEventsOptions _options;
	private readonly Uri _sourceUri;

		/// <summary>
		/// Constructs the publisher with the Hermodr <see cref="IEventPublisher"/>
		/// and the options controlling the CloudEvent mapping.
		/// </summary>
		/// <param name="publisher">
		/// The Hermodr publisher used to dispatch the CloudEvents.
		/// </param>
		/// <param name="options">
		/// The options for the CloudEvent mapping. When <c>null</c>, the
		/// default <see cref="HermodrEventsOptions"/> are used.
		/// </param>
		public HermodrEventPublisher(
		IEventPublisher publisher,
		IOptions<HermodrEventsOptions>? options = null) {
		_publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
		_options = options?.Value ?? new HermodrEventsOptions();
		_sourceUri = BuildSourceUri();
	}

		/// <inheritdoc/>
		public async ValueTask PublishAsync(EntityEventData<TEntity> data, CancellationToken cancellationToken) {
			ArgumentNullException.ThrowIfNull(data);

			var cloudEvent = BuildCloudEvent(data);
			await _publisher.PublishEventAsync(cloudEvent, null, cancellationToken);
		}

		/// <summary>
		/// Builds the canonical CNCF CloudEvent from the given event
		/// payload, mapping the payload type to the corresponding
		/// <c>type</c> string and carrying the entity key as the
		/// <c>subject</c>.
		/// </summary>
		/// <param name="data">
		/// The event payload to map.
		/// </param>
		/// <returns>
		/// Returns the <see cref="CloudEvent"/> ready to be dispatched
		/// through Hermodr.
		/// </returns>
		protected virtual CloudEvent BuildCloudEvent(EntityEventData<TEntity> data) {
		var eventType = GetCloudEventType(data);
		var source = _sourceUri;
		var subject = data.Key?.ToString();

			var cloudEvent = new CloudEvent {
				Type = eventType,
				Source = source,
				Subject = subject,
				Time = data.Timestamp,
				DataContentType = "application/json",
				Data = data
			};

			if (_options.DataSchemaBaseUri != null) {
				var schema = new Uri(_options.DataSchemaBaseUri, eventType);
				cloudEvent.DataSchema = schema;
			}

			if (data is EntityDeletedData<TEntity> deleted) {
				cloudEvent[DeleteKindAttribute] = deleted.DeleteKind.ToString();
			}

			return cloudEvent;
		}

		/// <summary>
		/// Maps an <see cref="EntityEventData{TEntity}"/> subclass to its
		/// canonical CloudEvent <c>type</c> string.
		/// </summary>
		/// <param name="data">
		/// The event payload to map.
		/// </param>
		/// <returns>
		/// Returns the canonical CloudEvent <c>type</c> string.
		/// </returns>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Thrown when the payload type does not map to any canonical
		/// CloudEvent type.
		/// </exception>
		protected static string GetCloudEventType(EntityEventData<TEntity> data) {
			return data switch {
				EntityCreatedData<TEntity> => CreatedEventType,
				EntityUpdatedData<TEntity> => UpdatedEventType,
				EntityDeletedData<TEntity> => DeletedEventType,
				EntityRestoredData<TEntity> => RestoredEventType,
				_ => throw new ArgumentOutOfRangeException(nameof(data), data.GetType(), $"No canonical CloudEvent type is defined for the event payload {data.GetType()}")
			};
		}

		private Uri BuildSourceUri() {
			var scheme = string.IsNullOrWhiteSpace(_options.SourceUriScheme) ? "kista" : _options.SourceUriScheme;
			var entityName = typeof(TEntity).Name.ToLowerInvariant();
			return new Uri($"{scheme}://{entityName}");
		}
	}
}