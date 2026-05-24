using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Deveel.Data
{
    /// <summary>
    /// Fluent builder for configuring the Entity Framework Core repository driver.
    /// </summary>
    public class EntityFrameworkRepositoryBuilder {
        private readonly RepositoryContextBuilder _parent;
        private readonly Type _dbContextType;
        private Action<DbContextOptionsBuilder>? _dbContextConfig;
        private ServiceLifetime _lifetime = ServiceLifetime.Scoped;
        private bool _enableLifecycle = true;

        internal EntityFrameworkRepositoryBuilder(RepositoryContextBuilder parent, Type dbContextType) {
            _parent = parent;
            _dbContextType = dbContextType;
        }

        /// <summary>
        /// Gets the underlying service collection.
        /// </summary>
        public IServiceCollection Services => _parent.Services;

        /// <summary>
        /// Gets the DbContext type being configured.
        /// </summary>
        public Type DbContextType => _dbContextType;

        /// <summary>
        /// Configures the DbContext options.
        /// </summary>
        public EntityFrameworkRepositoryBuilder ConfigureDbContext(Action<DbContextOptionsBuilder> configure) {
            _dbContextConfig = configure;
            return this;
        }

        /// <summary>
        /// Sets the service lifetime for the DbContext and repositories.
        /// </summary>
        public EntityFrameworkRepositoryBuilder WithLifetime(ServiceLifetime lifetime) {
            _lifetime = lifetime;
            return this;
        }

        /// <summary>
        /// Enables lifecycle support for the Entity Framework repositories,
        /// registering a <see cref="EntityFrameworkRepositoryLifecycleHandler{TEntity}"/>
        /// for each entity type.
        /// </summary>
        public EntityFrameworkRepositoryBuilder WithLifecycle() {
            _enableLifecycle = true;
            return this;
        }

        /// <summary>
        /// Disables lifecycle support for the Entity Framework repositories.
        /// </summary>
        public EntityFrameworkRepositoryBuilder WithoutLifecycle() {
            _enableLifecycle = false;
            return this;
        }

        internal void FinalizeRegistration() {
            if (_dbContextConfig != null) {
                RegisterDbContext(_parent.Services, _dbContextConfig, _lifetime);
            } else {
                RegisterDbContext(_parent.Services, _lifetime);
            }

            _parent.AddRepository(typeof(EntityRepository<>), _lifetime);
            _parent.AddRepository(typeof(EntityRepository<,>), _lifetime);

            if (_enableLifecycle) {
                _parent.Services.TryAddTransient(typeof(IRepositoryLifecycleHandler<>), typeof(EntityFrameworkRepositoryLifecycleHandler<>));
            }
        }

        private void RegisterDbContext(IServiceCollection services, Action<DbContextOptionsBuilder> configure, ServiceLifetime lifetime) {
            var method = typeof(EntityFrameworkServiceCollectionExtensions)
                .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .First(m => m.Name == nameof(EntityFrameworkServiceCollectionExtensions.AddDbContext) &&
                            m.IsGenericMethodDefinition &&
                            m.GetParameters().Length == 4 &&
                            m.GetParameters()[1].ParameterType == typeof(Action<DbContextOptionsBuilder>));

            method.MakeGenericMethod(_dbContextType)
                .Invoke(null, new object[] { services, configure, lifetime, lifetime });
        }

        private void RegisterDbContext(IServiceCollection services, ServiceLifetime lifetime) {
            var method = typeof(EntityFrameworkServiceCollectionExtensions)
                .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .First(m => m.Name == nameof(EntityFrameworkServiceCollectionExtensions.AddDbContext) &&
                            m.IsGenericMethodDefinition &&
                            m.GetParameters().Length == 3);

            method.MakeGenericMethod(_dbContextType)
                .Invoke(null, new object[] { services, lifetime, lifetime });
        }

        /// <summary>
        /// Finalizes the Entity Framework Core driver registration.
        /// </summary>
        public RepositoryContextBuilder Build() {
            FinalizeRegistration();
            return _parent;
        }

        /// <summary>
        /// Implicitly converts to the parent <see cref="RepositoryContextBuilder"/>.
        /// </summary>
        public static implicit operator RepositoryContextBuilder(EntityFrameworkRepositoryBuilder builder) {
            builder.FinalizeRegistration();
            return builder._parent;
        }
    }
}