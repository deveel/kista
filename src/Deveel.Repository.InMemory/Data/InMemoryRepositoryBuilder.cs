using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Deveel.Data
{
    /// <summary>
    /// Fluent builder for configuring the In-Memory repository driver.
    /// </summary>
    public class InMemoryRepositoryBuilder {
        private readonly RepositoryContextBuilder _parent;
        private bool _enableLifecycle = false;

        internal InMemoryRepositoryBuilder(RepositoryContextBuilder parent) {
            _parent = parent;
            RegisterOpenGenerics();
        }

        /// <summary>
        /// Gets the underlying service collection.
        /// </summary>
        public IServiceCollection Services => _parent.Services;

        private void RegisterOpenGenerics() {
            _parent.AddRepository(typeof(InMemoryRepository<>), ServiceLifetime.Singleton);
            _parent.AddRepository(typeof(InMemoryRepository<,>), ServiceLifetime.Singleton);
            UpdateLifecycleRegistration();
        }

        private void UpdateLifecycleRegistration() {
            if (_enableLifecycle) {
                _parent.Services.TryAddTransient(typeof(IRepositoryLifecycleHandler<>), typeof(InMemoryRepositoryLifecycleHandler<>));
            }
        }

        /// <summary>
        /// Enables lifecycle support for the In-Memory repositories,
        /// registering a <see cref="InMemoryRepositoryLifecycleHandler{TEntity}"/>
        /// for each entity type.
        /// </summary>
        public InMemoryRepositoryBuilder WithLifecycle() {
            _enableLifecycle = true;
            UpdateLifecycleRegistration();
            return this;
        }

        /// <summary>
        /// Registers a custom field mapper for a specific entity type.
        /// </summary>
        public InMemoryRepositoryBuilder WithFieldMapper<TEntity, TMapper>(ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TEntity : class
            where TMapper : class, IFieldMapper<TEntity> {
            Services.TryAdd(ServiceDescriptor.Singleton(typeof(IFieldMapper<TEntity>), typeof(TMapper)));
            return this;
        }

        /// <summary>
        /// Implicitly converts to the parent <see cref="RepositoryContextBuilder"/>.
        /// </summary>
        public static implicit operator RepositoryContextBuilder(InMemoryRepositoryBuilder builder) => builder._parent;
    }
}