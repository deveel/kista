using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;

using Microsoft.Extensions.DependencyInjection;

using System.Reflection;

namespace Kista
{
	public sealed class TenantExecutionContext<TTenantInfo>
		where TTenantInfo : class, ITenantInfo, new()
	{
		private readonly IServiceProvider _services;

		public TenantExecutionContext(IServiceProvider services)
		{
			_services = services;
		}

		private object?[] BuildArguments(IServiceProvider scope, Delegate action)
		{
			var method = action.Method;
			var parameters = method.GetParameters();
			var args = new object?[parameters.Length];
			for (int i = 0; i < parameters.Length; i++)
			{
				var parameter = parameters[i];
				if (parameter.ParameterType == typeof(IServiceProvider))
				{
					args[i] = scope;
				}
				else
				{
					args[i] = scope.GetService(parameter.ParameterType);
				}
			}

			return args;
		}

		public async Task<TResult> ExecuteInScopeAsync<TResult>(string tenantId, Delegate action)
		{
			ArgumentNullException.ThrowIfNull(tenantId);
			ArgumentNullException.ThrowIfNull(action);

			if (string.IsNullOrEmpty(tenantId))
				throw new ArgumentException("Tenant ID cannot be null or empty.", nameof(tenantId));

			if (!typeof(Task<TResult>).IsAssignableFrom(action.Method.ReturnType))
				throw new ArgumentException($"The return type '{typeof(TResult)}' is not compatible with the action's return type '{action.Method.ReturnType}'.", nameof(action));

			using (var scope = _services.CreateScope())
			{
				// Set the tenant context in the scope	
				var context = await ResolveContext(scope.ServiceProvider, tenantId).ConfigureAwait(false);
				SetTenantInScope(scope.ServiceProvider, context);

				var args = BuildArguments(scope.ServiceProvider, action);

				try
				{
					return await ((Task<TResult>)action.DynamicInvoke(args)!);
				} catch (TargetInvocationException ex)
				{
					throw ex.InnerException ?? ex;
				}
			}
		}

		public async Task ExecuteInScopeAsync(string tenantId, Delegate action)
		{
			ArgumentNullException.ThrowIfNull(tenantId);
			ArgumentNullException.ThrowIfNull(action);

			if (string.IsNullOrEmpty(tenantId))
				throw new ArgumentException("Tenant ID cannot be null or empty.", nameof(tenantId));

			using (var scope = _services.CreateScope())
			{
				// Set the tenant context in the scope	
				var context = await ResolveContext(scope.ServiceProvider, tenantId).ConfigureAwait(false);
				SetTenantInScope(scope.ServiceProvider, context);

				var args = BuildArguments(scope.ServiceProvider, action);

				try
				{
					await (Task)action.DynamicInvoke(args)!;
				} catch (TargetInvocationException ex)
				{
					throw ex.InnerException ?? ex;
				}
			}
		}

		class TenantIdentifier : ITenantIdentifier
		{
			public TenantIdentifier(string tenantId)
			{
				TenantId = tenantId;
			}
			public string TenantId { get; set; }
		}

		private async Task<IMultiTenantContext<TTenantInfo>> ResolveContext(IServiceProvider serviceProvider, string tenantId)
		{
			var resolver = serviceProvider.GetRequiredService<ITenantResolver<TTenantInfo>>();
			var context = await resolver.ResolveAsync(new TenantIdentifier(tenantId));
			if (context == null)
			{
				throw new InvalidOperationException($"No tenant context found for tenant ID '{tenantId}'.");
			}
			return context;
		}

		private void SetTenantInScope(IServiceProvider serviceProvider, IMultiTenantContext<TTenantInfo> context)
		{
			var setter = serviceProvider.GetService<IMultiTenantContextSetter>();
			if (setter != null)
			{
				setter.MultiTenantContext = context;
			}
		}
	}
}
