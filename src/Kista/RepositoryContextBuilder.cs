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

using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kista {
	/// <summary>
	/// A fluent builder for configuring a repository context across multiple drivers
	/// and cross-cutting concerns.
	/// </summary>
	public class RepositoryContextBuilder {
		private readonly IServiceCollection _services;
		private readonly HashSet<Type> _registeredRepositoryTypes = new();
		private readonly HashSet<Type> _registeredEntityTypes = new();
		private readonly List<Assembly> _scanAssemblies = new();
		private bool _entityTypesResolved;
		private bool _seedProvidersScanned;

		/// <summary>
		/// Initializes a new instance of the <see cref="RepositoryContextBuilder"/> class.
		/// </summary>
		/// <param name="services">The service collection to configure.</param>
		public RepositoryContextBuilder(IServiceCollection services) {
			_services = services;
		}

		/// <summary>
		/// Gets the underlying service collection for direct registration.
		/// </summary>
		public IServiceCollection Services => _services;

		/// <summary>
		/// Gets the set of repository types that have been explicitly registered.
		/// </summary>
		public IReadOnlyCollection<Type> RegisteredRepositoryTypes => _registeredRepositoryTypes;

		/// <summary>
		/// Gets the set of entity types that have repositories registered.
		/// Computed by scanning the service collection for repository registrations.
		/// </summary>
		public IReadOnlyCollection<Type> RegisteredEntityTypes {
			get {
				ResolveEntityTypes();
				return _registeredEntityTypes;
			}
		}

		/// <summary>
		/// Resolves entity types from the service collection by scanning registered repository types.
		/// </summary>
		private void ResolveEntityTypes() {
			if (_entityTypesResolved) return;

			foreach (var descriptor in _services) {
				var serviceType = descriptor.ServiceType;
				if (!serviceType.IsGenericType) continue;

				var genericDef = serviceType.GetGenericTypeDefinition();
				if (genericDef == typeof(IRepository<>) || genericDef == typeof(IRepository<,>)) {
					var entityType = RepositoryRegistrationUtil.GetEntityType(serviceType);
					if (entityType != null)
						_registeredEntityTypes.Add(entityType);
				}
			}

			_entityTypesResolved = true;
		}

		/// <summary>
		/// Tracks a repository type in the builder's internal collections.
		/// </summary>
		/// <param name="repositoryType">The repository type to track.</param>
		internal void TrackRepositoryType(Type repositoryType) {
			if (_registeredRepositoryTypes.Add(repositoryType)) {
				var entityType = RepositoryRegistrationUtil.GetEntityType(repositoryType);
				if (entityType != null) {
					_registeredEntityTypes.Add(entityType);
					_services.RegisterRepositoryForHealthCheck(repositoryType, entityType);
				}
			}
		}

	/// <summary>
	/// Registers a repository type and returns a <see cref="RepositoryBuilder"/> for
	/// further type-specific configuration (e.g. owner scoping).
	/// </summary>
	public RepositoryBuilder AddRepository<TRepository>(ServiceLifetime lifetime = ServiceLifetime.Scoped) where TRepository : class {
		var repoInterface = FindRepositoryInterface(typeof(TRepository));
		var serviceTypes = RepositoryRegistrationUtil.GetRepositoryServiceTypes(typeof(TRepository));
		foreach (var serviceType in serviceTypes) {
			_services.TryAdd(new ServiceDescriptor(serviceType, typeof(TRepository), lifetime));
		}
		_services.TryAdd(new ServiceDescriptor(typeof(TRepository), typeof(TRepository), lifetime));
		TrackRepositoryType(typeof(TRepository));

		var entityType = repoInterface.GetGenericArguments()[0];
		var keyType = repoInterface.GetGenericArguments()[1];

		return new RepositoryBuilder(Services, entityType, keyType, typeof(TRepository), repoInterface);
	}

	/// <summary>
	/// Registers a repository type and configures it via a delegate,
	/// returning to the context builder for further chaining.
	/// </summary>
	public RepositoryContextBuilder AddRepository<TRepository>(Action<RepositoryBuilder> configure, ServiceLifetime lifetime = ServiceLifetime.Scoped) where TRepository : class {
		var repo = AddRepository<TRepository>(lifetime);
		configure(repo);
		return this;
	}

	/// <summary>
	/// Registers a repository type in the service collection and tracks it.
	/// </summary>
	public RepositoryContextBuilder AddRepository(Type repositoryType, ServiceLifetime lifetime = ServiceLifetime.Scoped) {
		if (repositoryType.IsGenericTypeDefinition) {
			RegisterOpenGenericRepository(repositoryType, lifetime);
		} else {
			if (!RepositoryRegistrationUtil.IsValidRepositoryType(repositoryType))
				throw new ArgumentException($"The type '{repositoryType}' is not a valid repository type", nameof(repositoryType));

			var serviceTypes = RepositoryRegistrationUtil.GetRepositoryServiceTypes(repositoryType);

			foreach (var serviceType in serviceTypes) {
				_services.TryAdd(new ServiceDescriptor(serviceType, repositoryType, lifetime));
			}

			_services.Add(new ServiceDescriptor(repositoryType, repositoryType, lifetime));
		}
		TrackRepositoryType(repositoryType);
		return this;
	}

	private static Type FindRepositoryInterface(Type repositoryType) {
		foreach (var iface in repositoryType.GetInterfaces()) {
			if (iface.IsGenericType &&
				iface.GetGenericTypeDefinition() == typeof(IRepository<,>)) {
				return iface;
			}
		}

		throw new InvalidOperationException(
			$"The type '{repositoryType}' does not implement IRepository<,>");
	}

		/// <summary>
		/// Registers an open generic repository type with the service collection.
		/// </summary>
		/// <param name="repositoryType">The open generic repository type to register.</param>
		/// <param name="lifetime">The service lifetime for the registration.</param>
		private void RegisterOpenGenericRepository(Type repositoryType, ServiceLifetime lifetime) {
			var serviceTypes = RepositoryScanner.GetServiceTypes(repositoryType);
			foreach (var serviceType in serviceTypes) {
				_services.TryAdd(ServiceDescriptor.Describe(serviceType, repositoryType, lifetime));
			}
		}

		/// <summary>
		/// Scans the given assemblies for repository types and registers them.
		/// Open generic repositories are registered as open generics;
		/// closed repositories are registered via their service interfaces.
		/// </summary>
		public RepositoryContextBuilder ScanRepositories(params Assembly[] assemblies) {
			foreach (var assembly in assemblies) {
				if (_scanAssemblies.Contains(assembly)) continue;
				_scanAssemblies.Add(assembly);
				RepositoryScanner.Scan(assembly, _services, this);
			}
			return this;
		}

		/// <summary>
		/// Registers a seed data provider for the given entity type that will be
		/// used by the lifecycle orchestrator during repository seeding.
		/// </summary>
		/// <typeparam name="TEntity">The type of the entity to seed.</typeparam>
		/// <typeparam name="TProvider">
		/// The type of the <see cref="IRepositorySeedDataProvider{TEntity}"/> implementation.
		/// </typeparam>
		/// <param name="lifetime">The service lifetime (default: <see cref="ServiceLifetime.Singleton"/>).</param>
		/// <returns>The same builder for chaining.</returns>
		public RepositoryContextBuilder WithSeedData<TEntity, TProvider>(ServiceLifetime lifetime = ServiceLifetime.Singleton)
			where TEntity : class
			where TProvider : class, IRepositorySeedDataProvider<TEntity> {

			_services.Add(ServiceDescriptor.Describe(
				typeof(IRepositorySeedDataProvider<TEntity>),
				typeof(TProvider),
				lifetime));

			return this;
		}

		/// <summary>
		/// Registers inline seed data for the given entity type that will be
		/// used by the lifecycle orchestrator during repository seeding.
		/// </summary>
		/// <typeparam name="TEntity">The type of the entity to seed.</typeparam>
		/// <param name="data">The seed data to register.</param>
		/// <returns>The same builder for chaining.</returns>
		public RepositoryContextBuilder WithSeedData<TEntity>(IEnumerable<TEntity> data)
			where TEntity : class {

			_services.AddSingleton<IRepositorySeedDataProvider<TEntity>>(
				new CollectionSeedDataProvider<TEntity>(data));

			return this;
		}

		/// <summary>
		/// Scans the given assemblies for types implementing
		/// <see cref="IRepositorySeedDataProvider{TEntity}"/> and registers them
		/// as seed data providers in the service collection.
		/// </summary>
		/// <param name="assemblies">
		/// The assemblies to scan. If none are provided, the entry assembly is used.
		/// </param>
		/// <returns>The same builder for chaining.</returns>
		public RepositoryContextBuilder WithSeedDataFrom(params Assembly[] assemblies) {
			if (assemblies.Length == 0)
				assemblies = [Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly()];

			foreach (var assembly in assemblies) {
				if (assembly == null) continue;

				var providerTypes = assembly.GetTypes()
					.Where(t => !t.IsAbstract && !t.IsInterface && !t.IsGenericTypeDefinition)
					.Where(t => t.GetInterfaces()
						.Any(i => i.IsGenericType &&
								  i.GetGenericTypeDefinition() == typeof(IRepositorySeedDataProvider<>)));

				foreach (var type in providerTypes) {
					foreach (var iface in type.GetInterfaces()
						.Where(i => i.IsGenericType &&
									i.GetGenericTypeDefinition() == typeof(IRepositorySeedDataProvider<>))) {

						_services.TryAdd(ServiceDescriptor.Describe(iface, type, ServiceLifetime.Singleton));
					}
				}
			}

			_seedProvidersScanned = true;
			return this;
		}

		/// <summary>
		/// Ensures that seed data providers have been scanned from the entry assembly
		/// if the user did not explicitly call <see cref="WithSeedDataFrom(Assembly[])"/>
		/// or register providers via <see cref="WithSeedData{TEntity, TProvider}(ServiceLifetime)"/>.
		/// Called automatically by <c>ConfigureLifecycle</c>.
		/// </summary>
		internal void EnsureSeedProvidersScanned() {
			if (!_seedProvidersScanned)
				WithSeedDataFrom();
		}

		internal class CollectionSeedDataProvider<TEntity> : IRepositorySeedDataProvider<TEntity>
			where TEntity : class {

			private readonly IEnumerable<TEntity> data;

			public CollectionSeedDataProvider(IEnumerable<TEntity> data) {
				this.data = data;
			}

			/// <inheritdoc/>
			public IEnumerable<TEntity> GetSeedData() => data;

			IEnumerable<object> IRepositorySeedDataProvider.GetSeedData()
				=> data.Cast<object>();
		}
	}
}
