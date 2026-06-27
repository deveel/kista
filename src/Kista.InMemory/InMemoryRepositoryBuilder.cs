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