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

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kista {
	/// <summary>
	/// A lifecycle handler for Entity Framework Core repositories that uses
	/// <see cref="DbContext.Database"/> operations (<c>EnsureCreated</c>,
	/// <c>EnsureDeleted</c>, <c>CanConnect</c>) for create/drop/existence checks
	/// and <see cref="DbSet{TEntity}"/> operations for seeding.
	/// </summary>
	/// <typeparam name="TEntity">The type of the entity.</typeparam>
	public class EntityFrameworkRepositoryLifecycleHandler<TEntity> : RepositoryLifecycleHandler<TEntity>
		where TEntity : class {

		/// <summary>
		/// Creates a new instance of the handler for the given <see cref="DbContext"/>.
		/// </summary>
		/// <param name="context">The EF Core database context.</param>
		/// <param name="logger">An optional typed logger instance.</param>
		public EntityFrameworkRepositoryLifecycleHandler(DbContext context, ILogger<EntityFrameworkRepositoryLifecycleHandler<TEntity>>? logger = null)
			: base(logger) {
			Context = context;
		}

		/// <summary>
		/// Gets the underlying <see cref="DbContext"/> used for database operations.
		/// </summary>
		protected DbContext Context { get; }

		/// <inheritdoc/>
		public override ValueTask<bool> ExistsAsync(CancellationToken cancellationToken = default) {
			try {
				return ValueTask.FromResult(Context.Database.CanConnect());
			} catch (Exception ex) {
				throw new RepositoryException("Unable to determine the existence of the repository", ex);
			}
		}

		/// <inheritdoc/>
		public override async ValueTask CreateAsync(CancellationToken cancellationToken = default) {
			try {
				await Context.Database.EnsureCreatedAsync(cancellationToken);
			} catch (Exception ex) {
				throw new RepositoryException("Unable to create the repository", ex);
			}
		}

		/// <inheritdoc/>
		public override async ValueTask DropAsync(CancellationToken cancellationToken = default) {
			try {
				await Context.Database.EnsureDeletedAsync(cancellationToken);
			} catch (Exception ex) {
				throw new RepositoryException("Unable to drop the repository", ex);
			}
		}

		/// <inheritdoc/>
		protected override async ValueTask SeedEntitiesAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) {
			Context.Set<TEntity>().AddRange(entities);
			await Context.SaveChangesAsync(cancellationToken);
		}
	}
}
