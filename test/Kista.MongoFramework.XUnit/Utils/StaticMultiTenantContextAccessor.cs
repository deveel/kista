using Kista;

using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;

namespace Kista.Utils
{
	class StaticMultiTenantContextAccessor : IMultiTenantContextAccessor<MongoDbTenantInfo>
	{
		public StaticMultiTenantContextAccessor(MongoDbTenantInfo tenantInfo)
		{
#if NET8_0 || NET9_0
            MultiTenantContext = new MultiTenantContext<MongoDbTenantInfo>
            {
                TenantInfo = tenantInfo
            };
#else
			MultiTenantContext = new MultiTenantContext<MongoDbTenantInfo>(tenantInfo);
#endif
        }

		public IMultiTenantContext<MongoDbTenantInfo> MultiTenantContext { get; }

		IMultiTenantContext IMultiTenantContextAccessor.MultiTenantContext => MultiTenantContext;
	}
}
