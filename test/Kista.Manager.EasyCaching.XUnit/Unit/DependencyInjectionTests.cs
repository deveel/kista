using Kista.Caching;

using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
[Trait("Feature", "Caching")]
public class DependencyInjectionTests : EntityCacheDependencyInjectionTestBase {
	[Fact]
	public void Should_ResolveEntityCache_When_InMemoryEasyCacheRegistered() {
		// Arrange
		var services = new ServiceCollection();
		services.AddEasyCaching(options => options.UseInMemory("default"));
		services.AddRepositoryContext()
			.AddRepository<InMemoryRepository<Person, string>>(repo => repo
				.WithManagement(mgmt => mgmt.WithEasyCaching(options => {
					options.DefaultExpiration = TimeSpan.FromMinutes(15);
				})));

		// Act
		var provider = services.BuildServiceProvider();

		// Assert
		Assert.NotNull(provider.GetService<IEntityCache<Person>>());
		Assert.NotNull(provider.GetService<EntityEasyCache<Person>>());
	}

	[Fact]
	public void Should_NotRegisterCache_When_NoRepositoryForEntityType() {
		// Arrange
		var services = new ServiceCollection();
		services.AddEasyCaching(options => options.UseInMemory("default"));
		services.AddRepositoryContext()
			.WithEasyCaching();

		// Act
		var provider = services.BuildServiceProvider();

		// Assert
		Assert.Null(provider.GetService<IEntityCache<Person>>());
	}

	[Fact]
	public void Should_ResolveConverter_When_EntityEasyCacheConverterRegistered() {
		// Arrange
		var services = new ServiceCollection();
		services.TryAdd(new ServiceDescriptor(typeof(IEntityEasyCacheConverter<Person, CachedPerson>), typeof(PersonCacheConverter), ServiceLifetime.Singleton));
		services.Add(new ServiceDescriptor(typeof(PersonCacheConverter), typeof(PersonCacheConverter), ServiceLifetime.Singleton));

		// Act
		var provider = services.BuildServiceProvider();
		var converter = provider.GetService<IEntityEasyCacheConverter<Person, CachedPerson>>();

		// Assert
		Assert.NotNull(converter);
		Assert.IsType<PersonCacheConverter>(converter);
	}

	#region Support Types

	private sealed class PersonCacheConverter : IEntityEasyCacheConverter<Person, CachedPerson> {
		public Person ConvertFromCached(CachedPerson cached) => new Person {
			Id = cached.Id,
			FirstName = cached.FirstName,
			LastName = cached.LastName,
			DateOfBirth = cached.DateOfBirth,
			Email = cached.Email,
			PhoneNumber = cached.PhoneNumber
		};

		public CachedPerson ConvertToCached(Person entity) => new CachedPerson {
			Id = entity.Id,
			FirstName = entity.FirstName,
			LastName = entity.LastName,
			DateOfBirth = entity.DateOfBirth,
			Email = entity.Email,
			PhoneNumber = entity.PhoneNumber
		};
	}

	private sealed class CachedPerson : Person { }

	private sealed class NotCache { }

	#endregion
}