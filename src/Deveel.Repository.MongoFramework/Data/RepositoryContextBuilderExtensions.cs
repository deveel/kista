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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using MongoFramework;

namespace Deveel.Data {
	/// <summary>
	/// Fluent builder for configuring the MongoDB repository driver via MongoFramework.
	/// </summary>
	public class MongoDriverBuilder {
		private readonly RepositoryContextBuilder _parent;
		private readonly Type _contextType;
		private Action<MongoConnectionBuilder>? _connectionConfig;
		private string? _connectionString;
		private ServiceLifetime _lifetime = ServiceLifetime.Scoped;

		internal MongoDriverBuilder(RepositoryContextBuilder parent, Type contextType) {
			_parent = parent;
			_contextType = contextType;
		}

		/// <summary>
		/// Gets the underlying service collection.
		/// </summary>
		public IServiceCollection Services => _parent.Services;

		/// <summary>
		/// Returns to the parent <see cref="RepositoryContextBuilder"/>.
		/// </summary>
		public RepositoryContextBuilder ToParent() => _parent;

		/// <summary>
		/// Sets the connection string for the MongoDB connection.
		/// </summary>
		public MongoDriverBuilder WithConnectionString(string connectionString) {
			_connectionString = connectionString;
			return this;
		}

		/// <summary>
		/// Configures the MongoDB connection using a builder action.
		/// </summary>
		public MongoDriverBuilder WithConnection(Action<MongoConnectionBuilder> configure) {
			_connectionConfig = configure;
			return this;
		}

		/// <summary>
		/// Sets the service lifetime for the context and repositories.
		/// </summary>
		public MongoDriverBuilder WithLifetime(ServiceLifetime lifetime) {
			_lifetime = lifetime;
			return this;
		}

		/// <summary>
		/// Configures tenant-specific MongoDB connections.
		/// Requires the Deveel.Repository.MongoFramework.MultiTenant package and
		/// Finbuckle.MultiTenant to be configured separately.
		/// </summary>
		/// <remarks>
		/// This method is a placeholder. Use <c>WithMongoMultiTenancy</c> from the
		/// <c>Deveel.Repository.MongoFramework.MultiTenant</c> package for full support.
		/// </remarks>
		public MongoDriverBuilder WithTenantConnection(string? defaultConnection = null) {
			return this;
		}

		internal void FinalizeRegistration() {
			if (_connectionString != null) {
				RegisterMongoContext(_connectionString);
			} else if (_connectionConfig != null) {
				RegisterMongoContext(_connectionConfig);
			} else {
				_parent.Services.TryAdd(ServiceDescriptor.Scoped(_contextType, _contextType));
				_parent.Services.TryAdd(ServiceDescriptor.Scoped(typeof(IMongoDbContext), _contextType));
			}

			_parent.AddRepository(typeof(MongoRepository<>), _lifetime);
			_parent.AddRepository(typeof(MongoRepository<,>), _lifetime);
		}

		private void RegisterMongoContext(string connectionString) {
			var connectionBuilder = new MongoConnectionBuilder(_contextType, _parent.Services, _lifetime);
			connectionBuilder.UseConnection(connectionString);

			_parent.Services.TryAdd(ServiceDescriptor.Scoped(_contextType, _contextType));
			_parent.Services.TryAdd(ServiceDescriptor.Scoped(typeof(IMongoDbContext), _contextType));
		}

		private void RegisterMongoContext(Action<MongoConnectionBuilder> configure) {
			var cb = new MongoConnectionBuilder(_contextType, _parent.Services, _lifetime);
			configure(cb);

			_parent.Services.TryAdd(ServiceDescriptor.Scoped(_contextType, _contextType));
			_parent.Services.TryAdd(ServiceDescriptor.Scoped(typeof(IMongoDbContext), _contextType));
		}

		/// <summary>
		/// Implicitly converts to the parent <see cref="RepositoryContextBuilder"/>.
		/// </summary>
		public static implicit operator RepositoryContextBuilder(MongoDriverBuilder builder) {
			builder.FinalizeRegistration();
			return builder._parent;
		}
	}

	/// <summary>
	/// Extension methods for configuring the MongoDB driver on a <see cref="RepositoryContextBuilder"/>.
	/// </summary>
	public static class RepositoryContextBuilderExtensions {
		/// <summary>
		/// Configures the MongoDB repository driver.
		/// </summary>
		public static MongoDriverBuilder UseMongoDB<TContext>(this RepositoryContextBuilder builder)
			where TContext : class, IMongoDbContext {
			return new MongoDriverBuilder(builder, typeof(TContext));
		}

		/// <summary>
		/// Configures the MongoDB repository driver with a configuration action.
		/// </summary>
		public static RepositoryContextBuilder UseMongoDB<TContext>(this RepositoryContextBuilder builder, Action<MongoDriverBuilder> configure)
			where TContext : class, IMongoDbContext {
			var driverBuilder = new MongoDriverBuilder(builder, typeof(TContext));
			configure(driverBuilder);
			driverBuilder.FinalizeRegistration();
			return builder;
		}
	}
}
