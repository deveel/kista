using Bogus;

using Deveel;

using Microsoft.Extensions.DependencyInjection;

using MongoDB.Bson;
using MongoDB.Driver;

using MongoFramework;

namespace Kista {
	[Collection(nameof(MongoSingleDatabaseCollection))]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "SoftDelete")]
	public class MongoSoftDeleteTests : SoftDeleteRepositoryTestSuite<SoftDeletableMongoPerson, ObjectId> {
		public MongoSoftDeleteTests(MongoSingleDatabase mongo, ITestOutputHelper outputHelper) : base(outputHelper) {
			ConnectionString = mongo.ConnectionString;
		}

		protected string ConnectionString { get; }

		protected override Faker<SoftDeletableMongoPerson> PersonFaker { get; } = new SoftDeletableMongoPersonFaker();

		protected override ObjectId GeneratePersonId() => ObjectId.GenerateNewId();

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

	[Collection(nameof(MongoSingleDatabaseCollection))]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "SoftDelete")]
	public class MongoSoftDeleteRestoreTests : SoftDeleteRepositoryTestSuite<SoftDeletableMongoPerson, ObjectId> {
		public MongoSoftDeleteRestoreTests(MongoSingleDatabase mongo, ITestOutputHelper outputHelper) : base(outputHelper) {
			ConnectionString = mongo.ConnectionString;
		}

		protected string ConnectionString { get; }

		protected override Faker<SoftDeletableMongoPerson> PersonFaker { get; } = new SoftDeletableMongoPersonFaker();

		protected override ObjectId GeneratePersonId() => ObjectId.GenerateNewId();

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

		[Fact]
		public async Task Should_RestoreEntity_ThroughEntityManager_BringsEntityBackToQueries() {
			var person = await RandomPersonAsync();
			var personId = Repository.GetEntityKey(person)!;

			var services = new ServiceCollection();
			services.AddSingleton<IUserAccessor<string>>(new StaticUserAccessor("user-42"));
			services.AddLogging();
			var provider = services.BuildServiceProvider();

			var manager = new EntityManager<SoftDeletableMongoPerson, ObjectId>(Repository, services: provider);

			var removed = await manager.RemoveAsync(person, TestContext.Current.CancellationToken);
			Assert.True(removed.IsSuccess());

			var foundDefault = await Repository.FindAsync(personId, TestContext.Current.CancellationToken);
			Assert.Null(foundDefault);

			await manager.RestoreAsync(person, TestContext.Current.CancellationToken);

			// Re-fetch from the repository to verify the restored state is visible
			// to default-mode queries (the manager restores a fresh copy from the
			// repository, not the passed-in entity reference).
			var foundRestored = await Repository.FindAsync(personId, TestContext.Current.CancellationToken);
			Assert.NotNull(foundRestored);
			Assert.False(foundRestored!.IsDeleted);
			Assert.Null(foundRestored.DeletedAtUtc);
			Assert.Null(foundRestored.DeletedBy);
		}

		private sealed class StaticUserAccessor : IUserAccessor<string> {
			private readonly string _userId;

			public StaticUserAccessor(string userId) {
				_userId = userId;
			}

			public string? GetUserId() => _userId;
		}
	}
}