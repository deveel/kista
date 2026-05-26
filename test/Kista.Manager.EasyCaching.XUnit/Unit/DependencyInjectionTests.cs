using Kista.Caching;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
[Trait("Feature", "Caching")]
public class DependencyInjectionTests {
    #region EntityCacheOptions

    [Fact]
    public void Should_ResolveConfiguredOptions_When_OptionsConfiguredByAction() {
        // Arrange
        var services = new ServiceCollection();
        services.AddEntityCacheOptions<Person>(options => {
            options.Expiration = TimeSpan.FromMinutes(15);
        });

        // Act
        var provider = services.BuildServiceProvider();
        var options = provider.GetService<IOptions<EntityCacheOptions<Person>>>();

        // Assert
        Assert.NotNull(options);
        Assert.NotNull(options.Value);
        Assert.Equal(TimeSpan.FromMinutes(15), options.Value.Expiration);
    }

    [Fact]
    public void Should_ResolveConfiguredOptions_When_OptionsBindFromConfiguration() {
        // Arrange
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                { "EntityCacheOptions:Person:Expiration", "00:15:00" }
            });
        services.AddSingleton<IConfiguration>(config.Build());
        services.AddEntityCacheOptions<Person>("EntityCacheOptions:Person");

        // Act
        var provider = services.BuildServiceProvider();
        var options = provider.GetService<IOptions<EntityCacheOptions<Person>>>();

        // Assert
        Assert.NotNull(options);
        Assert.NotNull(options.Value);
        Assert.Equal(TimeSpan.FromMinutes(15), options.Value.Expiration);
    }

    #endregion

    #region EntityCache

    [Fact]
    public void Should_ResolveEntityCache_When_InMemoryEasyCacheRegistered() {
        // Arrange
        var services = new ServiceCollection();
        services.AddEasyCaching(options => options.UseInMemory("default"));
        services.AddEntityEasyCacheFor<Person>(options => {
            options.Expiration = TimeSpan.FromMinutes(15);
        });

        // Act
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<IEntityCache<Person>>());
        Assert.NotNull(provider.GetService<EntityEasyCache<Person>>());
    }

    [Fact]
    public void Should_ThrowArgumentException_When_RegisteringNonCacheType() {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => services.AddEntityEasyCache<NotCache>());
    }

    #endregion

    #region CacheKeyGenerator

    [Fact]
    public void Should_ResolveKeyGenerator_When_EntityCacheKeyGeneratorRegistered() {
        // Arrange
        var services = new ServiceCollection();
        services.AddEntityCacheKeyGenerator<PersonCacheKeyGenerator>();

        // Act
        var provider = services.BuildServiceProvider();
        var generator = provider.GetService<IEntityCacheKeyGenerator<Person>>();

        // Assert
        Assert.NotNull(generator);
        Assert.IsType<PersonCacheKeyGenerator>(generator);
    }

    [Fact]
    public void Should_ResolveConverter_When_EntityEasyCacheConverterRegistered() {
        // Arrange
        var services = new ServiceCollection();
        services.AddEntityEasyCacheConverter<PersonCacheConverter>();

        // Act
        var provider = services.BuildServiceProvider();
        var converter = provider.GetService<IEntityEasyCacheConverter<Person, CachedPerson>>();

        // Assert
        Assert.NotNull(converter);
        Assert.IsType<PersonCacheConverter>(converter);
    }

    #endregion

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
