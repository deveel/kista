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

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Deveel.Data {
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

		internal void TrackRepositoryType(Type repositoryType) {
			if (_registeredRepositoryTypes.Add(repositoryType)) {
				var entityType = RepositoryRegistrationUtil.GetEntityType(repositoryType);
				if (entityType != null)
					_registeredEntityTypes.Add(entityType);
			}
		}

		/// <summary>
		/// Registers a repository type in the service collection and tracks it.
		/// </summary>
		public RepositoryContextBuilder AddRepository<TRepository>(ServiceLifetime lifetime = ServiceLifetime.Scoped) {
			_services.AddRepository(typeof(TRepository), lifetime);
			TrackRepositoryType(typeof(TRepository));
			return this;
		}

		/// <summary>
		/// Registers a repository type in the service collection and tracks it.
		/// </summary>
		public RepositoryContextBuilder AddRepository(Type repositoryType, ServiceLifetime lifetime = ServiceLifetime.Scoped) {
			if (repositoryType.IsGenericTypeDefinition) {
				RegisterOpenGenericRepository(repositoryType, lifetime);
			} else {
				_services.AddRepository(repositoryType, lifetime);
			}
			TrackRepositoryType(repositoryType);
			return this;
		}

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

	}
}
