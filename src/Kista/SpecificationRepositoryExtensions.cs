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
	/// Provides extension methods for <see cref="IRepository{TEntity, TKey}"/>
	/// that accept <see cref="ISpecification{TEntity}"/> instances for domain-driven querying.
	/// </summary>
	public static class SpecificationRepositoryExtensions {
		const string NotFilterableMessage = "The repository does not support filtering";
		/// <summary>
		/// Finds the first entity matching the given specification.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity to search for.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the entity's key.
		/// </typeparam>
		/// <param name="repository">
		/// The repository to search in.
		/// </param>
		/// <param name="specification">
		/// The specification to match.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns the first entity matching the specification, or <c>null</c> if none found.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown when the repository does not support filtering.
		/// </exception>
		public static ValueTask<TEntity?> FindFirstAsync<TEntity, TKey>(this IRepository<TEntity, TKey> repository, ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
			where TEntity : class {
			ArgumentNullException.ThrowIfNull(repository);
			ArgumentNullException.ThrowIfNull(specification);

			if (!(repository is Repository<TEntity, TKey> repo))
				throw new NotSupportedException(NotFilterableMessage);

			var query = specification.ToQuery();
			return repo.FindFirstAsyncInternal(query, cancellationToken);
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
		/// <param name="repository">
		/// The repository to search in.
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
		/// <exception cref="NotSupportedException">
		/// Thrown when the repository does not support filtering.
		/// </exception>
		public static ValueTask<IReadOnlyList<TEntity>> FindAllAsync<TEntity, TKey>(this IRepository<TEntity, TKey> repository, ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
			where TEntity : class {
			ArgumentNullException.ThrowIfNull(repository);
			ArgumentNullException.ThrowIfNull(specification);

			if (!(repository is Repository<TEntity, TKey> repo))
				throw new NotSupportedException(NotFilterableMessage);

			var query = specification.ToQuery();
			return repo.FindAllAsyncInternal(query, cancellationToken);
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
		/// <param name="repository">
		/// The repository to count in.
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
		/// <exception cref="NotSupportedException">
		/// Thrown when the repository does not support filtering.
		/// </exception>
		public static ValueTask<long> CountAsync<TEntity, TKey>(this IRepository<TEntity, TKey> repository, ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
			where TEntity : class {
			ArgumentNullException.ThrowIfNull(repository);
			ArgumentNullException.ThrowIfNull(specification);

			if (!(repository is Repository<TEntity, TKey> repo))
				throw new NotSupportedException(NotFilterableMessage);

			var filter = specification.ToQuery().Filter;
			return repo.CountAsyncInternal(filter, cancellationToken);
		}

		/// <summary>
		/// Determines whether any entity in the repository matches the given specification.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity to check.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the entity's key.
		/// </typeparam>
		/// <param name="repository">
		/// The repository to check in.
		/// </param>
		/// <param name="specification">
		/// The specification to match.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns <c>true</c> if at least one entity matches the specification,
		/// or <c>false</c> otherwise.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown when the repository does not support filtering.
		/// </exception>
		public static async ValueTask<bool> ExistsAsync<TEntity, TKey>(this IRepository<TEntity, TKey> repository, ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
			where TEntity : class {
			ArgumentNullException.ThrowIfNull(repository);
			ArgumentNullException.ThrowIfNull(specification);

			if (!(repository is Repository<TEntity, TKey> repo))
				throw new NotSupportedException(NotFilterableMessage);

			var filter = specification.ToQuery().Filter;
			return await repo.ExistsAsyncInternal(filter, cancellationToken).ConfigureAwait(false);
		}
	}
}
