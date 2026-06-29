using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
		var repository = new MongoRepository<TestEntity>(context, (ILogger<MongoRepository<TestEntity>>?)null);

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
		var repository = new MongoRepository<TestEntity>(context, (ILogger<MongoRepository<TestEntity>>?)null);

		// Act
#pragma warning disable CS0618
		var queryable = repository.Queryable();
#pragma warning restore CS0618

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
		var repository = new MongoRepository<TestEntity>(context, (ILogger<MongoRepository<TestEntity>>?)null);
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
