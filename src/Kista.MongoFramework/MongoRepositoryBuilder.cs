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

        internal void FinalizeRegistration() {
            if (_connectionString != null) {
                RegisterMongoContext(_connectionString);
            } else if (_connectionConfig != null) {
                RegisterMongoContext(_connectionConfig);
            } else {
                Parent.Services.TryAdd(ServiceDescriptor.Scoped(_contextType, _contextType));
                Parent.Services.TryAdd(ServiceDescriptor.Scoped(typeof(IMongoDbContext), _contextType));
            }

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

        /// <summary>
        /// Implicitly converts to the parent <see cref="RepositoryContextBuilder"/>.
        /// </summary>
        public static implicit operator RepositoryContextBuilder(MongoRepositoryBuilder builder) {
            builder.FinalizeRegistration();
            return builder.Parent;
        }
    }
}