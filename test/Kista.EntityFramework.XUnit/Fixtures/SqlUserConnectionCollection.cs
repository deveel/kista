namespace Kista
{
	[CollectionDefinition(nameof(SqlUserConnectionCollection))]
	public class SqlUserConnectionCollection : ICollectionFixture<SqlUserTestConnection>
	{
	}

	public class SqlUserTestConnection : SqlTestConnection
	{
		public SqlUserTestConnection() : base("deveel-user-test")
		{
		}
	}
}
