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
	/// Scans assemblies for repository types and registers them in the service collection.
	/// </summary>
	static class RepositoryScanner {
		private static readonly Type[] _repoInterfaces = [
			typeof(IRepository<>),
			typeof(IQueryableRepository<>),
			typeof(IFilterableRepository<>),
			typeof(IPageableRepository<>),
			typeof(IRepository<,>),
			typeof(IQueryableRepository<,>),
			typeof(IFilterableRepository<,>),
			typeof(IPageableRepository<,>)
		];

		/// <summary>
		/// Scans the given assembly for repository types and registers them in the service collection.
		/// </summary>
		/// <param name="assembly">The assembly to scan for repository types.</param>
		/// <param name="services">The service collection to register repositories into.</param>
		/// <param name="builder">The builder to track registered repository types.</param>
		public static void Scan(Assembly assembly, IServiceCollection services, RepositoryContextBuilder builder) {
			var candidateTypes = assembly.GetTypes()
				.Where(t => !t.IsAbstract && !t.IsInterface)
				.Where(t => t.GetCustomAttributes(typeof(ExcludeFromScanAttribute), inherit: false).Length == 0)
				.Where(RepositoryRegistrationUtil.IsValidRepositoryType);

			foreach (var type in candidateTypes) {
				if (type.IsGenericTypeDefinition) {
					RegisterOpenGeneric(type, services);
				} else {
					RegisterClosedType(type, services);
				}

				builder.TrackRepositoryType(type);
			}
		}

		internal static IEnumerable<Type> GetServiceTypes(Type type) {
			var serviceTypes = new HashSet<Type>();
			var genericArgCount = type.GetGenericArguments().Length;

			if (type.IsGenericTypeDefinition) {
				foreach (var iface in _repoInterfaces) {
					var expectedArgCount = iface.GetGenericArguments().Length;
					if (expectedArgCount != genericArgCount)
						continue;

					if (type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == iface)) {
						serviceTypes.Add(iface);
					}
				}
			} else {
				foreach (var st in RepositoryRegistrationUtil.GetRepositoryServiceTypes(type)) {
					serviceTypes.Add(st);
				}
			}

			serviceTypes.Add(type);
			return serviceTypes;
		}

		private static void RegisterOpenGeneric(Type openGenericType, IServiceCollection services) {
			var serviceTypes = GetServiceTypes(openGenericType);
			foreach (var serviceType in serviceTypes) {
				services.TryAdd(ServiceDescriptor.Describe(serviceType, openGenericType, ServiceLifetime.Scoped));
			}
		}

		private static void RegisterClosedType(Type repositoryType, IServiceCollection services) {
			var serviceTypes = RepositoryRegistrationUtil.GetRepositoryServiceTypes(repositoryType);
			foreach (var serviceType in serviceTypes) {
				services.TryAdd(ServiceDescriptor.Describe(serviceType, repositoryType, ServiceLifetime.Scoped));
			}
			services.TryAdd(ServiceDescriptor.Describe(repositoryType, repositoryType, ServiceLifetime.Scoped));
		}
	}

	/// <summary>
	/// Attribute to exclude a type from assembly scanning.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = false)]
	public sealed class ExcludeFromScanAttribute : Attribute {
	}
}
