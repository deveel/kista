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
	/// The contract defining a repository of entities, accessible
	/// for read and write operations
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of entity handled by the repository.
	/// </typeparam>
	/// <typeparam name="TKey">
	/// The type of the unique identifier of the entity.
	/// </typeparam>
	public interface IRepository<TEntity, TKey> where TEntity : class {
		/// <summary>
		/// Gets the service provider associated with this repository, if any.
		/// </summary>
		/// <value>
		/// An <see cref="IServiceProvider"/> used to resolve infrastructure services
		/// such as expression caches, or <c>null</c> if no service provider is available.
		/// </value>
		/// <remarks>
		/// <para>
		/// The default implementation returns <c>null</c>. Repository implementations
		/// registered through dependency injection should expose their service provider
		/// through this property so that filters can resolve supporting infrastructure
		/// via <see cref="IFilterContext"/>.
		/// </para>
		/// <para>
		/// This property is primarily used internally by the repository to initialize
		/// filters before applying them to queries. Consumer code typically does not
		/// need to access this property directly.
		/// </para>
		/// </remarks>
		/// <seealso cref="IFilterContext"/>
		IServiceProvider? Services => null;

		/// <summary>
		/// Gets the unique identifier of the entity given
		/// </summary>
		/// <param name="entity">
		/// The instance of the entity to get the identifier of
		/// </param>
		/// <returns>
		/// Returns an object that is the unique identifier of the entity
		/// within the repository, or <c>null</c> if the entity is not
		/// identified.
		/// </returns>
		TKey? GetEntityKey(TEntity entity);

		/// <summary>
		/// Adds a new entity into the repository
		/// </summary>
		/// <example>
		/// <code>
		/// var repository = new MyEntityRepository(context);
		/// await repository.AddAsync(entity);
		/// </code>
		/// </example>
		/// <param name="entity">The entity to be added</param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns a task that will complete when the operation is completed
		/// </returns>
		/// <exception cref="RepositoryException">
		/// Thrown if it an error occurred while adding the entity
		/// </exception>
		/// <exception cref="ArgumentNullException">
		/// Thrown if the provided <paramref name="entity"/> is <c>null</c>
		/// </exception>
		ValueTask AddAsync(TEntity entity, CancellationToken cancellationToken = default);

		/// <summary>
		/// Adds a list of entities in the repository in one single operation
		/// </summary>
		/// <param name="entities">The enumeration of the entities to be added</param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <remarks>
		/// <para>
		/// The operation is intended to be <c>all-or-nothing</c> fashion, where it
		/// will succeed only if all the items in the list will be created. Anyway, the
		/// underlying storage system might have persisted some of the items before a
		/// failure.
		/// </para>
		/// </remarks>
		/// <returns>
		/// Returns a task that will complete when the operation is completed
		/// </returns>
		/// <exception cref="RepositoryException">
		/// Thrown if it an error occurred while adding one or more entities
		/// </exception>
		/// <exception cref="ArgumentNullException">
		/// Thrown if the provided list of <paramref name="entities"/> is <c>null</c>
		/// </exception>
		ValueTask AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);


		/// <summary>
		/// Updates an existing entity in the repository
		/// </summary>
		/// <param name="entity">The entity instance to be updated</param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns <c>true</c> if the entity was found and updated in 
		/// the repository, otherwise <c>false</c>
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown if the provided <paramref name="entity"/> is <c>null</c>
		/// </exception>
		/// <exception cref="RepositoryException">
		/// Thrown if it an error occurred while updating the entity
		/// </exception>
		ValueTask<bool> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

		/// <summary>
		/// Removes an entity from the repository
		/// </summary>
		/// <param name="entity">The entity to be deleted</param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns <c>true</c> if the entity was successfully removed 
		/// from the repository, otherwise <c>false</c>. 
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown if the provided <paramref name="entity"/> is <c>null</c>
		/// </exception>
		/// <exception cref="RepositoryException">
		/// Thrown if it an error occurred while removing the entity
		/// </exception>
		ValueTask<bool> RemoveAsync(TEntity entity, CancellationToken cancellationToken = default);

		/// <summary>
		/// Removes a list of entities from the repository in one 
		/// single operation.
		/// </summary>
		/// <param name="entities">
		/// The list of entities to be removed from the repository
		/// </param>
		/// <param name="cancellationToken">
		/// A token to cancel the operation
		/// </param>
		/// <returns>
		/// Returns a task that will complete when the operation is completed
		/// </returns>
		/// <exception cref="RepositoryException">
		/// Thrown if it an error occurred while removing one or more entities
		/// </exception>
		ValueTask RemoveRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

		/// <summary>
		/// Attempts to find in the repository an entity with the 
		/// given unique identifier
		/// </summary>
		/// <param name="key">The unique identifier of the entity to find</param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns the instance of the entity associated to the given <paramref name="key"/>,
		/// or <c>null</c> if none entity was found.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown if the provided <paramref name="key"/> is <c>null</c>
		/// </exception>
		ValueTask<TEntity?> FindAsync(TKey key, CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets a page of items from the repository using simple pagination
		/// without filtering or sorting.
		/// </summary>
		/// <param name="request">
		/// The page request specifying the page number and size.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns a <see cref="PageResult{TEntity}"/> containing the page
		/// items and pagination metadata.
		/// </returns>
		/// <exception cref="RepositoryException">
		/// Thrown if an error occurred while retrieving the page.
		/// </exception>
		ValueTask<PageResult<TEntity>> GetPageAsync(PageRequest request, CancellationToken cancellationToken = default);
	}
}