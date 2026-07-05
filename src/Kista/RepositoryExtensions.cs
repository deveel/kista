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

using System.Linq.Expressions;

namespace Kista {
	/// <summary>
	/// Extends the functionalities of a repository instance
	/// to provide a set of utility methods to perform common operations
	/// </summary>
	public static class RepositoryExtensions {

		#region Add

		/// <summary>
		/// Adds a new entity in the repository synchronously
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity to add
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository to use to create the entity
		/// </param>
		/// <param name="entity">
		/// The instance of the entity to create
		/// </param>
		/// <returns>
		/// Returns a string that uniquely identifies the created entity
		/// within the underlying storage.
		/// </returns>
		public static void Add<TEntity>(this IRepository<TEntity> repository, TEntity entity)
            where TEntity : class
            => repository.AddAsync(entity).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// Adds a new entity in the repository synchronously
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity to add
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key that uniquely identifies the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository to use to create the entity
		/// </param>
		/// <param name="entity">
		/// The instance of the entity to create
		/// </param>
		/// <returns>
		/// Returns a string that uniquely identifies the created entity
		/// within the underlying storage.
		/// </returns>
		public static void Add<TEntity, TKey>(this IRepository<TEntity, TKey> repository, TEntity entity)
			where TEntity : class
			where TKey : notnull
			=> repository.AddAsync(entity).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// Adds a range of entities in the repository synchronously.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity to add
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository to use to create the entities.
		/// </param>
		/// <param name="entities">
		/// The enumeration of entities to add.
		/// </param>
		/// <remarks>
		/// <para>
		/// <strong>Warning:</strong> This is a blocking synchronous call that wraps an
		/// asynchronous operation using <c>ConfigureAwait(false).GetAwaiter().GetResult()</c>.
		/// Prefer <see cref="IRepository{TEntity,TKey}.AddRangeAsync(IEnumerable{TEntity}, CancellationToken)"/>
		/// in async contexts to avoid potential deadlocks.
		/// </para>
		/// </remarks>
		public static void AddRange<TEntity>(this IRepository<TEntity> repository, IEnumerable<TEntity> entities)
			where TEntity : class
			=> repository.AddRangeAsync(entities).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// Adds a range of entities in the repository synchronously.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity to add
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key that uniquely identifies the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository to use to create the entities.
		/// </param>
		/// <param name="entities">
		/// The enumeration of entities to add.
		/// </param>
		/// <remarks>
		/// <para>
		/// <strong>Warning:</strong> This is a blocking synchronous call that wraps an
		/// asynchronous operation using <c>ConfigureAwait(false).GetAwaiter().GetResult()</c>.
		/// Prefer <see cref="IRepository{TEntity,TKey}.AddRangeAsync(IEnumerable{TEntity}, CancellationToken)"/>
		/// in async contexts to avoid potential deadlocks.
		/// </para>
		/// </remarks>
		public static void AddRange<TEntity, TKey>(this IRepository<TEntity, TKey> repository, IEnumerable<TEntity> entities)
			where TEntity : class
			=> repository.AddRangeAsync(entities).ConfigureAwait(false).GetAwaiter().GetResult();


		#endregion

		#region Remove

		/// <summary>
		/// Removes an entity from the repository synchronously
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository from which the entity is removed
		/// </param>
		/// <param name="entity">
		/// The instance of the entity to remove
		/// </param>
		/// <returns>
		/// Returns <c>true</c> if the entity was removed successfully,
		/// otherwise <c>false</c>.
		/// </returns>
		/// <seealso cref="IRepository{TEntity,TKey}.RemoveAsync(TEntity, CancellationToken)"/>
		public static bool Remove<TEntity>(this IRepository<TEntity> repository, TEntity entity)
            where TEntity : class
            => repository.RemoveAsync(entity).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// Removes an entity from the repository synchronously
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key that uniquely identifies the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository from which the entity is removed
		/// </param>
		/// <param name="entity">
		/// The instance of the entity to remove
		/// </param>
		/// <returns>
		/// Returns <c>true</c> if the entity was removed successfully,
		/// otherwise <c>false</c>.
		/// </returns>
		/// <seealso cref="IRepository{TEntity,TKey}.RemoveAsync(TEntity, CancellationToken)"/>
		public static bool Remove<TEntity, TKey>(this IRepository<TEntity, TKey> repository, TEntity entity)
			where TEntity : class
			where TKey : notnull
			=> repository.RemoveAsync(entity).ConfigureAwait(false).GetAwaiter().GetResult();


		/// <summary>
		/// Removes an entity, identified by the given key,
		/// from the repository
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository from which the entity is removed
		/// </param>
		/// <param name="key">
		/// The key that uniquely identifies the entity to remove
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns <c>true</c> if the entity was removed successfully,
		/// otherwise it returns <c>false</c>.
		/// </returns>
		public static async ValueTask<bool> RemoveByKeyAsync<TEntity>(this IRepository<TEntity> repository, object key, CancellationToken cancellationToken = default)
            where TEntity : class {
            var entity = await repository.FindAsync(key, cancellationToken);
            if (entity == null)
                return false;

            return await repository.RemoveAsync(entity, cancellationToken);
        }

		/// <summary>
		/// Removes an entity, identified by the given key,
		/// from the repository
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key that uniquely identifies the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository from which the entity is removed
		/// </param>
		/// <param name="key">
		/// The key that uniquely identifies the entity to remove
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns <c>true</c> if the entity was removed successfully,
		/// otherwise it returns <c>false</c>.
		/// </returns>
		public static async ValueTask<bool> RemoveByKeyAsync<TEntity, TKey>(this IRepository<TEntity, TKey> repository, TKey key, CancellationToken cancellationToken = default)
			where TEntity : class {
			var entity = await repository.FindAsync(key, cancellationToken);
			if (entity == null)
				return false;

			return await repository.RemoveAsync(entity, cancellationToken);
		}

		/// <summary>
		/// Synchronously removes an entity, identified by the given key,
		/// from the repository
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository from which the entity is removed.
		/// </param>
		/// <param name="key">
		/// The key that uniquely identifies the entity to remove.
		/// </param>
		/// <returns>
		/// Returns <c>true</c> if the entity was removed successfully,
		/// otherwise it returns <c>false</c>.
		/// </returns>
		/// <seealso cref="IRepository{TEntity,TKey}.RemoveAsync(TEntity, CancellationToken)"/>
		public static bool RemoveByKey<TEntity>(this IRepository<TEntity> repository, object key)
            where TEntity : class
            => repository.RemoveByKeyAsync(key).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// Synchronously removes an entity, identified by the given key,
		/// from the repository
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key that uniquely identifies the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository from which the entity is removed.
		/// </param>
		/// <param name="key">
		/// The key that uniquely identifies the entity to remove.
		/// </param>
		/// <returns>
		/// Returns <c>true</c> if the entity was removed successfully,
		/// otherwise it returns <c>false</c>.
		/// </returns>
		/// <seealso cref="IRepository{TEntity,TKey}.RemoveAsync(TEntity, CancellationToken)"/>
		public static bool RemoveByKey<TEntity, TKey>(this IRepository<TEntity, TKey> repository, TKey key)
			where TEntity : class
			=> repository.RemoveByKeyAsync(key).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// Permanently removes an entity, identified by the given key,
		/// from the repository, bypassing any soft-delete behaviour.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository from which the entity is hard-deleted.
		/// </param>
		/// <param name="key">
		/// The key that uniquely identifies the entity to hard-delete.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns <c>true</c> if the entity was hard-deleted successfully,
		/// otherwise <c>false</c>.
		/// </returns>
		public static async ValueTask<bool> HardDeleteByKeyAsync<TEntity>(this IRepository<TEntity> repository, object key, CancellationToken cancellationToken = default)
			where TEntity : class {
			var entity = await repository.FindAsync(key, cancellationToken);
			if (entity == null)
				return false;

			return await repository.HardDeleteAsync(entity, cancellationToken);
		}

		/// <summary>
		/// Permanently removes an entity, identified by the given key,
		/// from the repository, bypassing any soft-delete behaviour.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key that uniquely identifies the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository from which the entity is hard-deleted.
		/// </param>
		/// <param name="key">
		/// The key that uniquely identifies the entity to hard-delete.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns <c>true</c> if the entity was hard-deleted successfully,
		/// otherwise <c>false</c>.
		/// </returns>
		public static async ValueTask<bool> HardDeleteByKeyAsync<TEntity, TKey>(this IRepository<TEntity, TKey> repository, TKey key, CancellationToken cancellationToken = default)
			where TEntity : class {
			var entity = await repository.FindAsync(key, cancellationToken);
			if (entity == null)
				return false;

			return await repository.HardDeleteAsync(entity, cancellationToken);
		}

		/// <summary>
		/// Synchronously and permanently removes an entity, identified by
		/// the given key, from the repository, bypassing any soft-delete
		/// behaviour.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository from which the entity is hard-deleted.
		/// </param>
		/// <param name="key">
		/// The key that uniquely identifies the entity to hard-delete.
		/// </param>
		/// <returns>
		/// Returns <c>true</c> if the entity was hard-deleted successfully,
		/// otherwise <c>false</c>.
		/// </returns>
		public static bool HardDeleteByKey<TEntity>(this IRepository<TEntity> repository, object key)
			where TEntity : class
			=> repository.HardDeleteByKeyAsync(key).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// Synchronously and permanently removes an entity, identified by
		/// the given key, from the repository, bypassing any soft-delete
		/// behaviour.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key that uniquely identifies the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository from which the entity is hard-deleted.
		/// </param>
		/// <param name="key">
		/// The key that uniquely identifies the entity to hard-delete.
		/// </param>
		/// <returns>
		/// Returns <c>true</c> if the entity was hard-deleted successfully,
		/// otherwise <c>false</c>.
		/// </returns>
		public static bool HardDeleteByKey<TEntity, TKey>(this IRepository<TEntity, TKey> repository, TKey key)
			where TEntity : class
			=> repository.HardDeleteByKeyAsync(key).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// Permanently removes an entity from the repository synchronously,
		/// bypassing any soft-delete behaviour.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository from which the entity is hard-deleted.
		/// </param>
		/// <param name="entity">
		/// The instance of the entity to hard-delete.
		/// </param>
		/// <returns>
		/// Returns <c>true</c> if the entity was hard-deleted successfully,
		/// otherwise <c>false</c>.
		/// </returns>
		public static bool HardDelete<TEntity>(this IRepository<TEntity> repository, TEntity entity)
			where TEntity : class
			=> repository.HardDeleteAsync(entity).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// Permanently removes an entity from the repository synchronously,
		/// bypassing any soft-delete behaviour.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key that uniquely identifies the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository from which the entity is hard-deleted.
		/// </param>
		/// <param name="entity">
		/// The instance of the entity to hard-delete.
		/// </param>
		/// <returns>
		/// Returns <c>true</c> if the entity was hard-deleted successfully,
		/// otherwise <c>false</c>.
		/// </returns>
		public static bool HardDelete<TEntity, TKey>(this IRepository<TEntity, TKey> repository, TEntity entity)
			where TEntity : class
			=> repository.HardDeleteAsync(entity).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// Removes a range of entities from the repository synchronously.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository from which the entities are removed.
		/// </param>
		/// <param name="entities">
		/// The enumeration of entities to remove.
		/// </param>
		/// <remarks>
		/// <para>
		/// <strong>Warning:</strong> This is a blocking synchronous call that wraps an
		/// asynchronous operation using <c>ConfigureAwait(false).GetAwaiter().GetResult()</c>.
		/// Prefer <see cref="IRepository{TEntity,TKey}.RemoveRangeAsync(IEnumerable{TEntity}, CancellationToken)"/>
		/// in async contexts to avoid potential deadlocks.
		/// </para>
		/// </remarks>
		public static void RemoveRange<TEntity>(this IRepository<TEntity> repository, IEnumerable<TEntity> entities)
			where TEntity : class
			=> repository.RemoveRangeAsync(entities).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// Removes a range of entities from the repository synchronously.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key that uniquely identifies the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository from which the entities are removed.
		/// </param>
		/// <param name="entities">
		/// The enumeration of entities to remove.
		/// </param>
		/// <remarks>
		/// <para>
		/// <strong>Warning:</strong> This is a blocking synchronous call that wraps an
		/// asynchronous operation using <c>ConfigureAwait(false).GetAwaiter().GetResult()</c>.
		/// Prefer <see cref="IRepository{TEntity,TKey}.RemoveRangeAsync(IEnumerable{TEntity}, CancellationToken)"/>
		/// in async contexts to avoid potential deadlocks.
		/// </para>
		/// </remarks>
		public static void RemoveRange<TEntity, TKey>(this IRepository<TEntity, TKey> repository, IEnumerable<TEntity> entities)
			where TEntity : class
			=> repository.RemoveRangeAsync(entities).ConfigureAwait(false).GetAwaiter().GetResult();


		#endregion

		#region Update

		/// <summary>
		/// Updates an entity in the repository synchronously
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository from which the entity is updated
		/// </param>
		/// <param name="entity">
		/// The instance of the entity to update
		/// </param>
		/// <returns>
		/// Returns <c>true</c> if the entity was updated successfully,
		/// otherwise <c>false</c>.
		/// </returns>
		public static bool Update<TEntity>(this IRepository<TEntity> repository, TEntity entity)
            where TEntity : class
            => repository.UpdateAsync(entity).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// Updates an entity in the repository synchronously
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key that uniquely identifies the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository from which the entity is updated
		/// </param>
		/// <param name="entity">
		/// The instance of the entity to update
		/// </param>
		/// <returns>
		/// Returns <c>true</c> if the entity was updated successfully,
		/// otherwise <c>false</c>.
		/// </returns>
		public static bool Update<TEntity, TKey>(this IRepository<TEntity, TKey> repository, TEntity entity)
			where TEntity : class
			=> repository.UpdateAsync(entity).ConfigureAwait(false).GetAwaiter().GetResult();


		#endregion

		#region GetPage

		/// <summary>
		/// Gets a page of entities from the repository,
		/// given a request object that defines the scope
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key that uniquely identifies the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository to use to retrieve the page.
		/// </param>
		/// <param name="request">
		/// The request object that defines the scope of the page to retrieve
		/// from the repository.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <remarks>
		/// <para>
		/// This method performs simple unsorted pagination using the
		/// <see cref="IRepository{TEntity, TKey}.GetPageAsync(PageRequest, CancellationToken)"/>
		/// contract. For filtered and sorted queries, use the protected
		/// <c>Repository{TEntity, TKey}.QueryPageAsync(PageQuery{TEntity}, CancellationToken)</c>
		/// method inside your repository implementation.
		/// </para>
		/// </remarks>
		/// <returns>
		/// Returns an instance of <see cref="PageResult{TEntity}"/> that
		/// represents the result of the query.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown when the repository does not support paging.
		/// </exception>
		public static ValueTask<PageResult<TEntity>> GetPageAsync<TEntity, TKey>(this IRepository<TEntity, TKey> repository, PageRequest request, CancellationToken cancellationToken = default)
			where TEntity : class
			=> repository.GetPageAsync(request, cancellationToken);

		/// <summary>
		/// Gets a page of entities from the repository, given
		/// the page number and the size
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key that uniquely identifies the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository to use to retrieve the page.
		/// </param>
		/// <param name="page">
		/// The number of the page, starting from 1, to retrieve from the repository.
		/// </param>
		/// <param name="size">
		/// The size of the page to retrieve from the repository.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns an instance of <see cref="PageResult{TEntity}"/> that
		/// represents the result of the query.
		/// </returns>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Thrown when the given page number is less than 1, or the given
		/// size is less than zero.
		/// </exception>
		public static ValueTask<PageResult<TEntity>> GetPageAsync<TEntity, TKey>(this IRepository<TEntity, TKey> repository, int page, int size, CancellationToken cancellationToken = default)
			where TEntity : class
			=> repository.GetPageAsync(new PageRequest(page, size), cancellationToken);

        /// <summary>
        /// Gets a page of entities from the repository, given
        /// the query object that defines the scope
        /// </summary>
        /// <typeparam name="TEntity">
        /// The type of entity handled by the repository.
        /// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key that uniquely identifies the entity.
		/// </typeparam>
        /// <param name="repository">
        /// The instance of the repository to use to retrieve the page.
        /// </param>
        /// <param name="request">
        /// The request object that defines the scope of the page to retrieve
        /// </param>
		/// <remarks>
		/// <para>
		/// This method performs simple unsorted pagination using the
		/// <see cref="IRepository{TEntity, TKey}.GetPageAsync(PageRequest, CancellationToken)"/>
		/// contract. For filtered and sorted queries, use the protected
		/// <c>Repository{TEntity, TKey}.QueryPageAsync(PageQuery{TEntity}, CancellationToken)</c>
		/// method inside your repository implementation.
		/// </para>
		/// </remarks>
        /// <returns>
        /// Returns an instance of <see cref="PageResult{TEntity}"/> that
        /// represents the paged result of the query.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// Thrown when the repository does not support paging.
        /// </exception>
		public static PageResult<TEntity> GetPage<TEntity, TKey>(this IRepository<TEntity, TKey> repository, PageRequest request)
			where TEntity : class
			=> repository.GetPageAsync(request).GetAwaiter().GetResult();

		/// <summary>
		/// Synchronously gets a page of entities from the repository, given
		/// the page number and the size.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key that uniquely identifies the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository to use to retrieve the page.
		/// </param>
		/// <param name="page">
		/// The number of the page, starting from 1, to retrieve from the repository.
		/// </param>
		/// <param name="size">
		/// The size of the page to retrieve from the repository.
		/// </param>
		/// <returns>
		/// Returns an instance of <see cref="PageResult{TEntity}"/> that
		/// represents the result of the query.
		/// </returns>
		/// <remarks>
		/// <para>
		/// <strong>Warning:</strong> This is a blocking synchronous call that wraps an
		/// asynchronous operation using <c>ConfigureAwait(false).GetAwaiter().GetResult()</c>.
		/// Prefer <see cref="GetPageAsync{TEntity, TKey}(IRepository{TEntity, TKey}, int, int, CancellationToken)"/>
		/// in async contexts to avoid potential deadlocks.
		/// </para>
		/// </remarks>
		/// <exception cref="NotSupportedException">
		/// Thrown when the repository does not support paging.
		/// </exception>
		public static PageResult<TEntity> GetPage<TEntity, TKey>(this IRepository<TEntity, TKey> repository, int page, int size)
			where TEntity : class
			=> repository.GetPage(new PageRequest(page, size));

		#endregion

		#region Find

		/// <summary>
		/// Finds a single entity in the repository, given the key
		/// that uniquely identifies it
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key that uniquely identifies the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository to use to find the entity.
		/// </param>
		/// <param name="key">
		/// The key that uniquely identifies the entity to find.
		/// </param>
		/// <returns>
		/// Returns an instance of <typeparamref name="TEntity"/> that
		/// is identified by the given key, or <c>null</c> if no entity
		/// with the given key exists in the repository.
		/// </returns>
		/// <seealso cref="IRepository{TEntity, TKey}.FindAsync(TKey, CancellationToken)"/>
		public static TEntity? Find<TEntity, TKey>(this IRepository<TEntity, TKey> repository, TKey key)
            where TEntity : class
            => repository.FindAsync(key).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// Synchronously finds a single entity in the repository, given the object key
		/// that uniquely identifies it, using the single-type-parameter repository contract.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository to use to find the entity.
		/// </param>
		/// <param name="key">
		/// The key that uniquely identifies the entity to find.
		/// </param>
		/// <returns>
		/// Returns an instance of <typeparamref name="TEntity"/> identified by
		/// the given key, or <c>null</c> if no entity with the given key exists.
		/// </returns>
		/// <remarks>
		/// <para>
		/// <strong>Warning:</strong> This is a blocking synchronous call that wraps an
		/// asynchronous operation using <c>ConfigureAwait(false).GetAwaiter().GetResult()</c>.
		/// Prefer <see cref="IRepository{TEntity,TKey}.FindAsync(TKey, CancellationToken)"/>
		/// in async contexts to avoid potential deadlocks.
		/// </para>
		/// </remarks>
		public static TEntity? Find<TEntity>(this IRepository<TEntity> repository, object key)
			where TEntity : class
			=> repository.FindAsync(key).ConfigureAwait(false).GetAwaiter().GetResult();

		#endregion

    }
}