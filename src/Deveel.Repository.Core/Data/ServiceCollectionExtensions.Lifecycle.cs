using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Deveel.Data {
	/// <inheritdoc cref="ServiceCollectionExtensions"/>
	public static partial class ServiceCollectionExtensions {
		/// <summary>
		/// Registers the repository lifecycle service (<see cref="IRepositoryLifecycleService"/>)
		/// as a singleton service, with optional configuration of <see cref="RepositoryLifecycleOptions"/>.
		/// </summary>
		/// <param name="services">The service collection to register into.</param>
		/// <param name="configure">An optional delegate to configure lifecycle options.</param>
		/// <returns>The same service collection for chaining.</returns>
		public static IServiceCollection AddRepositoryLifecycleOrchestrator(this IServiceCollection services, Action<RepositoryLifecycleOptions>? configure = null) {
			var options = services.AddOptions<RepositoryLifecycleOptions>();

			if (configure != null)
				options.Configure(configure);

			services.TryAddSingleton<IRepositoryLifecycleService, RepositoryLifecycleService>();

			return services;
		}

		/// <summary>
		/// Registers a custom lifecycle handler for the given entity type on the repository context builder.
		/// </summary>
		/// <typeparam name="TEntity">The type of the entity.</typeparam>
		/// <typeparam name="THandler">
		/// The type of the <see cref="IRepositoryLifecycleHandler{TEntity}"/> implementation.
		/// </typeparam>
		/// <param name="builder">The repository context builder.</param>
		/// <param name="lifetime">The service lifetime (default: <see cref="ServiceLifetime.Scoped"/>).</param>
		/// <returns>The same builder for chaining.</returns>
		public static RepositoryContextBuilder WithLifecycleHandler<TEntity, THandler>(
			this RepositoryContextBuilder builder,
			ServiceLifetime lifetime = ServiceLifetime.Scoped)
			where TEntity : class
			where THandler : class, IRepositoryLifecycleHandler<TEntity> {

			builder.Services.Add(ServiceDescriptor.Describe(
				typeof(IRepositoryLifecycleHandler<TEntity>),
				typeof(THandler),
				lifetime));

			return builder;
		}

		/// <summary>
		/// Registers a lifecycle handler for the given entity type using a factory delegate.
		/// </summary>
		/// <typeparam name="TEntity">The type of the entity.</typeparam>
		/// <typeparam name="THandler">
		/// The type of the <see cref="IRepositoryLifecycleHandler{TEntity}"/> implementation.
		/// </typeparam>
		/// <param name="builder">The repository context builder.</param>
		/// <param name="factory">A factory delegate to create the handler instance.</param>
		/// <param name="lifetime">The service lifetime (default: <see cref="ServiceLifetime.Scoped"/>).</param>
		/// <returns>The same builder for chaining.</returns>
		public static RepositoryContextBuilder WithLifecycleHandler<TEntity, THandler>(
			this RepositoryContextBuilder builder,
			Func<IServiceProvider, THandler> factory,
			ServiceLifetime lifetime = ServiceLifetime.Scoped)
			where TEntity : class
			where THandler : class, IRepositoryLifecycleHandler<TEntity> {

			builder.Services.Add(ServiceDescriptor.Describe(
				typeof(IRepositoryLifecycleHandler<TEntity>),
				sp => factory(sp),
				lifetime));

			return builder;
		}

		/// <summary>
		/// Registers a lifecycle handler instance for the given entity type.
		/// </summary>
		/// <typeparam name="TEntity">The type of the entity.</typeparam>
		/// <param name="builder">The repository context builder.</param>
		/// <param name="handler">The lifecycle handler instance.</param>
		/// <returns>The same builder for chaining.</returns>
		public static RepositoryContextBuilder WithLifecycleHandler<TEntity>(
			this RepositoryContextBuilder builder,
			IRepositoryLifecycleHandler<TEntity> handler)
			where TEntity : class {

			builder.Services.AddSingleton<IRepositoryLifecycleHandler<TEntity>>(handler);

			return builder;
		}

		/// <summary>
		/// Registers a custom lifecycle profile on the repository context builder.
		/// </summary>
		/// <typeparam name="TProfile">
		/// The type of the <see cref="IRepositoryLifecycleProfile"/> implementation.
		/// </typeparam>
		/// <param name="builder">The repository context builder.</param>
		/// <param name="lifetime">The service lifetime (default: <see cref="ServiceLifetime.Singleton"/>).</param>
		/// <returns>The same builder for chaining.</returns>
		public static RepositoryContextBuilder WithLifecycleProfile<TProfile>(
			this RepositoryContextBuilder builder,
			ServiceLifetime lifetime = ServiceLifetime.Singleton)
			where TProfile : class, IRepositoryLifecycleProfile {

			builder.Services.Add(ServiceDescriptor.Describe(
				typeof(IRepositoryLifecycleProfile),
				typeof(TProfile),
				lifetime));

			return builder;
		}

		/// <summary>
		/// Registers a lifecycle profile instance on the repository context builder.
		/// </summary>
		/// <param name="builder">The repository context builder.</param>
		/// <param name="profile">The lifecycle profile instance.</param>
		/// <returns>The same builder for chaining.</returns>
		public static RepositoryContextBuilder WithLifecycleProfile(
			this RepositoryContextBuilder builder,
			IRepositoryLifecycleProfile profile) {

			builder.Services.AddSingleton<IRepositoryLifecycleProfile>(profile);

			return builder;
		}

		/// <summary>
		/// Configures repository lifecycle on the context builder with the given options.
		/// Automatically scans the entry assembly for <see cref="IRepositorySeedDataProvider{TEntity}"/>
		/// implementations if no explicit seed data registration or scan was performed.
		/// Registers a <see cref="DefaultRepositoryLifecycleProfile"/> if no profile is already registered.
		/// </summary>
		/// <param name="builder">The repository context builder.</param>
		/// <param name="configure">A delegate to configure lifecycle options.</param>
		/// <returns>The same builder for chaining.</returns>
		public static RepositoryContextBuilder ConfigureLifecycle(this RepositoryContextBuilder builder, Action<RepositoryLifecycleOptions> configure) {
			builder.Services.AddRepositoryLifecycleOrchestrator(configure);
			builder.Services.TryAddSingleton<IRepositoryLifecycleProfile, DefaultRepositoryLifecycleProfile>();
			builder.EnsureSeedProvidersScanned();
			return builder;
		}

		/// <summary>
		/// Configures repository lifecycle on the context builder with default options.
		/// Automatically scans the entry assembly for <see cref="IRepositorySeedDataProvider{TEntity}"/>
		/// implementations if no explicit seed data registration or scan was performed.
		/// Registers a <see cref="DefaultRepositoryLifecycleProfile"/> if no profile is already registered.
		/// </summary>
		/// <param name="builder">The repository context builder.</param>
		/// <returns>The same builder for chaining.</returns>
		public static RepositoryContextBuilder ConfigureLifecycle(this RepositoryContextBuilder builder) {
			builder.Services.AddRepositoryLifecycleOrchestrator();
			builder.Services.TryAddSingleton<IRepositoryLifecycleProfile, DefaultRepositoryLifecycleProfile>();
			builder.EnsureSeedProvidersScanned();
			return builder;
		}
	}
}
