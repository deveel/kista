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

﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using MongoFramework;

namespace Kista
{
	/// <summary>
	/// Provides extension methods for configuring tenant-specific MongoDB connections 
	/// in a <see cref="MongoConnectionBuilder"/>.
	/// </summary>
	/// <remarks>
	/// This class contains methods that extend the functionality of <see cref="MongoConnectionBuilder"/> 
	/// to support tenant-specific connection configurations. 
	/// It allows the registration of services necessary for handling connections 
	/// to MongoDB databases with tenant-specific settings.
	/// </remarks>
	public static class MongoConnectionBuilderExtensions
	{
		/// <summary>
		/// Configures the <see cref="MongoConnectionBuilder"/> to use 
		/// tenant-specific MongoDB connections.
		/// </summary>
		/// <remarks>
		/// This method sets up the necessary services to support tenant-specific 
		/// connections in a MongoDB context. 
		/// It registers the required options and services to enable the use of 
		/// tenant-specific connection strings.
		/// </remarks>
		/// <param name="builder">The <see cref="MongoConnectionBuilder"/> to configure.</param>
		/// <param name="defaultConnection">The default connection string to use if no tenant-specific 
		/// connection is provided. Can be <see langword="null"/>.</param>
		/// <returns>The configured <see cref="MongoConnectionBuilder"/> instance.</returns>
		public static MongoConnectionBuilder UseTenantConnection(this MongoConnectionBuilder builder, string? defaultConnection = null)
		{
			builder.Services.AddOptions<MongoTenantConnectionOptions>()
				.Configure(options => options.DefaultConnectionString = defaultConnection);

			var connectionType = typeof(IMongoDbConnection<>).MakeGenericType(builder.ContextType);
			builder.Services.TryAdd(ServiceDescriptor.Describe(connectionType, sp =>
			{
				var implementationType = typeof(MongoDbTenantConnection<>).MakeGenericType(builder.ContextType);
				return ActivatorUtilities.CreateInstance(sp, implementationType);
			}, builder.Lifetime));

			builder.Services.TryAdd(ServiceDescriptor.Describe(typeof(IMongoDbConnection), sp =>
			{
				var connection = sp.GetRequiredService(connectionType);
				return (IMongoDbConnection)connection;
			}, builder.Lifetime));

			return builder;
		}

	}
}
