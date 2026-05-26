using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Kista {
	/// <summary>
	/// Default implementation of <see cref="IRepositoryLifecycleProfile"/> that provides
	/// sensible environment-specific seed strategies and resolves seed data from
	/// registered <see cref="IRepositorySeedDataProvider{TEntity}"/> services.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The default seed strategy per environment is:
	/// <list type="bullet">
	///   <item><description><c>Development</c> — <see cref="SeedStrategy.Always"/></description></item>
	///   <item><description><c>Staging</c> — <see cref="SeedStrategy.IfMissing"/></description></item>
	///   <item><description><c>Testing</c>, <c>Test</c> — <see cref="SeedStrategy.Always"/></description></item>
	///   <item><description><c>Production</c> — <see cref="SeedStrategy.Never"/></description></item>
	///   <item><description>Unknown — <see cref="SeedStrategy.Always"/></description></item>
	/// </list>
	/// </para>
	/// <para>
	/// Seed data is resolved by asking the service provider for a registered
	/// <see cref="IRepositorySeedDataProvider{TEntity}"/> for each entity type.
	/// </para>
	/// </remarks>
	public class DefaultRepositoryLifecycleProfile : IRepositoryLifecycleProfile {
		private readonly IServiceProvider _serviceProvider;
		private readonly ConcurrentDictionary<Type, object?> _seedDataCache = new();

		/// <summary>
		/// Creates a new instance of the default lifecycle profile.
		/// </summary>
		/// <param name="serviceProvider">The service provider used to resolve seed data providers.</param>
		public DefaultRepositoryLifecycleProfile(IServiceProvider serviceProvider) {
			_serviceProvider = serviceProvider;
		}

		/// <inheritdoc/>
		public virtual SeedStrategy GetSeedStrategy(string? environmentName = null) {
			if (string.IsNullOrWhiteSpace(environmentName))
				return SeedStrategy.Always;

			return environmentName.ToLowerInvariant() switch {
				"development" or "dev" => SeedStrategy.Always,
				"staging" or "stage" => SeedStrategy.IfMissing,
				"testing" or "test" => SeedStrategy.Always,
				"production" or "prod" => SeedStrategy.Never,
				_ => SeedStrategy.Always
			};
		}

		/// <inheritdoc/>
		public virtual object? GetSeedData(Type entityType) {
			if (entityType == null)
				throw new ArgumentNullException(nameof(entityType));

			return _seedDataCache.GetOrAdd(entityType, ResolveSeedData);
		}

		private object? ResolveSeedData(Type entityType) {
			var providerType = typeof(IRepositorySeedDataProvider<>).MakeGenericType(entityType);
			var provider = _serviceProvider.GetService(providerType);
			if (provider == null)
				return null;

			var method = providerType.GetMethod(nameof(IRepositorySeedDataProvider<object>.GetSeedData), BindingFlags.Public | BindingFlags.Instance);
			return method?.Invoke(provider, null);
		}
	}
}
