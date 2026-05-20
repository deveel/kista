using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.DependencyInjection;

namespace Deveel.Data;

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
	}
}
