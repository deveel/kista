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

namespace Kista {
	/// <summary>
	/// Extends the <see cref="IQueryableRepository{TEntity}"/> interface
	/// to provide methods to query the repository.
	/// </summary>
	/// <remarks>
	/// This extension class is obsolete. It depends on the obsolete
	/// <see cref="IQueryableRepository{TEntity, TKey}"/> contract and is
	/// expected to be removed in a future major version. Repositories that
	/// inherit from <see cref="RepositoryBase{TEntity, TKey}"/> expose
	/// ready-made <c>FindAsync(IQuery, CancellationToken)</c> and
	/// <c>QueryPageAsync(PageQuery{TEntity}, CancellationToken)</c> methods.
	/// </remarks>
	[Obsolete("Use the abstract Kista.RepositoryBase<TEntity, TKey> base class instead. The IQueryable hatch is no longer exposed to consumers.", false)]
	public static class QueryableRepositoryExtensions {
		/// <summary>
		/// Gets a page of entities from the repository,
		/// given a request object that defines the scope
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of key used to identify the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository from which the entities are retrieved.
		/// </param>
		/// <param name="request">
		/// The request object that defines the scope of the page to retrieve.
		/// </param>
		/// <returns>
		/// Returns an instance of <see cref="PageQueryResult{TEntity}"/> that
		/// is the result of the query.
		/// </returns>
		[Obsolete("Use the abstract Kista.RepositoryBase<TEntity, TKey> base class instead. The IQueryable hatch is no longer exposed to consumers.", false)]
		public static PageQueryResult<TEntity> GetPage<TEntity, TKey>(this IQueryableRepository<TEntity, TKey> repository, PageQuery<TEntity> request)
			where TEntity : class {
			var query = request.ApplyQuery(repository.AsQueryable());

			var total = query.Count();
			var items = query.Skip(request.Offset)
				.Take(request.Size)
				.ToList();

			return new PageQueryResult<TEntity>(request, total, items);
		}
	}
}
