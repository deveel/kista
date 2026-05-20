using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.DependencyInjection;

namespace Deveel.Data;

[Trait("Category", "Integration")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "InMemoryDriver")]
public class InMemoryContextIntegrationTests {
	[Fact]
	public void UseInMemory_WithoutDelegate_ResolvesRepository() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.UseInMemory();

		var provider = services.BuildServiceProvider();
		var repo = provider.GetService<IRepository<IntegrationPerson>>();
		Assert.NotNull(repo);
	}

	[Fact]
	public void UseInMemory_WithDelegate_ResolvesRepository() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.UseInMemory(d => d.WithFieldMapper<IntegrationPerson, TestFieldMapper>());

		var provider = services.BuildServiceProvider();
		var repo = provider.GetService<IRepository<IntegrationPerson>>();
		Assert.NotNull(repo);
	}

	[Fact]
	public void UseInMemory_WithScanRepositories_ResolvesScannedTypes() {
		var services = new ServiceCollection();
		var builder = services.AddRepositoryContext();
		builder.UseInMemory();
		builder.ScanRepositories(typeof(InMemoryRepository<>).Assembly);

		var provider = services.BuildServiceProvider();
		var repo = provider.GetService<IRepository<IntegrationPerson>>();
		Assert.NotNull(repo);
		Assert.IsType<InMemoryRepository<IntegrationPerson>>(repo);
	}

	[Fact]
	public async Task UseInMemory_CanAddAndFindEntity() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.UseInMemory();

		var provider = services.BuildServiceProvider();
		using var scope = provider.CreateScope();
		var repo = scope.ServiceProvider.GetRequiredService<IRepository<IntegrationPerson>>();

		var person = new IntegrationPerson { Id = Guid.NewGuid().ToString(), Name = "Test" };
		await repo.AddAsync(person);

		var found = await repo.FindAsync(person.Id);
		Assert.NotNull(found);
		Assert.Equal("Test", found.Name);
	}

	public class IntegrationPerson {
		[Key]
		public string Id { get; set; } = string.Empty;
		public string? Name { get; set; }
	}

	public class TestFieldMapper : IFieldMapper<IntegrationPerson> {
		public System.Linq.Expressions.Expression<Func<IntegrationPerson, object?>> MapField(string fieldName) {
			throw new System.NotImplementedException();
		}
	}
}
