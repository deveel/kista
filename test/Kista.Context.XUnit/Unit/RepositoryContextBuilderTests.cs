using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.DependencyInjection;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Core")]
[Trait("Feature", "RepositoryContext")]
public class RepositoryContextBuilderTests {
	[Fact]
	public void AddRepositoryContext_ReturnsBuilder() {
		var services = new ServiceCollection();
		var builder = services.AddRepositoryContext();
		Assert.NotNull(builder);
		Assert.Same(services, builder.Services);
	}

	[Fact]
	public void Builder_Services_ReturnsSameCollection() {
		var services = new ServiceCollection();
		var builder = services.AddRepositoryContext();
		Assert.Same(services, builder.Services);
	}

	[Fact]
	public void AddRepository_TracksRepositoryType() {
		var services = new ServiceCollection();
		var builder = services.AddRepositoryContext();
		builder.AddRepository<TestRepository>();

		Assert.Contains(typeof(TestRepository), builder.RegisteredRepositoryTypes);
	}

	[Fact]
	public void AddRepository_TracksEntityType() {
		var services = new ServiceCollection();
		var builder = services.AddRepositoryContext();
		builder.AddRepository<TestRepository>();

		Assert.Contains(typeof(TestEntity), builder.RegisteredEntityTypes);
	}

	[Fact]
	public void AddRepository_ResolvesFromDI() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.AddRepository<TestRepository>();

		var provider = services.BuildServiceProvider();
		var repo = provider.GetService<IRepository<TestEntity>>();
		Assert.NotNull(repo);
		Assert.IsType<TestRepository>(repo);
	}

	[Fact]
	public void ImplicitConversion_FromInMemoryDriverBuilder() {
		var services = new ServiceCollection();
		var builder = services.AddRepositoryContext();
		RepositoryContextBuilder result = builder.UseInMemory();
		Assert.Same(builder, result);
	}

	[Fact]
	public void UseInMemory_WithDelegate_ReturnsBuilder() {
		var services = new ServiceCollection();
		var builder = services.AddRepositoryContext();
		var result = builder.UseInMemory(d => { });
		Assert.Same(builder, result);
	}

	[Fact]
	public void RegisteredEntityTypes_OnlyContainsEntitiesWithRepositories() {
		var services = new ServiceCollection();
		var builder = services.AddRepositoryContext();
		builder.AddRepository<TestRepository>();

		Assert.Single(builder.RegisteredEntityTypes);
		Assert.Contains(typeof(TestEntity), builder.RegisteredEntityTypes);
	}

	public class TestEntity {
		[Key]
		public string Id { get; set; } = Guid.NewGuid().ToString();
		public string? Name { get; set; }
	}

	public class TestRepository : IRepository<TestEntity> {
		private readonly List<TestEntity> _entities = new();

		public ValueTask AddAsync(TestEntity entity, CancellationToken cancellationToken = default) {
			_entities.Add(entity);
			return ValueTask.CompletedTask;
		}

		public ValueTask AddRangeAsync(IEnumerable<TestEntity> entities, CancellationToken cancellationToken = default) {
			_entities.AddRange(entities);
			return ValueTask.CompletedTask;
		}

		public ValueTask<bool> UpdateAsync(TestEntity entity, CancellationToken cancellationToken = default) {
			var idx = _entities.FindIndex(e => e.Id == entity.Id);
			if (idx < 0) return new ValueTask<bool>(false);
			_entities[idx] = entity;
			return new ValueTask<bool>(true);
		}

		public ValueTask<bool> RemoveAsync(TestEntity entity, CancellationToken cancellationToken = default) {
			return new ValueTask<bool>(_entities.Remove(entity));
		}

		public ValueTask RemoveRangeAsync(IEnumerable<TestEntity> entities, CancellationToken cancellationToken = default) {
			foreach (var e in entities) _entities.Remove(e);
			return ValueTask.CompletedTask;
		}

		public ValueTask<TestEntity?> FindAsync(object key, CancellationToken cancellationToken = default) {
			return new ValueTask<TestEntity?>(_entities.FirstOrDefault(e => e.Id == key));
		}

		public object? GetEntityKey(TestEntity entity) => (object?)entity.Id;

		public ValueTask<PageResult<TestEntity>> GetPageAsync(PageRequest request, CancellationToken cancellationToken = default) {
			var items = _entities.Skip(request.Offset).Take(request.Size).ToList();
			return new ValueTask<PageResult<TestEntity>>(new PageResult<TestEntity>(request, _entities.Count, items));
		}
	}

	public class TestEntityWithGuidId {
		[Key]
		public Guid Id { get; set; } = Guid.NewGuid();
		public string? Name { get; set; }
	}

	public class TestRepositoryWithKey : IRepository<TestEntityWithGuidId, Guid> {
		private readonly List<TestEntityWithGuidId> _entities = new();

		public ValueTask AddAsync(TestEntityWithGuidId entity, CancellationToken cancellationToken = default) {
			_entities.Add(entity);
			return ValueTask.CompletedTask;
		}

		public ValueTask AddRangeAsync(IEnumerable<TestEntityWithGuidId> entities, CancellationToken cancellationToken = default) {
			_entities.AddRange(entities);
			return ValueTask.CompletedTask;
		}

		public ValueTask<bool> UpdateAsync(TestEntityWithGuidId entity, CancellationToken cancellationToken = default) {
			var idx = _entities.FindIndex(e => e.Id == entity.Id);
			if (idx < 0) return new ValueTask<bool>(false);
			_entities[idx] = entity;
			return new ValueTask<bool>(true);
		}

		public ValueTask<bool> RemoveAsync(TestEntityWithGuidId entity, CancellationToken cancellationToken = default) {
			return new ValueTask<bool>(_entities.Remove(entity));
		}

		public ValueTask RemoveRangeAsync(IEnumerable<TestEntityWithGuidId> entities, CancellationToken cancellationToken = default) {
			foreach (var e in entities) _entities.Remove(e);
			return ValueTask.CompletedTask;
		}

		public ValueTask<TestEntityWithGuidId?> FindAsync(Guid key, CancellationToken cancellationToken = default) {
			return new ValueTask<TestEntityWithGuidId?>(_entities.FirstOrDefault(e => e.Id == key));
		}

		public Guid GetEntityKey(TestEntityWithGuidId entity) => entity.Id;

		public ValueTask<PageResult<TestEntityWithGuidId>> GetPageAsync(PageRequest request, CancellationToken cancellationToken = default) {
			var items = _entities.Skip(request.Offset).Take(request.Size).ToList();
			return new ValueTask<PageResult<TestEntityWithGuidId>>(new PageResult<TestEntityWithGuidId>(request, _entities.Count, items));
		}
	}

	public class AnotherTestEntity {
		[Key]
		public int Id { get; set; }
		public string? Description { get; set; }
	}

	public class AnotherTestRepository : IRepository<AnotherTestEntity> {
		public ValueTask AddAsync(AnotherTestEntity entity, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask AddRangeAsync(IEnumerable<AnotherTestEntity> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<bool> UpdateAsync(AnotherTestEntity entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask<bool> RemoveAsync(AnotherTestEntity entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask RemoveRangeAsync(IEnumerable<AnotherTestEntity> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<AnotherTestEntity?> FindAsync(object key, CancellationToken cancellationToken = default) => new((AnotherTestEntity?)null);
		public object? GetEntityKey(AnotherTestEntity entity) => entity.Id;
		public ValueTask<PageResult<AnotherTestEntity>> GetPageAsync(PageRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
	}

	public interface ICustomTestRepository : IRepository<TestEntity> {
		void CustomMethod();
	}

	public class CustomTestRepository : ICustomTestRepository {
		public void CustomMethod() { }
		public ValueTask AddAsync(TestEntity entity, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask AddRangeAsync(IEnumerable<TestEntity> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<bool> UpdateAsync(TestEntity entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask<bool> RemoveAsync(TestEntity entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask RemoveRangeAsync(IEnumerable<TestEntity> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<TestEntity?> FindAsync(object key, CancellationToken cancellationToken = default) => new((TestEntity?)null);
		public object? GetEntityKey(TestEntity entity) => (object?)entity.Id;
		public ValueTask<PageResult<TestEntity>> GetPageAsync(PageRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
	}
}

[Trait("Category", "Unit")]
[Trait("Layer", "Core")]
[Trait("Feature", "RepositoryContext")]
public class RepositoryContextBuilderExtendedTests {
	[Fact]
	public void AddRepository_WithTypeAndLifetime_RegistersWithCorrectLifetime() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.AddRepository<ExtendedTestRepository>(ServiceLifetime.Singleton);

		var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IRepository<ExtendedTestEntity>));
		Assert.NotNull(descriptor);
		Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
	}

	[Fact]
	public void AddRepository_WithTransientLifetime_CreatesNewInstances() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.AddRepository<ExtendedTestRepository>(ServiceLifetime.Transient);

		var provider = services.BuildServiceProvider();
		var repo1 = provider.GetService<IRepository<ExtendedTestEntity>>();
		var repo2 = provider.GetService<IRepository<ExtendedTestEntity>>();

		Assert.NotNull(repo1);
		Assert.NotNull(repo2);
		Assert.NotSame(repo1, repo2);
	}

	[Fact]
	public void AddRepository_WithSingletonLifetime_ReturnsSameInstance() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.AddRepository<ExtendedTestRepository>(ServiceLifetime.Singleton);

		var provider = services.BuildServiceProvider();
		var repo1 = provider.GetService<IRepository<ExtendedTestEntity>>();
		var repo2 = provider.GetService<IRepository<ExtendedTestEntity>>();

		Assert.NotNull(repo1);
		Assert.NotNull(repo2);
		Assert.Same(repo1, repo2);
	}

	[Fact]
	public void AddRepository_MultipleRepositories_TracksAllTypes() {
		var services = new ServiceCollection();
		var builder = services.AddRepositoryContext();
		builder.AddRepository<ExtendedTestRepository>(_ => { });
		builder.AddRepository<AnotherExtendedTestRepository>(_ => { });

		Assert.Contains(typeof(ExtendedTestRepository), builder.RegisteredRepositoryTypes);
		Assert.Contains(typeof(AnotherExtendedTestRepository), builder.RegisteredRepositoryTypes);
	}

	[Fact]
	public void AddRepository_MultipleRepositories_TracksAllEntityTypes() {
		var services = new ServiceCollection();
		var builder = services.AddRepositoryContext();
		builder.AddRepository<ExtendedTestRepository>(_ => { });
		builder.AddRepository<AnotherExtendedTestRepository>(_ => { });

		Assert.Contains(typeof(ExtendedTestEntity), builder.RegisteredEntityTypes);
		Assert.Contains(typeof(AnotherExtendedTestEntity), builder.RegisteredEntityTypes);
		Assert.Equal(2, builder.RegisteredEntityTypes.Count);
	}

	[Fact]
	public void AddRepository_DuplicateRegistration_DoesNotDuplicate() {
		var services = new ServiceCollection();
		var builder = services.AddRepositoryContext();
		builder.AddRepository<ExtendedTestRepository>(_ => { });
		builder.AddRepository<ExtendedTestRepository>(_ => { });

		Assert.Single(builder.RegisteredRepositoryTypes.Where(t => t == typeof(ExtendedTestRepository)));
	}

	[Fact]
	public void AddRepository_WithTypeParameter_ResolvesFromDI() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.AddRepository(typeof(ExtendedTestRepository));

		var provider = services.BuildServiceProvider();
		var repo = provider.GetService<IRepository<ExtendedTestEntity>>();
		Assert.NotNull(repo);
		Assert.IsType<ExtendedTestRepository>(repo);
	}

	[Fact]
	public void AddRepository_WithTypeParameter_ResolvesConcreteType() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.AddRepository(typeof(ExtendedTestRepository));

		var provider = services.BuildServiceProvider();
		var repo = provider.GetService<ExtendedTestRepository>();
		Assert.NotNull(repo);
	}

	[Fact]
	public void AddRepository_OpenGeneric_RegistersOpenGeneric() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.AddRepository(typeof(OpenGenericExtendedRepository<>));

		var provider = services.BuildServiceProvider();
		var repo = provider.GetService<IRepository<SimpleExtendedEntity>>();
		Assert.NotNull(repo);
		Assert.IsType<OpenGenericExtendedRepository<SimpleExtendedEntity>>(repo);
	}

	[Fact]
	public void AddRepository_OpenGenericWithKey_RegistersOpenGeneric() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.AddRepository(typeof(OpenGenericExtendedRepositoryWithKey<,>));

		var provider = services.BuildServiceProvider();
		var repo = provider.GetService<IRepository<SimpleExtendedEntity, string>>();
		Assert.NotNull(repo);
		Assert.IsType<OpenGenericExtendedRepositoryWithKey<SimpleExtendedEntity, string>>(repo);
	}

	[Fact]
	public void AddRepository_CustomInterface_ResolvesAllContracts() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.AddRepository<CustomExtendedTestRepository>();

		var provider = services.BuildServiceProvider();

		Assert.NotNull(provider.GetService<ICustomExtendedTestRepository>());
		Assert.NotNull(provider.GetService<IRepository<ExtendedTestEntity>>());
		Assert.NotNull(provider.GetService<CustomExtendedTestRepository>());
	}

	[Fact]
	public void AddRepository_WithKeyedRepository_ResolvesTwoParameterInterface() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.AddRepository<ExtendedTestRepositoryWithKey>();

		var provider = services.BuildServiceProvider();

		Assert.NotNull(provider.GetService<IRepository<ExtendedTestEntityWithGuidId, Guid>>());
		Assert.NotNull(provider.GetService<ExtendedTestRepositoryWithKey>());
	}

	[Fact]
	public void RegisteredEntityTypes_IsReadOnly() {
		var services = new ServiceCollection();
		var builder = services.AddRepositoryContext();
		builder.AddRepository<ExtendedTestRepository>();

		var entityTypes = builder.RegisteredEntityTypes;
		Assert.Throws<InvalidCastException>(() => {
			var list = (IList<Type>)entityTypes;
			list.Add(typeof(object));
		});
	}

	[Fact]
	public void Builder_Chaining_ReturnsSameBuilder() {
		var services = new ServiceCollection();
		var builder = services.AddRepositoryContext();
		var result = builder
			.AddRepository<ExtendedTestRepository>(_ => { })
			.AddRepository<AnotherExtendedTestRepository>(_ => { });

		Assert.Same(builder, result);
	}

	public class ExtendedTestEntity {
		[Key]
		public string Id { get; set; } = Guid.NewGuid().ToString();
		public string? Name { get; set; }
	}

	public class ExtendedTestRepository : IRepository<ExtendedTestEntity> {
		public ValueTask AddAsync(ExtendedTestEntity entity, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask AddRangeAsync(IEnumerable<ExtendedTestEntity> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<bool> UpdateAsync(ExtendedTestEntity entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask<bool> RemoveAsync(ExtendedTestEntity entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask RemoveRangeAsync(IEnumerable<ExtendedTestEntity> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<ExtendedTestEntity?> FindAsync(object key, CancellationToken cancellationToken = default) => new((ExtendedTestEntity?)null);
		public object? GetEntityKey(ExtendedTestEntity entity) => (object?)entity.Id;
		public ValueTask<PageResult<ExtendedTestEntity>> GetPageAsync(PageRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
	}

	public class ExtendedTestEntityWithGuidId {
		[Key]
		public Guid Id { get; set; } = Guid.NewGuid();
	}

	public class ExtendedTestRepositoryWithKey : IRepository<ExtendedTestEntityWithGuidId, Guid> {
		public ValueTask AddAsync(ExtendedTestEntityWithGuidId entity, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask AddRangeAsync(IEnumerable<ExtendedTestEntityWithGuidId> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<bool> UpdateAsync(ExtendedTestEntityWithGuidId entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask<bool> RemoveAsync(ExtendedTestEntityWithGuidId entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask RemoveRangeAsync(IEnumerable<ExtendedTestEntityWithGuidId> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<ExtendedTestEntityWithGuidId?> FindAsync(Guid key, CancellationToken cancellationToken = default) => new((ExtendedTestEntityWithGuidId?)null);
		public Guid GetEntityKey(ExtendedTestEntityWithGuidId entity) => entity.Id;
		public ValueTask<PageResult<ExtendedTestEntityWithGuidId>> GetPageAsync(PageRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
	}

	public class AnotherExtendedTestEntity {
		[Key]
		public int Id { get; set; }
	}

	public class AnotherExtendedTestRepository : IRepository<AnotherExtendedTestEntity> {
		public ValueTask AddAsync(AnotherExtendedTestEntity entity, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask AddRangeAsync(IEnumerable<AnotherExtendedTestEntity> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<bool> UpdateAsync(AnotherExtendedTestEntity entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask<bool> RemoveAsync(AnotherExtendedTestEntity entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask RemoveRangeAsync(IEnumerable<AnotherExtendedTestEntity> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<AnotherExtendedTestEntity?> FindAsync(object key, CancellationToken cancellationToken = default) => new((AnotherExtendedTestEntity?)null);
		public object? GetEntityKey(AnotherExtendedTestEntity entity) => entity.Id;
		public ValueTask<PageResult<AnotherExtendedTestEntity>> GetPageAsync(PageRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
	}

	public class SimpleExtendedEntity {
		[Key]
		public string Id { get; set; } = Guid.NewGuid().ToString();
	}

	public class OpenGenericExtendedRepository<TEntity> : IRepository<TEntity> where TEntity : class {
		public ValueTask AddAsync(TEntity entity, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<bool> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask<bool> RemoveAsync(TEntity entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask RemoveRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<TEntity?> FindAsync(object key, CancellationToken cancellationToken = default) => default;
		public object? GetEntityKey(TEntity entity) => null;
		public ValueTask<PageResult<TEntity>> GetPageAsync(PageRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
	}

	public class OpenGenericExtendedRepositoryWithKey<TEntity, TKey> : IRepository<TEntity, TKey> where TEntity : class {
		public ValueTask AddAsync(TEntity entity, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<bool> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask<bool> RemoveAsync(TEntity entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask RemoveRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<TEntity?> FindAsync(TKey key, CancellationToken cancellationToken = default) => default;
		public TKey? GetEntityKey(TEntity entity) => default;
		public ValueTask<PageResult<TEntity>> GetPageAsync(PageRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
	}

	public interface ICustomExtendedTestRepository : IRepository<ExtendedTestEntity> {
		void CustomMethod();
	}

	public class CustomExtendedTestRepository : ICustomExtendedTestRepository {
		public void CustomMethod() { }
		public ValueTask AddAsync(ExtendedTestEntity entity, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask AddRangeAsync(IEnumerable<ExtendedTestEntity> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<bool> UpdateAsync(ExtendedTestEntity entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask<bool> RemoveAsync(ExtendedTestEntity entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask RemoveRangeAsync(IEnumerable<ExtendedTestEntity> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<ExtendedTestEntity?> FindAsync(object key, CancellationToken cancellationToken = default) => new((ExtendedTestEntity?)null);
		public object? GetEntityKey(ExtendedTestEntity entity) => (object?)entity.Id;
		public ValueTask<PageResult<ExtendedTestEntity>> GetPageAsync(PageRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
	}
}
