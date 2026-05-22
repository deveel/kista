using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.DependencyInjection;

namespace Deveel.Data;

[Trait("Category", "Unit")]
[Trait("Layer", "Core")]
[Trait("Feature", "RepositoryRegistration")]
public class RepositoryRegistrationIntegrationTests {
	[Fact]
	public void AddRepositoryContext_AddRepository_ResolvesFromDI() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.AddRepository<SimpleTestRepository>();

		var provider = services.BuildServiceProvider();
		var repo = provider.GetService<IRepository<SimpleTestEntity>>();
		Assert.NotNull(repo);
		Assert.IsType<SimpleTestRepository>(repo);
	}

	[Fact]
	public void AddRepositoryContext_AddRepository_ResolvesConcreteType() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.AddRepository<SimpleTestRepository>();

		var provider = services.BuildServiceProvider();
		var repo = provider.GetService<SimpleTestRepository>();
		Assert.NotNull(repo);
	}

	[Fact]
	public void AddRepositoryContext_AddRepositoryWithKey_ResolvesTwoParameterInterface() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.AddRepository<TestRepositoryWithKey>();

		var provider = services.BuildServiceProvider();

		Assert.NotNull(provider.GetService<IRepository<TestEntityWithKey, string>>());
		Assert.NotNull(provider.GetService<TestRepositoryWithKey>());
	}

	[Fact]
	public void AddRepositoryContext_AddRepositoryOpenGeneric_CanResolveForMultipleEntityTypes() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.AddRepository(typeof(OpenGenericTestRepository<>));

		var provider = services.BuildServiceProvider();

		var repo1 = provider.GetService<IRepository<SimpleTestEntity>>();
		var repo2 = provider.GetService<IRepository<AnotherTestEntity>>();

		Assert.NotNull(repo1);
		Assert.NotNull(repo2);
		Assert.IsType<OpenGenericTestRepository<SimpleTestEntity>>(repo1);
		Assert.IsType<OpenGenericTestRepository<AnotherTestEntity>>(repo2);
	}

	[Fact]
	public void AddRepositoryContext_AddRepositoryOpenGenericWithKey_CanResolveForMultipleEntityTypes() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.AddRepository(typeof(OpenGenericTestRepositoryWithKey<,>));

		var provider = services.BuildServiceProvider();

		var repo1 = provider.GetService<IRepository<SimpleTestEntity, string>>();
		var repo2 = provider.GetService<IRepository<AnotherTestEntity, int>>();

		Assert.NotNull(repo1);
		Assert.NotNull(repo2);
	}

	[Fact]
	public void AddRepositoryContext_AddRepositoryCustomInterface_ResolvesAllContracts() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.AddRepository<CustomInterfaceTestRepository>();

		var provider = services.BuildServiceProvider();

		Assert.NotNull(provider.GetService<ICustomTestRepository>());
		Assert.NotNull(provider.GetService<IRepository<SimpleTestEntity>>());
		Assert.NotNull(provider.GetService<CustomInterfaceTestRepository>());
	}

	[Fact]
	public void AddRepositoryContext_AddRepositoryWithDifferentLifetimes() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.AddRepository<SimpleTestRepository>(ServiceLifetime.Singleton)
			.AddRepository<AnotherTestRepository>(ServiceLifetime.Transient);

		var provider = services.BuildServiceProvider();

		var singleton1 = provider.GetService<IRepository<SimpleTestEntity>>();
		var singleton2 = provider.GetService<IRepository<SimpleTestEntity>>();
		Assert.Same(singleton1, singleton2);

		var transient1 = provider.GetService<IRepository<AnotherTestEntity>>();
		var transient2 = provider.GetService<IRepository<AnotherTestEntity>>();
		Assert.NotSame(transient1, transient2);
	}

	[Fact]
	public void AddRepositoryContext_MultipleRepositories_TracksAllTypes() {
		var services = new ServiceCollection();
		var builder = services.AddRepositoryContext()
			.AddRepository<SimpleTestRepository>()
			.AddRepository<AnotherTestRepository>();

		Assert.Contains(typeof(SimpleTestRepository), builder.RegisteredRepositoryTypes);
		Assert.Contains(typeof(AnotherTestRepository), builder.RegisteredRepositoryTypes);
	}

	[Fact]
	public void AddRepositoryContext_MultipleRepositories_TracksAllEntityTypes() {
		var services = new ServiceCollection();
		var builder = services.AddRepositoryContext()
			.AddRepository<SimpleTestRepository>()
			.AddRepository<AnotherTestRepository>();

		Assert.Contains(typeof(SimpleTestEntity), builder.RegisteredEntityTypes);
		Assert.Contains(typeof(AnotherTestEntity), builder.RegisteredEntityTypes);
		Assert.Equal(2, builder.RegisteredEntityTypes.Count);
	}

	[Fact]
	public void AddRepositoryContext_DuplicateRegistration_DoesNotDuplicate() {
		var services = new ServiceCollection();
		var builder = services.AddRepositoryContext()
			.AddRepository<SimpleTestRepository>()
			.AddRepository<SimpleTestRepository>();

		Assert.Single(builder.RegisteredRepositoryTypes.Where(t => t == typeof(SimpleTestRepository)));
	}

	[Fact]
	public void AddRepositoryContext_ScanRepositories_RegistersOpenGenerics() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.ScanRepositories(typeof(InMemoryRepository<>).Assembly);

		var provider = services.BuildServiceProvider();

		var repo = provider.GetService<InMemoryRepository<SimpleTestEntity>>();
		Assert.NotNull(repo);
	}

	[Fact]
	public void AddRepositoryContext_ScanRepositories_RegistersRepositoryInterfaces() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.ScanRepositories(typeof(InMemoryRepository<>).Assembly);

		var provider = services.BuildServiceProvider();

		var repo = provider.GetService<IRepository<SimpleTestEntity>>();
		Assert.NotNull(repo);
		Assert.IsType<InMemoryRepository<SimpleTestEntity>>(repo);
	}

	[Fact]
	public void AddRepositoryContext_ScanRepositories_TracksEntityTypes() {
		var services = new ServiceCollection();
		var builder = services.AddRepositoryContext();
		builder.ScanRepositories(typeof(InMemoryRepository<>).Assembly);

		Assert.NotEmpty(builder.RegisteredEntityTypes);
	}

	[Fact]
	public void AddRepositoryContext_ScanRepositories_DoesNotDuplicateOnMultipleCalls() {
		var services = new ServiceCollection();
		var assembly = typeof(InMemoryRepository<>).Assembly;
		services.AddRepositoryContext()
			.ScanRepositories(assembly)
			.ScanRepositories(assembly);

		var provider = services.BuildServiceProvider();
		var repo = provider.GetService<IRepository<SimpleTestEntity>>();
		Assert.NotNull(repo);
	}

	[Fact]
	public void AddRepositoryContext_ScanRepositories_SkipsExcludedTypes() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.ScanRepositories(typeof(ExcludedTestRepository).Assembly);

		var provider = services.BuildServiceProvider();

		var repo = provider.GetService<ExcludedTestRepository>();
		Assert.Null(repo);
	}

	[Fact]
	public void AddRepositoryContext_Chaining_ReturnsSameBuilder() {
		var services = new ServiceCollection();
		var builder = services.AddRepositoryContext();
		var result = builder
			.AddRepository<SimpleTestRepository>()
			.AddRepository<AnotherTestRepository>();

		Assert.Same(builder, result);
	}

	public class SimpleTestEntity {
		[Key]
		public string Id { get; set; } = Guid.NewGuid().ToString();
		public string? Name { get; set; }
	}

	public class AnotherTestEntity {
		[Key]
		public int Id { get; set; }
		public string? Description { get; set; }
	}

	public class TestEntityWithKey {
		[Key]
		public string Id { get; set; } = Guid.NewGuid().ToString();
	}

	public class SimpleTestRepository : IRepository<SimpleTestEntity> {
		public ValueTask AddAsync(SimpleTestEntity entity, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask AddRangeAsync(IEnumerable<SimpleTestEntity> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<bool> UpdateAsync(SimpleTestEntity entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask<bool> RemoveAsync(SimpleTestEntity entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask RemoveRangeAsync(IEnumerable<SimpleTestEntity> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<SimpleTestEntity?> FindAsync(object key, CancellationToken cancellationToken = default) => new((SimpleTestEntity?)null);
		public object? GetEntityKey(SimpleTestEntity entity) => (object?)entity.Id;
	}

	public class AnotherTestRepository : IRepository<AnotherTestEntity> {
		public ValueTask AddAsync(AnotherTestEntity entity, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask AddRangeAsync(IEnumerable<AnotherTestEntity> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<bool> UpdateAsync(AnotherTestEntity entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask<bool> RemoveAsync(AnotherTestEntity entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask RemoveRangeAsync(IEnumerable<AnotherTestEntity> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<AnotherTestEntity?> FindAsync(object key, CancellationToken cancellationToken = default) => new((AnotherTestEntity?)null);
		public object? GetEntityKey(AnotherTestEntity entity) => entity.Id;
	}

	public class TestRepositoryWithKey : IRepository<TestEntityWithKey, string> {
		public ValueTask AddAsync(TestEntityWithKey entity, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask AddRangeAsync(IEnumerable<TestEntityWithKey> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<bool> UpdateAsync(TestEntityWithKey entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask<bool> RemoveAsync(TestEntityWithKey entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask RemoveRangeAsync(IEnumerable<TestEntityWithKey> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<TestEntityWithKey?> FindAsync(string key, CancellationToken cancellationToken = default) => new((TestEntityWithKey?)null);
		public string GetEntityKey(TestEntityWithKey entity) => entity.Id;
	}

	public class OpenGenericTestRepository<TEntity> : IRepository<TEntity> where TEntity : class {
		public ValueTask AddAsync(TEntity entity, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<bool> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask<bool> RemoveAsync(TEntity entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask RemoveRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<TEntity?> FindAsync(object key, CancellationToken cancellationToken = default) => default;
		public object? GetEntityKey(TEntity entity) => null;
	}

	public class OpenGenericTestRepositoryWithKey<TEntity, TKey> : IRepository<TEntity, TKey> where TEntity : class {
		public ValueTask AddAsync(TEntity entity, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<bool> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask<bool> RemoveAsync(TEntity entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask RemoveRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<TEntity?> FindAsync(TKey key, CancellationToken cancellationToken = default) => default;
		public TKey GetEntityKey(TEntity entity) => default!;
	}

	public interface ICustomTestRepository : IRepository<SimpleTestEntity> {
		void CustomMethod();
	}

	public class CustomInterfaceTestRepository : ICustomTestRepository {
		public void CustomMethod() { }
		public ValueTask AddAsync(SimpleTestEntity entity, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask AddRangeAsync(IEnumerable<SimpleTestEntity> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<bool> UpdateAsync(SimpleTestEntity entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask<bool> RemoveAsync(SimpleTestEntity entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask RemoveRangeAsync(IEnumerable<SimpleTestEntity> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<SimpleTestEntity?> FindAsync(object key, CancellationToken cancellationToken = default) => new((SimpleTestEntity?)null);
		public object? GetEntityKey(SimpleTestEntity entity) => (object?)entity.Id;
	}

	[ExcludeFromScan]
	public class ExcludedTestRepository : IRepository<SimpleTestEntity> {
		public ValueTask AddAsync(SimpleTestEntity entity, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask AddRangeAsync(IEnumerable<SimpleTestEntity> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<bool> UpdateAsync(SimpleTestEntity entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask<bool> RemoveAsync(SimpleTestEntity entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask RemoveRangeAsync(IEnumerable<SimpleTestEntity> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<SimpleTestEntity?> FindAsync(object key, CancellationToken cancellationToken = default) => new((SimpleTestEntity?)null);
		public object? GetEntityKey(SimpleTestEntity entity) => (object?)entity.Id;
	}
}
