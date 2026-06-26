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
	/// A composite specification that negates an inner specification.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of entity the specification applies to.
	/// </typeparam>
	public sealed class NotSpecification<TEntity> : Specification<TEntity> where TEntity : class {
		private readonly ISpecification<TEntity> inner;

		/// <summary>
		/// Constructs the specification that negates the given inner specification.
		/// </summary>
		/// <param name="inner">
		/// The specification to negate.
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// Thrown if the given specification is <c>null</c>.
		/// </exception>
		public NotSpecification(ISpecification<TEntity> inner) {
			ArgumentNullException.ThrowIfNull(inner);
			this.inner = inner;
		}

		/// <inheritdoc/>
		public override IQuery ToQuery() {
			var innerQuery = inner.ToQuery();

			if (innerQuery.Filter == null || innerQuery.Filter.IsEmpty())
				return Query.Empty;

			var negated = new NotQueryFilter(innerQuery.Filter);
			return new Query(negated);
		}
	}
}
