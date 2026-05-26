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

using System;

namespace Kista {
	/// <summary>
	/// The strongly typed page from a repository, obtained from 
	/// a paginated query
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of entity handled by the repository.
	/// </typeparam>
	/// <seealso cref="PageQuery{TEntity}"/>
	/// <seealso cref="IPageableRepository{TEntity,TKey}.GetPageAsync(PageQuery{TEntity}, CancellationToken)"/>
	public class PageResult<TEntity> where TEntity : class {
		/// <summary>
		/// Constructs the result referencing the original request, a count
		/// of the items in the repository and optionally a list of items in the page
		/// </summary>
		/// <param name="request">The original page request</param>
		/// <param name="totalItems">The total number of items in the context
		/// of the request given (filtered and sorted).</param>
		/// <param name="items">The list of items included in the page</param>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Thrown if the number of total items is smaller than zero.
		/// </exception>
		/// <exception cref="ArgumentNullException">
		/// Thrown if the <paramref name="request"/> is <c>null</c>.
		/// </exception>
		public PageResult(PageQuery<TEntity> request, int totalItems, IEnumerable<TEntity>? items = null) {
			if (totalItems < 0)
				throw new ArgumentOutOfRangeException(nameof(totalItems), "The number of total items must be zero or more");

			Request = request ?? throw new ArgumentNullException(nameof(request));
			TotalItems = totalItems;
			Items = items?.ToList().AsReadOnly();
		}

		/// <summary>
		/// Gets a reference to the request
		/// </summary>
		public PageQuery<TEntity> Request { get; }

		/// <summary>
		/// Gets a count of the total items in the repository
		/// for the context of the request
		/// </summary>
		public int TotalItems { get; }

		/// <summary>
		/// Gets a list of items included in the page
		/// </summary>
		public IReadOnlyList<TEntity>? Items { get; set; }

		/// <summary>
		/// Gets a count of the total available pages
		/// that can be requested from the repository
		/// </summary>
		public int TotalPages => (int)Math.Ceiling((double)TotalItems / Request.Size);

		/// <summary>
		/// Gets a value indicating if the current page is the first
		/// in the context of the request
		/// </summary>
		public bool IsFirstPage => Request.Page == 1;

		/// <summary>
		/// Gets a value indicating if the current page is the last
		/// in the context of the request
		/// </summary>
		public bool IsLastPage => Request.Page == TotalPages;

		/// <summary>
		/// Gets a value indicating if there is a next page
		/// in the context of the request
		/// </summary>
		public bool HasNextPage => !IsLastPage;

		/// <summary>
		/// Gets a value indicating if there is a previous page
		/// in the context of the request
		/// </summary>
		public bool HasPreviousPage => !IsFirstPage;

		/// <summary>
		/// When there is a next page, gets the number of the next page
		/// in the context of the request, or <c>null</c> if there is no
		/// page after the current one.
		/// </summary>
		public int? NextPage => HasNextPage ? Request.Page + 1 : null;

		/// <summary>
		/// When there is a previous page, gets the number of the previous page
		/// in the context of the request, or <c>null</c> if there is no
		/// page before the current one.
		/// </summary>
		public int? PreviousPage => HasPreviousPage ? Request.Page - 1 : null;

		/// <summary>
		/// Creates an empty page response to the given request
		/// </summary>
		/// <param name="page">
		/// The request that originated the page
		/// </param>
		/// <returns>
		/// Returns a new instance of <see cref="PageResult{TEntity}"/> that
		/// represents an empty page.
		/// </returns>
		public static PageResult<TEntity> Empty(PageQuery<TEntity> page) => new PageResult<TEntity>(page, 0);
	}
}