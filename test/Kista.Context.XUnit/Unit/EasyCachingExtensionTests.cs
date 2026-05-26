using System.ComponentModel.DataAnnotations;

using Kista.Caching;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Kista;

[Trait("Category", "Integration")]
[Trait("Layer", "Manager")]
[Trait("Feature", "EasyCaching")]
public class EasyCachingExtensionTests {
	[Fact]
	public void WithEasyCaching_RegistersCacheForTrackedEntityTypes() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.AddRepository<CachingTestRepository>()
			.WithEasyCaching();

		// Verify the service descriptor is registered
		var cacheDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IEntityCache<CachingTestEntity>));
		Assert.NotNull(cacheDescriptor);
	}

	[Fact]
	public void WithEasyCaching_DoesNotRegisterForUntrackedEntityTypes() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.WithEasyCaching();

		var cacheDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IEntityCache<CachingTestEntity>));
		Assert.Null(cacheDescriptor);
	}

	[Fact]
	public void WithEasyCaching_WithDefaultExpiration_RegistersOptions() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.AddRepository<CachingTestRepository>()
			.WithEasyCaching(opts => opts.DefaultExpiration = TimeSpan.FromMinutes(30));

		var optionsDescriptor = services.FirstOrDefault(d =>
			d.ServiceType == typeof(IConfigureOptions<EntityCacheOptions<CachingTestEntity>>));
		Assert.NotNull(optionsDescriptor);
	}

	[Fact]
	public void WithEasyCaching_WithCacheKeyPrefix_RegistersOptions() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.AddRepository<CachingTestRepository>()
			.WithEasyCaching(opts => opts.CacheKeyPrefix = "myapp:");

		var optionsDescriptor = services.FirstOrDefault(d =>
			d.ServiceType == typeof(IConfigureOptions<EntityCacheOptions<CachingTestEntity>>));
		Assert.NotNull(optionsDescriptor);
	}

	[Fact]
	public void WithEasyCaching_WithOptionsAppliesToAllEntityTypes() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.AddRepository<CachingTestRepository>()
			.AddRepository<SecondCachingRepository>()
			.WithEasyCaching(opts => opts.DefaultExpiration = TimeSpan.FromHours(1));

		var optionsForFirst = services.FirstOrDefault(d =>
			d.ServiceType == typeof(IConfigureOptions<EntityCacheOptions<CachingTestEntity>>));
		var optionsForSecond = services.FirstOrDefault(d =>
			d.ServiceType == typeof(IConfigureOptions<EntityCacheOptions<SecondCachingEntity>>));

		Assert.NotNull(optionsForFirst);
		Assert.NotNull(optionsForSecond);
	}

	public class SecondCachingEntity {
		[Key]
		public string Id { get; set; } = Guid.NewGuid().ToString();
	}

	public class SecondCachingRepository : IRepository<SecondCachingEntity> {
		public ValueTask AddAsync(SecondCachingEntity entity, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask AddRangeAsync(IEnumerable<SecondCachingEntity> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<bool> UpdateAsync(SecondCachingEntity entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask<bool> RemoveAsync(SecondCachingEntity entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask RemoveRangeAsync(IEnumerable<SecondCachingEntity> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<SecondCachingEntity?> FindAsync(object key, CancellationToken cancellationToken = default) => new((SecondCachingEntity?)null);
		public object? GetEntityKey(SecondCachingEntity entity) => (object?)entity.Id;
	}

	public class CachingTestEntity {
		[Key]
		public string Id { get; set; } = Guid.NewGuid().ToString();
	}

	public class CachingTestRepository : IRepository<CachingTestEntity> {
		public ValueTask AddAsync(CachingTestEntity entity, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask AddRangeAsync(IEnumerable<CachingTestEntity> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<bool> UpdateAsync(CachingTestEntity entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask<bool> RemoveAsync(CachingTestEntity entity, CancellationToken cancellationToken = default) => new(false);
		public ValueTask RemoveRangeAsync(IEnumerable<CachingTestEntity> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
		public ValueTask<CachingTestEntity?> FindAsync(object key, CancellationToken cancellationToken = default) => new((CachingTestEntity?)null);
		public object? GetEntityKey(CachingTestEntity entity) => (object?)entity.Id;
	}
}
