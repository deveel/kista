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

using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using MongoFramework;

namespace Deveel.Data {
	/// <summary>
	/// Extension methods for configuring MongoDB multi-tenancy on a <see cref="MongoRepositoryBuilder"/>.
	/// </summary>
	public static class RepositoryContextBuilderExtensions {
		/// <summary>
		/// Configures tenant-specific MongoDB connections using Finbuckle.MultiTenant.
		/// </summary>
		/// <param name="builder">The MongoDB driver builder.</param>
		/// <param name="defaultConnection">Optional default connection string when no tenant is resolved.</param>
		/// <returns>The same <see cref="MongoRepositoryBuilder"/> for chaining.</returns>
		public static MongoRepositoryBuilder WithMongoMultiTenancy<TTenantInfo>(
			this MongoRepositoryBuilder builder,
			string? defaultConnection = null)
			where TTenantInfo : class, ITenantInfo {
			builder.Services.AddOptions<MongoTenantConnectionOptions>()
				.Configure(options => options.DefaultConnectionString = defaultConnection);

#pragma warning disable S3011 // Accessing internal builder field to extract context type for multi-tenant setup
			var contextTypeField = builder.GetType().GetField("_contextType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
#pragma warning restore S3011
			var contextType = (Type?)contextTypeField?.GetValue(builder)
				?? throw new InvalidOperationException("Cannot determine context type from builder.");

			var connectionType = typeof(IMongoDbConnection<>).MakeGenericType(contextType);
			builder.Services.TryAdd(ServiceDescriptor.Describe(connectionType, sp => {
				var implementationType = typeof(MongoDbTenantConnection<>).MakeGenericType(contextType);
				return ActivatorUtilities.CreateInstance(sp, implementationType);
			}, ServiceLifetime.Scoped));

			builder.Services.TryAdd(ServiceDescriptor.Describe(typeof(IMongoDbConnection), sp => {
				var connection = sp.GetRequiredService(connectionType);
				return (IMongoDbConnection)connection;
			}, ServiceLifetime.Scoped));

			return builder;
		}
	}
}
