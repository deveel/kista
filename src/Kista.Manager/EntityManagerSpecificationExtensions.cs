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

using Deveel;

namespace Kista {
	/// <summary>
	/// Provides extension methods for <see cref="EntityManager{TEntity, TKey}"/>
	/// that accept <see cref="ISpecification{TEntity}"/> instances for domain-driven querying.
	/// </summary>
	public static class EntityManagerSpecificationExtensions {
		/// <summary>
		/// Finds the first entity matching the given specification.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity to search for.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the entity's key.
		/// </typeparam>
		/// <param name="manager">
		/// The entity manager to search through.
		/// </param>
		/// <param name="specification">
		/// The specification to match.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns an <see cref="OperationResult{TEntity}"/> containing the first
		/// entity matching the specification, or a failure result if none found.
		/// </returns>
		public static ValueTask<OperationResult<TEntity>> FindFirstAsync<TEntity, TKey>(this EntityManager<TEntity, TKey> manager, ISpecification<TEntity> specification, CancellationToken? cancellationToken = null)
			where TEntity : class
			where TKey : notnull {
			ArgumentNullException.ThrowIfNull(manager);
			ArgumentNullException.ThrowIfNull(specification);

			return manager.FindFirstAsync(specification.ToQuery(), cancellationToken);
		}

		/// <summary>
		/// Finds all entities matching the given specification.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity to search for.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the entity's key.
		/// </typeparam>
		/// <param name="manager">
		/// The entity manager to search through.
		/// </param>
		/// <param name="specification">
		/// The specification to match.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns a read-only list of entities matching the specification.
		/// </returns>
		public static ValueTask<IReadOnlyList<TEntity>> FindAllAsync<TEntity, TKey>(this EntityManager<TEntity, TKey> manager, ISpecification<TEntity> specification, CancellationToken? cancellationToken = null)
			where TEntity : class
			where TKey : notnull {
			ArgumentNullException.ThrowIfNull(manager);
			ArgumentNullException.ThrowIfNull(specification);

			return manager.FindAllAsync(specification.ToQuery(), cancellationToken);
		}

		/// <summary>
		/// Counts the number of entities matching the given specification.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity to count.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the entity's key.
		/// </typeparam>
		/// <param name="manager">
		/// The entity manager to count through.
		/// </param>
		/// <param name="specification">
		/// The specification to match.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns the number of entities matching the specification.
		/// </returns>
		public static ValueTask<long> CountAsync<TEntity, TKey>(this EntityManager<TEntity, TKey> manager, ISpecification<TEntity> specification, CancellationToken? cancellationToken = null)
			where TEntity : class
			where TKey : notnull {
			ArgumentNullException.ThrowIfNull(manager);
			ArgumentNullException.ThrowIfNull(specification);

			var query = specification.ToQuery();
			return manager.CountAsync(query.Filter ?? QueryFilter.Empty, cancellationToken);
		}
	}
}
