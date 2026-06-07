using Microsoft.Extensions.DependencyInjection;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Core")]
[Trait("Feature", "DependencyInjection")]
public class RepositoryBuilderSeedExtensionsTests {
    [Fact]
    public void WithSeedData_ProviderType_RegistersService() {
        var services = new ServiceCollection();
        var builder = new RepositoryContextBuilder(services);
        var repoBuilder = builder.AddRepository<TestRepo>();

        var result = repoBuilder.WithSeedData<TestSeedProvider>();

        Assert.Same(repoBuilder, result);
        var provider = services.BuildServiceProvider();
        var resolved = provider.GetService<IRepositorySeedDataProvider<Person>>();
        Assert.IsType<TestSeedProvider>(resolved);
    }

    [Fact]
    public void WithSeedData_InlineData_RegistersSingletonCollection() {
        var services = new ServiceCollection();
        var builder = new RepositoryContextBuilder(services);
        var repoBuilder = builder.AddRepository<TestRepo>();
        var data = new[] { new PersonFaker().Generate(), new PersonFaker().Generate() };

        var result = repoBuilder.WithSeedData(data);

        Assert.Same(repoBuilder, result);
        var provider = services.BuildServiceProvider();
        var resolved = provider.GetService<IRepositorySeedDataProvider<Person>>();
        Assert.NotNull(resolved);
    }

    public class TestRepo : IRepository<Person> {
        public IServiceProvider? Services => null;
        object? IRepository<Person, object>.GetEntityKey(Person entity) => entity.Id;
        public ValueTask<Person?> FindAsync(object key, CancellationToken cancellationToken = default) => ValueTask.FromResult<Person?>(null);
        public ValueTask<bool> ExistsAsync(object key, CancellationToken cancellationToken = default) => ValueTask.FromResult(false);
        public ValueTask AddAsync(Person entity, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask AddRangeAsync(IEnumerable<Person> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask<bool> UpdateAsync(Person entity, CancellationToken cancellationToken = default) => ValueTask.FromResult(true);
        public ValueTask<bool> RemoveAsync(Person entity, CancellationToken cancellationToken = default) => ValueTask.FromResult(true);
        public ValueTask RemoveRangeAsync(IEnumerable<Person> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask<PageResult<Person>> GetPageAsync(PageRequest request, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new PageResult<Person>(request, 0));
    }

    private sealed class TestSeedProvider : IRepositorySeedDataProvider<Person> {
        public IEnumerable<Person> GetSeedData() => Enumerable.Empty<Person>();
        IEnumerable<object> IRepositorySeedDataProvider.GetSeedData() => Enumerable.Empty<object>();
    }
}
