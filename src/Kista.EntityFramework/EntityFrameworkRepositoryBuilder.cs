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

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kista
{
    /// <summary>
    /// Fluent builder for configuring the Entity Framework Core repository driver.
    /// </summary>
    public class EntityFrameworkRepositoryBuilder : RepositoryDriverBuilder {
        private readonly Type _dbContextType;
        private Action<DbContextOptionsBuilder>? _dbContextConfig;
        private ServiceLifetime _lifetime = ServiceLifetime.Scoped;

        internal EntityFrameworkRepositoryBuilder(RepositoryContextBuilder parent, Type dbContextType)
            : base(parent) {
            _dbContextType = dbContextType;
        }

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
        public new EntityFrameworkRepositoryBuilder WithLifecycle() {
            base.WithLifecycle<EntityFrameworkRepositoryBuilder>();
            return this;
        }

        /// <summary>
        /// Disables lifecycle support for the Entity Framework repositories.
        /// </summary>
        public new EntityFrameworkRepositoryBuilder WithoutLifecycle() {
            base.WithoutLifecycle<EntityFrameworkRepositoryBuilder>();
            return this;
        }

        /// <summary>
        /// Registers soft-delete configuration for the Entity Framework
        /// driver. Soft-delete filtering activates automatically for any
        /// entity implementing <see cref="ISoftDeletable"/>: this call
        /// is reserved for future configuration knobs.
        /// </summary>
        /// <param name="configure">
        /// An optional delegate to configure the <see cref="SoftDeleteOptions"/>.
        /// </param>
        /// <returns>
        /// Returns the same builder for chaining.
        /// </returns>
        /// <remarks>
        /// <para>
        /// To enable the EF Core global query filter, call
        /// <see cref="SoftDeleteModelBuilderExtensions.HasSoftDeleteFilter(ModelBuilder)"/>
        /// in your <c>OnModelCreating</c> override.
        /// </para>
        /// </remarks>
        public EntityFrameworkRepositoryBuilder WithSoftDelete(Action<SoftDeleteOptions>? configure = null) {
            Parent.WithSoftDelete(configure);
            return this;
        }

        internal void FinalizeRegistration() {
            if (_dbContextConfig != null) {
                RegisterDbContext(Parent.Services, _dbContextConfig, _lifetime);
            } else {
                RegisterDbContext(Parent.Services, _lifetime);
            }

            Parent.AddRepository(typeof(EntityRepository<>), _lifetime);
            Parent.AddRepository(typeof(EntityRepository<,>), _lifetime);

            RegisterLifecycleHandler(typeof(EntityFrameworkRepositoryLifecycleHandler<>));
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
            return Parent;
        }

        /// <summary>
        /// Implicitly converts to the parent <see cref="RepositoryContextBuilder"/>.
        /// </summary>
        public static implicit operator RepositoryContextBuilder(EntityFrameworkRepositoryBuilder builder) {
            builder.FinalizeRegistration();
            return builder.Parent;
        }
    }
}