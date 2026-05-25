using Kista;

using Finbuckle.MultiTenant;

#if NET7_0_OR_GREATER
using Finbuckle.MultiTenant.Abstractions;
#endif

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kista.Utils
{
	public static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddMongoTenantContext(this IServiceCollection services, MongoDbTenantInfo tenantInfo)
		{
			services.AddSingleton<ITenantInfo>(tenantInfo);
			services.AddSingleton<IMultiTenantContextAccessor<MongoDbTenantInfo>>(new StaticMultiTenantContextAccessor(tenantInfo));
			services.TryAddSingleton<IMultiTenantContextAccessor>(sp => (IMultiTenantContextAccessor) sp.GetService<IMultiTenantContextAccessor<MongoDbTenantInfo>>());
			return services;
		}
	}
}
