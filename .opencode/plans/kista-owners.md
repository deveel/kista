# Kista.Owners - Implementation Plan

> **Created:** 2026-05-28
> **Status:** Design Complete, Ready for Implementation
> **Last Updated:** 2026-05-28

---

## Overview

Refactor user repository feature into a separate `Kista.Owners` package using a **decorator pattern** with **automatic type inference**. The decorator wraps any `IRepository<TEntity, TKey>` and automatically applies owner filtering on reads and owner assignment on writes.

---

## Design Decisions

### 1. Package Name: `Kista.Owners`
- Matches existing abstractions: `IHaveOwner`, `DataOwner`
- Future-proof: supports system owners, organizations, etc.
- Short and clear

### 2. Pattern: DI Decorator
- `UserScopedRepositoryDecorator<TEntity, TKey, TUserKey>` wraps `IRepository<TEntity, TKey>`
- Registered via Scrutor's `Services.Decorate()`
- Applied immediately when `.WithOwnerScoping()` is called

### 3. Type Inference: Reflection-Based
- `AddRepository<T>()` returns `RepositoryBuilder` with discovered types
- `.WithOwnerScoping()` infers `TUserKey` from `IUserRepository<,,>` interface
- No generic parameters needed on `.WithOwnerScoping()`

### 4. Property Discovery: Strict + Configured Fallback
1. `[DataOwner]` attribute (explicit)
2. `options.OwnerPropertyName` (configured)
3. Fallback to `"Owner"` constant
4. **NO** type-based discovery (error-prone)

### 5. Expression Composition: Parameter Normalization
- `ParameterReplacer` visitor in Kista core
- Ensures all expression filters use same parameter instance
- Prevents lambda variable collisions when composing filters

### 6. Breaking Change: Clean Break
- Move interfaces from Kista to Kista.Owners
- No forwarding stubs
- Users must add `Kista.Owners` package explicitly

### 7. Dependency Direction
- `Kista.Owners` → `Kista` (core) + `Scrutor`
- Driver packages → NO dependency on Kista.Owners
- Users explicitly opt-in by adding both packages

---

## Package Structure

### Kista (core - minimal changes)
```
src/Kista/
├── IRepository.cs
├── IRepository_T2.cs
├── IQuery.cs
├── IQueryFilter.cs
├── ExpressionQueryFilter.cs
├── Query.cs
├── CompositeQueryFilter.cs
├── ParameterReplacer.cs (NEW)
├── RepositoryContextBuilder.cs (modified)
├── RepositoryBuilder.cs (NEW)
└── [other core files...]

REMOVED:
├── IHaveOwner.cs (moved to Kista.Owners)
├── IUserAccessor.cs (moved to Kista.Owners)
├── IUserRepository.cs (moved to Kista.Owners)
├── IUserIdentifierStrategy.cs (moved to Kista.Owners)
├── StaticUserIdentifierStrategy.cs (moved to Kista.Owners)
├── CompositeUserIdentifierStrategy.cs (moved to Kista.Owners)
├── StrategyBasedUserAccessor.cs (moved to Kista.Owners)
└── UserIdentifierStrategyBuilder.cs (moved to Kista.Owners)
```

### Kista.Owners (NEW package)
```
src/Kista.Owners/
├── Kista.Owners.csproj
│   └── References: Kista, Scrutor
│
├── # Interfaces (MOVED from Kista)
├── IHaveOwner.cs
├── IUserAccessor.cs
├── IUserRepository.cs
├── DataOwnerAttribute.cs (MOVED from EF Core)
│
├── # User Resolution Strategies (MOVED from Kista + AspNetCore)
├── IUserIdentifierStrategy.cs
├── StaticUserIdentifierStrategy.cs
├── CompositeUserIdentifierStrategy.cs
├── UserIdentifierStrategyBuilder.cs
├── StrategyBasedUserAccessor.cs
├── ClaimUserIdentifierStrategy.cs
├── QueryStringUserIdentifierStrategy.cs
├── RouteUserIdentifierStrategy.cs
│
├── # Owner Scoping (NEW)
├── UserScopedRepositoryDecorator.cs
├── UserScopingOptions.cs
├── RepositoryBuilderExtensions.cs
└── ServiceCollectionExtensions.cs (MOVED from AspNetCore)
```

### Kista.EntityFramework (modified)
```
src/Kista.EntityFramework/
├── EntityRepository.cs
├── EntityRepository_T2.cs
├── EntityTypeBuilderExtensions.cs (keeps HasOwnerFilter)
└── REMOVE: EntityUserRepository.cs
```

### Kista.InMemory (modified)
```
src/Kista.InMemory/
├── InMemoryRepository.cs
│   └── ADD: protected virtual OnAddingEntity(TEntity entity)
└── REMOVE: InMemoryUserRepository.cs
```

### Kista.MongoFramework (modified)
```
src/Kista.MongoFramework/
├── MongoRepository.cs
└── REMOVE: MongoUserRepository.cs
```

### Kista.Manager.AspNetCore (modified)
```
src/Kista.Manager.AspNetCore/
├── HttpRequestCancellationSource.cs
├── ServiceCollectionExtensions.cs (keeps AddHttpRequestTokenSource)
└── REMOVE: HTTP user strategies (moved to Kista.Owners)
```

---

## Core Implementation Details

### 1. ParameterReplacer (Kista Core)

**File:** `src/Kista/ParameterReplacer.cs`

```csharp
namespace Kista;

internal class ParameterReplacer : ExpressionVisitor
{
    private readonly ParameterExpression _source;
    private readonly ParameterExpression _target;

    public ParameterReplacer(ParameterExpression source, ParameterExpression target)
    {
        _source = source;
        _target = target;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        return node == _source ? _target : base.VisitParameter(node);
    }

    public static Expression<Func<T, bool>> Combine<T>(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        var replacer = new ParameterReplacer(right.Parameters[0], left.Parameters[0]);
        var rightBody = replacer.Visit(right.Body);
        var combinedBody = Expression.AndAlso(left.Body, rightBody);
        return Expression.Lambda<Func<T, bool>>(combinedBody, left.Parameters);
    }
}
```

### 2. RepositoryBuilder (Kista Core)

**File:** `src/Kista/RepositoryBuilder.cs`

```csharp
namespace Kista;

public class RepositoryBuilder
{
    public IServiceCollection Services { get; }
    public Type EntityType { get; }
    public Type EntityKeyType { get; }
    public Type RepositoryType { get; }
    public Type ServiceType { get; }

    internal RepositoryBuilder(
        IServiceCollection services,
        Type entityType,
        Type entityKeyType,
        Type repositoryType,
        Type serviceType)
    {
        Services = services;
        EntityType = entityType;
        EntityKeyType = entityKeyType;
        RepositoryType = repositoryType;
        ServiceType = serviceType;
    }

    // WithOwnerScoping, WithTenantScoping, etc. implemented via extension methods
    // in Kista.Owners and other packages
}
```

### 3. RepositoryContextBuilder.AddRepository (Modified)

**File:** `src/Kista/RepositoryContextBuilder.cs`

```csharp
public RepositoryBuilder AddRepository<TRepository>() where TRepository : class
{
    var repositoryType = typeof(TRepository);
    var repoInterface = FindRepositoryInterface(repositoryType);
    
    if (repoInterface == null)
        throw new InvalidOperationException(
            $"Type {repositoryType.Name} does not implement IRepository<,>");

    var entityType = repoInterface.GetGenericArguments()[0];
    var keyType = repoInterface.GetGenericArguments()[1];

    Services.AddScoped(repoInterface, repositoryType);

    return new RepositoryBuilder(Services, entityType, keyType, repositoryType, repoInterface);
}

private Type? FindRepositoryInterface(Type repositoryType)
{
    foreach (var iface in repositoryType.GetInterfaces())
    {
        if (iface.IsGenericType && 
            iface.GetGenericTypeDefinition() == typeof(IRepository<,,>))
        {
            return iface;
        }
    }
    return null;
}
```

### 4. UserScopedRepositoryDecorator (Kista.Owners)

**File:** `src/Kista.Owners/UserScopedRepositoryDecorator.cs`

```csharp
namespace Kista.Owners;

public class UserScopedRepositoryDecorator<TEntity, TKey, TUserKey> 
    : IUserRepository<TEntity, TKey, TUserKey>
    where TEntity : class, IHaveOwner<TUserKey>
    where TKey : notnull
{
    private static readonly Lazy<PropertyInfo> _ownerProperty = new(DiscoverOwnerProperty);
    
    private readonly IRepository<TEntity, TKey> _inner;
    private readonly IUserAccessor<TUserKey> _userAccessor;
    private readonly UserScopingOptions _options;

    public UserScopedRepositoryDecorator(
        IRepository<TEntity, TKey> inner,
        IUserAccessor<TUserKey> userAccessor,
        IOptions<UserScopingOptions<TEntity, TKey, TUserKey>>? options = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _userAccessor = userAccessor ?? throw new ArgumentNullException(nameof(userAccessor));
        _options = options?.Value ?? new UserScopingOptions();
    }

    public IUserAccessor<TUserKey> UserAccessor => _userAccessor;

    // === READ OPERATIONS: Auto-filter by owner ===

    public async ValueTask<TEntity?> FindAsync(TKey key, CancellationToken ct = default)
    {
        var userId = _userAccessor.GetUserId();
        if (userId == null)
            return _options.ThrowWhenUserNotSet 
                ? throw new InvalidOperationException("User context is not set") 
                : null;

        var entity = await _inner.FindAsync(key, ct);
        return entity != null && Equals(entity.Owner, userId) ? entity : null;
    }

    public async ValueTask<IList<TEntity>> FindAllAsync(IQuery query, CancellationToken ct = default)
    {
        var userId = _userAccessor.GetUserId();
        if (userId == null)
            return _options.ThrowWhenUserNotSet 
                ? throw new InvalidOperationException("User context is not set") 
                : Array.Empty<TEntity>();

        var ownerFilter = BuildOwnerFilter(userId);
        var scopedQuery = query.WithFilter(ownerFilter);
        return await _inner.FindAllAsync(scopedQuery, ct);
    }

    public async ValueTask<TEntity?> FindFirstAsync(IQuery query, CancellationToken ct = default)
    {
        var userId = _userAccessor.GetUserId();
        if (userId == null) return null;

        var ownerFilter = BuildOwnerFilter(userId);
        var scopedQuery = query.WithFilter(ownerFilter);
        return await _inner.FindFirstAsync(scopedQuery, ct);
    }

    public async ValueTask<long> CountAsync(IQueryFilter filter, CancellationToken ct = default)
    {
        var userId = _userAccessor.GetUserId();
        if (userId == null) return 0;

        var ownerFilter = BuildOwnerFilter(userId);
        var combinedFilter = filter != null 
            ? new CompositeQueryFilter(filter, ownerFilter) 
            : ownerFilter;

        return await _inner.CountAsync(combinedFilter, ct);
    }

    public async ValueTask<bool> ExistsAsync(IQueryFilter filter, CancellationToken ct = default)
    {
        var userId = _userAccessor.GetUserId();
        if (userId == null) return false;

        var ownerFilter = BuildOwnerFilter(userId);
        var combinedFilter = new CompositeQueryFilter(filter, ownerFilter);
        return await _inner.ExistsAsync(combinedFilter, ct);
    }

    public async ValueTask<PageResult<TEntity>> GetPageAsync(PageQuery<TEntity> request, CancellationToken ct = default)
    {
        var userId = _userAccessor.GetUserId();
        if (userId == null)
            return new PageResult<TEntity>(request, 0, Array.Empty<TEntity>());

        var ownerFilter = BuildOwnerFilter(userId);
        var scopedRequest = request.WithFilter(ownerFilter);
        return await _inner.GetPageAsync(scopedRequest, ct);
    }

    // === WRITE OPERATIONS: Auto-set owner ===

    public ValueTask AddAsync(TEntity entity, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var userId = _userAccessor.GetUserId();
        if (userId != null)
        {
            _ownerProperty.Value.SetValue(entity, userId);
            _options.OnOwnerSet?.Invoke(entity, userId);
        }
        else if (_options.ThrowWhenUserNotSet)
        {
            throw new InvalidOperationException("User context is not set");
        }

        return _inner.AddAsync(entity, ct);
    }

    public ValueTask AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        var userId = _userAccessor.GetUserId();
        if (userId != null)
        {
            foreach (var entity in entities)
            {
                _ownerProperty.Value.SetValue(entity, userId);
                _options.OnOwnerSet?.Invoke(entity, userId);
            }
        }
        else if (_options.ThrowWhenUserNotSet)
        {
            throw new InvalidOperationException("User context is not set");
        }

        return _inner.AddRangeAsync(entities, ct);
    }

    // === PASS-THROUGH OPERATIONS ===

    public ValueTask<bool> UpdateAsync(TEntity entity, CancellationToken ct = default) =>
        _inner.UpdateAsync(entity, ct);

    public ValueTask<bool> RemoveAsync(TEntity entity, CancellationToken ct = default) =>
        _inner.RemoveAsync(entity, ct);

    public ValueTask RemoveRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default) =>
        _inner.RemoveRangeAsync(entities, ct);

    public ValueTask<TEntity?> FindOriginalAsync(TKey key, CancellationToken ct = default) =>
        _inner.FindOriginalAsync(key, ct);

    public IReadOnlyList<TEntity> Entities => _inner.Entities;

    public TKey? GetEntityKey(TEntity entity) => _inner.GetEntityKey(entity);

    public IServiceProvider? Services => _inner.Services;

    // === PROPERTY DISCOVERY ===

    private static PropertyInfo DiscoverOwnerProperty()
    {
        var entityType = typeof(TEntity);
        
        // Strategy 1: [DataOwner] attribute
        foreach (var prop in entityType.GetProperties())
        {
            if (Attribute.IsDefined(prop, typeof(DataOwnerAttribute)))
            {
                if (prop.PropertyType != typeof(TUserKey))
                    throw new InvalidOperationException(
                        $"Property '{prop.Name}' has type {prop.PropertyType.Name}, expected {typeof(TUserKey).Name}");
                return prop;
            }
        }

        // Strategy 2: Configured property name (will be set via options before first use)
        // This is handled at runtime, not in static constructor
        
        // Strategy 3: Fallback to "Owner"
        var ownerProp = entityType.GetProperty("Owner");
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
        return _options.OwnerFilterBuilder?.Invoke(userId) 
            ?? new ExpressionQueryFilter<TEntity>(BuildOwnerExpression(userId));
    }

    private Expression<Func<TEntity, bool>> BuildOwnerExpression(TUserKey userId)
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
}
```

### 5. UserScopingOptions (Kista.Owners)

**File:** `src/Kista.Owners/UserScopingOptions.cs`

```csharp
namespace Kista.Owners;

public class UserScopingOptions<TEntity, TKey, TUserKey>
    where TEntity : class, IHaveOwner<TUserKey>
    where TKey : notnull
{
    /// <summary>
    /// Whether to throw an exception when user context is not set.
    /// Default: false (returns null/empty instead).
    /// </summary>
    public bool ThrowWhenUserNotSet { get; set; }

    /// <summary>
    /// Override the owner property name instead of using [DataOwner] or "Owner".
    /// </summary>
    public string? OwnerPropertyName { get; set; }

    /// <summary>
    /// Custom owner filter builder. If null, uses default expression-based filter.
    /// </summary>
    public Func<TUserKey, IQueryFilter>? OwnerFilterBuilder { get; set; }

    /// <summary>
    /// Callback invoked when owner is set on an entity.
    /// Useful for logging or auditing.
    /// </summary>
    public Action<TEntity, TUserKey>? OnOwnerSet { get; set; }

    /// <summary>
    /// Callback invoked when an entity's owner doesn't match the current user.
    /// Useful for logging security events.
    /// </summary>
    public Action<TEntity, TUserKey>? OnOwnerMismatch { get; set; }
}
```

### 6. RepositoryBuilderExtensions (Kista.Owners)

**File:** `src/Kista.Owners/RepositoryBuilderExtensions.cs`

```csharp
namespace Kista.Owners;

public static class RepositoryBuilderExtensions
{
    public static RepositoryBuilder WithOwnerScoping(
        this RepositoryBuilder builder,
        Action<UserScopingOptions>? configure = null)
    {
        var userRepoInterface = FindUserRepositoryInterface(builder.RepositoryType, builder.EntityType, builder.EntityKeyType);
        if (userRepoInterface == null)
            throw new InvalidOperationException(
                $"Repository {builder.RepositoryType.Name} does not implement IUserRepository<,,>");

        var userKeyType = userRepoInterface.GetGenericArguments()[2];
        var decoratorType = typeof(UserScopedRepositoryDecorator<,,>)
            .MakeGenericType(builder.EntityType, builder.EntityKeyType, userKeyType);

        var optionsType = typeof(UserScopingOptions<,,>)
            .MakeGenericType(builder.EntityType, builder.EntityKeyType, userKeyType);

        var options = Activator.CreateInstance(optionsType);
        if (configure != null)
        {
            // Need to invoke configure with the generic options instance
            var configureMethod = configure.Method;
            configure.DynamicInvoke(options);
        }
        
        builder.Services.AddSingleton(optionsType, options!);
        builder.Services.Decorate(builder.ServiceType, decoratorType);

        return builder;
    }

    private static Type? FindUserRepositoryInterface(Type repositoryType, Type entityType, Type keyType)
    {
        foreach (var iface in repositoryType.GetInterfaces())
        {
            if (iface.IsGenericType && 
                iface.GetGenericTypeDefinition() == typeof(IUserRepository<,,>))
            {
                var args = iface.GetGenericArguments();
                if (args[0] == entityType && args[1] == keyType)
                    return iface;
            }
        }
        return null;
    }
}
```

---

## Usage Examples

### Example 1: Simple Owner-Scoped Repository

```csharp
// Entity
public class Article : IHaveOwner<string>
{
    public Guid Id { get; set; }
    
    [DataOwner]
    public string AuthorId { get; set; }
    
    public string Title { get; set; }
    
    string IHaveOwner<string>.Owner => AuthorId;
    void IHaveOwner<string>.SetOwner(string owner) => AuthorId = owner;
}

// Interface
public interface IArticleRepository : IUserRepository<Article, Guid, string>
{
    Task<Article?> FindByTitleAsync(string title);
}

// Implementation (ZERO owner-scoping code!)
public class ArticleRepository : EntityRepository<Article, Guid>, IArticleRepository
{
    public ArticleRepository(AppDbContext context) : base(context) { }
    
    public async Task<Article?> FindByTitleAsync(string title)
    {
        var results = await FindAllAsync(new Query(
            new ExpressionQueryFilter<Article>(a => a.Title == title)));
        return results.FirstOrDefault();
    }
}

// Registration
builder.Services.AddOwnerAccessor<string>(b => b.AddClaim("sub"));

builder.Services.AddRepositoryContext()
    .UseEntityFramework<AppDbContext>(...)
    .AddRepository<ArticleRepository>()
    .WithOwnerScoping();  // ONE LINE!
```

### Example 2: With Configuration

```csharp
builder.Services.AddRepositoryContext()
    .UseEntityFramework<AppDbContext>(...)
    .AddRepository<ArticleRepository>()
    .WithOwnerScoping(options => {
        options.ThrowWhenUserNotSet = true;
        options.OnOwnerSet = (entity, ownerId) => 
            logger.LogDebug("Owner {OwnerId} set on {Entity}", ownerId, entity.GetType().Name);
    });
```

### Example 3: Multiple Repositories

```csharp
builder.Services.AddRepositoryContext()
    .UseEntityFramework<AppDbContext>(...)
    .AddRepository<ArticleRepository>()
    .WithOwnerScoping()
    .AddRepository<CommentRepository>()
    .WithOwnerScoping()
    .AddRepository<ProductRepository>()  // Not owner-scoped
    .Build();
```

---

## Migration Steps

### Phase 1: Create Kista.Owners Package
1. Create `src/Kista.Owners/` directory
2. Create `Kista.Owners.csproj` with references to Kista and Scrutor
3. Move interfaces from Kista:
   - `IHaveOwner.cs`
   - `IUserAccessor.cs`
   - `IUserRepository.cs`
4. Move `DataOwnerAttribute.cs` from EF Core
5. Move strategy implementations from Kista and AspNetCore
6. Create new files:
   - `UserScopedRepositoryDecorator.cs`
   - `UserScopingOptions.cs`
   - `RepositoryBuilderExtensions.cs`
   - `ServiceCollectionExtensions.cs`

### Phase 2: Update Kista Core
1. Remove moved interfaces
2. Add `ParameterReplacer.cs`
3. Add `RepositoryBuilder.cs`
4. Update `RepositoryContextBuilder.AddRepository` to return `RepositoryBuilder`

### Phase 3: Update Driver Packages
1. **Kista.EntityFramework:**
   - Remove `EntityUserRepository.cs`
   - Keep `EntityTypeBuilderExtensions.HasOwnerFilter()`
2. **Kista.InMemory:**
   - Remove `InMemoryUserRepository.cs`
   - Add `protected virtual OnAddingEntity(TEntity entity)` to `InMemoryRepository`
3. **Kista.MongoFramework:**
   - Remove `MongoUserRepository.cs`

### Phase 4: Update Kista.Manager.AspNetCore
1. Remove HTTP user strategies (moved to Kista.Owners)
2. Keep `HttpRequestCancellationSource`

### Phase 5: Update Tests
1. Move user-related tests to `test/Kista.Owners.XUnit/`
2. Update driver tests to use decorator pattern
3. Add integration tests for composition scenarios

### Phase 6: Update Documentation
1. Update `docs/user-entities.md` with decorator pattern
2. Add migration guide
3. Update package dependency documentation
4. Update `docs/SUMMARY.md` if needed

---

## Test Strategy

### Kista.Owners.XUnit Tests

1. **UserScopedRepositoryDecoratorTests:**
   - Constructor validation
   - FindAsync with owner match/mismatch
   - FindAllAsync with owner filter
   - AddAsync sets owner automatically
   - AddRangeAsync sets owner on all entities
   - ThrowWhenUserNotSet behavior
   - OnOwnerSet callback invocation
   - OnOwnerMismatch callback invocation

2. **PropertyDiscoveryTests:**
   - [DataOwner] attribute detection
   - OwnerPropertyName configuration
   - Fallback to "Owner" property
   - Type mismatch errors
   - Missing property errors

3. **ExpressionCompositionTests:**
   - ParameterReplacer visitor
   - Multiple filter combination
   - No parameter collisions

4. **RegistrationTests:**
   - WithOwnerScoping type inference
   - Multiple repositories
   - Configuration options

### Driver Package Tests

1. Update existing tests to use decorator pattern
2. Verify query optimization is preserved
3. Test composition with other decorators

---

## Dependencies

### Kista.Owners.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <Description>Owner scoping and user resolution for Kista repositories</Description>
    <PackageTags>repository;ownership;user-scoping;multi-tenant;clean-architecture</PackageTags>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Kista\Kista.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Scrutor" Version="5.0.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.1" />
  </ItemGroup>
</Project>
```

---

## Open Questions (Resolved)

1. **Scrutor dependency:** ✅ Yes, add to Kista.Owners
2. **Breaking change:** ✅ Clean break, no forwarding stubs
3. **ExpressionQueryFilter location:** ✅ Stay in Kista core
4. **Driver package references:** ✅ Users explicitly opt-in
5. **Package name:** ✅ Kista.Owners
6. **Property discovery:** ✅ [DataOwner] → configured name → "Owner" fallback
7. **Parameter collision:** ✅ ParameterReplacer visitor in Kista core
8. **IServiceProviderFactory:** ✅ Not needed, immediate decorator registration

---

## Next Steps

When ready to resume implementation:

1. Start with Phase 1: Create Kista.Owners package structure
2. Move interfaces and strategies
3. Implement UserScopedRepositoryDecorator
4. Update Kista core with RepositoryBuilder and ParameterReplacer
5. Update driver packages
6. Write tests
7. Update documentation

---

## Notes

- The decorator pattern ensures driver-level query optimization is preserved
- Type inference eliminates verbose generic parameters
- Strict property discovery prevents runtime errors
- Parameter normalization prevents expression tree collisions
- Clean separation of concerns between core and owner scoping feature

## Final API

```csharp
services.AddRepositoryContext()
    .UseInMemory(b => b.WithLifecycle())
    .AddRepository<NoteRepository>(repo => repo
        .WithOwnerScoping()
        .WithSeedData<DefaultSeedData>(),
        ServiceLifetime.Singleton);
```

- `AddRepository<T>()` → `RepositoryBuilder` (for chaining repo-specific config)
- `AddRepository<T>(Action<RepositoryBuilder>, ServiceLifetime)` → `RepositoryContextBuilder` (inline config, chain unbroken)
- `RepositoryBuilder.WithOwnerScoping()` (from Kista.Owners) — registers decorator, infers `TUserKey` from `IHaveOwner<>` on entity
- `RepositoryBuilder.WithSeedData<TProvider>()` (from Kista core) — registers seed data provider, entity type inferred from builder
- No `Done()`, no `ConfigureRepository()`, no entity type repetition in seed data
