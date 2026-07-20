#pragma warning disable CS8618

using NSubstitute;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
[Trait("Feature", "OperationPipeline")]
public class RangeOperationInterceptorTests {
	private readonly PersonFaker _faker = new();

	[Fact]
	public async Task Should_CreateContextPerEntityInAddRange() {
		var interceptor = new CountingInterceptor();
		var (manager, _) = BuildManager(interceptor, r =>
			r.AddRangeAsync(Arg.Any<IEnumerable<Person>>(), Arg.Any<CancellationToken>()).Returns(ValueTask.CompletedTask));
		var people = GeneratePeople(3);

		await manager.AddRangeAsync(people, TestContext.Current.CancellationToken);

		Assert.Equal(3, interceptor.PreWriteCount);
		Assert.Equal(3, interceptor.PostWriteCount);
	}

	[Fact]
	public async Task Should_ShortCircuitRangeOnSecondEntity() {
		var interceptor = new ShortCircuitOnSecondInterceptor();
		var (manager, repo) = BuildManager(interceptor, r =>
			r.AddRangeAsync(Arg.Any<IEnumerable<Person>>(), Arg.Any<CancellationToken>()).Returns(ValueTask.CompletedTask));
		var people = GeneratePeople(3);

		var result = await manager.AddRangeAsync(people, TestContext.Current.CancellationToken);

		Assert.False(result.IsSuccess());
		Assert.Equal(2, interceptor.PreWriteCount);
		Assert.Equal(0, interceptor.PostWriteCount);
		await repo.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<Person>>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Should_CreateContextPerEntityInRemoveRange() {
		var interceptor = new CountingInterceptor();
		var (manager, _) = BuildManager(interceptor, r =>
			r.RemoveRangeAsync(Arg.Any<IEnumerable<Person>>(), Arg.Any<CancellationToken>()).Returns(ValueTask.CompletedTask));
		var people = GeneratePeople(2);

		await manager.RemoveRangeAsync(people, TestContext.Current.CancellationToken);

		Assert.Equal(2, interceptor.PreWriteCount);
		Assert.Equal(2, interceptor.PostWriteCount);
	}

	private static (EntityManager<Person, string> manager, IRepository<Person, string> repo) BuildManager(
		IEntityManagerInterceptor<Person, string> interceptor,
		Action<IRepository<Person, string>> configureRepo) {
		var repo = Substitute.For<IRepository<Person, string>>();
		repo.GetEntityKey(Arg.Any<Person>()).Returns(c => c.Arg<Person>().Id);
		configureRepo(repo);

		var services = new ServiceCollection();
		services.AddSingleton<IEntityManagerInterceptor<Person, string>>(interceptor);
		var provider = services.BuildServiceProvider();
		var manager = new EntityManager<Person, string>(repo, services: provider);
		return (manager, repo);
	}

	private List<Person> GeneratePeople(int count) {
		var people = new List<Person>();
		for (var i = 0; i < count; i++) {
			var p = _faker.Generate();
			p.Id = (i + 1).ToString();
			people.Add(p);
		}
		return people;
	}

	private sealed class ShortCircuitOnSecondInterceptor : IEntityManagerInterceptor<Person, string> {
		public int PreWriteCount;
		public int PostWriteCount;

		public ValueTask<IOperationResult?> PreWriteAsync(IEntityOperationContext<Person, string> context) {
			PreWriteCount++;
			if (PreWriteCount == 2)
				return new ValueTask<IOperationResult?>(OperationResult.Fail(new OperationError("SHORT_CIRCUIT", "Test", "Second entity rejected")));
			return default;
		}

		public ValueTask PostWriteAsync(IEntityOperationContext<Person, string> context, IOperationResult result) {
			PostWriteCount++;
			return ValueTask.CompletedTask;
		}
	}
}