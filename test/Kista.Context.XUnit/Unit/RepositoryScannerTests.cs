using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.DependencyInjection;

namespace Kista;

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

[Trait("Category", "Unit")]
[Trait("Layer", "Core")]
[Trait("Feature", "RepositoryScanner")]
public class RepositoryScannerExtendedTests {
	[Fact]
	public void ScanRepositories_ScansMultipleAssemblies() {
		var services = new ServiceCollection();
		var builder = services.AddRepositoryContext();
		builder.ScanRepositories(
			typeof(InMemoryRepository<>).Assembly,
			typeof(InMemoryRepository<,>).Assembly
		);

		var provider = services.BuildServiceProvider();
		var repo = provider.GetService<IRepository<ScanExtendedPerson>>();
		Assert.NotNull(repo);
	}

	[Fact]
	public void ScanRepositories_SkipsAbstractTypes() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.ScanRepositories(typeof(ScanExtendedPerson).Assembly);

		var provider = services.BuildServiceProvider();
		var repo = provider.GetService<AbstractScanExtendedRepository>();
		Assert.Null(repo);
	}

	[Fact]
	public void ScanRepositories_SkipsInterfaces() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.ScanRepositories(typeof(ScanExtendedPerson).Assembly);

		var provider = services.BuildServiceProvider();
		var repo = provider.GetService<IScanExtendedRepository>();
		Assert.Null(repo);
	}

	[Fact]
	public void ScanRepositories_TracksScannedRepositoryTypes() {
		var services = new ServiceCollection();
		var builder = services.AddRepositoryContext();
		builder.ScanRepositories(typeof(InMemoryRepository<>).Assembly);

		Assert.NotEmpty(builder.RegisteredRepositoryTypes);
		Assert.Contains(typeof(InMemoryRepository<>), builder.RegisteredRepositoryTypes);
	}

	[Fact]
	public void ScanRepositories_RegistersConcreteRepository_WhenPresent() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.ScanRepositories(typeof(ConcreteScanRepository).Assembly);

		var provider = services.BuildServiceProvider();
		var repo = provider.GetService<IRepository<ScanExtendedPerson>>();
		Assert.NotNull(repo);
		Assert.IsType<ConcreteScanRepository>(repo);
	}

	[Fact]
	public void ScanRepositories_ResolvesConcreteRepositoryDirectly() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.ScanRepositories(typeof(ConcreteScanRepository).Assembly);

		var provider = services.BuildServiceProvider();
		var repo = provider.GetService<ConcreteScanRepository>();
		Assert.NotNull(repo);
	}

	[Fact]
	public void ScanRepositories_WithEmptyAssembly_DoesNotThrow() {
		var services = new ServiceCollection();
		var builder = services.AddRepositoryContext();

		// Should not throw even if assembly has no repositories
		builder.ScanRepositories(typeof(object).Assembly);
	}

	[Fact]
	public void ScanRepositories_PreservesExistingRegistrations() {
		var services = new ServiceCollection();
		var builder = services.AddRepositoryContext();
		builder.AddRepository<ExplicitScanRepository>(_ => { });

		var initialCount = services.Count;
		builder.ScanRepositories(typeof(InMemoryRepository<>).Assembly);

		// Should have added more services
		Assert.True(services.Count > initialCount);
	}

	[Fact]
	public void ScanRepositories_OpenGenericRepository_CanResolveForMultipleEntityTypes() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.ScanRepositories(typeof(InMemoryRepository<>).Assembly);

		var provider = services.BuildServiceProvider();

		var repo1 = provider.GetService<IRepository<ScanExtendedPerson>>();
		var repo2 = provider.GetService<IRepository<AnotherScanEntity>>();

		Assert.NotNull(repo1);
		Assert.NotNull(repo2);
	}

	public class ScanExtendedPerson {
		[Key]
		public string Id { get; set; } = Guid.NewGuid().ToString();
		public string? Name { get; set; }
	}

	public class AnotherScanEntity {
		[Key]
		public int Id { get; set; }
	}

	public interface IScanExtendedRepository : IRepository<ScanExtendedPerson> { }

	public abstract class AbstractScanExtendedRepository : IRepository<ScanExtendedPerson> {
		public abstract ValueTask AddAsync(ScanExtendedPerson entity, CancellationToken cancellationToken = default);
		public abstract ValueTask AddRangeAsync(IEnumerable<ScanExtendedPerson> entities, CancellationToken cancellationToken = default);
		public abstract ValueTask<bool> UpdateAsync(ScanExtendedPerson entity, CancellationToken cancellationToken = default);
		public abstract ValueTask<bool> RemoveAsync(ScanExtendedPerson entity, CancellationToken cancellationToken = default);
		public abstract ValueTask RemoveRangeAsync(IEnumerable<ScanExtendedPerson> entities, CancellationToken cancellationToken = default);
		public abstract ValueTask<ScanExtendedPerson?> FindAsync(object key, CancellationToken cancellationToken = default);
		public abstract object? GetEntityKey(ScanExtendedPerson entity);
	}

	public class ConcreteScanRepository : IRepository<ScanExtendedPerson> {
		public ValueTask AddAsync(ScanExtendedPerson entity, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask AddRangeAsync(IEnumerable<ScanExtendedPerson> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<bool> UpdateAsync(ScanExtendedPerson entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask<bool> RemoveAsync(ScanExtendedPerson entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask RemoveRangeAsync(IEnumerable<ScanExtendedPerson> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<ScanExtendedPerson?> FindAsync(object key, CancellationToken cancellationToken = default) => new((ScanExtendedPerson?)null);
		public object? GetEntityKey(ScanExtendedPerson entity) => (object?)entity.Id;
	}

	public class ExplicitScanRepository : IRepository<ScanExtendedPerson> {
		public ValueTask AddAsync(ScanExtendedPerson entity, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask AddRangeAsync(IEnumerable<ScanExtendedPerson> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<bool> UpdateAsync(ScanExtendedPerson entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask<bool> RemoveAsync(ScanExtendedPerson entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask RemoveRangeAsync(IEnumerable<ScanExtendedPerson> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<ScanExtendedPerson?> FindAsync(object key, CancellationToken cancellationToken = default) => new((ScanExtendedPerson?)null);
		public object? GetEntityKey(ScanExtendedPerson entity) => (object?)entity.Id;
	}

	public interface ISpecializedScanRepository : IRepository<ScanExtendedPerson> {
		void SpecialMethod();
	}

	public class SpecializedScanRepository : ISpecializedScanRepository {
		public void SpecialMethod() { }
		public ValueTask AddAsync(ScanExtendedPerson entity, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask AddRangeAsync(IEnumerable<ScanExtendedPerson> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<bool> UpdateAsync(ScanExtendedPerson entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask<bool> RemoveAsync(ScanExtendedPerson entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask RemoveRangeAsync(IEnumerable<ScanExtendedPerson> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<ScanExtendedPerson?> FindAsync(object key, CancellationToken cancellationToken = default) => new((ScanExtendedPerson?)null);
		public object? GetEntityKey(ScanExtendedPerson entity) => (object?)entity.Id;
	}
}
