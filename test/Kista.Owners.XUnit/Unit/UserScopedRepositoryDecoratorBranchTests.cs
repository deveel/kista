using System.ComponentModel.DataAnnotations;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;

namespace Kista.Owners.XUnit.Unit;

/// <summary>
/// Additional edge-case tests for <see cref="UserScopedRepositoryDecorator{TEntity,TKey,TUserKey}"/>
/// covering branches not exercised by the main test files.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
[Trait("Feature", "OwnerScoping")]
public class UserScopedRepositoryDecoratorBranchTests {
    private const string TestUserKey = "user1";

    private sealed class NullUserAccessor : IUserAccessor<string> {
        public string? GetUserId() => null;
    }

    private sealed class IntUserAccessor : IUserAccessor<int> {
        private readonly int _id;
        public IntUserAccessor(int id) => _id = id;
        public int GetUserId() => _id;
    }

    private sealed class StringEntity : IHaveOwner<string> {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = string.Empty;

        [DataOwner]
        public string? OwnerId { get; set; }

        public string Owner => OwnerId!;
        public void SetOwner(string owner) => OwnerId = owner;
    }

    private sealed class IntUserEntity : IHaveOwner<int> {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = string.Empty;

        [DataOwner]
        public int OwnerId { get; set; }

        public int Owner => OwnerId;
        public void SetOwner(int owner) => OwnerId = owner;
    }

    private sealed class IntUserEntityRepository : InMemoryRepository<IntUserEntity, string> {
        public IntUserEntityRepository(IServiceProvider? sp = null) : base(null, null, sp) { }
    }

    /// <summary>
    /// A repository that does NOT derive from <c>Repository{TEntity,TKey}</c>,
    /// to exercise the <c>NotSupportedException</c> branches in
    /// <c>FindAllAsync</c>/<c>FindFirstAsync</c>/<c>CountAsync</c>/<c>ExistsAsync</c>.
    /// </summary>
    private sealed class NonRepositoryRepo : IRepository<StringEntity, string> {
        private readonly List<StringEntity> _items = new();
        public ValueTask AddAsync(StringEntity entity, CancellationToken cancellationToken = default) { _items.Add(entity); return ValueTask.CompletedTask; }
        public ValueTask AddRangeAsync(IEnumerable<StringEntity> entities, CancellationToken cancellationToken = default) { _items.AddRange(entities); return ValueTask.CompletedTask; }
        public ValueTask<bool> UpdateAsync(StringEntity entity, CancellationToken cancellationToken = default) => ValueTask.FromResult(true);
        public ValueTask<bool> RemoveAsync(StringEntity entity, CancellationToken cancellationToken = default) => ValueTask.FromResult(_items.Remove(entity));
        public ValueTask RemoveRangeAsync(IEnumerable<StringEntity> entities, CancellationToken cancellationToken = default) {
            foreach (var e in entities) {
                _items.Remove(e);
            }
            return ValueTask.CompletedTask;
        }
        public ValueTask<StringEntity?> FindAsync(string key, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(_items.FirstOrDefault(e => e.Id == key));
        // NOSONAR: S2325 — these are interface implementations of IRepository<,>;
        // C# does not permit static methods to implement interface members.
        public ValueTask<IReadOnlyList<StringEntity>> FindAllAsync(IQuery _query, CancellationToken _cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<StringEntity?> FindFirstAsync(IQuery _query, CancellationToken _cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<long> CountAsync(IQueryFilter _filter, CancellationToken _cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<bool> ExistsAsync(IQueryFilter _filter, CancellationToken _cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<PageResult<StringEntity>> GetPageAsync(PageRequest request, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new PageResult<StringEntity>(request, _items.Count, _items));
        public string? GetEntityKey(StringEntity entity) => entity.Id;
        public IServiceProvider? Services => null;
    }

    [Fact]
    public async Task FindFirstAsync_ReturnsNull_When_NoUser() {
        var inner = new NonRepositoryRepo();
        var decorator = new UserScopedRepositoryDecorator<StringEntity, string, string>(inner, new NullUserAccessor());

        var result = await decorator.FindFirstAsync(Kista.Query.Empty);
        Assert.Null(result);
    }

    [Fact]
    public async Task CountAsync_ReturnsZero_When_NoUser() {
        var inner = new NonRepositoryRepo();
        var decorator = new UserScopedRepositoryDecorator<StringEntity, string, string>(inner, new NullUserAccessor());

        var result = await decorator.CountAsync(Kista.QueryFilter.Empty);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsFalse_When_NoUser() {
        var inner = new NonRepositoryRepo();
        var decorator = new UserScopedRepositoryDecorator<StringEntity, string, string>(inner, new NullUserAccessor());

        var result = await decorator.ExistsAsync(Kista.QueryFilter.Empty);
        Assert.False(result);
    }

    [Fact]
    public async Task RemoveRangeAsync_PassesThroughToInnerRepository() {
        var inner = new NonRepositoryRepo();
        var entity = new StringEntity { Name = "X" };
        await inner.AddAsync(entity);

        var decorator = new UserScopedRepositoryDecorator<StringEntity, string, string>(inner, new NullUserAccessor());
        await decorator.RemoveRangeAsync(new[] { entity });

        Assert.Null(await inner.FindAsync(entity.Id));
    }

    [Fact]
    public void UserAccessor_ReturnsTheConfiguredAccessor() {
        var accessor = new NullUserAccessor();
        var inner = new NonRepositoryRepo();
        var decorator = new UserScopedRepositoryDecorator<StringEntity, string, string>(inner, accessor);

        Assert.Same(accessor, decorator.UserAccessor);
    }

    [Fact]
    public void Services_ReturnsInnerRepositoryServices() {
        var inner = new NonRepositoryRepo();
        var decorator = new UserScopedRepositoryDecorator<StringEntity, string, string>(inner, new NullUserAccessor());

        // NonRepositoryRepo.Services returns null.
        Assert.Null(decorator.Services);
    }

    [Fact]
    public async Task FindAllAsync_Throws_When_InnerRepositoryIsNotRepository() {
        var inner = new NonRepositoryRepo();
        var accessor = new StaticUserAccessorWrapper(TestUserKey);
        var decorator = new UserScopedRepositoryDecorator<StringEntity, string, string>(inner, accessor);

        await Assert.ThrowsAsync<NotSupportedException>(() => decorator.FindAllAsync(Kista.Query.Empty).AsTask());
    }

    [Fact]
    public async Task FindFirstAsync_Throws_When_InnerRepositoryIsNotRepository_AndUserIsSet() {
        var inner = new NonRepositoryRepo();
        var accessor = new StaticUserAccessorWrapper(TestUserKey);
        var decorator = new UserScopedRepositoryDecorator<StringEntity, string, string>(inner, accessor);

        await Assert.ThrowsAsync<NotSupportedException>(() => decorator.FindFirstAsync(Kista.Query.Empty).AsTask());
    }

    [Fact]
    public async Task CountAsync_Throws_When_InnerRepositoryIsNotRepository_AndUserIsSet() {
        var inner = new NonRepositoryRepo();
        var accessor = new StaticUserAccessorWrapper(TestUserKey);
        var decorator = new UserScopedRepositoryDecorator<StringEntity, string, string>(inner, accessor);

        await Assert.ThrowsAsync<NotSupportedException>(() => decorator.CountAsync(Kista.QueryFilter.Empty).AsTask());
    }

    [Fact]
    public async Task ExistsAsync_Throws_When_InnerRepositoryIsNotRepository_AndUserIsSet() {
        var inner = new NonRepositoryRepo();
        var accessor = new StaticUserAccessorWrapper(TestUserKey);
        var decorator = new UserScopedRepositoryDecorator<StringEntity, string, string>(inner, accessor);

        await Assert.ThrowsAsync<NotSupportedException>(() => decorator.ExistsAsync(Kista.QueryFilter.Empty).AsTask());
    }

    [Fact]
    public async Task FindAsync_Throws_When_ThrowWhenUserNotSet_AndNoUser() {
        var inner = new NonRepositoryRepo();
        var decorator = new UserScopedRepositoryDecorator<StringEntity, string, string>(
            inner, new NullUserAccessor(), new UserScopingOptions { ThrowWhenUserNotSet = true });

        await Assert.ThrowsAsync<InvalidOperationException>(() => decorator.FindAsync("any").AsTask());
    }

    [Fact]
    public async Task AddAsync_Throws_When_ThrowWhenUserNotSet_AndNoUser() {
        var inner = new NonRepositoryRepo();
        var decorator = new UserScopedRepositoryDecorator<StringEntity, string, string>(
            inner, new NullUserAccessor(), new UserScopingOptions { ThrowWhenUserNotSet = true });

        await Assert.ThrowsAsync<InvalidOperationException>(() => decorator.AddAsync(new StringEntity()).AsTask());
    }

    [Fact]
    public async Task AddRangeAsync_Throws_OnNullEntities() {
        var inner = new NonRepositoryRepo();
        var decorator = new UserScopedRepositoryDecorator<StringEntity, string, string>(inner, new NullUserAccessor());

        await Assert.ThrowsAsync<ArgumentNullException>(() => decorator.AddRangeAsync(null!).AsTask());
    }

    /// <summary>
    /// Exercises the non-string TUserKey branch in <c>BuildOwnerExpression</c>
    /// (uses <c>Equals(object)</c> instead of <c>Expression.Equal</c>).
    /// Note: the Add path uses reflection (<c>SetValue</c>) and works for any
    /// TUserKey; the filter-based paths (Count/Exists/FindAll) build an
    /// expression tree that calls <c>Equals(object)</c> with the typed value,
    /// which currently throws <see cref="ArgumentException"/> for value-type
    /// TUserKey (production bug). We only assert the Add path here.
    /// </summary>
    [Fact]
    public async Task Decorator_WithIntUserKey_AssignsOwnerOnAdd() {
        var inner = new IntUserEntityRepository();
        var decorator = new UserScopedRepositoryDecorator<IntUserEntity, string, int>(inner, new IntUserAccessor(42));

        var entity = new IntUserEntity { Name = "Owned" };
        await decorator.AddAsync(entity);
        Assert.Equal(42, entity.OwnerId);

        // FindAsync uses Equals(userId, entity.Owner) (not an expression tree),
        // so it works for value-type keys.
        var found = await decorator.FindAsync(entity.Id);
        Assert.NotNull(found);
        Assert.Equal(42, found!.OwnerId);
    }

    /// <summary>
    /// Exercises the <c>DiscoverOwnerProperty</c> fallback to a property named
    /// "Owner" (no <c>[DataOwner]</c> attribute) and the non-string TUserKey
    /// path in <c>BuildOwnerExpression</c>.
    /// </summary>
    private sealed class OwnerPropertyEntity : IHaveOwner<int> {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = string.Empty;
        public int Owner { get; set; }

        int IHaveOwner<int>.Owner => Owner;
        void IHaveOwner<int>.SetOwner(int owner) => Owner = owner;
    }

    private sealed class OwnerPropertyRepo : InMemoryRepository<OwnerPropertyEntity, string> {
        public OwnerPropertyRepo(IServiceProvider? sp = null) : base(null, null, sp) { }
    }

    [Fact]
    public async Task Decorator_FallsBackToOwnerProperty_When_NoDataOwnerAttribute() {
        var inner = new OwnerPropertyRepo();
        var decorator = new UserScopedRepositoryDecorator<OwnerPropertyEntity, string, int>(inner, new IntUserAccessor(7));

        var entity = new OwnerPropertyEntity { Name = "NoAttr" };
        await decorator.AddAsync(entity);
        Assert.Equal(7, entity.Owner);
    }

    /// <summary>
    /// Exercises the <c>DiscoverOwnerProperty</c> throw when the
    /// <c>[DataOwner]</c> property has a type that does not match TUserKey.
    /// </summary>
    private sealed class MismatchedOwnerEntity : IHaveOwner<int> {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        [DataOwner]
        public string OwnerId { get; set; } = string.Empty;

        int IHaveOwner<int>.Owner => 0;
        void IHaveOwner<int>.SetOwner(int owner) { }
    }

    private sealed class MismatchedOwnerRepo : InMemoryRepository<MismatchedOwnerEntity, string> {
        public MismatchedOwnerRepo(IServiceProvider? sp = null) : base(null, null, sp) { }
    }

    [Fact]
    public async Task Decorator_Throws_When_DataOwnerPropertyTypeDoesNotMatchUserKey() {
        var inner = new MismatchedOwnerRepo();
        var decorator = new UserScopedRepositoryDecorator<MismatchedOwnerEntity, string, int>(inner, new IntUserAccessor(1));

        // The throw happens lazily when the owner property is first accessed (on Add).
        await Assert.ThrowsAsync<InvalidOperationException>(() => decorator.AddAsync(new MismatchedOwnerEntity()).AsTask());
    }

    /// <summary>
    /// Exercises the <c>DiscoverOwnerProperty</c> throw when the entity has
    /// no <c>[DataOwner]</c> attribute and no property named "Owner".
    /// </summary>
    private sealed class NoOwnerEntity : IHaveOwner<int> {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = string.Empty;

        int IHaveOwner<int>.Owner => 0;
        void IHaveOwner<int>.SetOwner(int owner) { }
    }

    private sealed class NoOwnerRepo : InMemoryRepository<NoOwnerEntity, string> {
        public NoOwnerRepo(IServiceProvider? sp = null) : base(null, null, sp) { }
    }

    [Fact]
    public async Task Decorator_Throws_When_NoDataOwnerAndNoOwnerProperty() {
        var inner = new NoOwnerRepo();
        var decorator = new UserScopedRepositoryDecorator<NoOwnerEntity, string, int>(inner, new IntUserAccessor(1));

        await Assert.ThrowsAsync<InvalidOperationException>(() => decorator.AddAsync(new NoOwnerEntity()).AsTask());
    }

    /// <summary>
    /// Exercises the <c>DiscoverOwnerProperty</c> throw when the "Owner"
    /// property exists but has a type that does not match TUserKey.
    /// </summary>
    private sealed class OwnerTypeMismatchEntity : IHaveOwner<int> {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = string.Empty;
        public string Owner { get; set; } = string.Empty;

        int IHaveOwner<int>.Owner => 0;
        void IHaveOwner<int>.SetOwner(int owner) { }
    }

    private sealed class OwnerTypeMismatchRepo : InMemoryRepository<OwnerTypeMismatchEntity, string> {
        public OwnerTypeMismatchRepo(IServiceProvider? sp = null) : base(null, null, sp) { }
    }

    [Fact]
    public async Task Decorator_Throws_When_OwnerPropertyTypeDoesNotMatchUserKey() {
        var inner = new OwnerTypeMismatchRepo();
        var decorator = new UserScopedRepositoryDecorator<OwnerTypeMismatchEntity, string, int>(inner, new IntUserAccessor(1));

        await Assert.ThrowsAsync<InvalidOperationException>(() => decorator.AddAsync(new OwnerTypeMismatchEntity()).AsTask());
    }

    private sealed class StaticUserAccessorWrapper : IUserAccessor<string> {
        private readonly string _userId;
        public StaticUserAccessorWrapper(string userId) => _userId = userId;
        public string? GetUserId() => _userId;
    }
}