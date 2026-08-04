# Domain Events

The `EntityManager<TEntity, TKey>` can emit domain events for every meaningful lifecycle change (create, update, delete, restore) through the [Operation Pipeline](operation-pipeline.md), so downstream systems — notifications, search indexers, audit logs — can react without coupling to the repository internals.

Two packages make this possible:

| Package | Description |
|---------|-------------|
| `Kista.Manager.Events` | Framework-agnostic event model, a default in-memory publisher, and the `EntityEventInterceptor` that plugs into the operation pipeline |
| `Kista.Manager.Hermodr` | Adapter that bridges the event model with the [Hermodr](https://hermodr.deveel.org) CloudEvents framework, dispatching canonical CNCF CloudEvents through Hermodr's publisher pipeline |

```bash
dotnet add package Kista.Manager.Events      # base model + in-memory publisher
dotnet add package Kista.Manager.Hermodr      # Hermodr CloudEvents adapter (optional)
```

## Why events?

Before the event pipeline, repository writes were fire-and-forget from the framework's perspective. Teams needing side effects (send a notification when an entity is created, reindex a record on update, log an audit entry on delete) added glue code inside controllers or application services — scattered, untestable, and easy to miss when new code paths were added. Hand-rolled event envelopes drifted from contract to contract, and there was no path to a real message bus without a rewrite.

The domain event pipeline gives every lifecycle change a single, strongly-typed event that any subscriber can observe, with a CloudEvents-native adapter available out of the box.

## The event model

The base `Kista.Manager.Events` package provides an `IEntityEventPublisher<TEntity>` abstraction and an `EntityEventData<TEntity>` base class with per-operation POCO subclasses:

| Payload | Operation | Carries |
|---------|-----------|---------|
| `EntityCreatedData<TEntity>` | `Create` | The created entity |
| `EntityUpdatedData<TEntity>` | `Update` | The entity + the `Original` pre-image (for diffs / audit) |
| `EntityDeletedData<TEntity>` | `Remove` / `HardDelete` | The entity + a `DeleteKind` (`Soft` or `Hard`) discriminator |
| `EntityRestoredData<TEntity>` | `Restore` | The restored entity |

Every payload carries the entity, the key, the actor (from `IUserAccessor`), and the timestamp (from `ISystemTime`) resolved from the operation context.

```csharp
public interface IEntityEventPublisher<TEntity>
    where TEntity : class
{
    ValueTask PublishAsync(EntityEventData<TEntity> data, CancellationToken cancellationToken);
}
```

## The builtin `EntityEventInterceptor`

The `EntityEventInterceptor<TEntity, TKey>` is a builtin interceptor (built on the [Operation Pipeline](operation-pipeline.md)) that publishes through `IEntityEventPublisher<TEntity>` in `PostWriteAsync` after a successful write:

| Operation kind | Event payload |
|----------------|---------------|
| `Create` | `EntityCreatedData<TEntity>` |
| `Update` | `EntityUpdatedData<TEntity>` (carries `Original`) |
| `Restore` | `EntityRestoredData<TEntity>` |
| `Remove` (entity implements `ISoftDeletable`) | `EntityDeletedData<TEntity>` { `DeleteKind = Soft` } |
| `Remove` (entity does not implement `ISoftDeletable`) | `EntityDeletedData<TEntity>` { `DeleteKind = Hard` } |
| `HardDelete` | `EntityDeletedData<TEntity>` { `DeleteKind = Hard` } |

The action is only taken when the operation **succeeded**: a not-changed or failed result leaves the event stream untouched. Failures in the publisher are logged and swallowed, so an event outage never propagates to the caller of the write operation — mirroring the resilience posture of the builtin `CacheInterceptor`.

## Registration

### Base package (in-memory publisher)

`WithEntityEvents()` on `EntityManagerBuilder` wires the interceptor and a default in-memory publisher, so the base package is usable and testable on its own:

```csharp
services.AddRepositoryContext()
    .AddRepository<PersonRepository>(repo => repo
        .WithManagement(mgmt => mgmt
            .WithEntityEvents()))
    .UseInMemory();
```

A `RepositoryContextBuilder.WithEntityEvents()` overload applies the registration to all tracked entity types.

The `InMemoryEntityEventPublisher<TEntity>` enqueues published events into an unbounded channel and also records them in a `PublishedEvents` list for test assertions. Replace it with the Hermodr adapter or a custom implementation by registering a different `IEntityEventPublisher<TEntity>` in DI.

### Hermodr CloudEvents adapter

`WithHermodrEvents()` on `EntityManagerBuilder` bridges the event model with the [Hermodr](https://hermodr.deveel.org) CloudEvents framework:

```csharp
services.AddRepositoryContext()
    .AddRepository<PersonRepository>(repo => repo
        .WithManagement(mgmt => mgmt
            .WithHermodrEvents(options => {
                options.SourceUriScheme = "myapp";
            })))
    .UseEntityFramework();
```

This call:

1. Registers the Hermodr `IEventPublisher` pipeline (via `AddEventPublisher()` from `Hermodr.Publisher`).
2. Registers `HermodrEventPublisher<TEntity>` as `IEntityEventPublisher<TEntity>`.
3. Registers `EntityEventInterceptor<TEntity, TKey>` in the operation pipeline.

When a transport channel (Azure Service Bus, RabbitMQ, MassTransit, Webhook) has been registered separately through the Hermodr publisher builder, the CloudEvents are dispatched through it; otherwise the default in-process channel is used.

## Canonical CloudEvents

The `HermodrEventPublisher<TEntity>` maps each event payload to a canonical CNCF CloudEvent:

| Payload | CloudEvent `type` |
|---------|-------------------|
| `EntityCreatedData<TEntity>` | `kista.entity.created` |
| `EntityUpdatedData<TEntity>` | `kista.entity.updated` |
| `EntityDeletedData<TEntity>` | `kista.entity.deleted` |
| `EntityRestoredData<TEntity>` | `kista.entity.restored` |

The `source` is built as `<SourceUriScheme>://<entity-type-name>` (default `kista://person`), the `subject` is the stringified entity key, the `datacontenttype` is `application/json`, and the `data` is the original POCO payload. For `EntityDeletedData`, the `DeleteKind` (`Soft` / `Hard`) is carried as a CloudEvent extension attribute named `kistadeletekind`.

The CloudEvent is dispatched through Hermodr's `IEventPublisher.PublishEventAsync`, which runs the full middleware pipeline (enrichment, dead-letter capture, fan-out) before delivering to the registered channels.

## In-process subscribers

In-process subscribers are registered through `Hermodr.Subscriptions`:

```csharp
var builder = services.AddEventPublisher();
builder.AddSubscriptions(subs => subs
    .Subscribe("kista.entity.*", async (cloudEvent, ct) => {
        // React to any kista.entity.* event
        Console.WriteLine($"Received {cloudEvent.Type} for {cloudEvent.Subject}");
    }));
```

Filter expressions can narrow the subscription to specific event types or attributes.

## Test assertions

The `Hermodr.TestPublisher` package provides a `TestEventPublishChannel` that captures every published CloudEvent for assertions. Use it in unit tests by adding a test channel to the publisher builder:

```csharp
var publishedEvents = new List<CloudEvent>();
var builder = services.AddEventPublisher();
builder.AddTestChannel(e => publishedEvents.Add(e));

// ... exercise the manager ...

var evt = Assert.Single(publishedEvents);
Assert.Equal("kista.entity.created", evt.Type);
Assert.Equal("1", evt.Subject);
```

The base `InMemoryEntityEventPublisher<T>.PublishedEvents` list can be used directly when testing without the Hermodr adapter.

## Transactional outbox (future)

At-least-once delivery via `Hermodr.Publisher.Outbox` and `Hermodr.Publisher.Outbox.EntityFramework` is deferred to the v1.9.0 audit-trail milestone. The outbox packages already depend on `Kista` and `Kista.EntityFramework`, so the integration will land without new persistence abstractions.