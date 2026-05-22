using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Deveel.Data
{
    /// <summary>
    /// Fluent builder for configuring the In-Memory repository driver.
    /// </summary>
    public class InMemoryRepositoryBuilder {
        private readonly RepositoryContextBuilder _parent;

        internal InMemoryRepositoryBuilder(RepositoryContextBuilder parent) {
            _parent = parent;
            RegisterOpenGenerics();
        }

        /// <summary>
        /// Gets the underlying service collection.
        /// </summary>
        public IServiceCollection Services => _parent.Services;

        /// <summary>
        /// Returns to the parent <see cref="RepositoryContextBuilder"/>.
        /// </summary>
        public RepositoryContextBuilder ToParent() => _parent;

        private void RegisterOpenGenerics() {
            _parent.AddRepository(typeof(InMemoryRepository<>), ServiceLifetime.Singleton);
            _parent.AddRepository(typeof(InMemoryRepository<,>), ServiceLifetime.Singleton);
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
        /// Registers initial data for a specific entity type.
        /// </summary>
        public InMemoryRepositoryBuilder WithInitialData<TEntity>(IEnumerable<TEntity> data) where TEntity : class {
            Services.AddSingleton<IEnumerable<TEntity>>(data);
            return this;
        }

        /// <summary>
        /// Implicitly converts to the parent <see cref="RepositoryContextBuilder"/>.
        /// </summary>
        public static implicit operator RepositoryContextBuilder(InMemoryRepositoryBuilder builder) => builder._parent;
    }
}