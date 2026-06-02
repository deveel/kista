using System.ComponentModel.DataAnnotations;

using Kista.Caching;

using Microsoft.Extensions.DependencyInjection;

namespace Kista;

[Trait("Category", "Integration")]
[Trait("Layer", "Manager")]
[Trait("Feature", "Management")]
public class ManagementExtensionTests {
	[Fact]
	public void WithManagement_RegistersManagerForTrackedEntityTypes() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.AddRepository<ManagementTestRepository>(_ => { })
			.WithManagement();

		var provider = services.BuildServiceProvider();
		var manager = provider.GetService<EntityManager<ManagementTestEntity>>();
		Assert.NotNull(manager);
	}

	[Fact]
	public void WithManagement_DoesNotRegisterForUntrackedEntityTypes() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.WithManagement();

		var provider = services.BuildServiceProvider();
		// No entity types tracked, so no managers registered
		var manager = provider.GetService<EntityManager<ManagementTestEntity>>();
		Assert.Null(manager);
	}

	[Fact]
	public void WithManagement_WithTransientLifetime_CreatesNewInstances() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.AddRepository<ManagementTestRepository>(_ => { })
			.WithManagement(null, ServiceLifetime.Transient);

		var provider = services.BuildServiceProvider();
		var manager1 = provider.GetService<EntityManager<ManagementTestEntity>>();
		var manager2 = provider.GetService<EntityManager<ManagementTestEntity>>();
		Assert.NotNull(manager1);
		Assert.NotNull(manager2);
		Assert.NotSame(manager1, manager2);
	}

	public class ManagementTestEntity {
		[Key]
		public string Id { get; set; } = Guid.NewGuid().ToString();
	}

	public class ManagementTestRepository : IRepository<ManagementTestEntity> {
		public ValueTask AddAsync(ManagementTestEntity entity, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask AddRangeAsync(IEnumerable<ManagementTestEntity> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<bool> UpdateAsync(ManagementTestEntity entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask<bool> RemoveAsync(ManagementTestEntity entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask RemoveRangeAsync(IEnumerable<ManagementTestEntity> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<ManagementTestEntity?> FindAsync(object key, CancellationToken cancellationToken = default) => new((ManagementTestEntity?)null);
		public object? GetEntityKey(ManagementTestEntity entity) => (object?)entity.Id;
		public ValueTask<PageResult<ManagementTestEntity>> GetPageAsync(PageRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
	}
}
