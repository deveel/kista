using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using MongoFramework;

namespace Kista;

/// <summary>
/// Integration tests for <see cref="MongoRepositoryLifecycleHandler{TEntity}"/>
/// using the Testcontainers MongoDB fixture to exercise the real
/// <c>ExistsAsync</c>/<c>CreateAsync</c>/<c>DropAsync</c>/<c>SeedEntitiesAsync</c>
/// paths and their exception-wrapping branches.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "MongoRepositoryLifecycle")]
[Collection(nameof(MongoSingleDatabaseCollection))]
public class MongoRepositoryLifecycleHandlerTests {
    private readonly MongoSingleDatabase mongo;

    public MongoRepositoryLifecycleHandlerTests(MongoSingleDatabase mongo) {
        this.mongo = mongo;
    }

    /// <summary>
    /// Builds a connection string pointing at a dedicated database
    /// (<c>lifecycle_db</c>) so these tests don't conflict with other
    /// integration tests sharing the same MongoDB container.
    /// </summary>
    private string LifecycleConnectionString {
        get {
            var url = new MongoUrl(mongo.ConnectionString);
            return new MongoUrlBuilder(mongo.ConnectionString) {
                DatabaseName = "lifecycle_db"
            }.ToString();
        }
    }

    private IMongoDbContext CreateContext() {
        var services = new ServiceCollection();
        services.AddRepositoryContext().UseMongoDB<MongoDbContext>(mongoBuilder => mongoBuilder.WithConnection(builder => builder.UseConnection(LifecycleConnectionString)));
        return services.BuildServiceProvider().GetRequiredService<IMongoDbContext>();
    }

    private IMongoCollection<MongoPerson> GetPeopleCollection() {
        var url = new MongoUrl(LifecycleConnectionString);
        return new MongoClient(LifecycleConnectionString).GetDatabase(url.DatabaseName)
            .GetCollection<MongoPerson>("persons");
    }

    [Fact]
    public async Task ExistsAsync_ReturnsFalse_When_CollectionDoesNotExist() {
        var ctx = CreateContext();
        var handler = new MongoRepositoryLifecycleHandler<MongoPerson>(ctx);

        await ctx.Connection.GetDatabase().DropCollectionAsync("persons", TestContext.Current.CancellationToken);

        var exists = await handler.ExistsAsync(TestContext.Current.CancellationToken);
        Assert.False(exists);
    }

    [Fact]
    public async Task CreateAsync_CreatesCollection_When_NotExists() {
        var ctx = CreateContext();
        var handler = new MongoRepositoryLifecycleHandler<MongoPerson>(ctx);

        await ctx.Connection.GetDatabase().DropCollectionAsync("persons", TestContext.Current.CancellationToken);
        await handler.CreateAsync(TestContext.Current.CancellationToken);

        var exists = await handler.ExistsAsync(TestContext.Current.CancellationToken);
        Assert.True(exists);
    }

    [Fact]
    public async Task DropAsync_DropsCollection_When_Exists() {
        var ctx = CreateContext();
        var handler = new MongoRepositoryLifecycleHandler<MongoPerson>(ctx);

        await ctx.Connection.GetDatabase().DropCollectionAsync("persons", TestContext.Current.CancellationToken);
        await handler.CreateAsync(TestContext.Current.CancellationToken);
        Assert.True(await handler.ExistsAsync(TestContext.Current.CancellationToken));

        await handler.DropAsync(TestContext.Current.CancellationToken);
        Assert.False(await handler.ExistsAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SeedAsync_InsertsEntitiesIntoCollection() {
        var ctx = CreateContext();
        var handler = new MongoRepositoryLifecycleHandler<MongoPerson>(ctx);

        await ctx.Connection.GetDatabase().DropCollectionAsync("persons", TestContext.Current.CancellationToken);
        await handler.CreateAsync(TestContext.Current.CancellationToken);

        var faker = new MongoPersonFaker();
        var people = faker.Generate(3);
        await handler.SeedAsync(people, TestContext.Current.CancellationToken);

        var count = await GetPeopleCollection().CountDocumentsAsync(_ => true, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task SeedAsync_WithSingleEntity_InsertsIt() {
        var ctx = CreateContext();
        var handler = new MongoRepositoryLifecycleHandler<MongoPerson>(ctx);

        await ctx.Connection.GetDatabase().DropCollectionAsync("persons", TestContext.Current.CancellationToken);
        await handler.CreateAsync(TestContext.Current.CancellationToken);

        var person = new MongoPersonFaker().Generate();
        await handler.SeedAsync(person, TestContext.Current.CancellationToken);

        var count = await GetPeopleCollection().CountDocumentsAsync(_ => true, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task SeedAsync_WithObjectEnumerable_FiltersAndSeeds() {
        var ctx = CreateContext();
        var handler = new MongoRepositoryLifecycleHandler<MongoPerson>(ctx);

        await ctx.Connection.GetDatabase().DropCollectionAsync("persons", TestContext.Current.CancellationToken);
        await handler.CreateAsync(TestContext.Current.CancellationToken);

        var person = new MongoPersonFaker().Generate();
        await handler.SeedAsync(new object[] { person, new object() }, TestContext.Current.CancellationToken);

        var count = await GetPeopleCollection().CountDocumentsAsync(_ => true, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task SeedAsync_WithNullSeedData_DoesNothing() {
        var ctx = CreateContext();
        var handler = new MongoRepositoryLifecycleHandler<MongoPerson>(ctx);

        await ctx.Connection.GetDatabase().DropCollectionAsync("persons", TestContext.Current.CancellationToken);
        await handler.CreateAsync(TestContext.Current.CancellationToken);

        await handler.SeedAsync(null, TestContext.Current.CancellationToken);
        await handler.SeedAsync(new object(), TestContext.Current.CancellationToken);

        var count = await GetPeopleCollection().CountDocumentsAsync(_ => true, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(0, count);
    }
}