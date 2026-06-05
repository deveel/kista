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

using System;

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
	/// Unlike the obsolete <see cref="IQueryableRepository{TEntity, TKey}"/> contract,
	/// this base class does not expose an <c>AsQueryable()</c> entry point to consumers.
	/// The hatch is the <see cref="Query"/> method, which is <c>protected</c> and only
	/// available to subclasses that need to translate <see cref="IQuery"/> and
	/// <see cref="PageQuery{TEntity}"/> instances into provider-specific queries.
	/// </para>
	/// <para>
	/// Subclasses inherit ready-made implementations of the protected
	/// <c>FindAsync(IQuery, CancellationToken)</c>,
	/// <c>QueryPageAsync(PageQuery{TEntity}, CancellationToken)</c>,
	/// <c>ExistsAsync(IQueryFilter, CancellationToken)</c>,
	/// <c>CountAsync(IQueryFilter, CancellationToken)</c>,
	/// <c>FindFirstAsync(IQuery, CancellationToken)</c> and
	/// <c>FindAllAsync(IQuery, CancellationToken)</c> methods that
	/// unpack the query, apply sorting/filtering/pagination through the engine
	/// hatch, and surface the result. For convenience, each method also has a
	/// <c>Expression&lt;Func&lt;TEntity, bool&gt;&gt;</c>-based overload that
	/// automatically wraps the predicate in the appropriate filter or query. Engine-specific async execution can be
	/// customised by overriding <see cref="CountAsync(IQueryable{TEntity}, CancellationToken)"/> and
	/// <see cref="ToListAsync"/>.
	/// </para>
	/// </remarks>
	public abstract class Repository<TEntity, TKey> : IRepository<TEntity, TKey>, IFilterableRepository<TEntity, TKey>
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
		/// <c>protected</c> and never exposed to consumer code: deferring the
		/// execution of LINQ expressions outside the data layer is the very
		/// leak this base class is designed to close.
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
		/// (<see cref="CountAsync(IQueryable{TEntity}, CancellationToken)"/>, <see cref="ToListAsync"/>) and finally
		/// the unpacking primitives.
		/// </para>
		/// </remarks>
		protected abstract IQueryable<TEntity> Query();

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
		/// <see cref="Query"/> hatch and apply filters as
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
		/// The queryable produced by <see cref="Query"/> and the unpacking
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
		/// Materialises the given queryable to a list, honouring the engine's
		/// preferred async execution model.
		/// </summary>
		/// <param name="queryable">
		/// The queryable to materialise.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns the list of entities matched by the queryable.
		/// </returns>
		/// <remarks>
		/// The default implementation materialises the list synchronously via
		/// <c>Queryable.ToList&lt;TSource&gt;(IQueryable{TSource})</c>.
		/// Subclasses backed by a true async provider should override this hook.
		/// </remarks>
		protected virtual ValueTask<IList<TEntity>> ToListAsync(IQueryable<TEntity> queryable, CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(queryable);

			return new ValueTask<IList<TEntity>>(queryable.ToList());
		}

		/// <summary>
		/// Executes the given <see cref="IQuery"/> against the repository and
		/// returns the matching entities.
		/// </summary>
		/// <param name="query">
		/// The query to execute. Filter and order are unpacked and applied
		/// inside the data layer through the protected <see cref="Query"/>
		/// hatch.
		/// </param>
		/// <param name="cancellationToken">
		/// A token used to cancel the operation.
		/// </param>
		/// <returns>
		/// Returns the list of entities that match the query.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="query"/> is <c>null</c>.
		/// </exception>
		/// <remarks>
		/// <para>
		/// The unpacking and execution of the query is performed by this base
		/// class; subclasses only contribute the provider-specific
		/// <see cref="IQueryable{T}"/> through <see cref="Query"/> and may
		/// override <see cref="NormalizeQuery"/>, <see cref="CountAsync(IQueryable{TEntity}, CancellationToken)"/>
		/// and <see cref="ToListAsync"/> to plug engine-specific behaviour.
		/// </para>
		/// <para>
		/// Exposed as <c>protected</c> so that the translation pipeline stays
		/// inside the data layer. Consumer code should call
		/// <c>FindAsync(TKey, CancellationToken)</c> (single-entity lookup by
		/// key) or any filter-specific entry point on a more specialised
		/// contract.
		/// </para>
		/// </remarks>
		protected virtual ValueTask<IList<TEntity>> FindAsync(IQuery query, CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(query);
			cancellationToken.ThrowIfCancellationRequested();

			var queryable = NormalizeQuery(query.Apply(Query()));
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
		/// <see cref="IQueryable{T}"/> through <see cref="Query"/> and may
		/// override <see cref="NormalizeQuery"/>, <see cref="CountAsync(IQueryable{TEntity}, CancellationToken)"/>
		/// and <see cref="ToListAsync"/> to plug engine-specific behaviour.
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

			var queryable = NormalizeQuery(request.ApplyQuery(Query()));
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

			var queryable = NormalizeQuery(Query());
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
		/// <returns>
		/// Returns <c>true</c> if at least one item in the inventory matches the given
		/// filter, otherwise returns <c>false</c>.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown when <see cref="IsQueryable"/> is <c>false</c> and the subclass
		/// has not overridden this method.
		/// </exception>
		/// <remarks>
		/// The default implementation uses the <see cref="Query"/> hatch when
		/// <see cref="IsQueryable"/> is <c>true</c>.
		/// </remarks>
		protected virtual ValueTask<bool> ExistsAsync(IQueryFilter filter, CancellationToken cancellationToken = default) {
			if (IsQueryable) {
				var queryable = filter != null ? filter.Apply(Query()) : Query();
				return new ValueTask<bool>(queryable.Any());
			}
			throw new NotSupportedException("Filtering requires IQueryable support or a subclass override.");
		}

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
		/// The default implementation uses the <see cref="Query"/> hatch when
		/// <see cref="IsQueryable"/> is <c>true</c>.
		/// </para>
		/// </remarks>
		protected virtual ValueTask<long> CountAsync(IQueryFilter filter, CancellationToken cancellationToken = default) {
			if (IsQueryable) {
				var queryable = filter != null ? filter.Apply(Query()) : Query();
				return CountAsync(queryable, cancellationToken);
			}
			throw new NotSupportedException("Filtering requires IQueryable support or a subclass override.");
		}

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
		/// The default implementation uses the <see cref="Query"/> hatch when
		/// <see cref="IsQueryable"/> is <c>true</c>.
		/// </remarks>
		protected virtual ValueTask<TEntity?> FindFirstAsync(IQuery query, CancellationToken cancellationToken = default) {
			if (IsQueryable) {
				var queryable = NormalizeQuery(query.Apply(Query()));
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
		/// Returns a list of items in the repository that match the given query,
		/// or an empty list if none of the items matches the condition.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown when <see cref="IsQueryable"/> is <c>false</c> and the subclass
		/// has not overridden this method.
		/// </exception>
		/// <remarks>
		/// The default implementation uses the <see cref="Query"/> hatch when
		/// <see cref="IsQueryable"/> is <c>true</c>.
		/// </remarks>
		protected virtual ValueTask<IList<TEntity>> FindAllAsync(IQuery query, CancellationToken cancellationToken = default) {
			if (IsQueryable) {
				var queryable = NormalizeQuery(query.Apply(Query()));
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
		/// Returns a list of items in the repository that match the given predicate,
		/// or an empty list if none of the items matches the condition.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// Thrown when <see cref="IsQueryable"/> is <c>false</c> and the subclass
		/// has not overridden this method.
		/// </exception>
		protected virtual ValueTask<IList<TEntity>> FindAllAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
			=> FindAllAsync(Kista.Query.Where(predicate), cancellationToken);

		/// <inheritdoc />
		public abstract ValueTask AddAsync(TEntity entity, CancellationToken cancellationToken = default);

		/// <inheritdoc />
		public abstract ValueTask AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

		/// <inheritdoc />
		public abstract ValueTask<bool> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

		/// <inheritdoc />
		public abstract ValueTask<bool> RemoveAsync(TEntity entity, CancellationToken cancellationToken = default);

		/// <inheritdoc />
		public abstract ValueTask RemoveRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

		/// <inheritdoc />
		public abstract ValueTask<TEntity?> FindAsync(TKey key, CancellationToken cancellationToken = default);

		#region IFilterableRepository explicit implementations

		ValueTask<bool> IFilterableRepository<TEntity, TKey>.ExistsAsync(IQueryFilter filter, CancellationToken cancellationToken)
			=> ExistsAsync(filter, cancellationToken);

		ValueTask<long> IFilterableRepository<TEntity, TKey>.CountAsync(IQueryFilter filter, CancellationToken cancellationToken)
			=> CountAsync(filter, cancellationToken);

		ValueTask<TEntity?> IFilterableRepository<TEntity, TKey>.FindFirstAsync(IQuery query, CancellationToken cancellationToken)
			=> FindFirstAsync(query, cancellationToken);

		ValueTask<IList<TEntity>> IFilterableRepository<TEntity, TKey>.FindAllAsync(IQuery query, CancellationToken cancellationToken)
			=> FindAllAsync(query, cancellationToken);

		#endregion

		/// <summary>
		/// Returns a queryable view of the entity set.
		/// </summary>
		/// <returns>
		/// Returns the <see cref="IQueryable{T}"/> produced by
		/// <see cref="Query"/>.
		/// </returns>
		/// <remarks>
		/// <para>
		/// This method is kept as a back-compat bridge for the obsolete
		/// <see cref="IQueryableRepository{TEntity, TKey}"/> contract and is
		/// expected to be removed in a future major version. New code should
		/// not call this method: the data-access translation pipeline is
		/// intentionally hidden behind the protected <see cref="Query"/>
		/// hatch so that LINQ expressions cannot leak into the application
		/// layer and break at runtime far from the repository.
		/// </para>
		/// </remarks>
		[Obsolete("Use the abstract Kista.Repository<TEntity, TKey> base class instead. The IQueryable hatch is no longer exposed to consumers.", false)]
		public virtual IQueryable<TEntity> AsQueryable() => Query();
	}
}
