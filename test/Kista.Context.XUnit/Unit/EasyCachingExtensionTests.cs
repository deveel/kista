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
			.AddRepository<CachingTestRepository>(_ => { })
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
			.AddRepository<CachingTestRepository>(_ => { })
			.WithEasyCaching(opts => opts.DefaultExpiration = TimeSpan.FromMinutes(30));

		var optionsDescriptor = services.FirstOrDefault(d =>
			d.ServiceType == typeof(IConfigureOptions<EntityCacheOptions<CachingTestEntity>>));
		Assert.NotNull(optionsDescriptor);
	}

	[Fact]
	public void WithEasyCaching_WithCacheKeyPrefix_RegistersOptions() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.AddRepository<CachingTestRepository>(_ => { })
			.WithEasyCaching(opts => opts.CacheKeyPrefix = "myapp:");

		var optionsDescriptor = services.FirstOrDefault(d =>
			d.ServiceType == typeof(IConfigureOptions<EntityCacheOptions<CachingTestEntity>>));
		Assert.NotNull(optionsDescriptor);
	}

	[Fact]
	public void WithEasyCaching_WithOptionsAppliesToAllEntityTypes() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.AddRepository<CachingTestRepository>(_ => { })
			.AddRepository<SecondCachingRepository>(_ => { })
			.WithEasyCaching(opts => opts.DefaultExpiration = TimeSpan.FromHours(1));

		var optionsForFirst = services.FirstOrDefault(d =>
			d.ServiceType == typeof(IConfigureOptions<EntityCacheOptions<CachingTestEntity>>));
		var optionsForSecond = services.FirstOrDefault(d =>
			d.ServiceType == typeof(IConfigureOptions<EntityCacheOptions<SecondCachingEntity>>));

		Assert.NotNull(optionsForFirst);
		Assert.NotNull(optionsForSecond);
	}

}
