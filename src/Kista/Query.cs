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

using System.Linq.Expressions;

namespace Kista {
	/// <summary>
	/// A query that can be applied to a repository
	/// to filter and sort the results.
	/// </summary>
	public readonly struct Query : IQuery {
		/// <summary>
		/// Constructs the query with the given filter and sort.
		/// </summary>
		/// <param name="filter">
		/// The filter to apply to the query.
		/// </param>
		/// <param name="sort">
		/// An optional sort to apply to the query.
		/// </param>
		public Query(IQueryFilter filter, IQueryOrder? sort = null) {
			ArgumentNullException.ThrowIfNull(filter, nameof(filter));

			Filter = filter;
			Order = sort;
		}

		/// <summary>
		/// Gets the filter to apply to the query.
		/// </summary>
		public IQueryFilter Filter { get; }

		/// <summary>
		/// Gets the sort to apply to the results
		/// of the query.
		/// </summary>
		public IQueryOrder? Order { get; }

		/// <summary>
		/// Represents an empty query, that will apply
		/// no filter and no sort.
		/// </summary>
		public static IQuery Empty { get; } = new EmptyQuery();

		/// <summary>
		/// Creates a new query with the given filter.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity to filter.
		/// </typeparam>
		/// <param name="filter">
		/// The filter to apply to the query.
		/// </param>
		/// <returns>
		/// Returns a new <see cref="Query"/> that applies
		/// the given filter.
		/// </returns>
		public static Query Where<TEntity>(Expression<Func<TEntity, bool>>? filter)
			where TEntity : class
			=> new Query(filter == null ? QueryFilter.Empty : QueryFilter.Where<TEntity>(filter));


		readonly struct EmptyQuery : IQuery {
			/// <inheritdoc/>
			public IQueryFilter? Filter => QueryFilter.Empty;

			/// <inheritdoc/>
			public IQueryOrder? Order => null;
		}
	}
}
