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

﻿using Finbuckle.MultiTenant;

using Microsoft.Extensions.Options;

#if NET7_0_OR_GREATER
using Finbuckle.MultiTenant.Abstractions;
#endif

using MongoFramework;

namespace Kista
{
	/// <summary>
	/// Represents a connection to a MongoDB database that is specific to a tenant.
	/// </summary>
	/// <remarks>
	/// This class extends <see cref="MongoDbConnection{TContext}"/> to provide 
	/// tenant-specific connection functionality. 
	/// It retrieves the connection string from the tenant information or falls 
	/// back to a default connection string if necessary.
	/// </remarks>
	/// <typeparam name="TContext">The type of the MongoDB context, which must 
	/// implement <see cref="IMongoDbContext"/>.</typeparam>
	public class MongoDbTenantConnection<TContext> : MongoDbConnection<TContext>
		where TContext : class, IMongoDbContext
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MongoDbTenantConnection{TContext}"/> class 
		/// using the specified options and tenant context accessor.
		/// </summary>
		/// <remarks>This constructor sets up a MongoDB connection string based on the provided tenant-specific
		/// options and context.  Ensure that both <paramref name="options"/> and <paramref
		/// name="multiTenantContextAccessor"/> are properly configured  to reflect the desired tenant's connection
		/// settings.</remarks>
		/// <param name="options">The configuration options for the MongoDB tenant connection. This must not be null.</param>
		/// <param name="multiTenantContextAccessor">The accessor for the current multi-tenant context, providing tenant-specific information. This must not be null.</param>
		public MongoDbTenantConnection(IOptions<MongoTenantConnectionOptions> options, IMultiTenantContextAccessor<MongoDbTenantInfo> multiTenantContextAccessor)
			: base(GetConnectionString(options, multiTenantContextAccessor))
		{
		}

		private static string GetConnectionString(IOptions<MongoTenantConnectionOptions> options, IMultiTenantContextAccessor<MongoDbTenantInfo> multiTenantContextAccessor)
		{
			var tenantInfo = multiTenantContextAccessor.MultiTenantContext?.TenantInfo as MongoDbTenantInfo;
			if (tenantInfo == null)
				throw new InvalidOperationException("Tenant info is not available or not of type MongoDbTenantInfo.");

			var connectionString = tenantInfo.ConnectionString;
			if (string.IsNullOrEmpty(connectionString))
			{
				connectionString = options.Value.DefaultConnectionString;
			}

			if (string.IsNullOrEmpty(connectionString))
				throw new InvalidOperationException("Connection string is not provided in the tenant info or options.");

			return connectionString;
		}
	}
}
