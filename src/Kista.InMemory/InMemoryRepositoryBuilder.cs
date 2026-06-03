using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kista
{
    /// <summary>
    /// Fluent builder for configuring the In-Memory repository driver.
    /// </summary>
    public class InMemoryRepositoryBuilder : RepositoryDriverBuilder {
        internal InMemoryRepositoryBuilder(RepositoryContextBuilder parent)
            : base(parent, enableLifecycle: false) {
            RegisterOpenGenerics();
        }

        private void RegisterOpenGenerics() {
            Parent.AddRepository(typeof(InMemoryRepository<>), ServiceLifetime.Singleton);
            Parent.AddRepository(typeof(InMemoryRepository<,>), ServiceLifetime.Singleton);
            UpdateLifecycleRegistration();
        }

        private void UpdateLifecycleRegistration() {
            RegisterLifecycleHandler(typeof(InMemoryRepositoryLifecycleHandler<>));
        }

        /// <summary>
        /// Enables lifecycle support for the In-Memory repositories,
        /// registering a <see cref="InMemoryRepositoryLifecycleHandler{TEntity}"/>
        /// for each entity type.
        /// </summary>
        public new InMemoryRepositoryBuilder WithLifecycle() {
            base.WithLifecycle<InMemoryRepositoryBuilder>();
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
        public static implicit operator RepositoryContextBuilder(InMemoryRepositoryBuilder builder) => builder.Parent;
    }
}