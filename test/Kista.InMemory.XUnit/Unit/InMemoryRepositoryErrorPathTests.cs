#pragma warning disable CS8618

using System.ComponentModel.DataAnnotations;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "InMemory")]
[Trait("Feature", "InMemoryRepository")]
public class InMemoryRepositoryErrorPathTests {
	private readonly PersonFaker _faker = new();

	[Fact]
	public async Task Should_ReturnFalse_When_UpdatingEntityWithNonExistentKey() {
		var repo = new InMemoryRepository<Person, string>();
		var person = _faker.Generate();
		person.Id = "missing";
		var ct = TestContext.Current.CancellationToken;

		var result = await repo.UpdateAsync(person, ct);

		Assert.False(result);
	}

	[Fact]
	public async Task Should_ReturnNull_When_FindOriginalAsyncWithNonExistentKey() {
		var repo = new InMemoryRepository<Person, string>();
		var ct = TestContext.Current.CancellationToken;

		var result = await repo.FindOriginalAsync("missing", ct);

		Assert.Null(result);
	}

	[Fact]
	public async Task Should_ReturnFalse_When_HardDeleteEntityWithNonExistentKey() {
		var repo = new InMemoryRepository<Person, string>();
		var person = _faker.Generate();
		person.Id = "missing";
		var ct = TestContext.Current.CancellationToken;

		var result = await repo.HardDeleteAsync(person, ct);

		Assert.False(result);
	}

	[Fact]
	public async Task Should_ReturnNull_When_FindAsyncWithNonExistentKey() {
		var person = _faker.Generate();
		person.Id = "1";
		var repo = new InMemoryRepository<Person, string>(new[] { person });
		var ct = TestContext.Current.CancellationToken;

		var result = await repo.FindAsync("nonexistent", ct);

		Assert.Null(result);
	}

	[Fact]
	public async Task Should_ReturnFalse_When_RemoveEntityWithNonExistentKey() {
		var repo = new InMemoryRepository<Person, string>();
		var person = _faker.Generate();
		person.Id = "missing";
		var ct = TestContext.Current.CancellationToken;

		var result = await repo.RemoveAsync(person, ct);

		Assert.False(result);
	}
}