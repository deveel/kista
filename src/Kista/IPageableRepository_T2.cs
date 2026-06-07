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

using System;
using System.Diagnostics.CodeAnalysis;

namespace Kista {
	/// <summary>
	/// Represents a repository that is capable of returning a page of items
	/// of the given type contained in the underlying storage.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The strongly typed entity that is stored in the repository
	/// </typeparam>
	/// <typeparam name="TKey">
	/// The type of the key that uniquely identifies the entity
	/// </typeparam>
	/// <remarks>
	/// <para>
	/// This contract is obsolete. The <c>GetPageAsync(PageQuery{TEntity})</c>
	/// method it exposes leaks the <see cref="IQuery"/> capability into consumer
	/// code through the <see cref="PageQuery{TEntity}.Query"/> property.
	/// </para>
	/// <para>
	/// Inherit from the abstract <see cref="Repository{TEntity,TKey}"/>
	/// base class instead: it hides the query translation pipeline behind
	/// <c>protected</c> members and provides a public
	/// <c>GetPageAsync(PageRequest, CancellationToken)</c> for simple
	/// unsorted pagination, plus a protected
	/// <c>QueryPageAsync(PageQuery{TEntity}, CancellationToken)</c> for
	/// filtered/sorted queries inside the data layer.
	/// </para>
	/// </remarks>
	[Obsolete("Use the abstract Kista.Repository<TEntity, TKey> base class instead. Paging is now provided directly by the base class.", false)]
	public interface IPageableRepository<TEntity, TKey> : IRepository<TEntity, TKey> where TEntity : class {
		/// <summary>
		/// Gets a page of items from the repository
		/// </summary>
		/// <param name="request">The request to obtain a given page from the repository. This
		/// object provides the number of the page, the size of the items to return, filters and
		/// sorting order.</param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns an instance of <see cref="PageQueryResult{TEntity}"/> that provides the
		/// page items and a count of total items.
		/// </returns>
		/// <exception cref="RepositoryException">
		/// Thrown if an error occurred while retrieving the page
		/// </exception>
		/// <exception cref="NotSupportedException">
		/// Thrown if the filters or the sorting capabilities are not provided by the
		/// implementation of the repository
		/// </exception>
		/// <seealso cref="PageQueryResult{TEntity}"/>
		[Obsolete("Use the abstract Kista.Repository<TEntity, TKey> base class instead. Paging is now provided directly by the base class.", false)]
		[ExcludeFromCodeCoverage]
		ValueTask<PageQueryResult<TEntity>> GetPageAsync(PageQuery<TEntity> request, CancellationToken cancellationToken = default);
	}
}
