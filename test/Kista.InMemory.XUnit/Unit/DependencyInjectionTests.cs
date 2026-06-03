using Bogus;

using Microsoft.Extensions.DependencyInjection;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "DependencyInjection")]
public class DependencyInjectionTests {
    private static readonly Faker<Person> PersonFaker = new Faker<Person>("en")
        .RuleFor(x => x.Id, f => f.Random.Guid().ToString())
        .RuleFor(x => x.FirstName, f => f.Name.FirstName())
        .RuleFor(x => x.LastName, f => f.Name.LastName())
        .RuleFor(x => x.DateOfBirth, f => f.Date.Past(20))
        .RuleFor(x => x.Email, f => f.Internet.Email().OrNull(f))
        .RuleFor(x => x.PhoneNumber, f => f.Phone.PhoneNumber().OrNull(f));

    [Fact]
    public void Should_CreateRepository_When_SourceIsProvided() {
        // Arrange
        var items = PersonFaker.Generate(34);

        // Act
        var repository = new InMemoryRepository<Person>(items);

        // Assert
        Assert.NotNull(repository);
        Assert.Equal(34, repository.Entities.Count);
    }

    [Fact]
    public void Should_ThrowRepositoryException_When_ItemHasNoId() {
        // Arrange
        var faker = new Faker<Person>("en")
            .RuleFor(x => x.Id, f => f.Random.Guid().ToString().OrNull(f))
            .RuleFor(x => x.FirstName, f => f.Name.FirstName())
            .RuleFor(x => x.LastName, f => f.Name.LastName());
        var items = faker.Generate(34);

        // Act & Assert
        Assert.Throws<RepositoryException>(() => new InMemoryRepository<Person>(items));
    }

    [Fact]
    public void Should_ResolveAllRepositoryInterfaces_When_DefaultInMemoryRepositoryRegistered() {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddRepository<InMemoryRepository<Person>>();
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<InMemoryRepository<Person>>());
        Assert.NotNull(provider.GetService<InMemoryRepository<Person, string>>());
        Assert.NotNull(provider.GetService<IRepository<Person>>());
        Assert.NotNull(provider.GetService<IRepository<Person, string>>());
        Assert.NotNull(provider.GetService<IPageableRepository<Person>>());
        Assert.NotNull(provider.GetService<IPageableRepository<Person, string>>());
        Assert.NotNull(provider.GetService<IFilterableRepository<Person>>());
        Assert.NotNull(provider.GetService<IFilterableRepository<Person, string>>());
    }

    [Fact]
    public void Should_ResolveCustomRepositoryType_When_CustomRepositoryRegistered() {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddRepository<PersonRepository>();
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<PersonRepository>());
        Assert.NotNull(provider.GetService<IRepository<Person>>());
        Assert.IsType<PersonRepository>(provider.GetService<IRepository<Person>>());
        Assert.NotNull(provider.GetService<IPageableRepository<Person>>());
        Assert.IsType<PersonRepository>(provider.GetService<IPageableRepository<Person>>());
        Assert.NotNull(provider.GetService<IFilterableRepository<Person>>());
        Assert.IsType<PersonRepository>(provider.GetService<IFilterableRepository<Person>>());
    }
}
