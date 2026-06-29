using System.Collections.Immutable;
using System.Linq.Expressions;

using Bogus;

using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
#if !NET8_0 && !NET9_0
using Finbuckle.MultiTenant.Extensions;
#endif

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kista;

public abstract class MultiTenantRepositoryTestSuite<TTenantInfo, TPerson, TKey> : IAsyncLifetime, IAsyncDisposable
	where TTenantInfo : class, ITenantInfo, new()
	where TPerson : class, IPerson<TKey>
	where TKey : notnull {
	private IServiceProvider? rootServiceProvider;
	private AsyncServiceScope scope;

	protected MultiTenantRepositoryTestSuite(ITestOutputHelper? testOutput) {
		TestOutput = testOutput;
	}

	protected ITestOutputHelper? TestOutput { get; }

	protected IServiceProvider Services => scope.ServiceProvider;

	protected virtual int EntitySetCount => 100;

	protected virtual string[] TenantIds => new[] { "tenant1", "tenant2", "tenant3" };

	protected virtual IDictionary<string, IReadOnlyList<TPerson>>? People { get; private set; }

	protected abstract Faker<TPerson> CreatePersonFaker(string tenantId);

	protected TPerson GeneratePerson(string tenantId) => CreatePersonFaker(tenantId).Generate();

	protected ISystemTime TestTime { get; } = new TestTime();

	protected IList<TPerson> GeneratePeople(string tenantId, int count) => CreatePersonFaker(tenantId).Generate(count);

	protected abstract TKey GeneratePersonId();

	protected abstract TTenantInfo CreateTenantInfo(string tenantId);

	protected virtual void ConfigureServices(IServiceCollection services) {
		if (TestOutput != null)
			services.AddLogging(logging => { logging.ClearProviders(); logging.AddXUnit(TestOutput); });

		services.AddMultiTenant<TTenantInfo>()
			.WithContextStrategy()
			.WithInMemoryStore(tenants => {
				foreach (var tenantId in TenantIds) {
					tenants.Tenants.Add(CreateTenantInfo(tenantId));
				}
			});
	}

	private void BuildServices() {
		var services = new ServiceCollection();
		services.AddSystemTime(TestTime);

		ConfigureServices(services);

		rootServiceProvider = services.BuildServiceProvider();
		scope = rootServiceProvider.CreateAsyncScope();
	}

	async ValueTask IAsyncLifetime.InitializeAsync() {
		BuildServices();

		if (TenantIds != null) {
			People = TenantIds.ToDictionary(x => x, y => (IReadOnlyList<TPerson>)GeneratePeople(y, EntitySetCount).ToImmutableList());
		}

		await InitializeAsync();
	}

	protected virtual async ValueTask InitializeAsync() {
		foreach (var tenantId in TenantIds) {
			await ExecuteInTenantScopeAsync(tenantId, async (IRepository<TPerson, TKey> repository) => {
				await SeedAsync(tenantId, repository);
			});
		}
	}

	async ValueTask IAsyncDisposable.DisposeAsync() {
		People = null;

		await scope.DisposeAsync();
		(rootServiceProvider as IDisposable)?.Dispose();
	}

	protected virtual ValueTask DisposeAsync() {
		return ValueTask.CompletedTask;
	}

	protected virtual Task ExecuteInTenantScopeAsync(string tenantId, Delegate action) {
		var tenantContext = scope.ServiceProvider.GetRequiredService<TenantExecutionContext<TTenantInfo>>();
		return tenantContext.ExecuteInScopeAsync(tenantId, action);
	}

	protected virtual Task<TResult> ExecuteInTenantScopeAsync<TResult>(string tenantId, Delegate action) {
		var tenantContext = scope.ServiceProvider.GetRequiredService<TenantExecutionContext<TTenantInfo>>();
		return tenantContext.ExecuteInScopeAsync<TResult>(tenantId, action);
	}

	protected virtual async Task SeedAsync(string tenantId, IRepository<TPerson, TKey> repository) {
		if (People != null && People.TryGetValue(tenantId, out var people)) {
			await repository.AddRangeAsync(people);
		}
	}

	protected virtual Task<TPerson> RandomPersonAsync(string tenantId, Expression<Func<TPerson, bool>>? predicate = null) {
		if (People == null || !People.TryGetValue(tenantId, out var people))
			throw new InvalidOperationException($"No people found for tenant '{tenantId}'");

		var result = people?.Random(predicate?.Compile());

		if (result == null)
			throw new InvalidOperationException("No person found");

		return Task.FromResult(result);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "MultiTenancy")]
	public async Task Should_AddPerson_Within_TenantScope_When_PersonIsNew() {
		// Arrange
		var tenantId1 = TenantIds[0];
		var tenantId2 = TenantIds[1];
		var person = GeneratePerson(tenantId1);

		// Act
		var id = await ExecuteInTenantScopeAsync<TKey>(tenantId1, async (IRepository<TPerson, TKey> repository) => {
			await repository.AddAsync(person);
			return repository.GetEntityKey(person);
		});

		// Assert
		Assert.NotNull(id);

		await ExecuteInTenantScopeAsync(tenantId1, async (IRepository<TPerson, TKey> repository) => {
			var found = await repository.FindAsync(id);

			Assert.NotNull(found);
			Assert.Equal(person.FirstName, found.FirstName);
			Assert.Equal(person.LastName, found.LastName);
			Assert.Equal(person.Email, found.Email);
		});

		// verify that another tenant cannot access this data
		await ExecuteInTenantScopeAsync(tenantId2, async (IRepository<TPerson, TKey> repository) => {
			var found = await repository.FindAsync(id);
			Assert.Null(found);
		});
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "MultiTenancy")]
	public async Task Should_AddRange_Within_TenantScope_When_PeopleAreNew() {
		// Arrange
		var tenantId1 = TenantIds[0];
		var entities = GeneratePeople(tenantId1, 10);

		// Act & Assert (in-scope verification)
		await ExecuteInTenantScopeAsync(tenantId1, async (IRepository<TPerson, TKey> repository) => {
			await repository.AddRangeAsync(entities);

			foreach (var item in entities) {
				var key = repository.GetEntityKey(item);
				Assert.NotNull(key);

				var found = await repository.FindAsync(key);
				Assert.NotNull(found);
			}
		});

		// verify that another tenant cannot access this data
		var tenantId2 = TenantIds[1];
		await ExecuteInTenantScopeAsync(tenantId2, async (IRepository<TPerson, TKey> repository) => {
			foreach (var item in entities) {
				var key = repository.GetEntityKey(item);
				Assert.NotNull(key);
				var found = await repository.FindAsync(key);
				Assert.Null(found);
			}
		});
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "MultiTenancy")]
	public async Task Should_ReturnPerson_Only_In_OwnerTenant_When_PersonExists() {
		// Arrange
		var tenantId1 = TenantIds[0];
		var tenantId2 = TenantIds[1];
		var person = await RandomPersonAsync(tenantId1);

		// Act & Assert (owner tenant finds the person)
		await ExecuteInTenantScopeAsync(tenantId1, async (IRepository<TPerson, TKey> repository) => {
			var found = await repository.FindAsync(repository.GetEntityKey(person)!);

			Assert.NotNull(found);
			Assert.Equal(person.Id, found.Id);
			Assert.Equal(person.FirstName, found.FirstName);
			Assert.Equal(person.LastName, found.LastName);
		});

		// other tenant cannot find the same person
		await ExecuteInTenantScopeAsync(tenantId2, async (IRepository<TPerson, TKey> repository) => {
			var found = await repository.FindAsync(repository.GetEntityKey(person)!);
			Assert.Null(found);
		});
	}
}
