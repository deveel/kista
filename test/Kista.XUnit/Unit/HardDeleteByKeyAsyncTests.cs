#pragma warning disable CS8618

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Core")]
[Trait("Feature", "RepositoryExtensions")]
public class HardDeleteByKeyAsyncTests {
	private readonly PersonFaker _faker = new();

	[Fact]
	public async Task Should_ReturnTrue_When_HardDeleteByKeyAsyncFindsEntity_KeyedRepo() {
		var person = _faker.Generate();
		person.Id = "1";
		var repo = new InMemoryRepository<Person, string>(new[] { person });
		var ct = TestContext.Current.CancellationToken;

		var result = await ((IRepository<Person, string>)repo).HardDeleteByKeyAsync("1", ct);

		Assert.True(result);
		Assert.Null(await repo.FindAsync("1", ct));
	}

	[Fact]
	public async Task Should_ReturnFalse_When_HardDeleteByKeyAsyncDoesNotFindEntity_KeyedRepo() {
		var repo = new InMemoryRepository<Person, string>();
		var ct = TestContext.Current.CancellationToken;

		var result = await ((IRepository<Person, string>)repo).HardDeleteByKeyAsync("missing", ct);

		Assert.False(result);
	}

	[Fact]
	public async Task Should_ReturnTrue_When_HardDeleteByKeyAsyncFindsEntity_NonKeyedRepo() {
		var person = _faker.Generate();
		person.Id = "1";
		var repo = new InMemoryRepository<Person>(new[] { person });
		IRepository<Person> nonKeyed = repo;
		var ct = TestContext.Current.CancellationToken;

		var result = await nonKeyed.HardDeleteByKeyAsync("1", ct);

		Assert.True(result);
	}

	[Fact]
	public async Task Should_ReturnFalse_When_HardDeleteByKeyAsyncDoesNotFindEntity_NonKeyedRepo() {
		var repo = new InMemoryRepository<Person>();
		IRepository<Person> nonKeyed = repo;
		var ct = TestContext.Current.CancellationToken;

		var result = await nonKeyed.HardDeleteByKeyAsync("missing", ct);

		Assert.False(result);
	}
}

[Trait("Category", "Unit")]
[Trait("Layer", "Core")]
[Trait("Feature", "Repository")]
public class RepositoryDefaultInterfaceMethodTests {
	private readonly PersonFaker _faker = new();

	[Fact]
	public async Task Should_ThrowNotSupportedException_When_HardDeleteAsyncNotImplemented() {
		IRepository<Person, string> repo = new StubRepository();
		var person = _faker.Generate();
		var ct = TestContext.Current.CancellationToken;

		await Assert.ThrowsAsync<NotSupportedException>(() =>
			repo.HardDeleteAsync(person, ct).AsTask());
	}

	[Fact]
	public async Task Should_ThrowNotSupportedException_When_HardDeleteRangeAsyncNotImplemented() {
		IRepository<Person, string> repo = new StubRepository();
		var people = _faker.Generate(2);
		var ct = TestContext.Current.CancellationToken;

		await Assert.ThrowsAsync<NotSupportedException>(() =>
			repo.HardDeleteRangeAsync(people, ct).AsTask());
	}

	[Fact]
	public void Should_ReturnNull_When_ServicesNotImplemented() {
		IRepository<Person, string> repo = new StubRepository();
		Assert.Null(repo.Services);
	}

	private sealed class StubRepository : IRepository<Person, string> {
		public IServiceProvider? Services => null;
		public string? GetEntityKey(Person entity) => entity.Id;
		public ValueTask AddAsync(Person entity, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask AddRangeAsync(IEnumerable<Person> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<bool> UpdateAsync(Person entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask<bool> RemoveAsync(Person entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask RemoveRangeAsync(IEnumerable<Person> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<Person?> FindAsync(string key, CancellationToken cancellationToken = default) => new((Person?)null);
		public ValueTask<PageResult<Person>> GetPageAsync(PageRequest request, CancellationToken cancellationToken = default)
			=> new(new PageResult<Person>(request, 0, Array.Empty<Person>()));
	}
}