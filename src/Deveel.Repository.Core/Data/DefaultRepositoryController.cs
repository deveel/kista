// Copyright 2023-2025 Antonello Provenzano
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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Deveel.Data {
	/// <summary>
	/// A default implementation of the <see cref="IRepositoryController"/> interface
	/// </summary>
    [Obsolete("Use DefaultRepositoryLifecycleOrchestrator (via IRepositoryLifecycleOrchestrator) instead")]
    public class DefaultRepositoryController : IRepositoryController {
		private readonly RepositoryControllerOptions options;
		private readonly IServiceProvider serviceProvider;
		private readonly ILogger logger;

		/// <summary>
		/// Constructs a <see cref="DefaultRepositoryController"/> instance
		/// </summary>
		/// <param name="options">
		/// The options to configure the controller
		/// </param>
		/// <param name="serviceProvider">
		/// The service provider used to resolve the repositories
		/// </param>
		/// <param name="logger">
		/// A logger used to trace the operations
		/// </param>
		public DefaultRepositoryController(IOptions<RepositoryControllerOptions> options, IServiceProvider serviceProvider, ILogger<DefaultRepositoryController>? logger = null)
			: this(options, serviceProvider, (ILogger?) logger) {
		}

		/// <summary>
		/// Constructs a <see cref="DefaultRepositoryController"/> instance
		/// </summary>
		/// <param name="options">
		/// The options to configure the controller
		/// </param>
		/// <param name="serviceProvider">
		/// The service provider used to resolve the repositories
		/// </param>
		/// <param name="logger">
		/// A logger used to trace the operations
		/// </param>
		protected DefaultRepositoryController(IOptions<RepositoryControllerOptions> options, IServiceProvider serviceProvider, ILogger? logger = null) {
			this.options = options.Value;
			this.serviceProvider = serviceProvider;
			this.logger = logger ?? NullLogger.Instance;
		}

		private void LogTrace(string message, params object?[] args) {
			if (logger.IsEnabled(LogLevel.Trace))
				logger.LogTrace(message, args);
		}

		private void LogError(Exception ex, string message, params object?[] args) {
			if (logger.IsEnabled(LogLevel.Error))
				logger.LogError(ex, message, args);
		}

		private IControllableRepository? RequireRepository<TEntity>()
			where TEntity : class {

			LogTrace("Resolving a repository for entity of type {EntityType}", typeof(TEntity).Name);

			var repository = serviceProvider.GetService<IRepository<TEntity>>();
			
			if (repository == null)
				throw new NotSupportedException($"Unable to resolve any repository for entities of type {typeof(TEntity)}");

			LogTrace("The repository of type '{RepositoryType}' was resolved handling type '{EntityType}'", 
				repository.GetType().Name, typeof(TEntity).Name);

			return repository as IControllableRepository;
		}

		private IControllableRepository? RequireRepository<TEntity, TKey>()
			where TEntity : class {

			LogTrace("Resolving a repository for entity of type {EntityType}", typeof(TEntity).Name);

			var repository = serviceProvider.GetService<IRepository<TEntity, TKey>>();

			if (repository == null)
				throw new NotSupportedException($"Unable to resolve any repository for entities of type {typeof(TEntity)}");

			LogTrace("The repository of type '{RepositoryType}' was resolved handling type '{EntityType}'",
				repository.GetType().Name, typeof(TEntity).Name);

			return repository as IControllableRepository;
		}


		private async ValueTask CreateRepository(IControllableRepository? repository, CancellationToken cancellationToken) {
			if (repository != null) {
				try {
					if (await repository.ExistsAsync(cancellationToken)) {
						if (options.DeleteIfExists) {
							LogTrace("The repository already exists and the controller is deleting it first");

							await DropRepository(repository, cancellationToken);
						} else if (options.DontCreateExisting) {
							if (logger.IsEnabled(LogLevel.Warning))
								logger.LogWarning("The repository '{RepositoryType}' already exists and the controller is not deleting it",
									repository.GetType().FullName);
							return;
						} else {
							throw new RepositoryException("The repository already exists");
						}
					}

					LogTrace("Creating the repository");

					await repository.CreateAsync(cancellationToken);

					LogTrace("Repository created");
				} catch (NotSupportedException ex) {
					LogError(ex, "Not Supported Error while creating the repository {RepositoryType}",
						repository.GetType().FullName);
					throw;
				} catch (RepositoryException ex) {
					LogError(ex, "Not Supported Error while creating the repository {RepositoryType}",
						repository.GetType().FullName);
					throw;
				} catch(Exception ex) {
					LogError(ex, "Not Supported Error while creating the repository {RepositoryType}",
						repository.GetType().FullName);

					throw new RepositoryException($"Unable to create the repository '{repository.GetType().Name}'", ex);
				}
			}
		}

		private async ValueTask DropRepository(IControllableRepository? repository, CancellationToken cancellationToken) {
			if (repository != null) {
				try {
					if (!await repository.ExistsAsync(cancellationToken)) {
						LogTrace("The repository does not exist: it will be not deleted");
					} else {
						LogTrace("Dropping the repository");

						await repository.DropAsync(cancellationToken);

						LogTrace("The repository was dropped");
					}
				} catch (NotSupportedException ex) {
					LogError(ex, "Not Supported Error while dropping the repository {RepositoryType}",
						repository.GetType().Name);
					throw;
				} catch (RepositoryException ex) {
					LogError(ex, "Not Supported Error while dropping the repository {RepositoryType}",
						repository.GetType().Name);
					throw;
				} catch (Exception ex) {
					LogError(ex, "Not Supported Error while dropping the repository {RepositoryType}",
						repository.GetType().Name);

					throw new RepositoryException($"Unable to drop the repository '{repository.GetType().Name}'", ex);
				}
			}
		}

		/// <inheritdoc/>
		public virtual async ValueTask CreateRepositoryAsync<TEntity>(CancellationToken cancellationToken = default)
			where TEntity : class {
			LogTrace("Creating the repository for type '{EntityType}'", typeof(TEntity).Name);

			var repository = RequireRepository<TEntity>();

			await CreateRepository(repository, cancellationToken);

			LogTrace("The creation process for repository for type '{EntityType}' finished", typeof(TEntity).Name);
		}

		/// <inheritdoc/>
		public virtual async ValueTask CreateRepositoryAsync<TEntity, TKey>(CancellationToken cancellationToken = default)
			where TEntity : class {
			LogTrace("Creating the repository for type '{EntityType}'", typeof(TEntity).Name);

			var repository = RequireRepository<TEntity, TKey>();

			await CreateRepository(repository, cancellationToken);

			LogTrace("The creation process for repository for type '{EntityType}' finished", typeof(TEntity).Name);
		}


		/// <inheritdoc/>
		public virtual async ValueTask DropRepositoryAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class {
			LogTrace("Dropping the repository handling the type '{EntityType}'", typeof(TEntity).Name);

			var repository = RequireRepository<TEntity>();

			await DropRepository(repository, cancellationToken);

			LogTrace("The repository handling the type '{EntityType}' was dropped", typeof(TEntity).Name);
		}

		/// <inheritdoc/>
		public virtual async ValueTask DropRepositoryAsync<TEntity, TKey>(CancellationToken cancellationToken = default) where TEntity : class {
			LogTrace("Dropping the repository handling the type '{EntityType}'", typeof(TEntity).Name);

			var repository = RequireRepository<TEntity, TKey>();

			await DropRepository(repository, cancellationToken);

			LogTrace("The repository handling the type '{EntityType}' was dropped", typeof(TEntity).Name);
		}
	}
}
