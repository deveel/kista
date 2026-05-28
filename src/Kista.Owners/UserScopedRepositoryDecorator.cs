using System.Linq.Expressions;
using System.Reflection;

namespace Kista
{
	public class UserScopedRepositoryDecorator<TEntity, TKey, TUserKey>
		: IUserRepository<TEntity, TKey, TUserKey>,
		  IFilterableRepository<TEntity, TKey>,
		  IPageableRepository<TEntity, TKey>
		where TEntity : class, IHaveOwner<TUserKey>
		where TKey : notnull
	{
		private static readonly Lazy<PropertyInfo> _ownerProperty = new(DiscoverOwnerProperty);

		private readonly IRepository<TEntity, TKey> _inner;
		private readonly IUserAccessor<TUserKey> _userAccessor;
		private readonly UserScopingOptions? _options;

		public UserScopedRepositoryDecorator(
			IRepository<TEntity, TKey> inner,
			IUserAccessor<TUserKey> userAccessor,
			UserScopingOptions? options = null)
		{
			_inner = inner ?? throw new ArgumentNullException(nameof(inner));
			_userAccessor = userAccessor ?? throw new ArgumentNullException(nameof(userAccessor));
			_options = options;
		}

		public IUserAccessor<TUserKey> UserAccessor => _userAccessor;

		private UserScopingOptions Options => _options ?? new UserScopingOptions();

		// === IRepository<TEntity, TKey> ===

		public ValueTask<TEntity?> FindAsync(TKey key, CancellationToken cancellationToken = default)
		{
			var userId = _userAccessor.GetUserId();
			if (userId == null)
				return Options.ThrowWhenUserNotSet
					? throw new InvalidOperationException("User context is not set")
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

		public ValueTask AddAsync(TEntity entity, CancellationToken cancellationToken = default)
			=> ApplyOwnerAndCallAsync(entity, () => _inner.AddAsync(entity, cancellationToken));

		public ValueTask AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(entities);

			var userId = _userAccessor.GetUserId();
			if (userId != null)
			{
				foreach (var entity in entities)
				{
					_ownerProperty.Value.SetValue(entity, userId);
				}
			}
			else if (Options.ThrowWhenUserNotSet)
			{
				throw new InvalidOperationException("User context is not set");
			}

			return _inner.AddRangeAsync(entities, cancellationToken);
		}

		public ValueTask<bool> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
			=> _inner.UpdateAsync(entity, cancellationToken);

		public ValueTask<bool> RemoveAsync(TEntity entity, CancellationToken cancellationToken = default)
			=> _inner.RemoveAsync(entity, cancellationToken);

		public ValueTask RemoveRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
			=> _inner.RemoveRangeAsync(entities, cancellationToken);

		public TKey? GetEntityKey(TEntity entity) => _inner.GetEntityKey(entity);

		public IServiceProvider? Services => _inner.Services;

		// === IFilterableRepository<TEntity, TKey> ===

		public ValueTask<IList<TEntity>> FindAllAsync(IQuery query, CancellationToken cancellationToken = default)
			=> ApplyOwnerFilterAndCallAsync(query, q => _inner.FindAllAsync(q, cancellationToken));

		public ValueTask<TEntity?> FindFirstAsync(IQuery query, CancellationToken cancellationToken = default)
			=> ApplyOwnerFilterAndCallAsync(query, q => _inner.FindFirstAsync(q, cancellationToken));

		public ValueTask<long> CountAsync(IQueryFilter filter, CancellationToken cancellationToken = default)
			=> ApplyOwnerFilterAndCallAsync(filter, f => _inner.CountAsync(f, cancellationToken));

		public ValueTask<bool> ExistsAsync(IQueryFilter filter, CancellationToken cancellationToken = default)
			=> ApplyOwnerFilterAndCallAsync(filter, f => _inner.ExistsAsync(f, cancellationToken));

		// === IPageableRepository<TEntity, TKey> ===

		public ValueTask<PageResult<TEntity>> GetPageAsync(PageQuery<TEntity> request, CancellationToken cancellationToken = default)
			=> ApplyOwnerFilterAndCallAsync(request, r => _inner.GetPageAsync(r, cancellationToken));

		// === Helpers ===

		private ValueTask ApplyOwnerAndCallAsync(TEntity entity, Func<ValueTask> action)
		{
			ArgumentNullException.ThrowIfNull(entity);

			var userId = _userAccessor.GetUserId();
			if (userId != null)
			{
				_ownerProperty.Value.SetValue(entity, userId);
			}
			else if (Options.ThrowWhenUserNotSet)
			{
				throw new InvalidOperationException("User context is not set");
			}

			return action();
		}

		private async ValueTask<IList<TEntity>> ApplyOwnerFilterAndCallAsync(
			IQuery query, Func<IQuery, ValueTask<IList<TEntity>>> action)
		{
			var userId = _userAccessor.GetUserId();
			if (userId == null)
				return Options.ThrowWhenUserNotSet
					? throw new InvalidOperationException("User context is not set")
					: Array.Empty<TEntity>();

			var scopedQuery = ApplyOwnerToQuery(query, userId);
			return await action(scopedQuery);
		}

		private async ValueTask<TEntity?> ApplyOwnerFilterAndCallAsync(
			IQuery query, Func<IQuery, ValueTask<TEntity?>> action)
		{
			var userId = _userAccessor.GetUserId();
			if (userId == null)
				return null;

			var scopedQuery = ApplyOwnerToQuery(query, userId);
			return await action(scopedQuery);
		}

		private async ValueTask<long> ApplyOwnerFilterAndCallAsync(
			IQueryFilter filter, Func<IQueryFilter, ValueTask<long>> action)
		{
			var userId = _userAccessor.GetUserId();
			if (userId == null)
				return 0;

			var ownerFilter = BuildOwnerFilter(userId);
			var combined = CombineFilters(filter, ownerFilter);
			return await action(combined);
		}

		private async ValueTask<bool> ApplyOwnerFilterAndCallAsync(
			IQueryFilter filter, Func<IQueryFilter, ValueTask<bool>> action)
		{
			var userId = _userAccessor.GetUserId();
			if (userId == null)
				return false;

			var ownerFilter = BuildOwnerFilter(userId);
			var combined = CombineFilters(filter, ownerFilter);
			return await action(combined);
		}

		private async ValueTask<PageResult<TEntity>> ApplyOwnerFilterAndCallAsync(
			PageQuery<TEntity> request, Func<PageQuery<TEntity>, ValueTask<PageResult<TEntity>>> action)
		{
			var userId = _userAccessor.GetUserId();
			if (userId == null)
				return Options.ThrowWhenUserNotSet
					? throw new InvalidOperationException("User context is not set")
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
				foreach (var attr in prop.GetCustomAttributes())
				{
					var attrType = attr.GetType();
					if (attrType.Name == "DataOwnerAttribute" &&
						(attrType.Namespace == "Kista" || attrType.Namespace == "Kista.Owners"))
					{
						if (prop.PropertyType != typeof(TUserKey))
							throw new InvalidOperationException(
								$"Property '{prop.Name}' has type {prop.PropertyType.Name}, expected {typeof(TUserKey).Name}");
						return prop;
					}
				}
			}

			var ownerProp = entityType.GetProperty("Owner", BindingFlags.Public | BindingFlags.Instance);
			if (ownerProp == null)
				throw new InvalidOperationException(
					$"Entity {entityType.Name} has no [DataOwner] property and no 'Owner' property");
			if (ownerProp.PropertyType != typeof(TUserKey))
				throw new InvalidOperationException(
					$"Property 'Owner' has type {ownerProp.PropertyType.Name}, expected {typeof(TUserKey).Name}");
			return ownerProp;
		}

		// === FILTER BUILDING ===

		private IQueryFilter BuildOwnerFilter(TUserKey userId)
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
