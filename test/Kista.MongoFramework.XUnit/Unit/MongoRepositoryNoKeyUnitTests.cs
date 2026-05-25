using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoFramework;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "MongoRepository")]
public class MongoRepositoryNoKeyUnitTests
{
	[Fact]
	public void Constructor_WithContext_ShouldCreateInstance()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddMongoDbContext<TestMongoContext>(builder => {
			builder.UseConnection("mongodb://localhost:27017/testdb");
		});

		var provider = services.BuildServiceProvider();
		var context = provider.GetRequiredService<TestMongoContext>();

		// Act
		var repository = new MongoRepository<TestEntity>(context);

		// Assert
		Assert.NotNull(repository);
	}

	[Fact]
	public void AsQueryable_ShouldReturnQueryable()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddMongoDbContext<TestMongoContext>(builder => {
			builder.UseConnection("mongodb://localhost:27017/testdb");
		});

		var provider = services.BuildServiceProvider();
		var context = provider.GetRequiredService<TestMongoContext>();
		var repository = new MongoRepository<TestEntity>(context);
		var queryableRepo = (IQueryableRepository<TestEntity, object>)repository;

		// Act
		var queryable = queryableRepo.AsQueryable();

		// Assert
		Assert.NotNull(queryable);
	}

	[Fact]
	public void GetEntityKey_ViaInterface_ShouldReturnKey()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddMongoDbContext<TestMongoContext>(builder => {
			builder.UseConnection("mongodb://localhost:27017/testdb");
		});

		var provider = services.BuildServiceProvider();
		var context = provider.GetRequiredService<TestMongoContext>();
		var repository = new MongoRepository<TestEntity>(context);
		var keyedRepo = (IRepository<TestEntity, object>)repository;
		var entity = new TestEntity { Id = ObjectId.GenerateNewId(), Name = "Test" };

		// Act
		var key = keyedRepo.GetEntityKey(entity);

		// Assert
		Assert.NotNull(key);
		Assert.Equal(entity.Id, key);
	}

	private class TestEntity
	{
		public ObjectId Id { get; set; }
		public string Name { get; set; } = string.Empty;
	}

	private sealed class TestMongoContext(IMongoDbConnection<TestMongoContext> connection) : MongoDbContext(connection)
	{
	}
}
