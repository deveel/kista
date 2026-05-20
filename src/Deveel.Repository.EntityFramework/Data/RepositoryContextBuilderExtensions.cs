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

namespace Deveel.Data {
	/// <summary>
	/// Fluent builder for configuring the Entity Framework Core repository driver.
	/// </summary>
	public class EntityFrameworkBuilder {
		private readonly RepositoryContextBuilder _parent;
		private readonly Type _dbContextType;
		private Action<DbContextOptionsBuilder>? _dbContextConfig;
		private ServiceLifetime _lifetime = ServiceLifetime.Scoped;

		internal EntityFrameworkBuilder(RepositoryContextBuilder parent, Type dbContextType) {
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
		/// Returns to the parent <see cref="RepositoryContextBuilder"/>.
		/// </summary>
		public RepositoryContextBuilder ToParent() => _parent;

		/// <summary>
		/// Configures the DbContext options.
		/// </summary>
		public EntityFrameworkBuilder ConfigureDbContext(Action<DbContextOptionsBuilder> configure) {
			_dbContextConfig = configure;
			return this;
		}

		/// <summary>
		/// Sets the service lifetime for the DbContext and repositories.
		/// </summary>
		public EntityFrameworkBuilder WithLifetime(ServiceLifetime lifetime) {
			_lifetime = lifetime;
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
		public static implicit operator RepositoryContextBuilder(EntityFrameworkBuilder builder) {
			builder.FinalizeRegistration();
			return builder._parent;
		}
	}

	/// <summary>
	/// Extension methods for configuring the Entity Framework Core driver on a <see cref="RepositoryContextBuilder"/>.
	/// </summary>
	public static class RepositoryContextBuilderExtensions {
		/// <summary>
		/// Configures the Entity Framework Core repository driver.
		/// </summary>
		public static EntityFrameworkBuilder UseEntityFramework<TDbContext>(this RepositoryContextBuilder builder)
			where TDbContext : DbContext {
			return new EntityFrameworkBuilder(builder, typeof(TDbContext));
		}

		/// <summary>
		/// Configures the Entity Framework Core repository driver with a configuration action.
		/// </summary>
		public static RepositoryContextBuilder UseEntityFramework<TDbContext>(this RepositoryContextBuilder builder, Action<EntityFrameworkBuilder> configure)
			where TDbContext : DbContext {
			var driverBuilder = new EntityFrameworkBuilder(builder, typeof(TDbContext));
			configure(driverBuilder);
			driverBuilder.FinalizeRegistration();
			return builder;
		}
	}
}
