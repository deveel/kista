using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

namespace Kista;

/// <summary>
/// Tests for automatic repository registration via <see cref="ServiceCollectionExtensions"/>,
/// verifying that custom repository interfaces and their implementations are resolved
/// correctly from the dependency injection container.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Core")]
[Trait("Feature", "DependencyInjection")]
public class DependencyInjectionTests
{
    #region AddRepository

    [Fact]
    public void Should_ResolveAllContracts_When_RepositoryRegisteredFromCustomContract()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRepository<MyPersonRepository>();

        // Act
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<IPersonRepository>());
        Assert.NotNull(provider.GetService<IRepository<Person>>());
        Assert.NotNull(provider.GetService<MyRepository<Person>>());
        Assert.NotNull(provider.GetService<MyPersonRepository>());
    }

    #endregion
}

/// <summary>
/// A custom repository interface that extends <see cref="IRepository{Person}"/>
/// with a name-based lookup method, used to verify multi-contract resolution.
/// </summary>
interface IPersonRepository : IRepository<Person>
{
    ValueTask<Person?> FindByNameAsync(string name, CancellationToken cancellationToken = default);
}

/// <summary>
/// A base repository implementation that delegates all
/// <see cref="IRepository{TEntity}"/> operations to an in-memory list,
/// used to test generic repository registration.
/// </summary>
class MyRepository<TEntity> : IRepository<TEntity>
    where TEntity : class
{
    protected IRepository<TEntity> Repository { get; }

    public MyRepository()
    {
        Repository = new List<TEntity>(200).AsRepository();
    }

    public ValueTask AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        Repository.AddAsync(entity, cancellationToken);

    public ValueTask AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) =>
        Repository.AddRangeAsync(entities, cancellationToken);

    public ValueTask<TEntity?> FindAsync(object key, CancellationToken cancellationToken = default) =>
        Repository.FindAsync(key, cancellationToken);

    public object? GetEntityKey(TEntity entity) =>
        Repository.GetEntityKey(entity);

    public ValueTask<bool> RemoveAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        Repository.RemoveAsync(entity, cancellationToken);

    public ValueTask RemoveRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) =>
        Repository.RemoveRangeAsync(entities, cancellationToken);

    public ValueTask<bool> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        Repository.UpdateAsync(entity, cancellationToken);

    public ValueTask<PageResult<TEntity>> GetPageAsync(PageRequest request, CancellationToken cancellationToken = default) =>
        Repository.GetPageAsync(request, cancellationToken);
}

/// <summary>
/// A concrete repository for <see cref="Person"/> that implements both
/// <see cref="MyRepository{Person}"/> and <see cref="IPersonRepository"/>,
/// used to verify that the scanner registers all implemented interfaces.
/// </summary>
class MyPersonRepository : MyRepository<Person>, IPersonRepository
{
    public ValueTask<Person?> FindByNameAsync(string name, CancellationToken cancellationToken = default) =>
        Repository.FindFirstAsync(x => x.FirstName == name, cancellationToken);
}
