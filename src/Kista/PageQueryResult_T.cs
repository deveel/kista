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
	/// The strongly typed page from a repository, obtained from 
	/// a paginated query
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of entity handled by the repository.
	/// </typeparam>
	/// <seealso cref="PageQuery{TEntity}"/>
	/// <seealso cref="Repository{TEntity,TKey}.QueryPageAsync(PageQuery{TEntity}, CancellationToken)"/>
	public class PageQueryResult<TEntity> : PageResult<TEntity> where TEntity : class {
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
		public PageQueryResult(PageQuery<TEntity> request, int totalItems, IEnumerable<TEntity>? items = null)
            : base(request, totalItems, items) {
		}

		/// <summary>
		/// Gets a reference to the request
		/// </summary>
		public new PageQuery<TEntity> Request => (PageQuery<TEntity>)base.Request;
        
		/// <summary>
		/// Creates an empty page response to the given request
		/// </summary>
		/// <param name="page">
		/// The request that originated the page
		/// </param>
		/// <returns>
		/// Returns a new instance of <see cref="PageQueryResult{TEntity}"/> that
		/// represents an empty page.
		/// </returns>
		public static PageQueryResult<TEntity> Empty(PageQuery<TEntity> page) => new PageQueryResult<TEntity>(page, 0);
	}
}