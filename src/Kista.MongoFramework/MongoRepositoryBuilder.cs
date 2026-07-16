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

namespace Kista
{
    /// <summary>
    /// Fluent builder for configuring the MongoDB repository driver via MongoFramework.
    /// </summary>
    public class MongoRepositoryBuilder : RepositoryDriverBuilder {
        private readonly Type _contextType;
        private Action<MongoConnectionBuilder>? _connectionConfig;
        private string? _connectionString;
        private ServiceLifetime _lifetime = ServiceLifetime.Scoped;

        internal MongoRepositoryBuilder(RepositoryContextBuilder parent, Type contextType)
            : base(parent) {
            _contextType = contextType;
        }

        /// <summary>
        /// Sets the connection string for the MongoDB connection.
        /// </summary>
        public MongoRepositoryBuilder WithConnectionString(string connectionString) {
            _connectionString = connectionString;
            return this;
        }

        /// <summary>
        /// Configures the MongoDB connection using a builder action.
        /// </summary>
        public MongoRepositoryBuilder WithConnection(Action<MongoConnectionBuilder> configure) {
            _connectionConfig = configure;
            return this;
        }

        /// <summary>
        /// Sets the service lifetime for the context and repositories.
        /// </summary>
        public MongoRepositoryBuilder WithLifetime(ServiceLifetime lifetime) {
            _lifetime = lifetime;
            return this;
        }

        /// <summary>
        /// Enables lifecycle support for the MongoDB repositories,
        /// registering a <see cref="MongoRepositoryLifecycleHandler{TEntity}"/>
        /// for each entity type.
        /// </summary>
        public new MongoRepositoryBuilder WithLifecycle() {
            base.WithLifecycle<MongoRepositoryBuilder>();
            return this;
        }

        /// <summary>
        /// Disables lifecycle support for the MongoDB repositories.
        /// </summary>
        public new MongoRepositoryBuilder WithoutLifecycle() {
            base.WithoutLifecycle<MongoRepositoryBuilder>();
            return this;
        }

        /// <summary>
        /// Registers soft-delete configuration for the MongoDB driver.
        /// Soft-delete filtering activates automatically for any entity
        /// implementing <see cref="ISoftDeletable"/>: this call is
        /// reserved for future configuration knobs.
        /// </summary>
        /// <param name="configure">
        /// An optional delegate to configure the <see cref="SoftDeleteOptions"/>.
        /// </param>
        /// <returns>
        /// Returns the same builder for chaining.
        /// </returns>
        public MongoRepositoryBuilder WithSoftDelete(Action<SoftDeleteOptions>? configure = null) {
            Parent.WithSoftDelete(configure);
            return this;
        }

        internal void FinalizeRegistration() {
            if (_connectionString != null) {
                RegisterMongoContext(_connectionString);
            } else if (_connectionConfig != null) {
                RegisterMongoContext(_connectionConfig);
            } else {
                Parent.Services.TryAdd(ServiceDescriptor.Scoped(_contextType, _contextType));
                Parent.Services.TryAdd(ServiceDescriptor.Scoped(typeof(IMongoDbContext), _contextType));
            }

            RegisterAdditionalContextTypes();

            Parent.AddRepository(typeof(MongoRepository<>), _lifetime);
            Parent.AddRepository(typeof(MongoRepository<,>), _lifetime);

            RegisterLifecycleHandler(typeof(MongoRepositoryLifecycleHandler<>));
        }

        private void RegisterMongoContext(string connectionString) {
            var connectionBuilder = new MongoConnectionBuilder(_contextType, Parent.Services, _lifetime);
            connectionBuilder.UseConnection(connectionString);

            Parent.Services.TryAdd(ServiceDescriptor.Scoped(_contextType, _contextType));
            Parent.Services.TryAdd(ServiceDescriptor.Scoped(typeof(IMongoDbContext), _contextType));
        }

        private void RegisterMongoContext(Action<MongoConnectionBuilder> configure) {
            var cb = new MongoConnectionBuilder(_contextType, Parent.Services, _lifetime);
            configure(cb);

            Parent.Services.TryAdd(ServiceDescriptor.Scoped(_contextType, _contextType));
            Parent.Services.TryAdd(ServiceDescriptor.Scoped(typeof(IMongoDbContext), _contextType));
        }

        private void RegisterAdditionalContextTypes() {
            if (typeof(IMongoDbTenantContext).IsAssignableFrom(_contextType))
                Parent.Services.TryAdd(new ServiceDescriptor(typeof(IMongoDbTenantContext), _contextType, ServiceLifetime.Scoped));

            if (typeof(MongoDbContext).IsAssignableFrom(_contextType) &&
                typeof(MongoDbContext) != _contextType)
                Parent.Services.TryAdd(new ServiceDescriptor(typeof(MongoDbContext), _contextType, ServiceLifetime.Scoped));

            if (typeof(MongoDbTenantContext).IsAssignableFrom(_contextType) &&
                typeof(MongoDbTenantContext) != _contextType)
                Parent.Services.TryAdd(new ServiceDescriptor(typeof(MongoDbTenantContext), provider => provider.GetRequiredService(_contextType), ServiceLifetime.Scoped));
        }

        /// <summary>
        /// Implicitly converts to the parent <see cref="RepositoryContextBuilder"/>.
        /// </summary>
        public static implicit operator RepositoryContextBuilder(MongoRepositoryBuilder builder) {
            builder.FinalizeRegistration();
            return builder.Parent;
        }
    }
}