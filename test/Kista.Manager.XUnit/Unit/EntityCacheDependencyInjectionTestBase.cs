using Kista.Caching;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Kista {
	/// <summary>
	/// A base class that provides the dependency-injection test cases
	/// shared across all the entity-cache provider test suites
	/// (MemoryCache, DistributedCache, FusionCache, EasyCaching).
	/// </summary>
	/// <remarks>
	/// Derived classes are expected to add provider-specific test cases
	/// for the registration of their concrete <see cref="IEntityCache{TEntity}"/>
	/// implementation, while inheriting the common options and key-generator
	/// registration tests from this base class to avoid code duplication.
	/// </remarks>
	[Trait("Category", "Unit")]
	[Trait("Layer", "Application")]
	[Trait("Feature", "Caching")]
	public abstract class EntityCacheDependencyInjectionTestBase {
		[Fact]
		public void Should_ResolveConfiguredOptions_When_OptionsConfiguredByAction() {
			var services = new ServiceCollection();
			services.AddOptions<EntityCacheOptions<Person>>().Configure(options => {
				options.Expiration = TimeSpan.FromMinutes(15);
			});

			var provider = services.BuildServiceProvider();
			var options = provider.GetService<IOptions<EntityCacheOptions<Person>>>();

			Assert.NotNull(options);
			Assert.NotNull(options.Value);
			Assert.Equal(TimeSpan.FromMinutes(15), options.Value.Expiration);
		}

		[Fact]
		public void Should_ResolveConfiguredOptions_When_OptionsBindFromConfiguration() {
			var services = new ServiceCollection();
			var config = new ConfigurationBuilder()
				.AddInMemoryCollection(new Dictionary<string, string?> {
					{ "EntityCacheOptions:Person:Expiration", "00:15:00" }
				});
			services.AddSingleton<IConfiguration>(config.Build());
			services.AddOptions<EntityCacheOptions<Person>>().BindConfiguration("EntityCacheOptions:Person");

			var provider = services.BuildServiceProvider();
			var options = provider.GetService<IOptions<EntityCacheOptions<Person>>>();

			Assert.NotNull(options);
			Assert.NotNull(options.Value);
			Assert.Equal(TimeSpan.FromMinutes(15), options.Value.Expiration);
		}

		[Fact]
		public void Should_ResolveKeyGenerator_When_EntityCacheKeyGeneratorRegistered() {
			var services = new ServiceCollection();
			services.AddRepositoryContext()
				.AddRepository<InMemoryRepository<Person, string>>(repo => repo
					.WithManagement(mgmt => mgmt.WithCacheKeyGenerator<PersonCacheKeyGenerator>()));

			var provider = services.BuildServiceProvider();
			var generator = provider.GetService<IEntityCacheKeyGenerator<Person>>();

			Assert.NotNull(generator);
			Assert.IsType<PersonCacheKeyGenerator>(generator);
		}
	}
}