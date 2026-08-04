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

using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Kista.Events {
	/// <summary>
	/// A default in-process implementation of
	/// <see cref="IEntityEventPublisher{TEntity}"/> that enqueues published
	/// events into an unbounded <see cref="Channel{T}"/> for asynchronous
	/// consumption, and also records them in a list for test assertions.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of the entity targeted by the events.
	/// </typeparam>
	/// <remarks>
	/// <para>
	/// This publisher is registered by <c>WithEntityEvents()</c> as the
	/// default <see cref="IEntityEventPublisher{TEntity}"/>, making the base
	/// <c>Kista.Manager.Events</c> package usable and testable on its own
	/// without any messaging dependency.
	/// </para>
	/// <para>
	/// The <see cref="PublishedEvents"/> property exposes a thread-safe
	/// snapshot of all events published so far, for use in unit tests and
	/// debugging scenarios. For production workloads with real message
	/// buses, replace this publisher with the Hermodr CloudEvents adapter
	/// (<c>Kista.Manager.Hermodr</c>) or a custom implementation.
	/// </para>
	/// </remarks>
	public class InMemoryEntityEventPublisher<TEntity> : IEntityEventPublisher<TEntity>
		where TEntity : class {
		private readonly ConcurrentQueue<EntityEventData<TEntity>> _events = new();
		private readonly Channel<EntityEventData<TEntity>> _channel =
			Channel.CreateUnbounded<EntityEventData<TEntity>>();

		/// <summary>
		/// Gets a readable channel of all published events, for asynchronous
		/// consumption via <c>await foreach</c>.
		/// </summary>
		public ChannelReader<EntityEventData<TEntity>> Reader => _channel.Reader;

		/// <summary>
		/// Gets a thread-safe snapshot of all events published so far.
		/// </summary>
		public IReadOnlyList<EntityEventData<TEntity>> PublishedEvents => _events.ToArray();

		/// <inheritdoc/>
		public ValueTask PublishAsync(EntityEventData<TEntity> data, CancellationToken cancellationToken) {
			ArgumentNullException.ThrowIfNull(data);
			_events.Enqueue(data);
			_channel.Writer.TryWrite(data);
			return ValueTask.CompletedTask;
		}
	}
}