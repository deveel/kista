using Bogus;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoFramework;

namespace Kista;

[Trait("Category", "Integration")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "MongoRepository")]
public class MongoRepositoryTests : MongoRepositoryTestSuite<MongoPerson> {
	public MongoRepositoryTests(MongoSingleDatabase mongo, ITestOutputHelper outputHelper) : base(mongo, outputHelper) {
	}

	protected override Faker<MongoPerson> PersonFaker { get; } = new MongoPersonFaker();

	protected override void ConfigureServices(IServiceCollection services) {
		AddRepository(services);
		base.ConfigureServices(services);
	}

	protected virtual void AddRepository(IServiceCollection services) {
		services
			.AddMongoDbContext<MongoDbContext>(builder => builder.UseConnection(ConnectionString))
			.AddRepositoryController();
		services.AddRepository<MongoRepository<MongoPerson, ObjectId>>();
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
	public async Task Should_ReturnNull_When_FindByNonExistingId() {
		// Arrange
		var nonExistingId = ObjectId.GenerateNewId();

		// Act
		var result = await Repository.FindAsync(nonExistingId, TestContext.Current.CancellationToken);

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public async Task Should_AsQueryable_ReturnQueryable() {
		// Act
#pragma warning disable CS0618
		var queryable = ((MongoRepository<MongoPerson, ObjectId>)Repository).Queryable();
#pragma warning restore CS0618

		// Assert
		Assert.NotNull(queryable);
		Assert.NotEmpty(queryable);
	}

	[Fact]
	public async Task Should_GetEntityKey_When_PersonExists() {
		// Arrange
		var person = await RandomPersonAsync();
		var keyedRepo = (IRepository<MongoPerson, ObjectId>)Repository;

		// Act
		var key = keyedRepo.GetEntityKey(person);

		// Assert
		Assert.NotNull(key);
		Assert.Equal(person.Id, key);
	}
}
