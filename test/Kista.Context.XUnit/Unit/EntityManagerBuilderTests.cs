using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Deveel;
using Kista.Caching;

using Microsoft.Extensions.DependencyInjection;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
[Trait("Feature", "EntityManager")]
public class EntityManagerBuilderTests {
	[Fact]
	public void WithManagement_RegistersEntityManager() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.AddRepository<TestPersonRepository>(repo => repo
				.WithManagement());

		var provider = services.BuildServiceProvider();
		var manager = provider.GetService<EntityManager<TestPersonEntity>>();

		Assert.NotNull(manager);
	}

	[Fact]
	public void WithManagement_WithoutCallback_ReturnsRepositoryBuilder() {
		var services = new ServiceCollection();
		var builder = services.AddRepositoryContext();
		var repoBuilder = builder.AddRepository<TestPersonRepository>();
		var result = repoBuilder.WithManagement();

		Assert.Same(repoBuilder, result);
	}

	[Fact]
	public void WithManagement_WithCallback_ReturnsRepositoryBuilder() {
		var services = new ServiceCollection();
		var builder = services.AddRepositoryContext();
		var repoBuilder = builder.AddRepository<TestPersonRepository>();
		var result = repoBuilder.WithManagement(mgmt => { });

		Assert.Same(repoBuilder, result);
	}

	[Fact]
	public void WithManagement_RegistersValidator() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.AddRepository<TestPersonRepository>(repo => repo
				.WithManagement(mgmt => mgmt
					.WithValidator<TestPersonValidator>()));

		var provider = services.BuildServiceProvider();
		var validator = provider.GetService<IEntityValidator<TestPersonEntity>>();

		Assert.NotNull(validator);
		Assert.IsType<TestPersonValidator>(validator);
	}

	[Fact]
	public void WithManagement_RegistersCacheKeyGenerator() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.AddRepository<TestPersonRepository>(repo => repo
				.WithManagement(mgmt => mgmt
					.WithCacheKeyGenerator<TestPersonKeyGenerator>()));

		var provider = services.BuildServiceProvider();
		var generator = provider.GetService<IEntityCacheKeyGenerator<TestPersonEntity>>();

		Assert.NotNull(generator);
		Assert.IsType<TestPersonKeyGenerator>(generator);
	}

	[Fact]
	public void WithManagement_RegistersOperationErrorFactory() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.AddRepository<TestPersonRepository>(repo => repo
				.WithManagement(mgmt => mgmt
					.WithOperationErrorFactory<TestPersonErrorFactory>()));

		var provider = services.BuildServiceProvider();
		var factory = provider.GetService<IOperationErrorFactory<TestPersonEntity>>();

		Assert.NotNull(factory);
	}

	[Fact]
	public void GlobalWithManagement_StillWorks() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.AddRepository<TestPersonRepository>(_ => { })
			.WithManagement();

		var provider = services.BuildServiceProvider();
		var manager = provider.GetService<EntityManager<TestPersonEntity>>();

		Assert.NotNull(manager);
	}

	[Fact]
	public void GlobalWithManagement_Callback_StillWorks() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.AddRepository<TestPersonRepository>(_ => { })
			.WithManagement(opts => { });

		var provider = services.BuildServiceProvider();
		var manager = provider.GetService<EntityManager<TestPersonEntity>>();

		Assert.NotNull(manager);
	}

	#region Test Types

	public class TestPersonEntity {
		[Key]
		public string Id { get; set; } = Guid.NewGuid().ToString();
		public string? Name { get; set; }
	}

	public class TestPersonRepository : IRepository<TestPersonEntity> {
		private readonly List<TestPersonEntity> _entities = new();

		public ValueTask AddAsync(TestPersonEntity entity, CancellationToken cancellationToken = default) {
			_entities.Add(entity);
			return ValueTask.CompletedTask;
		}

		public ValueTask AddRangeAsync(IEnumerable<TestPersonEntity> entities, CancellationToken cancellationToken = default) {
			_entities.AddRange(entities);
			return ValueTask.CompletedTask;
		}

		public ValueTask<bool> UpdateAsync(TestPersonEntity entity, CancellationToken cancellationToken = default) {
			var idx = _entities.FindIndex(e => e.Id == entity.Id);
			if (idx < 0) return new ValueTask<bool>(false);
			_entities[idx] = entity;
			return new ValueTask<bool>(true);
		}

		public ValueTask<bool> RemoveAsync(TestPersonEntity entity, CancellationToken cancellationToken = default) {
			return new ValueTask<bool>(_entities.Remove(entity));
		}

		public ValueTask RemoveRangeAsync(IEnumerable<TestPersonEntity> entities, CancellationToken cancellationToken = default) {
			foreach (var e in entities) _entities.Remove(e);
			return ValueTask.CompletedTask;
		}

		public ValueTask<TestPersonEntity?> FindAsync(object key, CancellationToken cancellationToken = default) {
			return new ValueTask<TestPersonEntity?>(_entities.FirstOrDefault(e => e.Id == (string)key));
		}

		public object? GetEntityKey(TestPersonEntity entity) => entity.Id;

		public ValueTask<PageResult<TestPersonEntity>> GetPageAsync(PageRequest request, CancellationToken cancellationToken = default) {
			var items = _entities.Skip(request.Offset).Take(request.Size).ToList();
			return new ValueTask<PageResult<TestPersonEntity>>(new PageResult<TestPersonEntity>(request, _entities.Count, items));
		}
	}

	public class TestPersonValidator : IEntityValidator<TestPersonEntity> {
		public async IAsyncEnumerable<ValidationResult> ValidateAsync(
			EntityManager<TestPersonEntity> manager,
			TestPersonEntity entity,
			[EnumeratorCancellation] CancellationToken cancellationToken = default) {
			yield break;
		}
	}

	public class TestPersonKeyGenerator : IEntityCacheKeyGenerator<TestPersonEntity> {
		public string GenerateKey(object key) => $"person:{key}";
		public string[] GenerateAllKeys(TestPersonEntity entity) => [$"person:{entity.Id}"];
	}

	public class TestPersonErrorFactory : OperationErrorFactory {
	}

	#endregion
}
