namespace Kista {
	[CollectionDefinition(nameof(PostgresConnectionCollection))]
	public class PostgresConnectionCollection : ICollectionFixture<PostgresTestConnection> {
	}
}
