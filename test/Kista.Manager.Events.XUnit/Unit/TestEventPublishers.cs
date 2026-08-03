#pragma warning disable CS8618

namespace Kista;

/// <summary>
/// An <see cref="IEntityEventPublisher{TEntity}"/> stub that captures
/// every published event for assertions, mirroring the
/// <c>ResultCapturingInterceptor</c> pattern used by the operation-pipeline
/// tests.
/// </summary>
internal sealed class CapturingEventPublisher<TEntity> : IEntityEventPublisher<TEntity>
	where TEntity : class {
	public List<EntityEventData<TEntity>> PublishedEvents { get; } = new();

	public ValueTask PublishAsync(EntityEventData<TEntity> data, CancellationToken cancellationToken) {
		PublishedEvents.Add(data);
		return ValueTask.CompletedTask;
	}
}

/// <summary>
/// An <see cref="IEntityEventPublisher{TEntity}"/> stub that throws on
/// every publish, used to verify that publish failures are logged and
/// swallowed (the write still succeeds).
/// </summary>
internal sealed class ThrowingEventPublisher<TEntity> : IEntityEventPublisher<TEntity>
	where TEntity : class {
	public ValueTask PublishAsync(EntityEventData<TEntity> data, CancellationToken cancellationToken)
		=> throw new InvalidOperationException("Publisher outage");
}