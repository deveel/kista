// Copyright 2023-2026 Antonello Provenzano
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Deveel.Data {
	/// <summary>
	/// Extension methods for configuring Entity Framework Core multi-tenancy on an <see cref="EntityFrameworkRepositoryBuilder"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Two multi-tenancy strategies are supported:
	/// </para>
	/// <list type="bullet">
	/// <item>
	/// <description><b>Database-per-tenant</b>: Each tenant has its own database. The DbContext must override <c>OnConfiguring</c> to resolve the connection string from <see cref="ITenantInfo.ConnectionString"/> via <c>IMultiTenantContextAccessor</c>.</description>
	/// </item>
	/// <item>
	/// <description><b>Shared database</b>: All tenants share a single database with a TenantId column. Use <see cref="WithSharedTenantDatabase"/> when your DbContext derives from <c>MultiTenantDbContext</c>.</description>
	/// </item>
	/// </list>
	/// </remarks>
	public static class RepositoryContextBuilderEntityFrameworkMultiTenantExtensions {
		/// <summary>
		/// Configures database-per-tenant multi-tenancy using Finbuckle.MultiTenant.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This method registers a per-tenant options configuration that resolves the connection string
		/// from <see cref="ITenantInfo.ConnectionString"/> at runtime. The DbContext should be configured
		/// to use the connection string in its <c>OnConfiguring</c> method or via <c>ConfigureDbContext</c>.
		/// </para>
		/// <para>
		/// Your <c>ITenantInfo</c> implementation must have a <c>ConnectionString</c> property.
		/// </para>
		/// </remarks>
		/// <typeparam name="TTenantInfo">The type of tenant info, must implement <see cref="ITenantInfo"/>.</typeparam>
		/// <param name="builder">The Entity Framework driver builder.</param>
		/// <param name="defaultConnection">Optional default connection string when no tenant is resolved.</param>
		/// <returns>The same <see cref="EntityFrameworkRepositoryBuilder"/> for chaining.</returns>
		public static EntityFrameworkRepositoryBuilder WithDatabasePerTenant<TTenantInfo>(
			this EntityFrameworkRepositoryBuilder builder,
			string? defaultConnection = null)
			where TTenantInfo : class, ITenantInfo {
			builder.Services.AddOptions<EntityFrameworkTenantConnectionOptions>()
				.Configure(options => options.DefaultConnectionString = defaultConnection);

			return builder;
		}

		/// <summary>
		/// Configures shared-database multi-tenancy using Finbuckle.MultiTenant.
		/// </summary>
		/// <remarks>
		/// All tenants share a single database. Data isolation is achieved by filtering queries
		/// based on the current tenant's ID. The DbContext must derive from <c>MultiTenantDbContext</c>
		/// and entities must be configured with <c>IsMultiTenant()</c> in <c>OnModelCreating</c>.
		/// </remarks>
		/// <param name="builder">The Entity Framework driver builder.</param>
		/// <returns>The same <see cref="EntityFrameworkRepositoryBuilder"/> for chaining.</returns>
		public static EntityFrameworkRepositoryBuilder WithSharedTenantDatabase(this EntityFrameworkRepositoryBuilder builder) {
			var dbContextType = builder.DbContextType;
			if (dbContextType == null)
				throw new InvalidOperationException("DbContext type must be configured before enabling row-level filtering.");

			var baseType = dbContextType.BaseType;
			var isMultiTenantDbContext = false;
			while (baseType != null) {
				if (baseType.FullName == "Finbuckle.MultiTenant.EntityFrameworkCore.MultiTenantDbContext") {
					isMultiTenantDbContext = true;
					break;
				}
				baseType = baseType.BaseType;
			}

			if (!isMultiTenantDbContext)
				throw new InvalidOperationException(
					$"DbContext type '{dbContextType.FullName}' must derive from 'Finbuckle.MultiTenant.EntityFrameworkCore.MultiTenantDbContext' " +
					"to use row-level filtering. Configure your DbContext to inherit from MultiTenantDbContext and call " +
					"IsMultiTenant() on entity types in OnModelCreating.");

			return builder;
		}
	}
}
