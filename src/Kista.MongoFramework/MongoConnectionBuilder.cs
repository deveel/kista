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

﻿using System.Runtime.CompilerServices;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using MongoFramework;

namespace Kista
{
	/// <summary>
	/// Provides a builder for configuring MongoDB connections 
	/// within a service collection.
	/// </summary>
	/// <remarks>This class is used to set up MongoDB connections by specifying the connection string and 
	/// configuring the service lifetime. It is typically used during application startup to  register MongoDB services
	/// with dependency injection.</remarks>
	public sealed class MongoConnectionBuilder
	{
		public MongoConnectionBuilder(Type contextType, IServiceCollection services, ServiceLifetime lifetime)
		{
			ContextType = contextType ?? throw new ArgumentNullException(nameof(contextType));
			Services = services ?? throw new ArgumentNullException(nameof(services));
			Lifetime = lifetime;
		}

		public Type ContextType { get; }

		public IServiceCollection Services { get; }

		public ServiceLifetime Lifetime { get; }

		/// <summary>
		/// Configures the builder to use a MongoDB connection with 
		/// the specified connection string.
		/// </summary>
		/// <param name="connectionString">The connection string used to establish a 
		/// connection to the MongoDB database.Cannot be null, empty, or consist solely of whitespace.</param>
		/// <returns>The current instance of <see cref="MongoConnectionBuilder"/> to allow for method chaining.</returns>
		/// <exception cref="ArgumentException">Thrown if <paramref name="connectionString"/> is empty or consists only of whitespace.</exception>
		/// <exception cref="InvalidOperationException">Thrown if no suitable constructor is found for the MongoDB connection implementation.</exception>
		public MongoConnectionBuilder UseConnection(string connectionString)
		{
			ArgumentNullException.ThrowIfNull(connectionString);
			if (string.IsNullOrWhiteSpace(connectionString))
				throw new ArgumentException("Connection string cannot be empty.", nameof(connectionString));

			var connectionType = typeof(IMongoDbConnection<>).MakeGenericType(ContextType);

			Services.TryAdd(ServiceDescriptor.Describe(connectionType, sp =>
			{
				var implementationType = typeof(MongoDbConnection<>).MakeGenericType(ContextType);
				var ctor = implementationType.GetConstructor(new[] { typeof(string) });
				if (ctor == null)
					throw new InvalidOperationException($"No suitable constructor found for {implementationType.FullName} that accepts IMongoDbConnection.");

				return ctor.Invoke(new object[] { connectionString });
			}, Lifetime));

			Services.TryAdd(ServiceDescriptor.Describe(typeof(IMongoDbConnection), sp => (IMongoDbConnection) sp.GetRequiredService(connectionType), Lifetime));

			return this;
		}
	}
}
