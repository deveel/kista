using Bogus;

using Microsoft.Extensions.DependencyInjection;

using MongoDB.Bson;
using MongoDB.Driver;

namespace Kista {
	[Collection(nameof(MongoSingleDatabaseCollection))]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "MongoRepository")]
	public abstract class MongoRepositoryNoKeyTestSuite<TPerson> : NoKeyRepositoryTestSuite<TPerson, MongoPersonRelationship>
		where TPerson : MongoPerson {
		private readonly MongoSingleDatabase mongo;

		protected MongoRepositoryNoKeyTestSuite(MongoSingleDatabase mongo, ITestOutputHelper? testOutput) : base(testOutput) {
			this.mongo = mongo;
			ConnectionString = mongo.ConnectionString;
		}

		protected string ConnectionString { get; }

		protected override string GeneratePersonId() => ObjectId.GenerateNewId().ToString();

		protected override Faker<MongoPersonRelationship> RelationshipFaker => new MongoPersonRelationshipFaker();

		protected IMongoCollection<TPerson> MongoCollection => new MongoClient(mongo.ConnectionString)
			.GetDatabase(new MongoUrl(ConnectionString).DatabaseName)
			.GetCollection<TPerson>("persons");

		protected override Task AddRelationshipAsync(TPerson person, MongoPersonRelationship relationship) {
			if (person.Relationships == null)
				person.Relationships = new List<MongoPersonRelationship>();

			person.Relationships.Add(relationship);

			return Task.CompletedTask;
		}

		protected override Task RemoveRelationshipAsync(TPerson person, MongoPersonRelationship relationship) {
			if (person.Relationships != null)
				person.Relationships.Remove(relationship);

			return Task.CompletedTask;
		}

		protected override async ValueTask InitializeAsync() {
			var controller = Services.GetRequiredService<IRepositoryLifecycleService>();
			await controller.CreateRepositoryAsync<MongoPerson>();

			await base.InitializeAsync();
		}

		protected override async ValueTask DisposeAsync() {
			var controller = Services.GetRequiredService<IRepositoryLifecycleService>();
			await controller.DropRepositoryAsync<MongoPerson>();

			await base.DisposeAsync();
		}

		[Fact]
		public async Task FindByObjectId() {
			var person = await RandomPersonAsync();

			var found = await Repository.FindAsync(person.Id);

			Assert.NotNull(found);
			Assert.Equal(person.Id, found.Id);
		}

		[Fact]
		public async Task FindByObjectId_NotExisting() {
			var personId = ObjectId.GenerateNewId();

			var found = await Repository.FindAsync(personId);

			Assert.Null(found);
		}

		[Fact]
		public async Task FindFirstByGeoDistance() {
			var person = await RandomPersonAsync(x => x.Location != null);

			var point = new GeoPoint(person.Location!.Coordinates.Latitude, person.Location.Coordinates.Longitude);
			var found = await Repository.FindFirstByGeoDistanceAsync(x => x.Location!, point, 100);

			Assert.NotNull(found);
			Assert.Equal(person.Id, found.Id);
		}

		[Fact]
		public async Task FindAllByGeoDistance() {
			var person = await RandomPersonAsync(x => x.Location != null);

			var point = new GeoPoint(person.Location!.Coordinates.Latitude, person.Location.Coordinates.Longitude);
			var found = await Repository.FindAllByGeoDistanceAsync(x => x.Location!, point, 100);

			Assert.NotNull(found);
			Assert.NotEmpty(found);
			Assert.Contains(found, x => x.Id == person.Id);
		}
	}
}
