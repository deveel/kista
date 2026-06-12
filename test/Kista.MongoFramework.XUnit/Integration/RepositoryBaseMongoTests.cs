using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoFramework;

namespace Kista;

/// <summary>
/// Integration tests for <see cref="Repository{TEntity,TKey}"/> when
/// hosted on top of the MongoFramework provider. The tests verify that the new
/// translation pipeline binds <see cref="IQuery"/> and
/// <see cref="PageQuery{TEntity}"/> parameters correctly through the protected
/// <c>Query()</c> hatch without breaking the underlying MongoDB provider's
/// behaviour.
/// </summary>
[Collection(nameof(MongoSingleDatabaseCollection))]
[Trait("Category", "Integration")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "Repository")]
public class RepositoryBaseMongoTests : IAsyncLifetime {
	private readonly MongoSingleDatabase mongo;
	private IServiceProvider Services = null!;
	private TestMongoRepository Repository = null!;
	private IList<MongoPerson> Seed = null!;

	public RepositoryBaseMongoTests(MongoSingleDatabase mongo) {
		this.mongo = mongo;
	}

	public async ValueTask InitializeAsync() {
		var services = new ServiceCollection();

		services.AddMongoDbContext<MongoDbContext>(builder => builder.UseConnection(mongo.ConnectionString));

		services.AddSingleton<TestMongoRepository>();

		Services = services.BuildServiceProvider();
		Repository = Services.GetRequiredService<TestMongoRepository>();

		Seed = new List<MongoPerson>();
		for (var i = 0; i < 12; i++) {
			Seed.Add(new MongoPerson {
				Id = ObjectId.GenerateNewId(),
				FirstName = i % 3 == 0 ? "Alice" : $"First{i}",
				LastName = $"Last{i:D2}",
			});
		}
		await Repository.AddRangeAsync(Seed);
	}

	public async ValueTask DisposeAsync() {
		var client = new MongoClient(mongo.ConnectionString);
		var db = client.GetDatabase(MongoSingleDatabase.DatabaseName);
		await db.DropCollectionAsync("persons");

		if (Services is IDisposable d) d.Dispose();
	}

	#region Query translation

	[Fact]
	public async Task GetPageAsync_AppliesFilterAndOrder() {
		var request = new PageQuery<MongoPerson>(1, 5)
			.Where(p => p.FirstName!.StartsWith("A"))
			.OrderBy(p => p.LastName);

		var result = await Repository.PublicQueryPageAsync(request, TestContext.Current.CancellationToken);

		Assert.NotNull(result);
		Assert.NotNull(result.Items);
		Assert.All(result.Items!, p => Assert.StartsWith("A", p.FirstName));

		for (var i = 1; i < result.Items!.Count; i++) {
			Assert.True(string.CompareOrdinal(result.Items[i - 1].LastName, result.Items[i].LastName) <= 0,
				$"Items at index {i - 1} and {i} are not in ascending LastName order.");
		}
	}

	[Fact]
	public async Task GetPageAsync_TotalCount_ReflectsFilter() {
		var aliceCount = Seed.Count(p => p.FirstName == "Alice");
		var request = new PageQuery<MongoPerson>(1, 100)
			.Where(p => p.FirstName == "Alice");

		var result = await Repository.PublicQueryPageAsync(request, TestContext.Current.CancellationToken);

		Assert.Equal(aliceCount, result.TotalItems);
	}

	#endregion

	#region FindAsync(IQuery)

	[Fact]
	public async Task FindAsync_Filter_ReturnsMatchingSet() {
		var query = Query.Where<MongoPerson>(p => p.FirstName == "Alice");
		var result = await Repository.PublicFindAsync(query, TestContext.Current.CancellationToken);

		Assert.NotEmpty(result);
		Assert.All(result, p => Assert.Equal("Alice", p.FirstName));
	}

	#endregion

	#region Tracking-neutral

	[Fact]
	public async Task GetPageAsync_TrackingNeutral() {
		var result = await Repository.PublicQueryPageAsync(new PageQuery<MongoPerson>(1, 10), TestContext.Current.CancellationToken);

		Assert.NotNull(result);
		Assert.NotEmpty(result.Items!);
	}

	#endregion

	/// <summary>
	/// Test repository that exposes the protected <c>QueryPageAsync</c> and
	/// <c>FindAsync(IQuery)</c> methods from the base class.
	/// </summary>
	public sealed class TestMongoRepository : MongoRepository<MongoPerson, ObjectId> {
		public TestMongoRepository(IMongoDbContext context) : base(context, logger: null) { }

		public ValueTask<PageQueryResult<MongoPerson>> PublicQueryPageAsync(PageQuery<MongoPerson> request, CancellationToken ct = default) =>
			base.QueryPageAsync(request, ct);

		public ValueTask<IReadOnlyList<MongoPerson>> PublicFindAsync(IQuery query, CancellationToken ct = default) =>
			base.FindAsync(query, ct);
	}
}
