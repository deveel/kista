#pragma warning disable CS8618

using NSubstitute;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
[Trait("Feature", "OperationPipeline")]
public class InterceptorRegistrationTests {
	private readonly PersonFaker _faker = new();

	[Fact]
	public async Task Should_InvokeInterceptorRegisteredViaWithInterceptor() {
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddRepositoryContext()
			.AddRepository<InMemoryRepository<Person, string>>(repo => repo
				.WithManagement(mgmt => mgmt
					.WithInterceptor<CountingInterceptor>()))
			.UseInMemory();

		var provider = services.BuildServiceProvider();
		using var scope = provider.CreateAsyncScope();
		var scopedProvider = scope.ServiceProvider;

		var interceptors = scopedProvider.GetRequiredService<IEnumerable<IEntityManagerInterceptor<Person, string>>>();
		var interceptor = Assert.IsType<CountingInterceptor>(interceptors.Single());

		var repo = scopedProvider.GetRequiredService<IRepository<Person, string>>();
		var manager = new EntityManager<Person, string>(repo, services: scopedProvider);

		var person = _faker.Generate();
		person.Id = "1";

		await manager.AddAsync(person, TestContext.Current.CancellationToken);

		Assert.Equal(1, interceptor.PreWriteCount);
		Assert.Equal(1, interceptor.PostWriteCount);
	}

	[Fact]
	public void Should_ThrowWhenInterceptorTypeIsNotConcrete() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.AddRepository<InMemoryRepository<Person, string>>(repo => repo
				.WithManagement(mgmt => {
					var act = () => mgmt.WithInterceptor<AbstractInterceptor>();
					Assert.Throws<ArgumentException>(act);
				}))
			.UseInMemory();
	}

	[Fact]
	public async Task Should_NotInvokeAnyInterceptor_When_NoneRegistered() {
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddRepositoryContext()
			.AddRepository<InMemoryRepository<Person, string>>(repo => repo
				.WithManagement())
			.UseInMemory();

		var provider = services.BuildServiceProvider();
		using var scope = provider.CreateAsyncScope();
		var manager = scope.ServiceProvider.GetRequiredService<EntityManager<Person, string>>();

		var person = _faker.Generate();
		person.Id = "1";

		var result = await manager.AddAsync(person, TestContext.Current.CancellationToken);

		Assert.True(result.IsSuccess());
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("SonarQube", "S1694",
		Justification = "Deliberately abstract to test that WithInterceptor rejects non-concrete class types.")]
	private abstract class AbstractInterceptor : IEntityManagerInterceptor<Person, string> {
		public abstract ValueTask<IOperationResult?> PreWriteAsync(IEntityOperationContext<Person, string> context);
		public abstract ValueTask PostWriteAsync(IEntityOperationContext<Person, string> context, IOperationResult result);
	}
}