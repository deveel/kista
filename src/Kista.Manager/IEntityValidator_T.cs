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

using System.ComponentModel.DataAnnotations;

namespace Kista {
	/// <summary>
	/// A service used by the <see cref="EntityManager{TEntity}"/>
	/// to validate an entity before it is saved.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of the entity to be validated.
	/// </typeparam>
	public interface IEntityValidator<TEntity> where TEntity : class {
		/// <summary>
		/// Validates the given <paramref name="entity"/> asynchronously
		/// </summary>
		/// <param name="manager">
		/// The instance of the <see cref="EntityManager{TEntity}"/> that
		/// is performing the validation.
		/// </param>
		/// <param name="entity">
		/// The instance of the entity to be validated.
		/// </param>
		/// <param name="cancellationToken">
		/// A token that can be used to cancel the validation.
		/// </param>
		/// <returns>
		/// Returns an instance of <see cref="IAsyncEnumerable{T}"/> that
		/// can be used to iterate over the validation results.
		/// </returns>
		IAsyncEnumerable<ValidationResult> ValidateAsync(EntityManager<TEntity> manager, TEntity entity, CancellationToken cancellationToken = default);
	}
}
