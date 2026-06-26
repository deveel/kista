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
	/// An abstract base class for implementing the Specification pattern,
	/// providing composable AND, OR, and NOT operations.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of entity the specification applies to.
	/// </typeparam>
	public abstract class Specification<TEntity> : ISpecification<TEntity> where TEntity : class {
		/// <inheritdoc/>
		public abstract IQuery ToQuery();

		/// <summary>
		/// Combines two specifications with a logical AND.
		/// </summary>
		public static Specification<TEntity> operator &(Specification<TEntity> left, Specification<TEntity> right)
			=> new AndSpecification<TEntity>(left, right);

		/// <summary>
		/// Combines two specifications with a logical OR.
		/// </summary>
		public static Specification<TEntity> operator |(Specification<TEntity> left, Specification<TEntity> right)
			=> new OrSpecification<TEntity>(left, right);

		/// <summary>
		/// Negates the specification.
		/// </summary>
		public static Specification<TEntity> operator !(Specification<TEntity> spec)
			=> new NotSpecification<TEntity>(spec);
	}
}
