using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.DependencyInjection;

namespace Deveel.Data;

[Trait("Category", "Unit")]
[Trait("Layer", "Core")]
[Trait("Feature", "RepositoryScanner")]
public class RepositoryScannerTests {
	[Fact]
	public void ScanRepositories_RegistersOpenGenerics() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.ScanRepositories(typeof(InMemoryRepository<>).Assembly);

		var provider = services.BuildServiceProvider();

		// Open generic should be resolvable for closed type
		var repo = provider.GetService<InMemoryRepository<ScanPerson>>();
		Assert.NotNull(repo);
	}

	[Fact]
	public void ScanRepositories_RegistersRepositoryInterfaces() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.ScanRepositories(typeof(InMemoryRepository<>).Assembly);

		var provider = services.BuildServiceProvider();

		var repo = provider.GetService<IRepository<ScanPerson>>();
		Assert.NotNull(repo);
		Assert.IsType<InMemoryRepository<ScanPerson>>(repo);
	}

	[Fact]
	public void ScanRepositories_SkipsExcludedTypes() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.ScanRepositories(typeof(ExcludedRepository).Assembly);

		var provider = services.BuildServiceProvider();

		// ExcludedRepository should not be registered
		var repo = provider.GetService<ExcludedRepository>();
		Assert.Null(repo);
	}

	[Fact]
	public void ScanRepositories_TracksEntityTypes() {
		var services = new ServiceCollection();
		var builder = services.AddRepositoryContext();
		builder.ScanRepositories(typeof(InMemoryRepository<>).Assembly);

		// InMemoryRepository<> tracks TEntity as an entity type (open generic)
		Assert.NotEmpty(builder.RegisteredEntityTypes);
	}

	[Fact]
	public void ScanRepositories_DoesNotDuplicateOnMultipleCalls() {
		var services = new ServiceCollection();
		var assembly = typeof(InMemoryRepository<>).Assembly;
		services.AddRepositoryContext()
			.ScanRepositories(assembly)
			.ScanRepositories(assembly);

		// Should not throw - duplicate scans are skipped
		var provider = services.BuildServiceProvider();
		var repo = provider.GetService<IRepository<ScanPerson>>();
		Assert.NotNull(repo);
	}

	public class ScanPerson {
		[Key]
		public string Id { get; set; } = Guid.NewGuid().ToString();
		public string? Name { get; set; }
	}

	[ExcludeFromScan]
	public class ExcludedRepository : IRepository<ScanPerson> {
		public ValueTask AddAsync(ScanPerson entity, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask AddRangeAsync(IEnumerable<ScanPerson> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<bool> UpdateAsync(ScanPerson entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask<bool> RemoveAsync(ScanPerson entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask RemoveRangeAsync(IEnumerable<ScanPerson> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<ScanPerson?> FindAsync(object key, CancellationToken cancellationToken = default) => new((ScanPerson?)null);
		public object? GetEntityKey(ScanPerson entity) => (object?)entity.Id;
	}
}
