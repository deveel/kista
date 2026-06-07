using Microsoft.Extensions.Logging;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Core")]
[Trait("Feature", "Infrastructure")]
public class RepositoryLifecycleHandlerTests {
	[Fact]
	public async Task SeedAsync_NullData_DoesNothing() {
		var sut = new TestLifecycleHandler();

		await sut.SeedAsync(null);

		Assert.Empty(sut.SeededEntities);
	}

	[Fact]
	public async Task SeedAsync_SingleEntity_SeedsEntity() {
		var sut = new TestLifecycleHandler();
		var person = new PersonFaker().Generate();

		await sut.SeedAsync(person);

		Assert.Single(sut.SeededEntities);
		Assert.Same(person, sut.SeededEntities[0]);
	}

	[Fact]
	public async Task SeedAsync_TypedEnumerable_SeedsAll() {
		var sut = new TestLifecycleHandler();
		var people = new PersonFaker().Generate(5);

		await sut.SeedAsync(people);

		Assert.Equal(5, sut.SeededEntities.Count);
	}

	[Fact]
	public async Task SeedAsync_ObjectEnumerable_WithMatchingTypes_SeedsMatching() {
		var sut = new TestLifecycleHandler();
		var people = new PersonFaker().Generate(3);
		var mixed = people.Cast<object>().Concat(new object[] { "string", 42 }).ToList();

		await sut.SeedAsync((IEnumerable<object>)mixed);

		Assert.Equal(3, sut.SeededEntities.Count);
	}

	[Fact]
	public async Task SeedAsync_ObjectEnumerable_NoMatchingTypes_DoesNothing() {
		var sut = new TestLifecycleHandler();

		await sut.SeedAsync(new List<object> { "string", 42 });

		Assert.Empty(sut.SeededEntities);
	}

	[Fact]
	public void Constructor_NullLogger_UsesNullLogger() {
		var sut = new TestLifecycleHandler(null);

		Assert.NotNull(sut.Logger);
	}

	[Fact]
	public void Constructor_WithLogger_UsesProvidedLogger() {
		var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<RepositoryLifecycleHandler<Person>>();
		var sut = new TestLifecycleHandler(logger);

		Assert.Same(logger, sut.Logger);
	}

	public class TestLifecycleHandler : RepositoryLifecycleHandler<Person> {
		public List<Person> SeededEntities { get; } = new();

		public new ILogger Logger => base.Logger;

		public TestLifecycleHandler() { }

		public TestLifecycleHandler(ILogger? logger) : base(logger) { }

		public override ValueTask<bool> ExistsAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(true);
		public override ValueTask CreateAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public override ValueTask DropAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

		protected override ValueTask SeedEntitiesAsync(IEnumerable<Person> entities, CancellationToken cancellationToken = default) {
			SeededEntities.AddRange(entities);
			return ValueTask.CompletedTask;
		}
	}
}
