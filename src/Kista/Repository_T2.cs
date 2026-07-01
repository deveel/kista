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
	/// An abstract repository base class that hides the underlying
	/// <see cref="IQueryable{T}"/> data access hatch from consumer code.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type of the entity managed by the repository.
	/// </typeparam>
	/// <typeparam name="TKey">
	/// The type of the unique identifier of the entity.
	/// </typeparam>
	/// <remarks>
	/// <para>
	/// The data-access hatch is the <see cref="Queryable"/> method, which is
	/// <c>public</c> so that companion assemblies (e.g. EntityManager,
	/// decorators) can access the queryable, but consumer code should
	/// use <see cref="CreateQuery"/> instead. Subclasses return the
	/// engine-native queryable and the default implementations of the
	/// protected <c>FindAsync(IQuery, CancellationToken)</c>,
	/// <c>QueryPageAsync(PageQuery{TEntity}, CancellationToken)</c>,
	/// <c>ExistsAsync(IQueryFilter, CancellationToken)</c>,
	/// <c>CountAsync(IQueryFilter, CancellationToken)</c>,
	/// <c>FindFirstAsync(IQuery, CancellationToken)</c> and
	/// <c>FindAllAsync(IQuery, CancellationToken)</c> methods
	/// route the queryable returned here through
	/// <see cref="NormalizeQuery"/>, the engine async hooks
	/// (<see cref="CountAsync(IQueryable{TEntity}, CancellationToken)"/>,
	/// <see cref="ToListAsync(IQueryable{TEntity}, CancellationToken)"/>)
	/// and finally the unpacking primitives.
	/// </para>
	/// <para>
	/// This class also provides a <see cref="CreateQuery"/> factory that returns a
	/// <see cref="global::Kista.QueryBuilder{TEntity}"/> instance bound to this repository,
	/// to build type-safe queries against the entity set.
	/// </para>
	/// </remarks>
	public abstract class Repository<TEntity, TKey> : IRepository<TEntity, TKey>
		where TEntity : class {

		/// <summary>
		/// Initializes the given query filter with the current service provider.
		/// </summary>
		/// <param name="filter">
		/// The query filter to initialize, or <c>null</c>.
		/// </param>
		protected void InitializeFilter(IQueryFilter? filter) {
			if (filter != null && Services != null)
				filter.Initialize(new DefaultFilterContext(Services));
		}
		/// <summary>
		/// Gets the service provider associated with this repository, if any.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Exposed as <c>protected</c> so that the infrastructure plumbing of the
		/// repository is not part of the consumer-facing surface. The public
		/// surface is satisfied by an explicit interface implementation that
		/// forwards to this member.
		/// </para>
		/// </remarks>
		protected abstract IServiceProvider? Services { get; }

		IServiceProvider? IRepository<TEntity, TKey>.Services => Services;

		/// <summary>
		/// Gets the unique identifier of the given entity.
		/// </summary>
		/// <param name="entity">
		/// The entity to extract the identifier from.
		/// </param>
		/// <returns>
		/// Returns the unique identifier of the entity, or <c>null</c> if the
		/// entity is not identified.
		/// </returns>
		/// <remarks>
		/// Exposed as <c>protected</c>: the key-extraction strategy is an
		/// implementation detail of each engine. The public surface is
		/// satisfied by an explicit interface implementation that forwards to
		/// this member.
		/// </remarks>
		protected abstract TKey? GetEntityKey(TEntity entity);

		TKey? IRepository<TEntity, TKey>.GetEntityKey(TEntity entity) => GetEntityKey(entity);

		/// <summary>
		/// Gets the underlying <see cref="IQueryable{T}"/> that represents the
		/// entity set exposed by the repository.
		/// </summary>
		/// <returns>
		/// Returns an <see cref="IQueryable{T}"/> that can be used by the
		/// repository's own query translation pipeline. This hatch is
		/// <c>public</c> so that companion assemblies (e.g. EntityManager,
		/// decorators) can access the queryable, but consumer code should
		/// use <see cref="CreateQuery"/> instead.
		/// </returns>
		/// <remarks>
		/// <para>
		/// Subclasses return the engine-native queryable (for example
		/// <c>DbSet&lt;TEntity&gt;.AsQueryable()</c> for Entity Framework,
		/// <c>IMongoDbSet&lt;TEntity&gt;.AsQueryable()</c> for MongoDB, or a
		/// snapshot <see cref="IQueryable{T}"/> for in-memory stores).
		/// </para>
		/// <para>
		/// The default implementations of the protected
		/// <c>FindAsync(IQuery, CancellationToken)</c>,
		/// <c>QueryPageAsync(PageQuery{TEntity}, CancellationToken)</c>,
		/// <c>ExistsAsync(IQueryFilter, CancellationToken)</c>,
		/// <c>CountAsync(IQueryFilter, CancellationToken)</c>,
		/// <c>FindFirstAsync(IQuery, CancellationToken)</c> and
		/// <c>FindAllAsync(IQuery, CancellationToken)</c> methods
		/// route the queryable returned here through
		/// <see cref="NormalizeQuery"/>, the engine async hooks
		/// (<see cref="CountAsync(IQueryable{TEntity}, CancellationToken)"/>, <see cref="ToListAsync(IQueryable{TEntity}, CancellationToken)"/>) and finally
		/// the unpacking primitives.
		/// </para>
		/// </remarks>
		public abstract IQueryable<TEntity> Queryable();

		/// <summary>
		/// Gets a value indicating whether this repository supports
		/// <see cref="IQueryable{T}"/>-based filtering.
		/// </summary>
		/// <remarks>
		/// <para>
		/// When <c>true</c> (the default), the protected filterable methods
		/// (<see cref="ExistsAsync(IQueryFilter, CancellationToken)"/>,
		/// <see cref="CountAsync(IQueryFilter, CancellationToken)"/>,
		/// <see cref="FindFirstAsync(IQuery, CancellationToken)"/>,
		/// <see cref="FindAllAsync(IQuery, CancellationToken)"/>) use the
		/// <see cref="Queryable"/> hatch and apply filters as
		/// <see cref="IQueryable{T}"/> operations.
		/// </para>
		/// <para>
		/// When <c>false</c>, subclasses must override the filterable methods
		/// to provide their own filter expansion (compiling
		/// <see cref="IExpressionQueryFilter.AsLambda{TEntity}"/> delegates
		/// and applying them to enumerables). The default implementations
		/// throw <see cref="NotSupportedException"/> when this property
		/// returns <c>false</c>.
		/// </para>
		/// </remarks>
		protected virtual bool IsQueryable => false;

		/// <summary>
		/// Optional hook that engines can override to normalise the queryable
		/// before materialisation (for example, to strip redundant sub-expressions
		/// in Entity Framework).
		/// </summary>
		/// <param name="queryable">
		/// The queryable produced by <see cref="Queryable"/> and the unpacking
		/// pipeline.
		/// </param>
		/// <returns>
		/// Returns the normalised queryable, or the same instance if no
		/// normalisation is required.
		/// </returns>
		protected virtual IQueryable<TEntity> NormalizeQuery(IQueryable<TEntity> queryable) => queryable;

		/// <summary>
		/// Counts the number of entities in the given queryable, honouring the
		/// engine's preferred async execution model.
		/// </summary>
		/// <param name="queryable">
		/// The queryable to count.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns the number of entities matched by the queryable.
		/// </returns>
		/// <remarks>
		/// The default implementation materialises the count synchronously via
		/// <see cref="Queryable.LongCount{TSource}(IQueryable{TSource})"/>.
		/// Subclasses backed by a true async provider (for example Entity
		/// Framework's <c>IQueryableAsync</c>) should override this hook.
		/// </remarks>
		protected virtual ValueTask<long> CountAsync(IQueryable<TEntity> queryable, CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(queryable);

			return new ValueTask<long>(queryable.LongCount());
		}

		/// <summary>
		/// Materialises the given queryable to a read-only list, honouring the
		/// engine's preferred async execution model.
		/// </summary>
		/// <param name="queryable">
		/// The queryable to materialise.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns the read-only list of entities matched by the queryable.
		/// </returns>
		/// <remarks>
		/// The default implementation materialises the list synchronously via
		/// <c>Queryable.ToList&lt;TSource&gt;(IQueryable{TSource})</c>.
		/// Subclasses backed by a true async provider should override this hook.
		/// </remarks>
		protected virtual ValueTask<IReadOnlyList<TEntity>> ToListAsync(IQueryable<TEntity> queryable, CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(queryable);

			return new ValueTask<IReadOnlyList<TEntity>>(queryable.ToList());
		}

		/// <summary>
		/// Executes the given <see cref="IQuery"/> against the repository and
		/// returns the matching entities.
		/// </summary>
		/// <param name="query">
		/// The query to execute. Filter and order are unpacked and applied
		/// inside the data layer through the protected <see cref="Queryable"/>
		/// hatch.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns the read-only list of entities that match the query.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="query"/> is <c>null</c>.
		/// </exception>
		/// <remarks>
		/// <para>
		/// The unpacking and execution of the query is performed by this base
		/// class; subclasses only contribute the provider-specific
		/// <see cref="IQueryable{T}"/> through <see cref="Queryable"/> and may
		/// override <see cref="NormalizeQuery"/>, <see cref="CountAsync(IQueryable{TEntity}, CancellationToken)"/>
		/// and <see cref="ToListAsync(IQueryable{TEntity}, CancellationToken)"/> to plug engine-specific behaviour.
		/// </para>
		/// <para>
		/// Exposed as <c>protected</c> so that the translation pipeline stays
		/// inside the data layer. Consumer code should call
		/// <c>FindAsync(TKey, CancellationToken)</c> (single-entity lookup by
		/// key) or any filter-specific entry point on a more specialised
		/// contract.
		/// </para>
		/// </remarks>
		protected virtual ValueTask<IReadOnlyList<TEntity>> FindAsync(IQuery query, CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(query);
			cancellationToken.ThrowIfCancellationRequested();

			var queryable = NormalizeQuery(query.Apply(Queryable()));
			return ToListAsync(queryable, cancellationToken);
		}

		/// <summary>
		/// Executes the given <see cref="PageQuery{TEntity}"/> against the
		/// repository and returns the resulting page.
		/// </summary>
		/// <param name="request">
		/// The page request to execute. The page number, size, filter and order
		/// are unpacked and applied inside the data layer.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns a <see cref="PageQueryResult{TEntity}"/> that contains the page
		/// of entities and the total number of items matched by the query
		/// (independent of the page slice).
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="request"/> is <c>null</c>.
		/// </exception>
		/// <remarks>
		/// <para>
		/// The unpacking, count and slice are performed by this base class;
		/// subclasses contribute the provider-specific
		/// <see cref="IQueryable{T}"/> through <see cref="Queryable"/> and may
		/// override <see cref="NormalizeQuery"/>, <see cref="CountAsync(IQueryable{TEntity}, CancellationToken)"/>
		/// and <see cref="ToListAsync(IQueryable{TEntity}, CancellationToken)"/> to plug engine-specific behaviour.
		/// </para>
		/// <para>
		/// Exposed as <c>protected</c> so that the translation pipeline stays
		/// inside the data layer. Consumer code should call
		/// <c>GetPageAsync(PageRequest, CancellationToken)</c> for simple
		/// unsorted pagination, or use this method internally when filtered
		/// and sorted paging is required.
		/// </para>
		/// </remarks>
		protected virtual async ValueTask<PageQueryResult<TEntity>> QueryPageAsync(PageQuery<TEntity> request, CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(request);
			cancellationToken.ThrowIfCancellationRequested();

			var queryable = NormalizeQuery(request.ApplyQuery(Queryable()));
			var total = await CountAsync(queryable, cancellationToken).ConfigureAwait(false);
			var items = await ToListAsync(queryable.Skip(request.Offset).Take(request.Size), cancellationToken).ConfigureAwait(false);
			return new PageQueryResult<TEntity>(request, (int)total, items);
		}

		/// <inheritdoc />
		public virtual async ValueTask<PageResult<TEntity>> GetPageAsync(PageRequest request, CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(request);
			cancellationToken.ThrowIfCancellationRequested();

			if (request is PageQuery<TEntity> pageQuery)
				return await QueryPageAsync(pageQuery, cancellationToken).ConfigureAwait(false);

			var queryable = NormalizeQuery(Queryable());
			var total = await CountAsync(queryable, cancellationToken).ConfigureAwait(false);
			var items = await ToListAsync(queryable.Skip(request.Offset).Take(request.Size), cancellationToken).ConfigureAwait(false);
			return new PageResult<TEntity>(request, (int)total, items);
		}

		/// <summary>
		/// Determines if at least one item in the repository exists for the
		/// given filtering conditions.
		/// </summary>
		/// <param name="filter">
		/// The filter used to identify the items.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <param name="options">
		/// An optional bag of query options that influence how the query
		/// is executed by the driver, such as the soft-delete mode. When
		/// <c>null</c>, defaults to <see cref="QueryOptions.Default"/>.
		/// </param>
		/// <returns>
		/// Returns <c>true</c> if at least one item in the inventory matches the given
		/// filter, otherwise returns <c>false</c>.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown when <see cref="IsQueryable"/> is <c>false</c> and the subclass
		/// has not overridden this method.
		/// </exception>
		/// <remarks>
		/// The default implementation uses the <see cref="Queryable"/> hatch when
		/// <see cref="IsQueryable"/> is <c>true</c>.
		/// </remarks>
		protected virtual ValueTask<bool> ExistsAsync(IQueryFilter? filter, IQueryOptions? options, CancellationToken cancellationToken = default) {
			if (IsQueryable) {
				var queryable = filter != null ? filter.Apply(Queryable()) : Queryable();
				return new ValueTask<bool>(queryable.Any());
			}
			throw new NotSupportedException("Filtering requires IQueryable support or a subclass override.");
		}

		/// <summary>
		/// Determines if at least one item in the repository exists for the
		/// given filtering conditions.
		/// </summary>
		/// <param name="filter">
		/// The filter used to identify the items.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns <c>true</c> if at least one item in the inventory matches the given
		/// filter, otherwise returns <c>false</c>.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown when <see cref="IsQueryable"/> is <c>false</c> and the subclass
		/// has not overridden this method.
		/// </exception>
		/// <remarks>
		/// The default implementation uses the <see cref="Queryable"/> hatch when
		/// <see cref="IsQueryable"/> is <c>true</c>.
		/// </remarks>
		protected virtual ValueTask<bool> ExistsAsync(IQueryFilter filter, CancellationToken cancellationToken = default)
			=> ExistsAsync(filter, null, cancellationToken);

		/// <summary>
		/// Checks if an entity exists in the repository that matches the given
		/// predicate expression.
		/// </summary>
		/// <param name="predicate">
		/// The predicate expression used to check the existence of any matching entity.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns <c>true</c> if any entity exists in the repository that matches
		/// the given predicate, otherwise <c>false</c>.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown when <see cref="IsQueryable"/> is <c>false</c> and the subclass
		/// has not overridden this method.
		/// </exception>
		protected virtual ValueTask<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
			=> ExistsAsync(new ExpressionQueryFilter<TEntity>(predicate), cancellationToken);

		/// <summary>
		/// Counts the number of items in the repository matching the given
		/// filtering conditions.
		/// </summary>
		/// <param name="filter">
		/// The filter used to identify the items.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <param name="options">
		/// An optional bag of query options that influence how the query
		/// is executed by the driver, such as the soft-delete mode. When
		/// <c>null</c>, defaults to <see cref="QueryOptions.Default"/>.
		/// </param>
		/// <returns>
		/// Returns the total count of items matching the given filtering conditions.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown when <see cref="IsQueryable"/> is <c>false</c> and the subclass
		/// has not overridden this method.
		/// </exception>
		/// <remarks>
		/// Passing a <c>null</c> filter or passing <see cref="QueryFilter.Empty"/> as
		/// argument is equivalent to ask the repository not to use any filter, returning the
		/// total count of all items in the inventory.
		/// <para>
		/// The default implementation uses the <see cref="Queryable"/> hatch when
		/// <see cref="IsQueryable"/> is <c>true</c>.
		/// </para>
		/// </remarks>
		protected virtual ValueTask<long> CountAsync(IQueryFilter? filter, IQueryOptions? options, CancellationToken cancellationToken = default) {
			if (IsQueryable) {
				var queryable = filter != null ? filter.Apply(Queryable()) : Queryable();
				return CountAsync(queryable, cancellationToken);
			}
			throw new NotSupportedException("Filtering requires IQueryable support or a subclass override.");
		}

		/// <summary>
		/// Counts the number of items in the repository matching the given
		/// filtering conditions.
		/// </summary>
		/// <param name="filter">
		/// The filter used to identify the items.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns the total count of items matching the given filtering conditions.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown when <see cref="IsQueryable"/> is <c>false</c> and the subclass
		/// has not overridden this method.
		/// </exception>
		/// <remarks>
		/// Passing a <c>null</c> filter or passing <see cref="QueryFilter.Empty"/> as
		/// argument is equivalent to ask the repository not to use any filter, returning the
		/// total count of all items in the inventory.
		/// <para>
		/// The default implementation uses the <see cref="Queryable"/> hatch when
		/// <see cref="IsQueryable"/> is <c>true</c>.
		/// </para>
		/// </remarks>
		protected virtual ValueTask<long> CountAsync(IQueryFilter filter, CancellationToken cancellationToken = default)
			=> CountAsync(filter, null, cancellationToken);

		/// <summary>
		/// Counts the number of items in the repository that match the given
		/// predicate expression.
		/// </summary>
		/// <param name="predicate">
		/// The predicate expression used to count the matching entities.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns the total count of items matching the given predicate.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown when <see cref="IsQueryable"/> is <c>false</c> and the subclass
		/// has not overridden this method.
		/// </exception>
		protected virtual ValueTask<long> CountAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
			=> CountAsync(new ExpressionQueryFilter<TEntity>(predicate), cancellationToken);

		/// <summary>
		/// Finds the first item in the repository that matches the given query.
		/// </summary>
		/// <param name="query">
		/// The query definition used to identify the item to return
		/// and eventually sort the results.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns the first item in the repository that matches the given filtering condition,
		/// or <c>null</c> if none of the items matches the condition.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown when <see cref="IsQueryable"/> is <c>false</c> and the subclass
		/// has not overridden this method.
		/// </exception>
		/// <remarks>
		/// The default implementation uses the <see cref="Queryable"/> hatch when
		/// <see cref="IsQueryable"/> is <c>true</c>.
		/// </remarks>
		protected virtual ValueTask<TEntity?> FindFirstAsync(IQuery query, CancellationToken cancellationToken = default) {
			if (IsQueryable) {
				var queryable = NormalizeQuery(query.Apply(Queryable()));
				return new ValueTask<TEntity?>(queryable.FirstOrDefault());
			}
			throw new NotSupportedException("Querying requires IQueryable support or a subclass override.");
		}

		/// <summary>
		/// Finds the first item in the repository that matches the given
		/// predicate expression.
		/// </summary>
		/// <param name="predicate">
		/// The predicate expression used to identify the item to return.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns the first item in the repository that matches the given predicate,
		/// or <c>null</c> if none of the items matches the condition.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown when <see cref="IsQueryable"/> is <c>false</c> and the subclass
		/// has not overridden this method.
		/// </exception>
		protected virtual ValueTask<TEntity?> FindFirstAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
			=> FindFirstAsync(Kista.Query.Where(predicate), cancellationToken);

		/// <summary>
		/// Finds all the items in the repository that match the given filtering condition.
		/// </summary>
		/// <param name="query">
		/// The query definition used to identify the items to return
		/// and eventually sort the results.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns a read-only list of items in the repository that match the given query,
		/// or an empty list if none of the items matches the condition.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown when <see cref="IsQueryable"/> is <c>false</c> and the subclass
		/// has not overridden this method.
		/// </exception>
		/// <remarks>
		/// The default implementation uses the <see cref="Queryable"/> hatch when
		/// <see cref="IsQueryable"/> is <c>true</c>.
		/// </remarks>
		protected virtual ValueTask<IReadOnlyList<TEntity>> FindAllAsync(IQuery query, CancellationToken cancellationToken = default) {
			if (IsQueryable) {
				var queryable = NormalizeQuery(query.Apply(Queryable()));
				return ToListAsync(queryable, cancellationToken);
			}
			throw new NotSupportedException("Querying requires IQueryable support or a subclass override.");
		}

		/// <summary>
		/// Finds all the items in the repository that match the given
		/// predicate expression.
		/// </summary>
		/// <param name="predicate">
		/// The predicate expression used to identify the items to return.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns a read-only list of items in the repository that match the given predicate,
		/// or an empty list if none of the items matches the condition.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown when <see cref="IsQueryable"/> is <c>false</c> and the subclass
		/// has not overridden this method.
		/// </exception>
		protected virtual ValueTask<IReadOnlyList<TEntity>> FindAllAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
			=> FindAllAsync(Kista.Query.Where(predicate), cancellationToken);

		/// <summary>
		/// Finds the first entity matching the given query, with the
		/// specified query options influencing the soft-delete filter mode.
		/// </summary>
		/// <param name="query">
		/// The query to execute.
		/// </param>
		/// <param name="options">
		/// The query options that influence how the query is executed,
		/// such as the soft-delete mode.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns the first entity matching the query, or <c>null</c> if
		/// none found.
		/// </returns>
		/// <remarks>
		/// This is the public entry point to the protected
		/// <see cref="FindFirstAsync(IQuery, CancellationToken)"/> pipeline,
		/// allowing companion assemblies (such as EntityManager) to execute
		/// queries with specific soft-delete modes (for example
		/// <see cref="SoftDeleteMode.IncludeDeleted"/> or
		/// <see cref="SoftDeleteMode.OnlyDeleted"/>) without accessing the
		/// protected query hatch directly.
		/// </remarks>
		public virtual ValueTask<TEntity?> FindFirstAsync(IQuery query, IQueryOptions? options, CancellationToken cancellationToken = default) {
			var effectiveOptions = options ?? query.Options;
			if (effectiveOptions == query.Options)
				return FindFirstAsync(query, cancellationToken);

			var queryWithOptions = new Query(query.Filter ?? QueryFilter.Empty, query.Order, effectiveOptions);
			return FindFirstAsync(queryWithOptions, cancellationToken);
		}

		/// <inheritdoc />
		public abstract ValueTask AddAsync(TEntity entity, CancellationToken cancellationToken = default);

		/// <inheritdoc />
		public abstract ValueTask AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

		/// <inheritdoc />
		public abstract ValueTask<bool> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

		/// <inheritdoc />
		public abstract ValueTask<bool> RemoveAsync(TEntity entity, CancellationToken cancellationToken = default);

		/// <inheritdoc />
		public virtual ValueTask<bool> HardDeleteAsync(TEntity entity, CancellationToken cancellationToken = default) {
			throw new NotSupportedException("Hard delete is not supported by this repository.");
		}

		/// <inheritdoc />
		public abstract ValueTask RemoveRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

		/// <inheritdoc />
		public virtual ValueTask HardDeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) {
			throw new NotSupportedException("Hard delete range is not supported by this repository.");
		}

		/// <inheritdoc />
		public abstract ValueTask<TEntity?> FindAsync(TKey key, CancellationToken cancellationToken = default);

		/// <summary>
		/// Creates a new <see cref="global::Kista.QueryBuilder{TEntity}"/>
		/// instance bound to this repository, allowing fluent
		/// construction and execution of queries.
		/// </summary>
		/// <returns>
		/// Returns a <see cref="global::Kista.QueryBuilder{TEntity}"/>
		/// that wraps this repository and provides terminal methods
		/// to execute the built query.
		/// </returns>
		/// <remarks>
		/// <para>
		/// The default implementation returns a <c>private</c> nested
		/// builder that inherits from
		/// <see cref="global::Kista.QueryBuilder{TEntity}"/> and
		/// dispatches the terminal methods to the protected
		/// <c>FindAsync</c>/<c>FindAllAsync</c>/<c>CountAsync</c>/
		/// <c>ExistsAsync</c>/<c>GetPageAsync</c> entry points of
		/// this repository.
		/// </para>
		/// <para>
		/// Subclasses can override this method to return a custom
		/// <see cref="global::Kista.QueryBuilder{TEntity}"/> subclass
		/// (declared at the call site) that overrides the
		/// <c>virtual</c> terminal methods to add cross-cutting
		/// concerns such as logging, caching, or authorization before
		/// query execution.
		/// </para>
		/// <para>
		/// The returned builder is not thread-safe: build a new
		/// instance per logical operation. See
		/// <see cref="IQueryBuilder{TEntity}"/> for the full contract.
		/// </para>
		/// </remarks>
		protected virtual global::Kista.QueryBuilder<TEntity> CreateQuery() => new QueryBuilder(this);

		/// <summary>
		/// A private, repository-bound query builder that inherits the
		/// fluent composition and the <see cref="IQueryBuilder{TEntity}"/>
		/// contract from the public <see cref="QueryBuilder{TEntity}"/>
		/// base class and overrides the terminal methods to dispatch
		/// through this repository's protected pipeline.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This class is the single, well-known bridge between
		/// <see cref="CreateQuery"/> and the protected
		/// <c>FindAsync</c>/<c>FindAllAsync</c>/<c>CountAsync</c>/
		/// <c>ExistsAsync</c>/<c>GetPageAsync</c> methods. It is
		/// declared <c>private</c> so that consumer code cannot
		/// directly construct a bound instance: the only way to obtain
		/// one is through <see cref="CreateQuery"/>.
		/// </para>
		/// <para>
		/// Subclasses of <see cref="Repository{TEntity, TKey}"/> that
		/// want to add cross-cutting concerns (logging, caching,
		/// authorization) to every query can override
		/// <see cref="CreateQuery"/> to return a custom
		/// <see cref="QueryBuilder{TEntity}"/> subclass that overrides
		/// the <c>virtual</c> terminal methods. Such a subclass is
		/// declared at the call site and does not need to be a member
		/// of this assembly.
		/// </para>
		/// </remarks>
		private sealed class QueryBuilder : QueryBuilder<TEntity> {
			private readonly Repository<TEntity, TKey> _repository;

			/// <summary>
			/// Creates a new query builder bound to the given repository.
			/// </summary>
			/// <param name="repository">
			/// The repository instance that will execute the built query.
			/// </param>
			internal QueryBuilder(Repository<TEntity, TKey> repository)
				: base() {
				ArgumentNullException.ThrowIfNull(repository);

				_repository = repository;
			}

			/// <inheritdoc />
			public override ValueTask<TEntity?> FirstOrDefaultAsync(CancellationToken cancellationToken = default) {
				cancellationToken.ThrowIfCancellationRequested();

				return _repository.FindFirstAsync(Query, cancellationToken);
			}

			/// <inheritdoc />
			public override ValueTask<IReadOnlyList<TEntity>> ToListAsync(CancellationToken cancellationToken = default) {
				cancellationToken.ThrowIfCancellationRequested();

				return _repository.FindAllAsync(Query, cancellationToken);
			}

			/// <inheritdoc />
			public override ValueTask<long> CountAsync(CancellationToken cancellationToken = default) {
				cancellationToken.ThrowIfCancellationRequested();

				return _repository.CountAsync(Query.Filter, Query.Options, cancellationToken);
			}

			/// <inheritdoc />
			public override ValueTask<bool> AnyAsync(CancellationToken cancellationToken = default) {
				cancellationToken.ThrowIfCancellationRequested();

				return _repository.ExistsAsync(Query.Filter, Query.Options, cancellationToken);
			}

			/// <inheritdoc />
			public override ValueTask<PageResult<TEntity>> GetPageAsync(int page, int size, CancellationToken cancellationToken = default) {
				cancellationToken.ThrowIfCancellationRequested();

				var pageQuery = new PageQuery<TEntity>(page, size) { Query = Query };
				return _repository.GetPageAsync(pageQuery, cancellationToken);
			}
		}
	}
}
