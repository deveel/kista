using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;

namespace Kista
{
	/// <summary>
	/// An implementation of <see cref="TenantInfo"/> that
	/// provides the connection string for a MongoDB tenant.
	/// </summary>
	public class MongoDbTenantInfo : ITenantInfo
	{
		/// <summary>
		/// Gets or sets the connection string for the MongoDB tenant.
		/// </summary>
		public string? ConnectionString { get; set;}

#if NET8_0 || NET9_0
        string? ITenantInfo.Id
        {
            get => Id ?? "";
            set => Id = value ?? "";
        }

        string? ITenantInfo.Identifier
        {
            get => Identifier ?? ""; 
            set => Identifier = value ?? "";
        }
#endif
        /// <summary>
        /// Gets or sets the unique identifier of the tenant.
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Gets or sets the unique identifier used to identify the tenant.
        /// </summary>
        public string Identifier { get; set; }
        
        /// <summary>
        /// Gets or sets the display name of the tenant.
        /// </summary>
        public string? Name { get; set; }
    }
}
