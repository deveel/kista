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

namespace Kista {
	/// <summary>
/// Utility class for repository type registration and validation.
/// </summary>
static class RepositoryRegistrationUtil {
/// <summary>
/// Determines whether the specified type is a valid repository type by checking
/// if it implements <see cref="IRepository{TEntity}"/> or <see cref="IRepository{TEntity,TKey}"/>.
/// </summary>
/// <param name="repositoryType">The type to check.</param>
/// <returns>True if the type is a valid repository type; otherwise, false.</returns>
public static bool IsValidRepositoryType(Type repositoryType)
		=> Implements(typeof(IRepository<>), repositoryType) ||
			Implements(typeof(IRepository<,>), repositoryType);


		/// <summary>
/// Checks whether a type implements a specific generic type, either directly or through inheritance.
/// </summary>
/// <param name="genericType">The generic type definition to check for.</param>
/// <param name="type">The type to examine.</param>
/// <returns>True if the type implements the generic type; otherwise, false.</returns>
public static bool Implements(Type genericType, Type type) {
			if (type.IsGenericType) {
				var genericTypeDefinition = type.GetGenericTypeDefinition();
				if (genericTypeDefinition == genericType)
					return true;
			}

			foreach (var iface in type.GetInterfaces()) {
				if (Implements(genericType, iface))
					return true;
			}

			var baseType = type.BaseType;
			while (baseType != null) {
				if (Implements(genericType, baseType))
					return true;

				baseType = baseType.BaseType;
			}

			return false;
		}

		/// <summary>
/// Extracts the entity type from a repository service type.
/// </summary>
/// <param name="serviceType">The repository service type (e.g., IRepository&lt;TEntity&gt;).</param>
/// <returns>The entity type if found; otherwise, null.</returns>
internal static Type? GetEntityType(Type serviceType) {
			if (serviceType.IsGenericType) {
				var genericTypeDefinition = serviceType.GetGenericTypeDefinition();
				var genericTypes = serviceType.GenericTypeArguments;

				if (genericTypes.Length == 1 && genericTypes[0].IsClass &&
					typeof(IRepository<>).IsAssignableFrom(genericTypeDefinition)) {
					return genericTypes[0];
				} else if (genericTypes.Length == 2 && genericTypes[0].IsClass &&
					typeof(IRepository<,>).IsAssignableFrom(genericTypeDefinition)) {
					return genericTypes[0];
				}
			}

			foreach (var iface in serviceType.GetInterfaces()) {
				var entityType = GetEntityType(iface);
				if (entityType != null)
					return entityType;
			}

			return null;
		}

		/// <summary>
/// Extracts the key type from a repository service type.
/// </summary>
/// <param name="serviceType">The repository service type (e.g., IRepository&lt;TEntity, TKey&gt;).</param>
/// <returns>The key type if found; otherwise, null.</returns>
internal static Type? GetKeyType(Type serviceType) {
			if (serviceType.IsGenericType) {
				var genericTypeDefinition = serviceType.GetGenericTypeDefinition();
				var genericTypes = serviceType.GenericTypeArguments;

				if (genericTypes.Length == 2 && genericTypes[0].IsClass &&
					typeof(IRepository<,>).IsAssignableFrom(genericTypeDefinition))
					return genericTypes[1];
			}

			foreach (var iface in serviceType.GetInterfaces()) {
				var keyType = GetKeyType(iface);
				if (keyType != null)
					return keyType;
			}

			return null;
		}

		/// <summary>
/// Registers a service type if the repository type implements the generic repository interface.
/// </summary>
/// <param name="types">The collection to add service types to.</param>
/// <param name="genericType">The generic repository interface type (e.g., IRepository&lt;&gt;).</param>
/// <param name="entityType">The entity type to use for the generic argument.</param>
/// <param name="repositoryType">The repository type to check.</param>
/// <returns>True if the type was registered; otherwise, false.</returns>
private static bool RegisterIfAssignable(IList<Type> types, Type genericType, Type entityType, Type repositoryType) {
			var serviceType = genericType.MakeGenericType(entityType);
			if (serviceType.IsAssignableFrom(repositoryType)) {
				if (!types.Contains(serviceType))
					types.Add(serviceType);

				return true;
			}

			return false;
		}

		/// <summary>
/// Registers a service type if the repository type implements the generic repository interface with entity and key types.
/// </summary>
/// <param name="types">The collection to add service types to.</param>
/// <param name="genericType">The generic repository interface type (e.g., IRepository&lt;,&gt;).</param>
/// <param name="entityType">The entity type to use for the generic argument.</param>
/// <param name="keyType">The key type to use for the generic argument.</param>
/// <param name="repositoryType">The repository type to check.</param>
/// <returns>True if the type was registered; otherwise, false.</returns>
private static bool RegisterIfAssignable(IList<Type> types, Type genericType, Type entityType, Type keyType, Type repositoryType) {
			var serviceType = genericType.MakeGenericType(entityType, keyType);
			if (serviceType.IsAssignableFrom(repositoryType)) {
				if (!types.Contains(serviceType))
					types.Add(serviceType);

				return true;
			}

			return false;
		}


		/// <summary>
		/// Gets all the service types that a repository type should be registered as.
		/// </summary>
		/// <param name="repositoryType">The repository type to analyze.</param>
		/// <returns>A read-only list of service types the repository implements.</returns>
		public static IReadOnlyList<Type> GetRepositoryServiceTypes(Type repositoryType) {
			if (!Implements(typeof(IRepository<>), repositoryType) &&
				!Implements(typeof(IRepository<,>), repositoryType))
				return Array.Empty<Type>();

			var types = new List<Type>();

			foreach (var iface in repositoryType.GetInterfaces()) {
				RegisterSingleTypeInterfaces(types, iface, repositoryType);
			}

			RegisterBaseTypes(types, repositoryType);

			return types.AsReadOnly();
		}

		private static void RegisterSingleTypeInterfaces(IList<Type> types, Type iface, Type repositoryType) {
			var entityType = GetEntityType(iface);
			if (entityType == null)
				return;

			if (RegisterIfAssignable(types, typeof(IRepository<>), entityType, repositoryType)) {
				RegisterIfAssignable(types, typeof(IQueryableRepository<>), entityType, repositoryType);
				RegisterIfAssignable(types, typeof(IFilterableRepository<>), entityType, repositoryType);
				RegisterIfAssignable(types, typeof(IPageableRepository<>), entityType, repositoryType);
				AddIfMissing(types, iface);
			}

			var keyType = GetKeyType(iface);
			if (keyType == null)
				return;

			if (RegisterIfAssignable(types, typeof(IRepository<,>), entityType, keyType, repositoryType)) {
				RegisterIfAssignable(types, typeof(IQueryableRepository<,>), entityType, keyType, repositoryType);
				RegisterIfAssignable(types, typeof(IFilterableRepository<,>), entityType, keyType, repositoryType);
				RegisterIfAssignable(types, typeof(IPageableRepository<,>), entityType, keyType, repositoryType);
				AddIfMissing(types, iface);
			}
		}

		private static void RegisterBaseTypes(IList<Type> types, Type repositoryType) {
			var baseType = repositoryType.BaseType;
			while (baseType != null) {
				if (Implements(typeof(IRepository<>), baseType))
					AddIfMissing(types, baseType);
				if (Implements(typeof(IRepository<,>), baseType))
					AddIfMissing(types, baseType);
				baseType = baseType.BaseType;
			}
		}

		private static void AddIfMissing(IList<Type> types, Type type) {
			if (!types.Contains(type))
				types.Add(type);
		}
	}
}