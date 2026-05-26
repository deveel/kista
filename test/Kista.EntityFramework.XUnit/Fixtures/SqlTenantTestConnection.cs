namespace Kista
{
	public class SqlTenantTestConnection : SqlTestConnection
	{
		public SqlTenantTestConnection() : base("deveel-tenant-test") {
			// This constructor initializes the connection for tenant tests
		}
	}
}
