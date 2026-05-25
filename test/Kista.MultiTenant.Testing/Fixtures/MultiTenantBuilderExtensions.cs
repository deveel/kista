using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;

using Microsoft.Extensions.DependencyInjection;

namespace Kista
{
	public static class MultiTenantBuilderExtensions
	{
		public static MultiTenantBuilder<TTenantInfo> WithContextStrategy<TTenantInfo>(
			this MultiTenantBuilder<TTenantInfo> builder)
			where TTenantInfo : class, ITenantInfo, new()
		{
			builder.WithStrategy<ExecutionContextStreategy>(ServiceLifetime.Scoped);
			builder.Services.AddSingleton<TenantExecutionContext<TTenantInfo>>();

			return builder;
		}
	}
}
