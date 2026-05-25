using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;

namespace Kista
{
	public class DbTenantInfo : ITenantInfo
	{
        public DbTenantInfo()
        {
        }

        public DbTenantInfo(string id, string identifier, string connectionString)
        {
            Id = id;
            Identifier = identifier;
            ConnectionString = connectionString;
        }

        public string? Name { get; set; }
        
        public string Identifier { get; set; }
        
        public string Id { get; set; }
        
#if NET9_0 || NET8_0
        string ITenantInfo.Id
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
		public string ConnectionString { get; set; }
	}
}
