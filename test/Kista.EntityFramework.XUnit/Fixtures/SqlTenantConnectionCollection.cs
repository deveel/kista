namespace Kista {
	[CollectionDefinition(nameof(SqlTenantConnectionCollection))]
	public class SqlTenantConnectionCollection : ICollectionFixture<SqlTenantTestConnection> {
	}
}
