// Copyright 2023-2026 Antonello Provenzano
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/LICENSE-2.0
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
	public sealed class OrSpecification<TEntity> : BinaryCompositeSpecification<TEntity> where TEntity : class {
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
		public OrSpecification(ISpecification<TEntity> left, ISpecification<TEntity> right)
			: base(left, right, FilterLogicalOperator.Or) {
		}
	}
}