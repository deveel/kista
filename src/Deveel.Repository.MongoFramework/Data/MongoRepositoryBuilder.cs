using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using MongoFramework;

namespace Deveel.Data
{
    /// <summary>
    /// Fluent builder for configuring the MongoDB repository driver via MongoFramework.
    /// </summary>
    public class MongoRepositoryBuilder {
        private readonly RepositoryContextBuilder _parent;
        private readonly Type _contextType;
        private Action<MongoConnectionBuilder>? _connectionConfig;
        private string? _connectionString;
        private ServiceLifetime _lifetime = ServiceLifetime.Scoped;

        internal MongoRepositoryBuilder(RepositoryContextBuilder parent, Type contextType) {
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
        /// Configures tenant-specific MongoDB connections.
        /// Requires the Deveel.Repository.MongoFramework.MultiTenant package and
        /// Finbuckle.MultiTenant to be configured separately.
        /// </summary>
        /// <remarks>
        /// This method is a placeholder. Use <c>WithMongoMultiTenancy</c> from the
        /// <c>Deveel.Repository.MongoFramework.MultiTenant</c> package for full support.
        /// </remarks>
        public MongoRepositoryBuilder WithTenantConnection(string? defaultConnection = null) {
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
        public static implicit operator RepositoryContextBuilder(MongoRepositoryBuilder builder) {
            builder.FinalizeRegistration();
            return builder._parent;
        }
    }
}