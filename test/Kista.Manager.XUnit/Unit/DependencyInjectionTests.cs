// Async method lacks 'await' operators and will run synchronously

using System.Runtime.CompilerServices;

using Microsoft.Extensions.Logging;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
[Trait("Feature", "DependencyInjection")]
public class DependencyInjectionTests {
    #region AddManager

    [Fact]
    public void Should_ResolveDefaultManager_When_ManagerForEntityRegistered() {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddRepositoryContext()
            .AddRepository<InMemoryRepository<Person>>(_ => { })
            .WithManagement();
        var provider = services.BuildServiceProvider();
        var manager = provider.GetRequiredService<EntityManager<Person>>();

        // Assert
        Assert.NotNull(manager);
        Assert.True(manager.SupportsPaging);
    }

    [Fact]
    public void Should_ResolveCustomManager_When_EntityManagerRegistered() {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddRepositoryContext()
            .AddRepository<InMemoryRepository<Person>>()
            .WithManagement(mgmt => mgmt
                .UsingManager<MyPersonManager>());
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<EntityManager<Person>>());
        Assert.NotNull(provider.GetService<MyPersonManager>());
        var manager = provider.GetRequiredService<EntityManager<Person>>();
        Assert.True(manager.SupportsPaging);
    }

    [Fact]
    public void Should_ThrowArgumentException_When_RegisteringNonManagerType() {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => services.AddRepositoryContext()
            .AddRepository<InMemoryRepository<Person>>()
            .WithManagement(mgmt => mgmt.UsingManager<NotEntityManager>()));
    }

    #endregion

    #region AddValidator

    [Fact]
    public void Should_ResolveValidator_When_EntityValidatorRegistered() {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddRepositoryContext()
            .AddRepository<InMemoryRepository<Person>>()
            .WithManagement(mgmt => mgmt
                .WithValidator<PersonValidator>());
        var provider = services.BuildServiceProvider();

        // Assert
        var validator = provider.GetRequiredService<IEntityValidator<Person>>();
        Assert.NotNull(validator);
    }

    #endregion

    #region AddCancellationSource

    [Fact]
    public void Should_ResolveCancellationSource_When_OperationTokenSourceRegistered() {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddOperationTokenSource<TestCancellationTokenSource>();
        var provider = services.BuildServiceProvider();

        // Assert
        var source = provider.GetService<IOperationCancellationSource>();
        Assert.NotNull(source);
        Assert.IsType<TestCancellationTokenSource>(source);
    }

    [Fact]
    public void Should_ResolveHttpRequestCancellationSource_When_HttpRequestTokenSourceRegistered() {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddHttpRequestTokenSource();
        var provider = services.BuildServiceProvider();

        // Assert
        var source = provider.GetService<IOperationCancellationSource>();
        Assert.NotNull(source);
        Assert.IsType<HttpRequestCancellationSource>(source);
        Assert.Equal(CancellationToken.None, source.Token);
    }

    #endregion

    #region AddErrorFactory

    [Fact]
    public void Should_ResolveDefaultErrorFactory_When_DefaultFactoryRegistered() {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddOperationErrorFactory<Person, OperationErrorFactory>();
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<IOperationErrorFactory<Person>>());
        Assert.NotNull(provider.GetService<OperationErrorFactory>());
    }

    [Fact]
    public void Should_ResolveCustomErrorFactory_When_CustomFactoryRegistered() {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddOperationErrorFactory<Person, PersonErrorFactory>();
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<IOperationErrorFactory<Person>>());
        Assert.NotNull(provider.GetService<PersonErrorFactory>());
        Assert.Null(provider.GetService<OperationErrorFactory>());
    }

    #endregion

    #region Support Types

    private sealed class NotEntityManager { }

    private sealed class PersonValidator : IEntityValidator<Person> {
#pragma warning disable CS1998
        public async IAsyncEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> ValidateAsync(
            EntityManager<Person> manager,
            Person entity,
            [EnumeratorCancellation] CancellationToken cancellationToken = default) {
            yield return new System.ComponentModel.DataAnnotations.ValidationResult("Test error");
        }
#pragma warning restore CS1998
    }

    #endregion
}
