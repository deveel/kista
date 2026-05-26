using MongoDB.Driver.Core.Configuration;

using MongoFramework;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "MongoDbConnection")]
public class MongoDbConnectionTests {
    [Fact]
    public void Should_ParseConnectionString_When_UrlBuiltFromConnection() {
        // Arrange
        const string connectionString = "mongodb://localhost:27017/testdb";

        // Act
        var connection = MongoDbConnection.FromConnectionString(connectionString);
        var url = connection.GetUrl();

        // Assert
        Assert.NotNull(connection);
        Assert.NotNull(url);
        Assert.Equal(ConnectionStringScheme.MongoDB, url.Scheme);
        Assert.Equal("localhost", url.Server.Host);
        Assert.Equal(27017, url.Server.Port);
        Assert.Equal("testdb", url.DatabaseName);
    }

    [Fact]
    public void Should_ParseConnectionString_When_TypedConnectionCreated() {
        // Arrange
        const string connectionString = "mongodb://localhost:27017/testdb";

        // Act
        var connection = new MongoDbConnection<MongoDbContext>(connectionString);
        var url = connection.GetUrl();

        // Assert
        Assert.NotNull(connection);
        Assert.NotNull(url);
        Assert.Equal(ConnectionStringScheme.MongoDB, url.Scheme);
        Assert.Equal("localhost", url.Server.Host);
        Assert.Equal(27017, url.Server.Port);
        Assert.Equal("testdb", url.DatabaseName);
    }
}
