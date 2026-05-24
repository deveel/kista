namespace Deveel.Data {
	[CollectionDefinition(nameof(PostgresConnectionCollection))]
	public class PostgresConnectionCollection : ICollectionFixture<PostgresTestConnection> {
	}
}
