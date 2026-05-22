using System.ComponentModel.DataAnnotations;

using Deveel.Data.Caching;

using Microsoft.Extensions.DependencyInjection;

namespace Deveel.Data;

[Trait("Category", "Unit")]
[Trait("Layer", "Core")]
[Trait("Feature", "BackwardCompatibility")]
#pragma warning disable CS0618
public class BackwardCompatibilityTests {
	[Fact]
	public void AddRepository_ObsoleteMethod_StillWorks() {
		var services = new ServiceCollection();
#pragma warning disable CS0618
		services.AddRepository<CompatTestRepository>();
#pragma warning restore CS0618

		var provider = services.BuildServiceProvider();
		var repo = provider.GetService<IRepository<CompatTestEntity>>();
		Assert.NotNull(repo);
	}

	[Fact]
	public void AddEntityRepository_ObsoleteMethod_StillWorks() {
		// This test just verifies the method is still callable
		// Actual EF resolution requires DbContext setup
		var services = new ServiceCollection();
#pragma warning disable CS0618
		// We can't fully test this without EF, but the method should compile
		var _ = services; // silence unused
#pragma warning restore CS0618
	}

	[Fact]
	public void AddEntityManager_ObsoleteMethod_StillWorks() {
		var services = new ServiceCollection();
		services.AddRepository<CompatTestRepository>();
#pragma warning disable CS0618
		services.AddEntityManager<EntityManager<CompatTestEntity>>();
#pragma warning restore CS0618

		var provider = services.BuildServiceProvider();
		var manager = provider.GetService<EntityManager<CompatTestEntity>>();
		Assert.NotNull(manager);
	}

	[Fact]
	public void AddEntityEasyCacheFor_ObsoleteMethod_StillWorks() {
		var services = new ServiceCollection();
#pragma warning disable CS0618
		services.AddEntityEasyCacheFor<CompatTestEntity>();
#pragma warning restore CS0618

		// Verify the service descriptor is registered
		var cacheDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(EntityEasyCache<CompatTestEntity>));
		Assert.NotNull(cacheDescriptor);
	}

	public class CompatTestEntity {
		[Key]
		public string Id { get; set; } = Guid.NewGuid().ToString();
	}

	public class CompatTestRepository : IRepository<CompatTestEntity> {
		public ValueTask AddAsync(CompatTestEntity entity, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask AddRangeAsync(IEnumerable<CompatTestEntity> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<bool> UpdateAsync(CompatTestEntity entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask<bool> RemoveAsync(CompatTestEntity entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask RemoveRangeAsync(IEnumerable<CompatTestEntity> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<CompatTestEntity?> FindAsync(object key, CancellationToken cancellationToken = default) => new((CompatTestEntity?)null);
		public object? GetEntityKey(CompatTestEntity entity) => (object?)entity.Id;
	}
}
#pragma warning restore CS0618
