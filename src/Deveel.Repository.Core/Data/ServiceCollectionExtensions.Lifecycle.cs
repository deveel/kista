using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Deveel.Data {
	// Partial class: XML docs are on the main declaration in ServiceCollectionExtensions.cs
	public static partial class ServiceCollectionExtensions {
		/// <summary>
		/// Registers the repository lifecycle orchestrator (<see cref="IRepositoryLifecycleOrchestrator"/>)
		/// as a singleton service, with optional configuration of <see cref="RepositoryLifecycleOptions"/>.
		/// </summary>
		/// <param name="services">The service collection to register into.</param>
		/// <param name="configure">An optional delegate to configure lifecycle options.</param>
		/// <returns>The same service collection for chaining.</returns>
		public static IServiceCollection AddRepositoryLifecycleOrchestrator(this IServiceCollection services, Action<RepositoryLifecycleOptions>? configure = null) {
			var options = services.AddOptions<RepositoryLifecycleOptions>();

			if (configure != null)
				options.Configure(configure);

			services.TryAddSingleton<IRepositoryLifecycleOrchestrator, DefaultRepositoryLifecycleOrchestrator>();

			return services;
		}

		/// <summary>
		/// Configures repository lifecycle on the context builder with the given options.
		/// Automatically scans the entry assembly for <see cref="IRepositorySeedDataProvider{TEntity}"/>
		/// implementations if no explicit seed data registration or scan was performed.
		/// </summary>
		/// <param name="builder">The repository context builder.</param>
		/// <param name="configure">A delegate to configure lifecycle options.</param>
		/// <returns>The same builder for chaining.</returns>
		public static RepositoryContextBuilder ConfigureLifecycle(this RepositoryContextBuilder builder, Action<RepositoryLifecycleOptions> configure) {
			builder.Services.AddRepositoryLifecycleOrchestrator(configure);
			builder.EnsureSeedProvidersScanned();
			return builder;
		}

		/// <summary>
		/// Configures repository lifecycle on the context builder with default options.
		/// Automatically scans the entry assembly for <see cref="IRepositorySeedDataProvider{TEntity}"/>
		/// implementations if no explicit seed data registration or scan was performed.
		/// </summary>
		/// <param name="builder">The repository context builder.</param>
		/// <returns>The same builder for chaining.</returns>
		public static RepositoryContextBuilder ConfigureLifecycle(this RepositoryContextBuilder builder) {
			builder.Services.AddRepositoryLifecycleOrchestrator();
			builder.EnsureSeedProvidersScanned();
			return builder;
		}
	}
}
