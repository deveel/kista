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
	/// Extends the functionalities of a repository instance
	/// to provide a set of utility methods to perform common operations
	/// </summary>
	public static class RepositoryExtensions {
		private static bool IsFilterable<TEntity, TKey>(this IRepository<TEntity, TKey> repository) where TEntity : class {
			return repository is IFilterableRepository<TEntity, TKey>;
		}

		private static bool IsQueryable<TEntity, TKey>(this IRepository<TEntity, TKey> repository) where TEntity : class {
			return repository is IQueryableRepository<TEntity, TKey>;
		}

		private static bool IsPageable<TEntity, TKey>(this IRepository<TEntity, TKey> repository) where TEntity : class {
			return repository is IPageableRepository<TEntity, TKey>;
		}

		private static IPageableRepository<TEntity, TKey> RequirePageable<TEntity, TKey>(this IRepository<TEntity, TKey> repository) where TEntity : class {
			if (!(repository is IPageableRepository<TEntity, TKey> pageable))
				throw new NotSupportedException("The repository is not pageable");

			return pageable;
		}

		#region AsFilterable

		/// <summary>
		/// Gets a version of the repository that is filterable
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity to filter
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository to get the filterable version.
		/// </param>
		/// <returns>
		/// Returns an instance of <see cref="IFilterableRepository{TEntity}"/>
		/// that can be used to filter the entities in the repository.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown when the repository is not filterable
		/// </exception>
		public static IFilterableRepository<TEntity> AsFilterable<TEntity>(this IRepository<TEntity> repository)
			where TEntity : class {
			if (!(repository is IFilterableRepository<TEntity> filterable))
				throw new NotSupportedException("The repository is not filterable");

			// TODO: If the repository is a queryable, we can wrap it
			//        in a filterable version, so that we can use the filter

			return filterable;
		}

		/// <summary>
		/// Gets a version of the repository that is filterable
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity to filter
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key that uniquely identifies the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository to get the filterable version.
		/// </param>
		/// <returns>
		/// Returns an instance of <see cref="IFilterableRepository{TEntity}"/>
		/// that can be used to filter the entities in the repository.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown when the repository is not filterable
		/// </exception>
		public static IFilterableRepository<TEntity, TKey> AsFilterable<TEntity, TKey>(this IRepository<TEntity, TKey> repository)
			where TEntity : class {
			if (!(repository is IFilterableRepository<TEntity, TKey> filterable))
				throw new NotSupportedException("The repository is not filterable");

			// TODO: If the repository is a queryable, we can wrap it
			//        in a filterable version, so that we can use the filter

			return filterable;
		}


		/// <summary>
		/// Gets a version of the repository that is queryable.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity to query
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository to get the queryable version.
		/// </param>
		/// <returns>
		/// Returns an instance of <see cref="IQueryableRepository{TEntity}"/>
		/// if the repository is queryable, otherwise it throws an exception.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown when the repository is not queryable
		/// </exception>
		public static IQueryableRepository<TEntity> AsQueryable<TEntity>(this IRepository<TEntity> repository)
			where TEntity : class {
			if (!(repository is IQueryableRepository<TEntity> queryable))
				throw new NotSupportedException("The repository is not queryable");

			return queryable;
		}

		/// <summary>
		/// Gets a version of the repository that is queryable.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity to query
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key that uniquely identifies the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository to get the queryable version.
		/// </param>
		/// <returns>
		/// Returns an instance of <see cref="IQueryableRepository{TEntity}"/>
		/// if the repository is queryable, otherwise it throws an exception.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown when the repository is not queryable
		/// </exception>
		public static IQueryableRepository<TEntity, TKey> AsQueryable<TEntity, TKey>(this IRepository<TEntity, TKey> repository)
			where TEntity : class {
			if (!(repository is IQueryableRepository<TEntity, TKey> queryable))
				throw new NotSupportedException("The repository is not queryable");

			return queryable;
		}


		#endregion


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
		/// This method attempts to cast the given repository to a
		/// <see cref="IPageableRepository{TEntity}"/> and invoke the
		/// native method <see cref="IPageableRepository{TEntity, TKey}.GetPageAsync(PageQuery{TEntity}, CancellationToken)"/>.
		/// </para>
		/// <para>
		/// If the repository does not implement the interface, the method
		/// attempts to cast it to a <see cref="IQueryableRepository{TEntity}"/>
		/// and invoke the a paging operation on the <see cref="IQueryable{T}"/>.
		/// </para>
		/// </remarks>
		/// <returns>
		/// Returns an instance of <see cref="PageResult{TEntity}"/> that
		/// represents the result of the query.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown when the repository does not support paging.
		/// </exception>
		public static ValueTask<PageResult<TEntity>> GetPageAsync<TEntity, TKey>(this IRepository<TEntity, TKey> repository, PageQuery<TEntity> request, CancellationToken cancellationToken = default)
			where TEntity : class {
			if (repository.IsPageable())
				return repository.RequirePageable().GetPageAsync(request, cancellationToken);
			if (repository.IsQueryable())
				return new ValueTask<PageResult<TEntity>>(repository.AsQueryable().GetPage(request));

			throw new NotSupportedException("The repository does not support paging");
		}

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
		/// <exception cref="NotSupportedException">
		/// Thrown when the repository does not support paging.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Thrown when the given page number is less than 1, or the given
		/// size is less than zero.
		/// </exception>
		public static ValueTask<PageResult<TEntity>> GetPageAsync<TEntity, TKey>(this IRepository<TEntity, TKey> repository, int page, int size, CancellationToken cancellationToken = default)
			where TEntity : class
			=> repository.GetPageAsync(new PageQuery<TEntity>(page, size), cancellationToken);

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
        /// <param name="query">
        /// The query object that defines the scope of the page to retrieve
        /// </param>
		/// <remarks>
		/// <para>
		/// This method attempts to cast the given repository to a
		/// <see cref="IPageableRepository{TEntity}"/> and invoke the
		/// native method <see cref="IPageableRepository{TEntity, TKey}.GetPageAsync(PageQuery{TEntity}, CancellationToken)"/>.
		/// </para>
		/// <para>
		/// If the repository does not implement the interface, the method
		/// attempts to cast it to a <see cref="IQueryableRepository{TEntity}"/>
		/// and invoke the a paging operation on the <see cref="IQueryable{T}"/>.
		/// </para>
		/// </remarks>
        /// <returns>
        /// Returns an instance of <see cref="PageResult{TEntity}"/> that
        /// represents the paged result of the query.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// Thrown when the repository does not support paging or querying.
        /// </exception>
		public static PageResult<TEntity> GetPage<TEntity, TKey>(this IRepository<TEntity, TKey> repository, PageQuery<TEntity> query)
			where TEntity : class {
			if (repository.IsPageable())
				return repository.RequirePageable().GetPage(query);
			if (repository.IsQueryable())
				return repository.AsQueryable().GetPage(query);

			throw new NotSupportedException("The repository does not support paging");
		}

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
			=> repository.GetPage(new PageQuery<TEntity>(page, size));

		#endregion

		#region Exists

		/// <summary>
		/// Checks if an entity exists in the repository,
		/// that matches the given filter
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key that uniquely identifies the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository to use to check the existence 
		/// of any entity that matches the given filter.
		/// </param>
		/// <param name="filter">
		/// The filtering expression to use to check the existence of
		/// any matching entity.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns <c>true</c> if any entity exists in the repository
		/// that matches the given filter, otherwise <c>false</c>.
		/// </returns>
		public static ValueTask<bool> ExistsAsync<TEntity, TKey>(this IRepository<TEntity, TKey> repository, Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default)
            where TEntity : class
            => repository.ExistsAsync(new ExpressionQueryFilter<TEntity>(filter), cancellationToken);

		/// <summary>
		/// Checks if an entity exists in the repository,
		/// that matches the given filter
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key that uniquely identifies the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository to use to check the existence
		/// of any entity that matches the given filter.
		/// </param>
		/// <param name="filter">
		/// The filter used to check the existence of any matching entity.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
        /// <remarks>
        /// <para>
        /// This method attempts to cast the given repository to a
        /// <see cref="IFilterableRepository{TEntity}"/> to invoke the
        /// native method <see cref="IFilterableRepository{TEntity, TKey}.ExistsAsync(IQueryFilter, CancellationToken)"/>.
        /// </para>
        /// <para>
        /// When the repository does not implement the interface, the method
        /// attempts to cast it to a <see cref="IQueryableRepository{TEntity}"/>
        /// to invoke the a query operation on the <see cref="IQueryable{T}"/>.
        /// </para>
        /// </remarks>
		/// <returns>
		/// Returns <c>true</c> if any entity exists in the repository,
		/// or <c>false</c> if not.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown when the repository does not support querying or filtering.
		/// </exception>
		public static ValueTask<bool> ExistsAsync<TEntity, TKey>(this IRepository<TEntity, TKey> repository, IQueryFilter filter, CancellationToken cancellationToken = default)
			where TEntity : class {
			if (repository.IsFilterable())
				return repository.AsFilterable().ExistsAsync(filter, cancellationToken);
			if (repository.IsQueryable())
				return new ValueTask<bool>(repository.AsQueryable().AsQueryable().Any(filter.AsLambda<TEntity>()));

			throw new NotSupportedException("The repository does not support querying");
		}

		/// <summary>
		/// Synchronously checks if an entity exists in the repository,
		/// that matches the given filter
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key that uniquely identifies the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository to use to check the existence
		/// of any entity that matches the given filter.
		/// </param>
		/// <param name="filter">
		/// The filter used to check the existence of any matching entity.
		/// </param>
		/// <returns>
		/// Returns <c>true</c> if any entity exists in the repository
		/// that matches the given filter, otherwise <c>false</c>.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown when the repository does not support querying or filtering.
		/// </exception>
		public static bool Exists<TEntity, TKey>(this IRepository<TEntity, TKey> repository, IQueryFilter filter)
			where TEntity : class
			=> repository.ExistsAsync(filter).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// Synchronously checks if an entity exists in the repository,
		/// given a filter expression
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key that uniquely identifies the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository to use to check the existence
		/// of any entity that matches the given filter.
		/// </param>
		/// <param name="filter">
		/// The filter expression used to check the existence of any matching entity.
		/// </param>
		/// <returns>
		/// Returns <c>true</c> if any entity exists in the repository,
		/// otherwise it returns <c>false</c>.
		/// </returns>
		/// <seealso cref="IFilterableRepository{TEntity, TKey}.ExistsAsync(IQueryFilter, CancellationToken)"/>
        public static bool Exists<TEntity, TKey>(this IRepository<TEntity, TKey> repository, Expression<Func<TEntity, bool>> filter)
            where TEntity : class
            => Exists(repository, new ExpressionQueryFilter<TEntity>(filter));

		/// <summary>
		/// Checks if an entity exists in the repository that matches the given filter,
		/// using the single-type-parameter repository contract.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository to use to check the existence
		/// of any entity that matches the given filter.
		/// </param>
		/// <param name="filter">
		/// The filter used to check the existence of any matching entity.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns <c>true</c> if any entity exists in the repository
		/// that matches the given filter, otherwise <c>false</c>.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown when the repository does not support querying or filtering.
		/// </exception>
		public static ValueTask<bool> ExistsAsync<TEntity>(this IRepository<TEntity> repository, IQueryFilter filter, CancellationToken cancellationToken = default)
			where TEntity : class
			=> ExistsAsync<TEntity, object>(repository, filter, cancellationToken);

		/// <summary>
		/// Checks if an entity exists in the repository that matches the given expression filter,
		/// using the single-type-parameter repository contract.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository to use to check the existence
		/// of any entity that matches the given filter.
		/// </param>
		/// <param name="filter">
		/// The filter expression used to check the existence of any matching entity.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns <c>true</c> if any entity exists in the repository
		/// that matches the given filter, otherwise <c>false</c>.
		/// </returns>
		public static ValueTask<bool> ExistsAsync<TEntity>(this IRepository<TEntity> repository, Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default)
			where TEntity : class
			=> ExistsAsync<TEntity, object>(repository, filter, cancellationToken);

		/// <summary>
		/// Synchronously checks if an entity exists in the repository that matches
		/// the given filter, using the single-type-parameter repository contract.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository to use to check the existence
		/// of any entity that matches the given filter.
		/// </param>
		/// <param name="filter">
		/// The filter used to check the existence of any matching entity.
		/// </param>
		/// <returns>
		/// Returns <c>true</c> if any entity exists in the repository
		/// that matches the given filter, otherwise <c>false</c>.
		/// </returns>
		/// <remarks>
		/// <para>
		/// <strong>Warning:</strong> This is a blocking synchronous call that wraps an
		/// asynchronous operation using <c>ConfigureAwait(false).GetAwaiter().GetResult()</c>.
		/// Prefer <see cref="ExistsAsync{TEntity}(IRepository{TEntity}, IQueryFilter, CancellationToken)"/>
		/// in async contexts to avoid potential deadlocks.
		/// </para>
		/// </remarks>
		public static bool Exists<TEntity>(this IRepository<TEntity> repository, IQueryFilter filter)
			where TEntity : class
			=> repository.ExistsAsync(filter).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// Synchronously checks if an entity exists in the repository given a filter expression,
		/// using the single-type-parameter repository contract.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository to use to check the existence
		/// of any entity that matches the given filter.
		/// </param>
		/// <param name="filter">
		/// The filter expression used to check the existence of any matching entity.
		/// </param>
		/// <returns>
		/// Returns <c>true</c> if any entity exists in the repository
		/// that matches the given filter, otherwise <c>false</c>.
		/// </returns>
		/// <remarks>
		/// <para>
		/// <strong>Warning:</strong> This is a blocking synchronous call that wraps an
		/// asynchronous operation using <c>ConfigureAwait(false).GetAwaiter().GetResult()</c>.
		/// Prefer <see cref="ExistsAsync{TEntity}(IRepository{TEntity}, Expression{Func{TEntity, bool}}, CancellationToken)"/>
		/// in async contexts to avoid potential deadlocks.
		/// </para>
		/// </remarks>
		public static bool Exists<TEntity>(this IRepository<TEntity> repository, Expression<Func<TEntity, bool>> filter)
			where TEntity : class
			=> repository.ExistsAsync(filter).ConfigureAwait(false).GetAwaiter().GetResult();

        #endregion

        #region Count

		/// <summary>
		/// Counts the number of entities in the repository,
		/// given a filter expression
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key that uniquely identifies the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository to use to count the entities.
		/// </param>
		/// <param name="filter">
		/// A filter expression used to count the matching entities.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns the number of entities in the repository that match
		/// the given filter.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown when the repository does not support querying or filtering.
		/// </exception>
        public static ValueTask<long> CountAsync<TEntity, TKey>(this IRepository<TEntity, TKey> repository, Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default)
            where TEntity : class {
			if (repository.IsFilterable())
				return repository.AsFilterable().CountAsync(new ExpressionQueryFilter<TEntity>(filter), cancellationToken);
			if (repository.IsQueryable())
				return new ValueTask<long>(repository.AsQueryable().AsQueryable().LongCount(filter));

			throw new NotSupportedException("The repository does not support querying");
		}

		/// <summary>
		/// Counts the number of entities in the repository
		/// that match the given query filter.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key that uniquely identifies the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository to use to count the entities.
		/// </param>
		/// <param name="filter">
		/// The filter used to count the matching entities.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns the number of entities in the repository that match
		/// the given filter.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown when the repository does not support querying or filtering.
		/// </exception>
		public static ValueTask<long> CountAsync<TEntity, TKey>(this IRepository<TEntity, TKey> repository, IQueryFilter filter, CancellationToken cancellationToken = default)
			where TEntity : class {
			if (repository.IsFilterable())
				return repository.AsFilterable().CountAsync(filter, cancellationToken);
			if (repository.IsQueryable())
				return new ValueTask<long>(repository.AsQueryable().AsQueryable().LongCount(filter.AsLambda<TEntity>()));

			throw new NotSupportedException("The repository does not support querying");
		}

		/// <summary>
		/// Counts the number of entities in the repository
		/// </summary>
		/// <typeparam name="TEntity">
        /// The type of entity handled by the repository.
        /// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key that uniquely identifies the entity.
		/// </typeparam>
		/// <param name="repository">
        /// The instance of the repository to use to count the entities.
        /// </param>
		/// <param name="cancellationToken">
        /// A token used to cancel the operation.
        /// </param>
		/// <returns>
        /// Returns the number of entities in the repository.
        /// </returns>
		/// <exception cref="NotSupportedException">
        /// Thrown when the repository does not support querying or filtering.
        /// </exception>
        /// <seealso cref="IFilterableRepository{TEntity, TKey}.CountAsync(IQueryFilter, CancellationToken)"/>
		public static ValueTask<long> CountAllAsync<TEntity, TKey>(this IRepository<TEntity, TKey> repository, CancellationToken cancellationToken = default)
			where TEntity : class {
			if (repository.IsFilterable())
				return repository.AsFilterable().CountAsync(QueryFilter.Empty, cancellationToken);
			if (repository.IsQueryable())
				return new ValueTask<long>(repository.AsQueryable().AsQueryable().LongCount());

			throw new NotSupportedException("The repository does not support querying");
		}

		/// <summary>
		/// Counts the number of entities in the repository,
		/// given a filter expression to match.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key that uniquely identifies the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository to use to count the entities.
		/// </param>
		/// <param name="filter">
		/// The filter used to count the matching entities.
		/// </param>
		/// <returns>
		/// Returns the number of entities in the repository that match
		/// the given filter.
		/// </returns>
		/// <seealso cref="CountAsync{TEntity, TKey}(IRepository{TEntity, TKey}, Expression{Func{TEntity, bool}}, CancellationToken)"/>
		public static long Count<TEntity, TKey>(this IRepository<TEntity, TKey> repository, Expression<Func<TEntity, bool>> filter)
            where TEntity : class
            => repository.CountAsync(filter).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// Synchronously counts the number of entities in the repository
		/// that match the given query filter.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key that uniquely identifies the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository to use to count the entities.
		/// </param>
		/// <param name="filter">
		/// The filter used to count the matching entities.
		/// </param>
		/// <returns>
		/// Returns the number of entities in the repository that match
		/// the given filter.
		/// </returns>
		/// <remarks>
		/// <para>
		/// <strong>Warning:</strong> This is a blocking synchronous call that wraps an
		/// asynchronous operation using <c>ConfigureAwait(false).GetAwaiter().GetResult()</c>.
		/// Prefer <see cref="CountAsync{TEntity, TKey}(IRepository{TEntity, TKey}, IQueryFilter, CancellationToken)"/>
		/// in async contexts to avoid potential deadlocks.
		/// </para>
		/// </remarks>
		/// <seealso cref="CountAsync{TEntity, TKey}(IRepository{TEntity, TKey}, IQueryFilter, CancellationToken)"/>
		public static long Count<TEntity, TKey>(this IRepository<TEntity, TKey> repository, IQueryFilter filter)
			where TEntity : class
			=> repository.CountAsync(filter).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// Synchronously counts the number of entities in the repository
		/// that match the given query filter.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository to use to count the entities.
		/// </param>
		/// <param name="filter">
		/// The filter used to count the matching entities.
		/// </param>
		/// <returns>
		/// Returns the number of entities in the repository that match
		/// the given filter.
		/// </returns>
		/// <remarks>
		/// <para>
		/// <strong>Warning:</strong> This is a blocking synchronous call that wraps an
		/// asynchronous operation using <c>ConfigureAwait(false).GetAwaiter().GetResult()</c>.
		/// Prefer <see cref="CountAsync{TEntity, TKey}(IRepository{TEntity, TKey}, IQueryFilter, CancellationToken)"/>
		/// in async contexts to avoid potential deadlocks.
		/// </para>
		/// </remarks>
		/// <seealso cref="CountAsync{TEntity, TKey}(IRepository{TEntity, TKey}, IQueryFilter, CancellationToken)"/>
		public static long Count<TEntity>(this IRepository<TEntity> repository, IQueryFilter filter)
			where TEntity : class
			=> repository.CountAsync(filter).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// Counts the number of entities in the repository
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key that uniquely identifies the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository to use to count the entities.
		/// </param>
		/// <returns>
		/// Returns the number of entities in the repository.
		/// </returns>
		/// <seealso cref="CountAllAsync{TEntity, TKey}(IRepository{TEntity, TKey}, CancellationToken)"/>
		public static long CountAll<TEntity, TKey>(this IRepository<TEntity, TKey> repository)
			where TEntity : class
			=> repository.CountAllAsync().ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// Counts the number of entities in the repository that match the given
		/// filter expression, using the single-type-parameter repository contract.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository to use to count the entities.
		/// </param>
		/// <param name="filter">
		/// A filter expression used to count the matching entities.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns the number of entities in the repository that match the given filter.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown when the repository does not support querying or filtering.
		/// </exception>
		public static ValueTask<long> CountAsync<TEntity>(this IRepository<TEntity> repository, Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default)
			where TEntity : class
			=> CountAsync<TEntity, object>(repository, filter, cancellationToken);

		/// <summary>
		/// Counts the number of entities in the repository,
		/// using the single-type-parameter repository contract.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository to use to count the entities.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns the number of entities in the repository.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown when the repository does not support querying or filtering.
		/// </exception>
		public static ValueTask<long> CountAllAsync<TEntity>(this IRepository<TEntity> repository, CancellationToken cancellationToken = default)
			where TEntity : class
			=> CountAllAsync<TEntity, object>(repository, cancellationToken);

		/// <summary>
		/// Synchronously counts the number of entities in the repository that match
		/// the given filter expression, using the single-type-parameter repository contract.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository to use to count the entities.
		/// </param>
		/// <param name="filter">
		/// The filter expression used to count the matching entities.
		/// </param>
		/// <returns>
		/// Returns the number of entities in the repository that match the given filter.
		/// </returns>
		/// <remarks>
		/// <para>
		/// <strong>Warning:</strong> This is a blocking synchronous call that wraps an
		/// asynchronous operation using <c>ConfigureAwait(false).GetAwaiter().GetResult()</c>.
		/// Prefer <see cref="CountAsync{TEntity}(IRepository{TEntity}, Expression{Func{TEntity, bool}}, CancellationToken)"/>
		/// in async contexts to avoid potential deadlocks.
		/// </para>
		/// </remarks>
		public static long Count<TEntity>(this IRepository<TEntity> repository, Expression<Func<TEntity, bool>> filter)
			where TEntity : class
			=> repository.CountAsync(filter).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// Synchronously counts all entities in the repository,
		/// using the single-type-parameter repository contract.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <param name="repository">
		/// The instance of the repository to use to count the entities.
		/// </param>
		/// <returns>
		/// Returns the number of entities in the repository.
		/// </returns>
		/// <remarks>
		/// <para>
		/// <strong>Warning:</strong> This is a blocking synchronous call that wraps an
		/// asynchronous operation using <c>ConfigureAwait(false).GetAwaiter().GetResult()</c>.
		/// Prefer <see cref="CountAllAsync{TEntity}(IRepository{TEntity}, CancellationToken)"/>
		/// in async contexts to avoid potential deadlocks.
		/// </para>
		/// </remarks>
		public static long CountAll<TEntity>(this IRepository<TEntity> repository)
			where TEntity : class
			=> repository.CountAllAsync().ConfigureAwait(false).GetAwaiter().GetResult();

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

		#region FindFirst

		/// <summary>
		/// Finds the first entity in the repository that matches
		/// the given query.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key that uniquely identifies the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The repository instance to use to find the entity.
		/// </param>
		/// <param name="query">
		/// The query object used to find the entity, and
		/// eventually sort the result.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns an instance of <typeparamref name="TEntity"/> that
		/// matched the given query, or <c>null</c> if no entity matches
		/// the given filter.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown when the repository does not support querying.
		/// </exception>
		public static ValueTask<TEntity?> FindFirstAsync<TEntity, TKey>(this IRepository<TEntity, TKey> repository, IQuery query, CancellationToken cancellationToken = default)
			where TEntity : class {
			if (repository.IsFilterable())
				return repository.AsFilterable().FindFirstAsync(query, cancellationToken);
			if (repository.IsQueryable())
				return new ValueTask<TEntity?>(query.Apply(repository.AsQueryable().AsQueryable()).FirstOrDefault());

			throw new NotSupportedException("The repository does not support querying");
		}

		/// <summary>
		/// Finds the first entity in the repository that matches
		/// the given filter.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key that uniquely identifies the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The repository instance to use to find the entity.
		/// </param>
		/// <param name="filter">
		/// A filter used to find the entity.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns an instance of <typeparamref name="TEntity"/> that
		/// matched the given filter, or <c>null</c> if no entity matches
		/// the given filter.
		/// </returns>
		public static ValueTask<TEntity?> FindFirstAsync<TEntity, TKey>(this IRepository<TEntity, TKey> repository, IQueryFilter filter, CancellationToken cancellationToken = default)
			where TEntity : class
			=> repository.FindFirstAsync(new Query(filter), cancellationToken);

		/// <summary>
		/// Finds the first entity in the repository that matches
		/// the given filter expression
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key that uniquely identifies the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The repository instance to use to find the entity.
		/// </param>
		/// <param name="filter">
		/// The filter expression used to find the entity.
		/// </param>
		/// <returns>
		/// Returns an instance of <typeparamref name="TEntity"/> that
		/// can be identified by the given filter, or <c>null</c> if no
		/// entity matches the given filter.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown when the repository does not support filtering.
		/// </exception>
		public static TEntity? FindFirst<TEntity, TKey>(this IRepository<TEntity, TKey> repository, IQueryFilter filter)
            where TEntity : class
            => repository.FindFirstAsync(new Query(filter)).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// Finds the first entity in the repository that matches
		/// the given filter expression
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key that uniquely identifies the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The repository instance to use to find the entity.
		/// </param>
		/// <param name="filter">
		/// The filter expression used to find the entity.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns an instance of <typeparamref name="TEntity"/> that
		/// matched the given filter, or <c>null</c> if no
		/// entity matches the given filter.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown when the repository does not support filtering.
		/// </exception>
		public static ValueTask<TEntity?> FindFirstAsync<TEntity, TKey>(this IRepository<TEntity, TKey> repository, Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default)
            where TEntity : class
            => repository.FindFirstAsync(Query.Where(filter), cancellationToken);

		/// <summary>
		/// Finds the first entity in the repository, naturally ordered.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key that uniquely identifies the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The repository instance to use to find the entity.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns an instance of <typeparamref name="TEntity"/> that
		/// is the first entity in the repository, or <c>null</c> if the
		/// repository is empty.
		/// </returns>
		public static ValueTask<TEntity?> FindFirstAsync<TEntity, TKey>(this IRepository<TEntity, TKey> repository, CancellationToken cancellationToken = default)
            where TEntity : class
            => repository.AsFilterable().FindFirstAsync(Query.Empty, cancellationToken);

		/// <summary>
		/// Synchronously finds the first entity in the repository that matches
		/// the given query, including optional sort order.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key that uniquely identifies the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The repository instance to use to find the entity.
		/// </param>
		/// <param name="query">
		/// The query object used to find the entity, and eventually sort the result.
		/// </param>
		/// <returns>
		/// Returns an instance of <typeparamref name="TEntity"/> that matched the given
		/// query, or <c>null</c> if no entity matches.
		/// </returns>
		/// <remarks>
		/// <para>
		/// <strong>Warning:</strong> This is a blocking synchronous call that wraps an
		/// asynchronous operation using <c>ConfigureAwait(false).GetAwaiter().GetResult()</c>.
		/// Prefer <see cref="FindFirstAsync{TEntity, TKey}(IRepository{TEntity, TKey}, IQuery, CancellationToken)"/>
		/// in async contexts to avoid potential deadlocks.
		/// </para>
		/// </remarks>
		/// <exception cref="NotSupportedException">
		/// Thrown when the repository does not support querying.
		/// </exception>
		public static TEntity? FindFirst<TEntity, TKey>(this IRepository<TEntity, TKey> repository, IQuery query)
			where TEntity : class
			=> repository.FindFirstAsync(query).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// Synchronously finds the first entity in the repository that matches
		/// the given filter expression.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key that uniquely identifies the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The repository instance to use to find the entity.
		/// </param>
		/// <param name="filter">
		/// The filter expression used to find the entity.
		/// </param>
		/// <returns>
		/// Returns an instance of <typeparamref name="TEntity"/> that matched the given
		/// filter, or <c>null</c> if no entity matches.
		/// </returns>
		/// <remarks>
		/// <para>
		/// <strong>Warning:</strong> This is a blocking synchronous call that wraps an
		/// asynchronous operation using <c>ConfigureAwait(false).GetAwaiter().GetResult()</c>.
		/// Prefer <see cref="FindFirstAsync{TEntity, TKey}(IRepository{TEntity, TKey}, Expression{Func{TEntity, bool}}, CancellationToken)"/>
		/// in async contexts to avoid potential deadlocks.
		/// </para>
		/// </remarks>
		/// <exception cref="NotSupportedException">
		/// Thrown when the repository does not support filtering.
		/// </exception>
		public static TEntity? FindFirst<TEntity, TKey>(this IRepository<TEntity, TKey> repository, Expression<Func<TEntity, bool>> filter)
			where TEntity : class
			=> repository.FindFirstAsync(filter).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// Synchronously finds the first entity in the repository, naturally ordered.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key that uniquely identifies the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The repository instance to use to find the entity.
		/// </param>
		/// <returns>
		/// Returns an instance of <typeparamref name="TEntity"/> that
		/// is the first entity in the repository, or <c>null</c> if the
		/// repository is empty.
		/// </returns>
		/// <remarks>
		/// <para>
		/// <strong>Warning:</strong> This is a blocking synchronous call that wraps an
		/// asynchronous operation using <c>ConfigureAwait(false).GetAwaiter().GetResult()</c>.
		/// Prefer <see cref="FindFirstAsync{TEntity, TKey}(IRepository{TEntity, TKey}, CancellationToken)"/>
		/// in async contexts to avoid potential deadlocks.
		/// </para>
		/// </remarks>
		public static TEntity? FindFirst<TEntity, TKey>(this IRepository<TEntity, TKey> repository)
			where TEntity : class
			=> repository.FindFirstAsync().ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// Finds the first entity in the repository, using the single-type-parameter
		/// repository contract, filtered by the given query.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <param name="repository">
		/// The repository instance to use to find the entity.
		/// </param>
		/// <param name="query">
		/// The query object used to find the entity, and eventually sort the result.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns an instance of <typeparamref name="TEntity"/> that matched the given
		/// query, or <c>null</c> if no entity matches.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown when the repository does not support querying.
		/// </exception>
		public static ValueTask<TEntity?> FindFirstAsync<TEntity>(this IRepository<TEntity> repository, IQuery query, CancellationToken cancellationToken = default)
			where TEntity : class
			=> FindFirstAsync<TEntity, object>(repository, query, cancellationToken);

		/// <summary>
		/// Finds the first entity in the repository that matches the given filter,
		/// using the single-type-parameter repository contract.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <param name="repository">
		/// The repository instance to use to find the entity.
		/// </param>
		/// <param name="filter">
		/// A filter used to find the entity.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns an instance of <typeparamref name="TEntity"/> that matched the given
		/// filter, or <c>null</c> if no entity matches.
		/// </returns>
		public static ValueTask<TEntity?> FindFirstAsync<TEntity>(this IRepository<TEntity> repository, IQueryFilter filter, CancellationToken cancellationToken = default)
			where TEntity : class
			=> FindFirstAsync<TEntity, object>(repository, filter, cancellationToken);

		/// <summary>
		/// Finds the first entity in the repository that matches the given
		/// filter expression, using the single-type-parameter repository contract.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <param name="repository">
		/// The repository instance to use to find the entity.
		/// </param>
		/// <param name="filter">
		/// The filter expression used to find the entity.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns an instance of <typeparamref name="TEntity"/> that matched the
		/// given filter, or <c>null</c> if no entity matches.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown when the repository does not support filtering.
		/// </exception>
		public static ValueTask<TEntity?> FindFirstAsync<TEntity>(this IRepository<TEntity> repository, Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default)
			where TEntity : class
			=> FindFirstAsync<TEntity, object>(repository, filter, cancellationToken);

		/// <summary>
		/// Finds the first entity in the repository, naturally ordered,
		/// using the single-type-parameter repository contract.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <param name="repository">
		/// The repository instance to use to find the entity.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns an instance of <typeparamref name="TEntity"/> that is the first
		/// entity in the repository, or <c>null</c> if the repository is empty.
		/// </returns>
		public static ValueTask<TEntity?> FindFirstAsync<TEntity>(this IRepository<TEntity> repository, CancellationToken cancellationToken = default)
			where TEntity : class
			=> FindFirstAsync<TEntity, object>(repository, cancellationToken);

		/// <summary>
		/// Synchronously finds the first entity in the repository that matches the given
		/// query, using the single-type-parameter repository contract.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <param name="repository">
		/// The repository instance to use to find the entity.
		/// </param>
		/// <param name="query">
		/// The query object used to find the entity, and eventually sort the result.
		/// </param>
		/// <returns>
		/// Returns an instance of <typeparamref name="TEntity"/> that matched the given
		/// query, or <c>null</c> if no entity matches.
		/// </returns>
		/// <remarks>
		/// <para>
		/// <strong>Warning:</strong> This is a blocking synchronous call that wraps an
		/// asynchronous operation using <c>ConfigureAwait(false).GetAwaiter().GetResult()</c>.
		/// Prefer <see cref="FindFirstAsync{TEntity}(IRepository{TEntity}, IQuery, CancellationToken)"/>
		/// in async contexts to avoid potential deadlocks.
		/// </para>
		/// </remarks>
		public static TEntity? FindFirst<TEntity>(this IRepository<TEntity> repository, IQuery query)
			where TEntity : class
			=> repository.FindFirstAsync(query).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// Synchronously finds the first entity in the repository that matches the given
		/// filter expression, using the single-type-parameter repository contract.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <param name="repository">
		/// The repository instance to use to find the entity.
		/// </param>
		/// <param name="filter">
		/// The filter expression used to find the entity.
		/// </param>
		/// <returns>
		/// Returns an instance of <typeparamref name="TEntity"/> that matched the given
		/// filter, or <c>null</c> if no entity matches.
		/// </returns>
		/// <remarks>
		/// <para>
		/// <strong>Warning:</strong> This is a blocking synchronous call that wraps an
		/// asynchronous operation using <c>ConfigureAwait(false).GetAwaiter().GetResult()</c>.
		/// Prefer <see cref="FindFirstAsync{TEntity}(IRepository{TEntity}, Expression{Func{TEntity, bool}}, CancellationToken)"/>
		/// in async contexts to avoid potential deadlocks.
		/// </para>
		/// </remarks>
		public static TEntity? FindFirst<TEntity>(this IRepository<TEntity> repository, Expression<Func<TEntity, bool>> filter)
			where TEntity : class
			=> repository.FindFirstAsync(filter).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// Synchronously finds the first entity in the repository, naturally ordered,
		/// using the single-type-parameter repository contract.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <param name="repository">
		/// The repository instance to use to find the entity.
		/// </param>
		/// <returns>
		/// Returns an instance of <typeparamref name="TEntity"/> that is the first
		/// entity in the repository, or <c>null</c> if the repository is empty.
		/// </returns>
		/// <remarks>
		/// <para>
		/// <strong>Warning:</strong> This is a blocking synchronous call that wraps an
		/// asynchronous operation using <c>ConfigureAwait(false).GetAwaiter().GetResult()</c>.
		/// Prefer <see cref="FindFirstAsync{TEntity}(IRepository{TEntity}, CancellationToken)"/>
		/// in async contexts to avoid potential deadlocks.
		/// </para>
		/// </remarks>
		public static TEntity? FindFirst<TEntity>(this IRepository<TEntity> repository)
			where TEntity : class
			=> repository.FindFirstAsync().ConfigureAwait(false).GetAwaiter().GetResult();

		#endregion


		#region FindAll

		/// <summary>
		/// Finds all the entities in the repository that match
		/// the given query.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key that uniquely identifies the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The repository instance to use to find the entities.
		/// </param>
		/// <param name="query">
		/// The query object used to find the entities, and
		/// eventually sort the result.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns a list of <typeparamref name="TEntity"/> from
		/// the repository that match the given query, eventually
		/// sorted by the given query.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown when the repository does not support querying 
		/// or filtering.
		/// </exception>
		public static ValueTask<IList<TEntity>> FindAllAsync<TEntity, TKey>(this IRepository<TEntity, TKey> repository, IQuery query, CancellationToken cancellationToken = default)
			where TEntity : class {
			if (repository.IsFilterable())
				return repository.AsFilterable().FindAllAsync(query, cancellationToken);
			if (repository.IsQueryable())
				return new ValueTask<IList<TEntity>>(query.Apply(repository.AsQueryable().AsQueryable()).ToList());

			throw new NotSupportedException("The repository does not support querying");
		}

		/// <summary>
		/// Finds all the entities in the repository that match
		/// the given filter.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key that uniquely identifies the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The repository instance to use to find the entities.
		/// </param>
		/// <param name="filter">
		/// The filter used to find the entities.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns a list of <typeparamref name="TEntity"/> from
		/// the repository that match the given filter.
		/// </returns>
		public static ValueTask<IList<TEntity>> FindAllAsync<TEntity, TKey>(this IRepository<TEntity, TKey> repository, IQueryFilter filter, CancellationToken cancellationToken = default)
			where TEntity : class
			=> repository.FindAllAsync(new Query(filter), cancellationToken);

		/// <summary>
		/// Finds all the entities in the repository that match
		/// the given filter.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key that uniquely identifies the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The repository instance to use to find the entities.
		/// </param>
		/// <param name="filter">
		/// The filter used to find the entities.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns a list of <typeparamref name="TEntity"/> from
		/// the repository that match the given filter.
		/// </returns>
		public static ValueTask<IList<TEntity>> FindAllAsync<TEntity, TKey>(this IRepository<TEntity, TKey> repository, Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default)
            where TEntity : class
            => repository.FindAllAsync(Query.Where(filter), cancellationToken);

		/// <summary>
		/// Finds all the entities in the repository, naturally ordered.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key that uniquely identifies the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The repository instance to use to find the entities.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns a list of <typeparamref name="TEntity"/> from
		/// the repository, naturally ordered.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown when the repository does not support querying or filtering.
		/// </exception>
		public static ValueTask<IList<TEntity>> FindAllAsync<TEntity, TKey>(this IRepository<TEntity, TKey> repository, CancellationToken cancellationToken = default)
            where TEntity : class
            => repository.FindAllAsync(Query.Empty, cancellationToken);

		/// <summary>
		/// Finds all the entities in the repository, naturally ordered.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key that uniquely identifies the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The repository instance to use to find the entities.
		/// </param>
		/// <returns>
		/// Returns a list of <typeparamref name="TEntity"/> from
		/// the repository, naturally ordered.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown when the repository does not support querying or filtering.
		/// </exception>
		public static IList<TEntity> FindAll<TEntity, TKey>(this IRepository<TEntity, TKey> repository)
            where TEntity : class
            => repository.FindAll(QueryFilter.Empty);

		/// <summary>
		/// Finds all the entities in the repository that match
		/// the given filter.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key that uniquely identifies the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The repository instance to use to find the entities.
		/// </param>
		/// <param name="filter">
		/// The filter used to find the entities.
		/// </param>
		/// <returns>
		/// Returns a list of <typeparamref name="TEntity"/> from
		/// the repository that match the given filter.
		/// </returns>
		public static IList<TEntity> FindAll<TEntity, TKey>(this IRepository<TEntity, TKey> repository, IQueryFilter filter)
            where TEntity : class
            => repository.FindAllAsync(filter).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// Synchronously finds all the entities in the repository that match
		/// the given query, including optional sort order.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key that uniquely identifies the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The repository instance to use to find the entities.
		/// </param>
		/// <param name="query">
		/// The query object used to find the entities, and eventually sort the result.
		/// </param>
		/// <returns>
		/// Returns a list of <typeparamref name="TEntity"/> from
		/// the repository that match the given query, eventually sorted.
		/// </returns>
		/// <remarks>
		/// <para>
		/// <strong>Warning:</strong> This is a blocking synchronous call that wraps an
		/// asynchronous operation using <c>ConfigureAwait(false).GetAwaiter().GetResult()</c>.
		/// Prefer <see cref="FindAllAsync{TEntity, TKey}(IRepository{TEntity, TKey}, IQuery, CancellationToken)"/>
		/// in async contexts to avoid potential deadlocks.
		/// </para>
		/// </remarks>
		/// <exception cref="NotSupportedException">
		/// Thrown when the repository does not support querying or filtering.
		/// </exception>
		public static IList<TEntity> FindAll<TEntity, TKey>(this IRepository<TEntity, TKey> repository, IQuery query)
			where TEntity : class
			=> repository.FindAllAsync(query).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// Synchronously finds all the entities in the repository that match
		/// the given filter expression.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <typeparam name="TKey">
		/// The type of the key that uniquely identifies the entity.
		/// </typeparam>
		/// <param name="repository">
		/// The repository instance to use to find the entities.
		/// </param>
		/// <param name="filter">
		/// The filter expression used to find the entities.
		/// </param>
		/// <returns>
		/// Returns a list of <typeparamref name="TEntity"/> from
		/// the repository that match the given filter.
		/// </returns>
		/// <remarks>
		/// <para>
		/// <strong>Warning:</strong> This is a blocking synchronous call that wraps an
		/// asynchronous operation using <c>ConfigureAwait(false).GetAwaiter().GetResult()</c>.
		/// Prefer <see cref="FindAllAsync{TEntity, TKey}(IRepository{TEntity, TKey}, Expression{Func{TEntity, bool}}, CancellationToken)"/>
		/// in async contexts to avoid potential deadlocks.
		/// </para>
		/// </remarks>
		public static IList<TEntity> FindAll<TEntity, TKey>(this IRepository<TEntity, TKey> repository, Expression<Func<TEntity, bool>> filter)
			where TEntity : class
			=> repository.FindAllAsync(filter).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// Synchronously finds all the entities in the repository that match
		/// the given query filter.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <param name="repository">
		/// The repository instance to use to find the entities.
		/// </param>
		/// <param name="filter">
		/// The filter used to find the entities.
		/// </param>
		/// <returns>
		/// Returns a list of <typeparamref name="TEntity"/> from
		/// the repository that match the given filter.
		/// </returns>
		/// <remarks>
		/// <para>
		/// <strong>Warning:</strong> This is a blocking synchronous call that wraps an
		/// asynchronous operation using <c>ConfigureAwait(false).GetAwaiter().GetResult()</c>.
		/// Prefer <see cref="FindAllAsync{TEntity, TKey}(IRepository{TEntity, TKey}, IQueryFilter, CancellationToken)"/>
		/// in async contexts to avoid potential deadlocks.
		/// </para>
		/// </remarks>
		public static IList<TEntity> FindAll<TEntity>(this IRepository<TEntity> repository, IQueryFilter filter)
			where TEntity : class
			=> repository.FindAllAsync(filter).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// Finds all the entities in the repository that match the given query,
		/// using the single-type-parameter repository contract.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <param name="repository">
		/// The repository instance to use to find the entities.
		/// </param>
		/// <param name="query">
		/// The query object used to find the entities, and eventually sort the result.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns a list of <typeparamref name="TEntity"/> from
		/// the repository that match the given query.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown when the repository does not support querying or filtering.
		/// </exception>
		public static ValueTask<IList<TEntity>> FindAllAsync<TEntity>(this IRepository<TEntity> repository, IQuery query, CancellationToken cancellationToken = default)
			where TEntity : class
			=> FindAllAsync<TEntity, object>(repository, query, cancellationToken);

		/// <summary>
		/// Finds all the entities in the repository that match the given filter,
		/// using the single-type-parameter repository contract.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <param name="repository">
		/// The repository instance to use to find the entities.
		/// </param>
		/// <param name="filter">
		/// The filter used to find the entities.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns a list of <typeparamref name="TEntity"/> from
		/// the repository that match the given filter.
		/// </returns>
		public static ValueTask<IList<TEntity>> FindAllAsync<TEntity>(this IRepository<TEntity> repository, IQueryFilter filter, CancellationToken cancellationToken = default)
			where TEntity : class
			=> FindAllAsync<TEntity, object>(repository, filter, cancellationToken);

		/// <summary>
		/// Finds all the entities in the repository that match the given filter expression,
		/// using the single-type-parameter repository contract.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <param name="repository">
		/// The repository instance to use to find the entities.
		/// </param>
		/// <param name="filter">
		/// The filter expression used to find the entities.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns a list of <typeparamref name="TEntity"/> from
		/// the repository that match the given filter.
		/// </returns>
		public static ValueTask<IList<TEntity>> FindAllAsync<TEntity>(this IRepository<TEntity> repository, Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default)
			where TEntity : class
			=> FindAllAsync<TEntity, object>(repository, filter, cancellationToken);

		/// <summary>
		/// Finds all the entities in the repository, naturally ordered,
		/// using the single-type-parameter repository contract.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <param name="repository">
		/// The repository instance to use to find the entities.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns a list of <typeparamref name="TEntity"/> from
		/// the repository, naturally ordered.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown when the repository does not support querying or filtering.
		/// </exception>
		public static ValueTask<IList<TEntity>> FindAllAsync<TEntity>(this IRepository<TEntity> repository, CancellationToken cancellationToken = default)
			where TEntity : class
			=> FindAllAsync<TEntity, object>(repository, cancellationToken);

		/// <summary>
		/// Synchronously finds all the entities in the repository that match the given
		/// query, using the single-type-parameter repository contract.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <param name="repository">
		/// The repository instance to use to find the entities.
		/// </param>
		/// <param name="query">
		/// The query object used to find the entities, and eventually sort the result.
		/// </param>
		/// <returns>
		/// Returns a list of <typeparamref name="TEntity"/> from
		/// the repository that match the given query.
		/// </returns>
		/// <remarks>
		/// <para>
		/// <strong>Warning:</strong> This is a blocking synchronous call that wraps an
		/// asynchronous operation using <c>ConfigureAwait(false).GetAwaiter().GetResult()</c>.
		/// Prefer <see cref="FindAllAsync{TEntity}(IRepository{TEntity}, IQuery, CancellationToken)"/>
		/// in async contexts to avoid potential deadlocks.
		/// </para>
		/// </remarks>
		public static IList<TEntity> FindAll<TEntity>(this IRepository<TEntity> repository, IQuery query)
			where TEntity : class
			=> repository.FindAllAsync(query).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// Synchronously finds all the entities in the repository that match the given
		/// filter expression, using the single-type-parameter repository contract.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <param name="repository">
		/// The repository instance to use to find the entities.
		/// </param>
		/// <param name="filter">
		/// The filter expression used to find the entities.
		/// </param>
		/// <returns>
		/// Returns a list of <typeparamref name="TEntity"/> from
		/// the repository that match the given filter.
		/// </returns>
		/// <remarks>
		/// <para>
		/// <strong>Warning:</strong> This is a blocking synchronous call that wraps an
		/// asynchronous operation using <c>ConfigureAwait(false).GetAwaiter().GetResult()</c>.
		/// Prefer <see cref="FindAllAsync{TEntity}(IRepository{TEntity}, Expression{Func{TEntity, bool}}, CancellationToken)"/>
		/// in async contexts to avoid potential deadlocks.
		/// </para>
		/// </remarks>
		public static IList<TEntity> FindAll<TEntity>(this IRepository<TEntity> repository, Expression<Func<TEntity, bool>> filter)
			where TEntity : class
			=> repository.FindAllAsync(filter).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// Synchronously finds all the entities in the repository, naturally ordered,
		/// using the single-type-parameter repository contract.
		/// </summary>
		/// <typeparam name="TEntity">
		/// The type of entity handled by the repository.
		/// </typeparam>
		/// <param name="repository">
		/// The repository instance to use to find the entities.
		/// </param>
		/// <returns>
		/// Returns a list of <typeparamref name="TEntity"/> from
		/// the repository, naturally ordered.
		/// </returns>
		/// <remarks>
		/// <para>
		/// <strong>Warning:</strong> This is a blocking synchronous call that wraps an
		/// asynchronous operation using <c>ConfigureAwait(false).GetAwaiter().GetResult()</c>.
		/// Prefer <see cref="FindAllAsync{TEntity}(IRepository{TEntity}, CancellationToken)"/>
		/// in async contexts to avoid potential deadlocks.
		/// </para>
		/// </remarks>
		public static IList<TEntity> FindAll<TEntity>(this IRepository<TEntity> repository)
			where TEntity : class
			=> repository.FindAllAsync().ConfigureAwait(false).GetAwaiter().GetResult();


        #endregion
    }
}