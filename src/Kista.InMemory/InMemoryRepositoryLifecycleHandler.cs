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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kista {
	/// <summary>
	/// A lifecycle handler for in-memory repositories that performs no-op
	/// create/drop operations (since in-memory storage is transient) and
	/// resolves seed data via <see cref="IRepository{TEntity}"/> from DI.
	/// </summary>
	/// <typeparam name="TEntity">The type of the entity.</typeparam>
	public class InMemoryRepositoryLifecycleHandler<TEntity> : RepositoryLifecycleHandler<TEntity>
		where TEntity : class {

		/// <summary>
		/// Creates a new instance of the handler.
		/// </summary>
		/// <param name="serviceProvider">The service provider to resolve the repository.</param>
		/// <param name="logger">An optional typed logger instance.</param>
		public InMemoryRepositoryLifecycleHandler(IServiceProvider serviceProvider, ILogger<InMemoryRepositoryLifecycleHandler<TEntity>>? logger = null)
			: base(logger) {
			ServiceProvider = serviceProvider;
		}

		/// <summary>
		/// Gets the service provider used to resolve the repository instance.
		/// </summary>
		protected IServiceProvider ServiceProvider { get; }

		/// <inheritdoc/>
		public override ValueTask<bool> ExistsAsync(CancellationToken cancellationToken = default) {
			return ValueTask.FromResult(false);
		}

		/// <inheritdoc/>
		public override ValueTask CreateAsync(CancellationToken cancellationToken = default) {
			return ValueTask.CompletedTask;
		}

		/// <inheritdoc/>
		public override ValueTask DropAsync(CancellationToken cancellationToken = default) {
			return ValueTask.CompletedTask;
		}

		/// <inheritdoc/>
		protected override async ValueTask SeedEntitiesAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) {
			var repository = ServiceProvider.GetRequiredService<IRepository<TEntity>>();
			await repository.AddRangeAsync(entities, cancellationToken);
		}
	}
}
