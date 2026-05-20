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

namespace Deveel.Data {
	/// <summary>
	/// Fluent builder for configuring the In-Memory repository driver.
	/// </summary>
	public class InMemoryDriverBuilder {
		private readonly RepositoryContextBuilder _parent;

		internal InMemoryDriverBuilder(RepositoryContextBuilder parent) {
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
		public InMemoryDriverBuilder WithFieldMapper<TEntity, TMapper>(ServiceLifetime lifetime = ServiceLifetime.Singleton)
			where TEntity : class
			where TMapper : class, IFieldMapper<TEntity> {
			Services.TryAdd(ServiceDescriptor.Singleton(typeof(IFieldMapper<TEntity>), typeof(TMapper)));
			return this;
		}

		/// <summary>
		/// Registers initial data for a specific entity type.
		/// </summary>
		public InMemoryDriverBuilder WithInitialData<TEntity>(IEnumerable<TEntity> data) where TEntity : class {
			Services.AddSingleton<IEnumerable<TEntity>>(data);
			return this;
		}

		/// <summary>
		/// Implicitly converts to the parent <see cref="RepositoryContextBuilder"/>.
		/// </summary>
		public static implicit operator RepositoryContextBuilder(InMemoryDriverBuilder builder) => builder._parent;
	}

	/// <summary>
	/// Extension methods for configuring the In-Memory driver on a <see cref="RepositoryContextBuilder"/>.
	/// </summary>
	public static class RepositoryContextBuilderExtensions {
		/// <summary>
		/// Configures the In-Memory repository driver.
		/// </summary>
		public static InMemoryDriverBuilder UseInMemory(this RepositoryContextBuilder builder) {
			return new InMemoryDriverBuilder(builder);
		}

		/// <summary>
		/// Configures the In-Memory repository driver with a configuration action.
		/// </summary>
		public static RepositoryContextBuilder UseInMemory(this RepositoryContextBuilder builder, Action<InMemoryDriverBuilder> configure) {
			var driverBuilder = new InMemoryDriverBuilder(builder);
			configure(driverBuilder);
			return builder;
		}
	}
}
