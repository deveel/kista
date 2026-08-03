#pragma warning disable CS8618

using System.Threading.Channels;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
[Trait("Feature", "DomainEvents")]
public class InMemoryEntityEventPublisherTests {
	private readonly PersonFaker _faker = new();

	[Fact]
	public async Task Should_MakePublishedEventAvailable_ThroughReader() {
		var publisher = new InMemoryEntityEventPublisher<Person>();
		var person = _faker.Generate();
		person.Id = "1";
		var data = new EntityCreatedData<Person>(person, person.Id, "actor", DateTimeOffset.UtcNow);

		await publisher.PublishAsync(data, TestContext.Current.CancellationToken);

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
		var read = await publisher.Reader.ReadAsync(cts.Token);

		Assert.Same(data, read);
		Assert.Same(person, read.Entity);
	}

	[Fact]
	public async Task Should_ThrowArgumentNullException_When_PublishingNullData() {
		var publisher = new InMemoryEntityEventPublisher<Person>();

		await Assert.ThrowsAsync<ArgumentNullException>(() =>
			publisher.PublishAsync(null, TestContext.Current.CancellationToken).AsTask());
	}

	[Fact]
	public async Task Should_RecordPublishedEvent_InPublishedEventsList() {
		var publisher = new InMemoryEntityEventPublisher<Person>();
		var person = _faker.Generate();
		person.Id = "1";
		var data = new EntityCreatedData<Person>(person, person.Id, null, DateTimeOffset.UtcNow);

		await publisher.PublishAsync(data, TestContext.Current.CancellationToken);
		await publisher.PublishAsync(data, TestContext.Current.CancellationToken);

		Assert.Equal(2, publisher.PublishedEvents.Count);
		Assert.Same(data, publisher.PublishedEvents[0]);
		Assert.Same(data, publisher.PublishedEvents[1]);
	}
}