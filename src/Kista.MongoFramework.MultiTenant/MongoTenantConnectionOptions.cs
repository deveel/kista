namespace Kista
{
	/// <summary>
	/// Provides options for configuring the connection
	/// for a multi-tenant MongoDB application.
	/// </summary>
	public class MongoTenantConnectionOptions
	{
		/// <summary>
		/// Gets or sets the default connection string used to 
		/// connect to the database, when no tenant-specific
		/// is provided.
		/// </summary>
		public string? DefaultConnectionString { get; set; }
	}
}
