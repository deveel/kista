using Bogus;

using Microsoft.Extensions.DependencyInjection;

using MongoDB.Bson;
using MongoDB.Driver;

using MongoFramework;

namespace Kista {
	[Collection(nameof(MongoSingleDatabaseCollection))]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "SoftDelete")]
	public class MongoSoftDeleteTests : SoftDeleteRepositoryTestSuite<SoftDeletableMongoPerson, ObjectId, MongoPersonRelationship> {
		private readonly MongoSingleDatabase mongo;

		public MongoSoftDeleteTests(MongoSingleDatabase mongo, ITestOutputHelper outputHelper) : base(outputHelper) {
			this.mongo = mongo;
			ConnectionString = mongo.ConnectionString;
		}

		protected string ConnectionString { get; }

		protected override Faker<SoftDeletableMongoPerson> PersonFaker { get; } = new SoftDeletableMongoPersonFaker();

		protected override Faker<MongoPersonRelationship> RelationshipFaker => new MongoPersonRelationshipFaker();

		protected override ObjectId GeneratePersonId() => ObjectId.GenerateNewId();

		protected override Task AddRelationshipAsync(SoftDeletableMongoPerson person, MongoPersonRelationship relationship) {
			if (person.Relationships == null)
				person.Relationships = new List<MongoPersonRelationship>();

			person.Relationships.Add(relationship);

			return Task.CompletedTask;
		}

		protected override Task RemoveRelationshipAsync(SoftDeletableMongoPerson person, MongoPersonRelationship relationship) {
			if (person.Relationships != null)
				person.Relationships.Remove(relationship);

			return Task.CompletedTask;
		}

		protected IMongoCollection<SoftDeletableMongoPerson> MongoCollection => new MongoClient(ConnectionString)
			.GetDatabase(new MongoUrl(ConnectionString).DatabaseName)
			.GetCollection<SoftDeletableMongoPerson>("soft_deletable_persons");

		protected override void ConfigureServices(IServiceCollection services) {
			services
				.AddMongoDbContext<MongoDbContext>(builder => builder.UseConnection(ConnectionString))
				.AddRepositoryController();
			services.AddRepository<TestSoftDeletableMongoPersonRepository>();

			base.ConfigureServices(services);
		}

		protected override async ValueTask InitializeAsync() {
			var controller = Services.GetRequiredService<IRepositoryController>();
			await controller.CreateRepositoryAsync<SoftDeletableMongoPerson, ObjectId>();

			await base.InitializeAsync();
		}

		protected override async ValueTask DisposeAsync() {
			await MongoCollection.DeleteManyAsync(x => true);
			await base.DisposeAsync();
		}

		protected override IEnumerable<SoftDeletableMongoPerson> NaturalOrder(IEnumerable<SoftDeletableMongoPerson> source) => source.OrderBy(x => x.Id);
	}
}