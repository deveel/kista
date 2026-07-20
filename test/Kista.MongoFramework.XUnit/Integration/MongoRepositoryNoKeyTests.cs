using Bogus;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoFramework;

namespace Kista;

[Trait("Category", "Integration")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "MongoRepository")]
public class MongoRepositoryNoKeyTests : MongoRepositoryNoKeyTestSuite<MongoPerson> {
	public MongoRepositoryNoKeyTests(MongoSingleDatabase mongo, ITestOutputHelper outputHelper) : base(mongo, outputHelper) {
	}

	protected override Faker<MongoPerson> PersonFaker { get; } = new MongoPersonFaker();

	protected override void ConfigureServices(IServiceCollection services) {
		AddRepository(services);
		base.ConfigureServices(services);
	}

	protected virtual void AddRepository(IServiceCollection services) {
		services.AddRepositoryContext()
			.UseMongoDB<MongoDbContext>(mongoBuilder => mongoBuilder.WithConnection(builder => builder.UseConnection(ConnectionString)))
			.ConfigureLifecycle()
			.AddRepository<TestMongoPersonNoKeyRepository>();
	}

	protected async Task<MongoPerson?> FindPerson(ObjectId id) {
		var collection = MongoCollection;
		var result = await collection.FindAsync(x => x.Id == id);
		return await result.FirstOrDefaultAsync();
	}

	protected override async ValueTask DisposeAsync() {
		await MongoCollection.DeleteManyAsync(x => true);
		await base.DisposeAsync();
	}

	[Fact]
	public async Task Should_ReturnSortedPage_When_OrderedByFieldName() {
		// Arrange
		var cancellationToken = TestContext.Current.CancellationToken;
		var totalPages = (int)Math.Ceiling((double)PeopleCount / 10);
		var sorted = People!.OrderBy(x => x.FirstName).Take(10).ToList();
		var request = new PageQuery<MongoPerson>(1, 10).OrderBy("FirstName");

		// Act
		var result = await Repository.GetPageAsync(request, cancellationToken);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(totalPages, result.TotalPages);
		Assert.Equal(PeopleCount, result.TotalItems);
		Assert.NotNull(result.Items);
		Assert.Equal(10, result.Items.Count);
		for (int i = 0; i < sorted.Count; i++) {
			Assert.Equal(sorted[i].FirstName, result.Items.ElementAt(i).FirstName);
		}
	}

	[Fact]
	public async Task Should_GetEntityKey_ViaInterface() {
		// Arrange
		var person = await RandomPersonAsync();
		var keyedRepo = (IRepository<MongoPerson, object>)Repository;

		// Act
		var key = keyedRepo.GetEntityKey(person);

		// Assert
		Assert.NotNull(key);
	}
}
