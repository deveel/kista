using Finbuckle.MultiTenant;
#if NET7_0_OR_GREATER
using Finbuckle.MultiTenant.Abstractions;
#endif

namespace Kista
{
	class ExecutionContextStreategy : IMultiTenantStrategy
	{
		public Task<string?> GetIdentifierAsync(object context)
		{
			if (context is ITenantIdentifier tenantIdentifier)
			{
				return Task.FromResult<string?>(tenantIdentifier.TenantId);
			}
			

			return Task.FromResult<string?>(null);
		}
	}
}
