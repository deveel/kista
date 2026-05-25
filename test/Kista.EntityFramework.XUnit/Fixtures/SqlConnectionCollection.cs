namespace Kista {
	[CollectionDefinition(nameof(SqlConnectionCollection))]
	public class SqlConnectionCollection : ICollectionFixture<SqlTestConnection> {
	}
}
