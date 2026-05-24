using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Deveel.Data {
	/// <summary>
	/// The default implementation of <see cref="IRepositoryLifecycleOrchestrator"/> that
	/// manages create, drop, and seed operations for repositories using
	/// <see cref="IRepositoryLifecycleHandler{TEntity}"/> instances or
	/// <see cref="IControllableRepository"/> fallback.
	/// </summary>
	public class DefaultRepositoryLifecycleOrchestrator : IRepositoryLifecycleOrchestrator {
		/// <summary>
		/// The default environment name used as a last resort when no environment
		/// is configured and <c>IHostEnvironment</c> is not available.
		/// </summary>
		public const string ProductionEnvironment = "Production";

		private readonly RepositoryLifecycleOptions options;
		private readonly IServiceProvider serviceProvider;
		private readonly ILogger logger;

		/// <summary>
		/// Creates a new instance with the given options, service provider, and optional typed logger.
		/// </summary>
		/// <param name="options">The lifecycle configuration options.</param>
		/// <param name="serviceProvider">The service provider for dependency resolution.</param>
		/// <param name="logger">An optional typed logger instance.</param>
		public DefaultRepositoryLifecycleOrchestrator(
			IOptions<RepositoryLifecycleOptions> options,
			IServiceProvider serviceProvider,
			ILogger<DefaultRepositoryLifecycleOrchestrator>? logger = null)
			: this(options, serviceProvider, (ILogger?)logger) {
		}

		/// <summary>
		/// Creates a new instance with the given options, service provider, and optional untyped logger.
		/// </summary>
		/// <param name="options">The lifecycle configuration options.</param>
		/// <param name="serviceProvider">The service provider for dependency resolution.</param>
		/// <param name="logger">An optional untyped logger instance.</param>
		protected DefaultRepositoryLifecycleOrchestrator(
			IOptions<RepositoryLifecycleOptions> options,
			IServiceProvider serviceProvider,
			ILogger? logger = null) {
			this.options = options.Value;
			this.serviceProvider = serviceProvider;
			this.logger = logger ?? NullLogger.Instance;
		}

		/// <summary>
		/// Resolves a lifecycle handler for the given entity type (without key).
		/// Falls back to <see cref="IControllableRepository"/> when no handler is registered.
		/// </summary>
		protected virtual IRepositoryLifecycleHandler<TEntity>? ResolveHandler<TEntity>()
			where TEntity : class {
			logger.LogResolvingHandler(typeof(TEntity).Name);

			var handler = serviceProvider.GetService<IRepositoryLifecycleHandler<TEntity>>();
			if (handler != null) {
				logger.LogHandlerResolved(handler.GetType().Name, typeof(TEntity).Name);
				return handler;
			}

			var repository = serviceProvider.GetService<IRepository<TEntity>>();
			if (repository is IControllableRepository controllable) {
				logger.LogFallingBackToControllable(typeof(TEntity).Name);
				return new ControllableRepositoryHandler<TEntity>(controllable);
			}

			if (options.FailFast)
				throw new RepositoryException($"No lifecycle handler available for entity of type '{typeof(TEntity)}' and the repository does not support lifecycle control");

			logger.LogNoHandlerFound(typeof(TEntity).Name);
			return null;
		}

		/// <summary>
		/// Resolves a lifecycle handler for the given entity type with a key type.
		/// Falls back to <see cref="IControllableRepository"/> when no handler is registered.
		/// </summary>
		protected virtual IRepositoryLifecycleHandler<TEntity>? ResolveHandler<TEntity, TKey>()
			where TEntity : class {
			logger.LogResolvingHandler(typeof(TEntity).Name);

			var handler = serviceProvider.GetService<IRepositoryLifecycleHandler<TEntity>>();
			if (handler != null) {
				logger.LogHandlerResolved(handler.GetType().Name, typeof(TEntity).Name);
				return handler;
			}

			var repository = serviceProvider.GetService<IRepository<TEntity, TKey>>();
			if (repository is IControllableRepository controllable) {
				logger.LogFallingBackToControllable(typeof(TEntity).Name);
				return new ControllableRepositoryHandler<TEntity>(controllable);
			}

			if (options.FailFast)
				throw new RepositoryException($"No lifecycle handler available for entity of type '{typeof(TEntity)}' and the repository does not support lifecycle control");

			logger.LogNoHandlerFound(typeof(TEntity).Name);
			return null;
		}

		/// <summary>
		/// Creates the repository via the given handler, respecting
		/// <see cref="RepositoryLifecycleOptions.DeleteIfExists"/> and
		/// <see cref="RepositoryLifecycleOptions.DontCreateExisting"/> settings.
		/// </summary>
		protected virtual async ValueTask CreateRepository<TEntity>(IRepositoryLifecycleHandler<TEntity>? handler, CancellationToken cancellationToken)
			where TEntity : class {
			if (handler == null)
				return;

			try {
				if (await handler.ExistsAsync(cancellationToken)) {
					if (options.DeleteIfExists) {
						logger.TraceDeletingExisting();
						await DropRepository(handler, cancellationToken);
					} else if (options.DontCreateExisting) {
						logger.WarnSkippingExisting();
						return;
					} else {
						throw new RepositoryException("The repository already exists");
					}
				}

				logger.TraceCreatingRepository();
				await handler.CreateAsync(cancellationToken);
				logger.TraceRepositoryCreated();
			} catch (NotSupportedException ex) {
				logger.LogNotSupportedError(ex, "creating");
				throw;
			} catch (RepositoryException ex) {
				logger.LogRepositoryError(ex, "creating");
				throw;
			} catch (Exception ex) {
				logger.LogGeneralError(ex, "creating");
				throw new RepositoryException($"Unable to create the repository", ex);
			}
		}

		/// <summary>
		/// Drops the repository via the given handler, skipping if it does not exist.
		/// </summary>
		protected virtual async ValueTask DropRepository<TEntity>(IRepositoryLifecycleHandler<TEntity>? handler, CancellationToken cancellationToken)
			where TEntity : class {
			if (handler == null)
				return;

			try {
				if (!await handler.ExistsAsync(cancellationToken)) {
					logger.TraceNotExistsSkipping();
				} else {
					logger.TraceDroppingRepository();
					await handler.DropAsync(cancellationToken);
					logger.TraceRepositoryDropped();
				}
			} catch (NotSupportedException ex) {
				logger.LogNotSupportedError(ex, "dropping");
				throw;
			} catch (RepositoryException ex) {
				logger.LogRepositoryError(ex, "dropping");
				throw;
			} catch (Exception ex) {
				logger.LogGeneralError(ex, "dropping");
				throw new RepositoryException($"Unable to drop the repository", ex);
			}
		}

		/// <summary>
		/// Seeds the repository via the given handler, respecting the configured
		/// <see cref="SeedStrategy"/> and optional <see cref="RepositoryLifecycleOptions.SeedAction"/>.
		/// </summary>
		protected virtual async ValueTask SeedRepository<TEntity>(IRepositoryLifecycleHandler<TEntity>? handler, object? seedData, CancellationToken cancellationToken)
			where TEntity : class {
			if (handler == null)
				return;

			var strategy = ResolveSeedStrategy();

			if (strategy == SeedStrategy.Never)
				return;

			if (strategy == SeedStrategy.IfMissing || strategy == SeedStrategy.ByEnvironment) {
				if (await handler.ExistsAsync(cancellationToken)) {
					logger.WarnSkippingSeed();
					return;
				}
			}

			try {
				logger.TraceSeedingRepository(typeof(TEntity).Name);

				if (options.SeedAction != null) {
					options.SeedAction(serviceProvider, typeof(TEntity), seedData);
				} else {
					var resolvedSeedData = seedData ?? ResolveSeedDataFromProvider<TEntity>();
					if (resolvedSeedData == null) {
						logger.TraceNoSeedData(typeof(TEntity).Name);
						return;
					}

					await handler.SeedAsync(resolvedSeedData, cancellationToken);
				}

				logger.TraceRepositorySeeded(typeof(TEntity).Name);
			} catch (NotSupportedException ex) {
				logger.LogNotSupportedError(ex, "seeding");
				throw;
			} catch (RepositoryException ex) {
				logger.LogRepositoryError(ex, "seeding");
				throw;
			} catch (Exception ex) {
				logger.LogGeneralError(ex, "seeding");
				throw new RepositoryException($"Unable to seed the repository", ex);
			}
		}

		/// <summary>
		/// Resolves the effective <see cref="SeedStrategy"/> from options,
		/// taking into account the environment profile when the strategy is
		/// <see cref="SeedStrategy.ByEnvironment"/>.
		/// </summary>
		protected virtual SeedStrategy ResolveSeedStrategy() {
			if (options.SeedStrategy != SeedStrategy.ByEnvironment)
				return options.SeedStrategy;

			var envName = ResolveEnvironmentName();

			var profile = serviceProvider.GetService<IRepositoryLifecycleProfile>();
			if (profile != null)
				return profile.GetSeedStrategy(envName);

			return SeedStrategy.Always;
		}

		/// <summary>
		/// Resolves the hosting environment name by checking in order:
		/// <list type="number">
		///   <item><description><see cref="RepositoryLifecycleOptions.EnvironmentName"/> if configured.</description></item>
		///   <item><description><c>IHostEnvironment.EnvironmentName</c> from the service provider.</description></item>
		///   <item><description>The <see cref="ProductionEnvironment"/> constant as a last resort.</description></item>
		/// </list>
		/// </summary>
		protected virtual string ResolveEnvironmentName() {
			if (!string.IsNullOrWhiteSpace(options.EnvironmentName))
				return options.EnvironmentName;

			var hostEnvType = Type.GetType(
				"Microsoft.Extensions.Hosting.IHostEnvironment, Microsoft.Extensions.Hosting.Abstractions",
				throwOnError: false);

			if (hostEnvType != null) {
				var hostEnv = serviceProvider.GetService(hostEnvType);
				if (hostEnv != null) {
					var envNameProp = hostEnvType.GetProperty("EnvironmentName");
					if (envNameProp != null &&
						envNameProp.GetValue(hostEnv) is string envName &&
						!string.IsNullOrWhiteSpace(envName)) {
						return envName;
					}
				}
			}

			return ProductionEnvironment;
		}

		/// <summary>
		/// Resolves seed data from a registered <see cref="IRepositorySeedDataProvider{TEntity}"/>.
		/// </summary>
		protected virtual object? ResolveSeedDataFromProvider<TEntity>() where TEntity : class {
			var provider = serviceProvider.GetService<IRepositorySeedDataProvider<TEntity>>();
			if (provider != null)
				return provider.GetSeedData();

			return null;
		}

		/// <inheritdoc/>
		public virtual async ValueTask CreateRepositoryAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class {
			logger.TraceCreatingRepository();
			var handler = ResolveHandler<TEntity>();
			await CreateRepository(handler, cancellationToken);
			logger.TraceRepositoryCreated();
		}

		/// <inheritdoc/>
		public virtual async ValueTask CreateRepositoryAsync<TEntity, TKey>(CancellationToken cancellationToken = default) where TEntity : class {
			logger.TraceCreatingRepository();
			var handler = ResolveHandler<TEntity, TKey>();
			await CreateRepository(handler, cancellationToken);
			logger.TraceRepositoryCreated();
		}

		/// <inheritdoc/>
		public virtual async ValueTask DropRepositoryAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class {
			logger.TraceDroppingRepository();
			var handler = ResolveHandler<TEntity>();
			await DropRepository(handler, cancellationToken);
			logger.TraceRepositoryDropped();
		}

		/// <inheritdoc/>
		public virtual async ValueTask DropRepositoryAsync<TEntity, TKey>(CancellationToken cancellationToken = default) where TEntity : class {
			logger.TraceDroppingRepository();
			var handler = ResolveHandler<TEntity, TKey>();
			await DropRepository(handler, cancellationToken);
			logger.TraceRepositoryDropped();
		}

		/// <inheritdoc/>
		public virtual async ValueTask SeedRepositoryAsync<TEntity>(object? seedData = null, CancellationToken cancellationToken = default) where TEntity : class {
			logger.TraceSeedingRepository(typeof(TEntity).Name);
			var handler = ResolveHandler<TEntity>();
			await SeedRepository(handler, seedData, cancellationToken);
			logger.TraceRepositorySeeded(typeof(TEntity).Name);
		}

		/// <inheritdoc/>
		public virtual async ValueTask SeedRepositoryAsync<TEntity, TKey>(object? seedData = null, CancellationToken cancellationToken = default) where TEntity : class {
			logger.TraceSeedingRepository(typeof(TEntity).Name);
			var handler = ResolveHandler<TEntity, TKey>();
			await SeedRepository(handler, seedData, cancellationToken);
			logger.TraceRepositorySeeded(typeof(TEntity).Name);
		}

		class ControllableRepositoryHandler<TEntity> : IRepositoryLifecycleHandler<TEntity> where TEntity : class {
			private readonly IControllableRepository repository;

			public ControllableRepositoryHandler(IControllableRepository repository) {
				this.repository = repository;
			}

			/// <inheritdoc/>
			public ValueTask<bool> ExistsAsync(CancellationToken cancellationToken)
				=> repository.ExistsAsync(cancellationToken);

			/// <inheritdoc/>
			public ValueTask CreateAsync(CancellationToken cancellationToken)
				=> repository.CreateAsync(cancellationToken);

			/// <inheritdoc/>
			public ValueTask DropAsync(CancellationToken cancellationToken)
				=> repository.DropAsync(cancellationToken);

			/// <inheritdoc/>
			public ValueTask SeedAsync(object? seedData = null, CancellationToken cancellationToken = default)
				=> ValueTask.CompletedTask;
		}
	}
}
