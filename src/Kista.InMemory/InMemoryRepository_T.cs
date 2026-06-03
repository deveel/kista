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
	/// A repository that uses the memory of the process to store
	/// the entities.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of entity managed by the repository.
	/// </typeparam>
	public class InMemoryRepository<TEntity> :
		InMemoryRepository<TEntity, string>,
		IQueryableRepository<TEntity>,
		IRepository<TEntity>,
		IPageableRepository<TEntity>,
		IFilterableRepository<TEntity>,
		IDisposable
		where TEntity : class {
		/// <summary>
		/// Constructs the repository with the given list of
		/// initial entities.
		/// </summary>
		/// <param name="list">
		/// The list of entities to initialize the repository with.
		/// </param>
		/// <param name="fieldMapper">
		/// A service that maps a field by name to an expression that
		/// can select the field from an entity.
		/// </param>
		/// <param name="services">
		/// An optional service provider used to resolve infrastructure services
		/// such as expression caches for filter optimization.
		/// </param>
		public InMemoryRepository(
			IEnumerable<TEntity>? list = null,
			IFieldMapper<TEntity>? fieldMapper = null,
			IServiceProvider? services = null) : base(list, fieldMapper, services) {
		}

		/// <summary>
		/// Destroys the instance of the repository.
		/// </summary>
		~InMemoryRepository() {
			Dispose(disposing: false);
		}

		/// <inheritdoc/>
		public new void Dispose() {
			base.Dispose();
			GC.SuppressFinalize(this);
		}

		[Obsolete("Use the abstract Kista.Repository<TEntity, TKey> base class instead. The IQueryable hatch is no longer exposed to consumers.", false)]
		public new IQueryable<TEntity> AsQueryable() => base.AsQueryable();

		IQueryable<TEntity> IQueryableRepository<TEntity, object>.AsQueryable() => Query();

		IServiceProvider? IRepository<TEntity, object>.Services => Services;

		object? IRepository<TEntity, object>.GetEntityKey(TEntity entity) {
			return GetEntityKey(entity);
		}

		ValueTask<TEntity?> IRepository<TEntity, object>.FindAsync(object key, CancellationToken cancellationToken) {
			return FindAsync(NormalizeKey(key), cancellationToken);
		}

		ValueTask<bool> IFilterableRepository<TEntity, object>.ExistsAsync(IQueryFilter filter, CancellationToken cancellationToken)
			=> ExistsAsync(filter, cancellationToken);

		ValueTask<long> IFilterableRepository<TEntity, object>.CountAsync(IQueryFilter filter, CancellationToken cancellationToken)
			=> CountAsync(filter, cancellationToken);

		ValueTask<TEntity?> IFilterableRepository<TEntity, object>.FindFirstAsync(IQuery query, CancellationToken cancellationToken)
			=> FindFirstAsync(query, cancellationToken);

		ValueTask<IList<TEntity>> IFilterableRepository<TEntity, object>.FindAllAsync(IQuery query, CancellationToken cancellationToken)
			=> FindAllAsync(query, cancellationToken);

#pragma warning disable CS0618 // Type or member is obsolete
		ValueTask<PageQueryResult<TEntity>> IPageableRepository<TEntity, object>.GetPageAsync(PageQuery<TEntity> request, CancellationToken cancellationToken)
			=> ((IPageableRepository<TEntity, string>)this).GetPageAsync(request, cancellationToken);
#pragma warning restore CS0618 // Type or member is obsolete

		private string NormalizeKey(object key) {
			ArgumentNullException.ThrowIfNull(key);

			if (key is string s)
				return s;
			if (key is Guid guid)
				return guid.ToString("N");

			throw new RepositoryException($"The key '{key}' is not supported");
		}
	}
}
