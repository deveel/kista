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
	/// A composite specification that combines two inner specifications
	/// with a logical OR.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of entity the specification applies to.
	/// </typeparam>
	public sealed class OrSpecification<TEntity> : Specification<TEntity> where TEntity : class {
		private readonly ISpecification<TEntity> left;
		private readonly ISpecification<TEntity> right;

		/// <summary>
		/// Constructs the specification that combines the two given
		/// specifications with a logical OR.
		/// </summary>
		/// <param name="left">
		/// The left-hand side specification.
		/// </param>
		/// <param name="right">
		/// The right-hand side specification.
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// Thrown if either of the given specifications is <c>null</c>.
		/// </exception>
		public OrSpecification(ISpecification<TEntity> left, ISpecification<TEntity> right) {
			ArgumentNullException.ThrowIfNull(left);
			ArgumentNullException.ThrowIfNull(right);

			this.left = left;
			this.right = right;
		}

		/// <inheritdoc/>
		public override IQuery ToQuery() {
			var leftQuery = left.ToQuery();
			var rightQuery = right.ToQuery();

			var filters = new List<IQueryFilter>(2);
			if (leftQuery.Filter != null && !leftQuery.Filter.IsEmpty())
				filters.Add(leftQuery.Filter);
			if (rightQuery.Filter != null && !rightQuery.Filter.IsEmpty())
				filters.Add(rightQuery.Filter);

			if (filters.Count == 0)
				return Query.Empty;

			var combined = filters.Count == 1
				? filters[0]
				: new CombinedQueryFilter(filters, FilterLogicalOperator.Or);

			return new Query(combined);
		}
	}
}
