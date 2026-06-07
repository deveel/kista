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

using System.Diagnostics.CodeAnalysis;

namespace Kista {
	/// <summary>
	/// Defines a set of extension methods for the <see cref="IPageableRepository{TEntity}"/>
	/// that allows to retrieve a page of entities from the repository.
	/// </summary>
	/// <remarks>
	/// These methods are obsolete. Use <see cref="IRepository{TEntity, TKey}.GetPageAsync(PageRequest, CancellationToken)"/>
	/// for simple pagination, or the protected <see cref="Repository{TEntity,TKey}.QueryPage(PageQuery{TEntity}, CancellationToken)"/>
	/// for filtered and sorted queries inside the data layer.
	/// </remarks>
	[Obsolete("Use IRepository<TEntity, TKey>.GetPageAsync(PageRequest, CancellationToken) for simple pagination instead.", false)]
	[ExcludeFromCodeCoverage]
	public static class PageableRepositoryExtensions {
		/// <summary>
		/// Gets a page of entities from the repository,
		/// given a page number and a page size
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key used to identify the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository from which the entities are retrieved.
		/// </param>
		/// <param name="page">
		/// The number of the page to retrieve from the repository.
		/// </param>
		/// <param name="size">
		/// The size of the page to retrieve from the repository.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <remarks>
		/// This method is a shortcut for the invocation of
		/// <see cref="IPageableRepository{TEntity,TKey}.GetPageAsync(PageQuery{TEntity}, CancellationToken)"/>,
		/// without filtering and sorting.
		/// </remarks>
		/// <returns>
		/// Returns an instance of <see cref="PageQueryResult{TEntity}"/> that
		/// is the result of the query.
		/// </returns>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Thrown when the given page number is less than 1, or
		/// if the given page size is less than 0.
		/// </exception>
		[Obsolete("Use IRepository<TEntity, TKey>.GetPageAsync(PageRequest, CancellationToken) instead.", false)]
		[ExcludeFromCodeCoverage]
		public static ValueTask<PageQueryResult<TEntity>> GetPageAsync<TEntity, TKey>(this IPageableRepository<TEntity, TKey> repository, int page, int size, CancellationToken cancellationToken = default)
			where TEntity : class
			=> repository.GetPageAsync(new PageQuery<TEntity>(page, size), cancellationToken);

		/// <summary>
		/// Gets a page of entities from the repository,
		/// given the request object that defines the scope
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key used to identify the entity.
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
		[Obsolete("Use IRepository<TEntity, TKey>.GetPageAsync(PageRequest, CancellationToken) for simple pagination, or the protected QueryPageAsync for filtered queries.", false)]
		[ExcludeFromCodeCoverage]
		public static PageQueryResult<TEntity> GetPage<TEntity, TKey>(this IPageableRepository<TEntity, TKey> repository, PageQuery<TEntity> request)
			where TEntity : class
			=> repository.GetPageAsync(request).GetAwaiter().GetResult();
	}
}
