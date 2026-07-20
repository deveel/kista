using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kista;

/// <summary>
/// Shared test helpers for registering entity managers and validators
/// via reflection, used by the abstract <see cref="EntityManagerTestSuite{TManager, TPerson}"/>
/// and <see cref="EntityManagerTestSuite{TManager, TPerson, TKey}"/> suites
/// to avoid duplicating registration logic.
/// </summary>
internal static class TestServiceRegistration {
	/// <summary>
	/// Registers a custom <see cref="EntityManager{TEntity}"/> subclass
	/// by walking its base types to find the matching
	/// <see cref="EntityManager{TEntity}"/> or <see cref="EntityManager{TEntity, TKey}"/>
	/// generic definition, and registering the manager for each.
	/// </summary>
	public static void RegisterManager(IServiceCollection services, Type managerType) {
		if (!managerType.IsClass || managerType.IsAbstract)
			throw new ArgumentException($"The type {managerType} is not a concrete class");

		var serviceTypes = CollectManagerServiceTypes(managerType);

		if (serviceTypes.Count == 0)
			throw new ArgumentException($"The type {managerType} is not a valid manager type");

		if (!serviceTypes.Contains(managerType))
			serviceTypes.Add(managerType);

		foreach (var serviceType in serviceTypes) {
			if (serviceType == managerType) {
				services.Add(ServiceDescriptor.Describe(serviceType, managerType, ServiceLifetime.Scoped));
			} else {
				services.TryAdd(ServiceDescriptor.Describe(serviceType, managerType, ServiceLifetime.Scoped));
			}
		}
	}

	/// <summary>
	/// Registers an entity validator by scanning its implemented
	/// <see cref="IEntityValidator{TEntity}"/> and
	/// <see cref="IEntityValidator{TEntity, TKey}"/> interfaces
	/// and registering the validator for each matching interface type.
	/// </summary>
	public static void RegisterValidator(IServiceCollection services, Type validatorType) {
		if (!validatorType.IsClass || validatorType.IsAbstract)
			throw new ArgumentException($"The type {validatorType} is not a concrete class");

		foreach (var iface in validatorType.GetInterfaces()) {
			if (!iface.IsGenericType) continue;
			var def = iface.GetGenericTypeDefinition();
			if (def == typeof(IEntityValidator<>)) {
				var compareType = typeof(IEntityValidator<>).MakeGenericType(iface.GetGenericArguments()[0]);
				services.TryAdd(new ServiceDescriptor(compareType, validatorType, ServiceLifetime.Transient));
			} else if (def == typeof(IEntityValidator<,>)) {
				var args = iface.GetGenericArguments();
				var compareType = typeof(IEntityValidator<,>).MakeGenericType(args[0], args[1]);
				services.TryAdd(new ServiceDescriptor(compareType, validatorType, ServiceLifetime.Transient));
			}
		}

		services.Add(new ServiceDescriptor(validatorType, validatorType, ServiceLifetime.Transient));
	}

	private static List<Type> CollectManagerServiceTypes(Type managerType) {
		var serviceTypes = new List<Type>();
		var baseType = managerType;
		while (baseType != null) {
			if (baseType.IsGenericType) {
				var genericType = baseType.GetGenericTypeDefinition();
				var genericArgs = baseType.GetGenericArguments();

				if (genericType == typeof(EntityManager<>)) {
					serviceTypes.Add(genericType.MakeGenericType(genericArgs[0]));
				} else if (genericType == typeof(EntityManager<,>)) {
					serviceTypes.Add(genericType.MakeGenericType(genericArgs[0], genericArgs[1]));
				}
			}
			baseType = baseType.BaseType;
		}
		return serviceTypes;
	}
}