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

namespace Kista {
	/// <summary>
	/// Represents a repository that can be filtered to retrieve a subset of
	/// the entities it contains.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The strongly typed entity that is stored in the repository
	/// </typeparam>
	/// <typeparam name="TKey">
	/// The type of key used to identify the entity in the repository
	/// </typeparam>
	[Obsolete("Use the abstract Kista.RepositoryBase<TEntity, TKey> base class instead. Filtering is now provided directly by the base class.", false)]
	public interface IFilterableRepository<TEntity, TKey> : IRepository<TEntity, TKey> where TEntity : class {
		/// <summary>
		/// Determines if at least one item in the repository exists for the
		/// given filtering conditions
		/// </summary>
		/// <param name="filter">The filter used to identify the items</param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns <c>true</c> if at least one item in the inventory matches the given
		/// filter, otherwise returns <c>false</c>
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown if the repository does not support filtering
		/// </exception>
		/// <exception cref="ArgumentException">
		/// Throw if the <paramref name="filter"/> is not supported by the repository
		/// </exception>
		ValueTask<bool> ExistsAsync(IQueryFilter filter, CancellationToken cancellationToken = default);

		/// <summary>
		/// Counts the number of items in the repository matching the given 
		/// filtering conditions
		/// </summary>
		/// <param name="filter">The filter used to identify the items</param>
		/// <param name="cancellationToken"></param>
		/// <remarks>
		/// Passing a <c>null</c> filter or passing <see cref="QueryFilter.Empty"/> as
		/// argument is equivalent to ask the repository not to use any filter, returning the 
		/// total count of all items int the inventory.
		/// </remarks>
		/// <returns>
		/// Returns the total count of items matching the given filtering conditions
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown if the repository does not support filtering
		/// </exception>
		/// <exception cref="ArgumentException">
		/// Throw if the <paramref name="filter"/> is not supported by the repository
		/// </exception>
		ValueTask<long> CountAsync(IQueryFilter filter, CancellationToken cancellationToken = default);

		/// <summary>
		/// Finds the first item in the repository that matches the given query
		/// </summary>
		/// <param name="query">
		/// The query definition used to identify the item to return
		/// and eventually sort the results.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation
		/// </param>
		/// <returns>
		/// Returns the first item in the repository that matches the given filtering condition,
		/// or <c>null</c> if none of the items matches the condition.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown if the repository does not support filtering
		/// </exception>
		/// <exception cref="ArgumentException">
		/// Throw if the <paramref name="query"/> defines a filter or a sort rule
		/// that is not supported by the repository
		/// </exception>
		ValueTask<TEntity?> FindFirstAsync(IQuery query, CancellationToken cancellationToken = default);

		/// <summary>
		/// Finds all the items in the repository that match the given filtering condition
		/// </summary>
		/// <param name="query">
		/// The query definition used to identify the item to return
		/// and eventually sort the results.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation
		/// </param>
		/// <returns>
		/// Returns a list of items in the repository that match the given query,
		/// or an empty list if none of the items matches the condition.
		/// </returns>
		ValueTask<IList<TEntity>> FindAllAsync(IQuery query, CancellationToken cancellationToken = default);
	}
}
