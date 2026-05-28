using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Kista
{
	/// <summary>
	/// A decorator that wraps an <see cref="IRepository{TEntity, TKey}"/> to provide
	/// automatic user scoping — assigning the current user as owner on writes and
	/// filtering all reads by the current user's identity.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The decorator is registered via Scrutor's <c>Decorate</c> method when
	/// <see cref="RepositoryBuilderExtensions.WithOwnerScoping(RepositoryBuilder, System.Action{UserScopingOptions}?)"/>
	/// is called. Consumers continue to resolve <c>IRepository&lt;TEntity, TKey&gt;</c> as usual;
	/// the decorator intercepts all operations transparently.
	/// </para>
	/// <para>
	/// The entity type must implement <see cref="IHaveOwner{TKey}"/>. The owner property is
	/// discovered automatically by scanning for the <see cref="DataOwnerAttribute"/> attribute,
	/// then falling back to a property named <c>"Owner"</c>.
	/// </para>
	/// <para>
	/// The user identity is resolved via <see cref="IUserAccessor{TKey}"/>, which is typically
	/// backed by a <see cref="CompositeUserIdentifierStrategy{TKey}"/> chain of strategies
	/// (claims, query string, route values, static fallback, etc.).
	/// </para>
	/// </remarks>
	/// <typeparam name="TEntity">The type of entity managed by the repository.</typeparam>
	/// <typeparam name="TKey">The type of the entity's primary key.</typeparam>
	/// <typeparam name="TUserKey">The type of the user identifier.</typeparam>
	public class UserScopedRepositoryDecorator<TEntity, TKey, TUserKey>
		: IUserRepository<TEntity, TKey, TUserKey>,
		  IFilterableRepository<TEntity, TKey>,
		  IPageableRepository<TEntity, TKey>
		where TEntity : class, IHaveOwner<TUserKey>
		where TKey : notnull
	{
		private const string UserContextNotSetMessage = "User context is not set";
		private static readonly Lazy<PropertyInfo> _ownerProperty = new(DiscoverOwnerProperty);

		private readonly IRepository<TEntity, TKey> _inner;
		private readonly IUserAccessor<TUserKey> _userAccessor;
		private readonly UserScopingOptions? _options;

		/// <summary>
		/// Initializes a new instance of the decorator.
		/// </summary>
		/// <param name="inner">The inner repository to wrap.</param>
		/// <param name="userAccessor">The user accessor for resolving the current user identity.</param>
		/// <param name="options">Optional scoping configuration. When <c>null</c>, defaults are used.</param>
		/// <exception cref="System.ArgumentNullException">
		/// Thrown when <paramref name="inner"/> or <paramref name="userAccessor"/> is <c>null</c>.
		/// </exception>
		public UserScopedRepositoryDecorator(
			IRepository<TEntity, TKey> inner,
			IUserAccessor<TUserKey> userAccessor,
			UserScopingOptions? options = null)
		{
			_inner = inner ?? throw new System.ArgumentNullException(nameof(inner));
			_userAccessor = userAccessor ?? throw new System.ArgumentNullException(nameof(userAccessor));
			_options = options;
		}

		/// <summary>
		/// Gets the user accessor used to resolve the current user identity.
		/// </summary>
		public IUserAccessor<TUserKey> UserAccessor => _userAccessor;

		private UserScopingOptions Options => _options ?? new UserScopingOptions();

		/// <inheritdoc />
		public ValueTask<TEntity?> FindAsync(TKey key, CancellationToken cancellationToken = default)
		{
			var userId = _userAccessor.GetUserId();
			if (EqualityComparer<TUserKey>.Default.Equals(userId, default))
				return Options.ThrowWhenUserNotSet
					? throw new System.InvalidOperationException(UserContextNotSetMessage)
					: new ValueTask<TEntity?>(default(TEntity));

			return FindScopedAsync(key, userId, cancellationToken);
		}

		private async ValueTask<TEntity?> FindScopedAsync(TKey key, TUserKey userId, CancellationToken ct)
		{
			var entity = await _inner.FindAsync(key, ct);
			if (entity == null)
				return null;

			if (!Equals(userId, entity.Owner))
				return null;

			return entity;
		}

		/// <inheritdoc />
		public ValueTask AddAsync(TEntity entity, CancellationToken cancellationToken = default)
			=> ApplyOwnerAndCallAsync(entity, () => _inner.AddAsync(entity, cancellationToken));

		/// <inheritdoc />
		public ValueTask AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
		{
			System.ArgumentNullException.ThrowIfNull(entities);

			var userId = _userAccessor.GetUserId();
			if (!EqualityComparer<TUserKey>.Default.Equals(userId, default))
			{
				foreach (var entity in entities)
				{
					_ownerProperty.Value.SetValue(entity, userId);
				}
			}
			else if (Options.ThrowWhenUserNotSet)
			{
				throw new System.InvalidOperationException(UserContextNotSetMessage);
			}

			return _inner.AddRangeAsync(entities, cancellationToken);
		}

		/// <inheritdoc />
		public ValueTask<bool> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
			=> _inner.UpdateAsync(entity, cancellationToken);

		/// <inheritdoc />
		public ValueTask<bool> RemoveAsync(TEntity entity, CancellationToken cancellationToken = default)
			=> _inner.RemoveAsync(entity, cancellationToken);

		/// <inheritdoc />
		public ValueTask RemoveRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
			=> _inner.RemoveRangeAsync(entities, cancellationToken);

		/// <inheritdoc />
		public TKey? GetEntityKey(TEntity entity) => _inner.GetEntityKey(entity);

		/// <inheritdoc />
		public IServiceProvider? Services => _inner.Services;

		/// <inheritdoc />
		public ValueTask<IList<TEntity>> FindAllAsync(IQuery query, CancellationToken cancellationToken = default)
			=> ApplyOwnerFilterAndCallAsync(query, q => _inner.FindAllAsync(q, cancellationToken));

		/// <inheritdoc />
		public ValueTask<TEntity?> FindFirstAsync(IQuery query, CancellationToken cancellationToken = default)
			=> ApplyOwnerFilterAndCallAsync(query, q => _inner.FindFirstAsync(q, cancellationToken));

		/// <inheritdoc />
		public ValueTask<long> CountAsync(IQueryFilter filter, CancellationToken cancellationToken = default)
			=> ApplyOwnerFilterAndCallAsync(filter, f => _inner.CountAsync(f, cancellationToken));

		/// <inheritdoc />
		public ValueTask<bool> ExistsAsync(IQueryFilter filter, CancellationToken cancellationToken = default)
			=> ApplyOwnerFilterAndCallAsync(filter, f => _inner.ExistsAsync(f, cancellationToken));

		/// <inheritdoc />
		public ValueTask<PageResult<TEntity>> GetPageAsync(PageQuery<TEntity> request, CancellationToken cancellationToken = default)
			=> ApplyOwnerFilterAndCallAsync(request, r => _inner.GetPageAsync(r, cancellationToken));

		// === Helpers ===

		private ValueTask ApplyOwnerAndCallAsync(TEntity entity, Func<ValueTask> action)
		{
			System.ArgumentNullException.ThrowIfNull(entity);

			var userId = _userAccessor.GetUserId();
			if (!EqualityComparer<TUserKey>.Default.Equals(userId, default))
			{
				_ownerProperty.Value.SetValue(entity, userId);
			}
			else if (Options.ThrowWhenUserNotSet)
			{
				throw new System.InvalidOperationException(UserContextNotSetMessage);
			}

			return action();
		}

		private async ValueTask<IList<TEntity>> ApplyOwnerFilterAndCallAsync(
			IQuery query, Func<IQuery, ValueTask<IList<TEntity>>> action)
		{
			var userId = _userAccessor.GetUserId();
			if (EqualityComparer<TUserKey>.Default.Equals(userId, default))
				return Options.ThrowWhenUserNotSet
					? throw new System.InvalidOperationException(UserContextNotSetMessage)
					: Array.Empty<TEntity>();

			var scopedQuery = ApplyOwnerToQuery(query, userId);
			return await action(scopedQuery);
		}

		private async ValueTask<TEntity?> ApplyOwnerFilterAndCallAsync(
			IQuery query, Func<IQuery, ValueTask<TEntity?>> action)
		{
			var userId = _userAccessor.GetUserId();
			if (EqualityComparer<TUserKey>.Default.Equals(userId, default))
				return null;

			var scopedQuery = ApplyOwnerToQuery(query, userId);
			return await action(scopedQuery);
		}

		private async ValueTask<long> ApplyOwnerFilterAndCallAsync(
			IQueryFilter filter, Func<IQueryFilter, ValueTask<long>> action)
		{
			var userId = _userAccessor.GetUserId();
			if (EqualityComparer<TUserKey>.Default.Equals(userId, default))
				return 0;

			var ownerFilter = BuildOwnerFilter(userId);
			var combined = CombineFilters(filter, ownerFilter);
			return await action(combined);
		}

		private async ValueTask<bool> ApplyOwnerFilterAndCallAsync(
			IQueryFilter filter, Func<IQueryFilter, ValueTask<bool>> action)
		{
			var userId = _userAccessor.GetUserId();
			if (EqualityComparer<TUserKey>.Default.Equals(userId, default))
				return false;

			var ownerFilter = BuildOwnerFilter(userId);
			var combined = CombineFilters(filter, ownerFilter);
			return await action(combined);
		}

		private async ValueTask<PageResult<TEntity>> ApplyOwnerFilterAndCallAsync(
			PageQuery<TEntity> request, Func<PageQuery<TEntity>, ValueTask<PageResult<TEntity>>> action)
		{
			var userId = _userAccessor.GetUserId();
			if (EqualityComparer<TUserKey>.Default.Equals(userId, default))
				return Options.ThrowWhenUserNotSet
					? throw new System.InvalidOperationException(UserContextNotSetMessage)
					: new PageResult<TEntity>(request, 0, Array.Empty<TEntity>());

			var scopedRequest = ApplyOwnerToRequest(request, userId);
			return await action(scopedRequest);
		}

		private IQuery ApplyOwnerToQuery(IQuery query, TUserKey userId)
		{
			var ownerFilter = BuildOwnerFilter(userId);
			var combinedFilter = CombineFilters(query.Filter, ownerFilter);
			return new Query(combinedFilter, query.Order);
		}

		private PageQuery<TEntity> ApplyOwnerToRequest(PageQuery<TEntity> request, TUserKey userId)
		{
			var ownerFilter = BuildOwnerFilter(userId);
			var combinedFilter = CombineFilters(((IQuery)request).Filter, ownerFilter);

			return new PageQuery<TEntity>(request.Page, request.Size)
			{
				Query = new Query(combinedFilter, ((IQuery)request).Order)
			};
		}

		// === PROPERTY DISCOVERY ===

		private static PropertyInfo DiscoverOwnerProperty()
		{
			var entityType = typeof(TEntity);

			foreach (var prop in entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
			{
				foreach (var attrType in prop.GetCustomAttributes().Select(attr => attr.GetType()))
				{
					if (attrType.Name == "DataOwnerAttribute" &&
						(attrType.Namespace == "Kista" || attrType.Namespace == "Kista.Owners"))
					{
						if (prop.PropertyType != typeof(TUserKey))
							throw new System.InvalidOperationException(
								$"Property '{prop.Name}' has type {prop.PropertyType.Name}, expected {typeof(TUserKey).Name}");
						return prop;
					}
				}
			}

			var ownerProp = entityType.GetProperty("Owner", BindingFlags.Public | BindingFlags.Instance);
			if (ownerProp == null)
				throw new System.InvalidOperationException(
					$"Entity {entityType.Name} has no [DataOwner] property and no 'Owner' property");
			if (ownerProp.PropertyType != typeof(TUserKey))
				throw new System.InvalidOperationException(
					$"Property 'Owner' has type {ownerProp.PropertyType.Name}, expected {typeof(TUserKey).Name}");
			return ownerProp;
		}

		// === FILTER BUILDING ===

		private static IQueryFilter BuildOwnerFilter(TUserKey userId)
		{
			return new ExpressionQueryFilter<TEntity>(BuildOwnerExpression(userId));
		}

		private static Expression<Func<TEntity, bool>> BuildOwnerExpression(TUserKey userId)
		{
			var param = Expression.Parameter(typeof(TEntity), "x");
			var ownerProperty = Expression.Property(param, _ownerProperty.Value);
			var constant = Expression.Constant(userId, typeof(TUserKey));

			Expression comparison;
			if (typeof(TUserKey) == typeof(string))
			{
				comparison = Expression.Equal(ownerProperty, constant);
			}
			else
			{
				var equalsMethod = typeof(TUserKey).GetMethod(
					nameof(object.Equals), new[] { typeof(object) });
				comparison = Expression.Call(ownerProperty, equalsMethod!, constant);
			}

			return Expression.Lambda<Func<TEntity, bool>>(comparison, param);
		}

		private static IQueryFilter CombineFilters(IQueryFilter? existing, IQueryFilter ownerFilter)
		{
			if (existing == null || existing.IsEmpty())
				return ownerFilter;

			return QueryFilter.Combine(existing, ownerFilter);
		}
	}
}
