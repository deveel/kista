using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;

namespace Kista.Owners.XUnit.Unit;

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
[Trait("Feature", "OwnerScoping")]
public class UserScopedRepositoryDecoratorEdgeTests
{
    #region Constructor Validation

    [Fact]
    public void Should_Throw_When_InnerRepositoryIsNull()
    {
        var accessor = new StaticUserAccessor("user");
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new UserScopedRepositoryDecorator<StringKeyEntity, string, string>(null!, accessor));
        Assert.Contains("inner", ex.Message);
    }

    [Fact]
    public void Should_Throw_When_UserAccessorIsNull()
    {
        var repo = new InMemoryRepository<StringKeyEntity, string>(null, null, null);
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new UserScopedRepositoryDecorator<StringKeyEntity, string, string>(repo, null!));
        Assert.Contains("userAccessor", ex.Message);
    }

    #endregion

    #region FindFirstAsync

    [Fact]
    public void Should_FindFirstOwnedEntity()
    {
        var services = CreateDefaultServices("user1");
        var repo = services.GetRequiredService<IRepository<StringKeyEntity, string>>();

        repo.AddRangeAsync(new[]
        {
            new StringKeyEntity { Name = "A" },
            new StringKeyEntity { Name = "B" }
        }).GetAwaiter().GetResult();

        var filter = new ExpressionQueryFilter<StringKeyEntity>(x => x.Name == "A");
        var first = repo.FindFirstAsync(filter).GetAwaiter().GetResult();
        Assert.NotNull(first);
        Assert.Equal("A", first!.Name);
    }

    [Fact]
    public void Should_ReturnNull_When_FindFirstNoMatch()
    {
        var services = CreateDefaultServices("user1");
        var repo = services.GetRequiredService<IRepository<StringKeyEntity, string>>();

        var filter = new ExpressionQueryFilter<StringKeyEntity>(x => x.Name == "Nope");
        var first = repo.FindFirstAsync(filter).GetAwaiter().GetResult();
        Assert.Null(first);
    }

    [Fact]
    public void Should_FilterByOwner_When_FindFirstWithFilter()
    {
        var alice = CreateDefaultServices("alice");
        var aliceRepo = alice.GetRequiredService<IRepository<StringKeyEntity, string>>();

        aliceRepo.AddAsync(new StringKeyEntity { Name = "Alice's" }).GetAwaiter().GetResult();

        var bob = CreateDefaultServices("bob");
        var bobRepo = bob.GetRequiredService<IRepository<StringKeyEntity, string>>();

        bobRepo.AddAsync(new StringKeyEntity { Name = "Bob's" }).GetAwaiter().GetResult();

        var filter = new ExpressionQueryFilter<StringKeyEntity>(x => x.Name == "Alice's");
        var aliceFirst = aliceRepo.FindFirstAsync(filter).GetAwaiter().GetResult();
        Assert.NotNull(aliceFirst);
        Assert.Equal("alice", aliceFirst.OwnerId);

        var bobFilter = new ExpressionQueryFilter<StringKeyEntity>(x => x.Name == "Bob's");
        var bobFirst = bobRepo.FindFirstAsync(bobFilter).GetAwaiter().GetResult();
        Assert.NotNull(bobFirst);
        Assert.Equal("bob", bobFirst.OwnerId);
    }

    #endregion

    #region Composite Strategy — Multiple Strategies

    [Fact]
    public void Should_UseFirstNonNullStrategy_When_CompositeHasMultiple()
    {
        var services = new ServiceCollection();
        services.AddRepositoryContext()
            .AddRepository<StringKeyRepo>(repo => repo
                .WithOwnerScoping(), ServiceLifetime.Singleton);

        var composite = new CompositeUserIdentifierStrategy<string>();
        composite.Add(new StaticUserIdentifierStrategy<string>(null!));
        composite.Add(new StaticUserIdentifierStrategy<string>("second"));
        services.AddSingleton(composite);
        services.AddSingleton<IUserAccessor<string>>(
            sp => new StrategyBasedUserAccessor<string>(composite, sp));

        var sp = services.BuildServiceProvider();
        var repo = sp.GetRequiredService<IRepository<StringKeyEntity, string>>();

        var entity = new StringKeyEntity { Name = "Test" };
        repo.AddAsync(entity).GetAwaiter().GetResult();

        Assert.Equal("second", entity.OwnerId);
    }

    [Fact]
    public void Should_ReturnNull_When_AllStrategiesReturnNull()
    {
        var services = new ServiceCollection();
        services.AddRepositoryContext()
            .AddRepository<StringKeyRepo>(repo => repo
                .WithOwnerScoping(opts => opts.ThrowWhenUserNotSet = false),
                ServiceLifetime.Singleton);

        var composite = new CompositeUserIdentifierStrategy<string>();
        services.AddSingleton(composite);
        services.AddSingleton<IUserAccessor<string>>(
            sp => new StrategyBasedUserAccessor<string>(composite, sp));

        var sp = services.BuildServiceProvider();
        var repo = sp.GetRequiredService<IRepository<StringKeyEntity, string>>();

        var entity = new StringKeyEntity { Name = "Test" };
        repo.AddAsync(entity).GetAwaiter().GetResult();

        Assert.Null(entity.OwnerId);
    }

    #endregion

    #region Int Key Entity

    [Fact]
    public void Should_WorkWith_IntKeyEntity()
    {
        var services = new ServiceCollection();
        services.AddRepositoryContext()
            .AddRepository<IntKeyRepo>(repo => repo
                .WithOwnerScoping(), ServiceLifetime.Singleton);

        var strategy = new StaticUserIdentifierStrategy<string>("alice");
        var composite = new CompositeUserIdentifierStrategy<string>();
        composite.Add(strategy);
        services.AddSingleton(composite);
        services.AddSingleton<IUserAccessor<string>>(
            sp => new StrategyBasedUserAccessor<string>(composite, sp));

        var sp = services.BuildServiceProvider();
        var repo = sp.GetRequiredService<IRepository<IntKeyEntity, int>>();

        var entity = new IntKeyEntity { Name = "Test" };
        repo.AddAsync(entity).GetAwaiter().GetResult();
        Assert.Equal("alice", entity.OwnerId);

        var found = repo.FindAsync(entity.Id).GetAwaiter().GetResult();
        Assert.NotNull(found);
    }

    #endregion

    #region Direct Decorator Usage

    [Fact]
    public void Should_Work_When_DecoratorUsedDirectly()
    {
        var inner = new InMemoryRepository<StringKeyEntity, string>(null, null, null);
        var accessor = new StaticUserAccessor("direct-user");
        var decorator = new UserScopedRepositoryDecorator<StringKeyEntity, string, string>(inner, accessor);

        var entity = new StringKeyEntity { Name = "Direct" };
        decorator.AddAsync(entity).GetAwaiter().GetResult();
        Assert.Equal("direct-user", entity.OwnerId);

        var found = decorator.FindAsync(entity.Id).GetAwaiter().GetResult();
        Assert.NotNull(found);
    }

    [Fact]
    public void Should_FilterByOwner_When_DecoratorUsedDirectly()
    {
        var aliceInner = new InMemoryRepository<StringKeyEntity, string>(null, null, null);
        var aliceDecorator = new UserScopedRepositoryDecorator<StringKeyEntity, string, string>(
            aliceInner, new StaticUserAccessor("alice"));

        var bobInner = new InMemoryRepository<StringKeyEntity, string>(null, null, null);
        var bobDecorator = new UserScopedRepositoryDecorator<StringKeyEntity, string, string>(
            bobInner, new StaticUserAccessor("bob"));

        aliceDecorator.AddAsync(new StringKeyEntity { Name = "Alice's" }).GetAwaiter().GetResult();
        bobDecorator.AddAsync(new StringKeyEntity { Name = "Bob's" }).GetAwaiter().GetResult();

        var aliceItems = aliceDecorator.FindAllAsync().GetAwaiter().GetResult();
        Assert.Single(aliceItems);
        Assert.Equal("Alice's", aliceItems[0].Name);

        var bobItems = bobDecorator.FindAllAsync().GetAwaiter().GetResult();
        Assert.Single(bobItems);
        Assert.Equal("Bob's", bobItems[0].Name);
    }

    #endregion

    #region AddRange — NotFound Cases

    [Fact]
    public void Should_NotAddRange_When_ThrowWhenUserNotSet()
    {
        var services = new ServiceCollection();
        services.AddRepositoryContext()
            .AddRepository<StringKeyRepo>(repo => repo
                .WithOwnerScoping(opts => opts.ThrowWhenUserNotSet = true),
                ServiceLifetime.Singleton);

        var composite = new CompositeUserIdentifierStrategy<string>();
        services.AddSingleton(composite);
        services.AddSingleton<IUserAccessor<string>>(
            sp => new StrategyBasedUserAccessor<string>(composite, sp));

        var sp = services.BuildServiceProvider();
        var repo = sp.GetRequiredService<IRepository<StringKeyEntity, string>>();

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            repo.AddRangeAsync(new[]
            {
                new StringKeyEntity { Name = "A" },
                new StringKeyEntity { Name = "B" }
            }).GetAwaiter().GetResult();
        });
        Assert.Contains("User context is not set", ex.Message);
    }

    #endregion

    #region FindAll — Ensure No Leak When No User

    [Fact]
    public void Should_ReturnEmpty_When_FindAllAndNoUser()
    {
        var services = new ServiceCollection();
        services.AddRepositoryContext()
            .AddRepository<StringKeyRepo>(repo => repo
                .WithOwnerScoping(opts => opts.ThrowWhenUserNotSet = false),
                ServiceLifetime.Singleton);

        var composite = new CompositeUserIdentifierStrategy<string>();
        services.AddSingleton(composite);
        services.AddSingleton<IUserAccessor<string>>(
            sp => new StrategyBasedUserAccessor<string>(composite, sp));

        var sp = services.BuildServiceProvider();
        var repo = sp.GetRequiredService<IRepository<StringKeyEntity, string>>();

        var result = repo.FindAllAsync().GetAwaiter().GetResult();
        Assert.Empty(result);
    }

    #endregion

    // ============================================================
    // Helpers
    // ============================================================

    private static ServiceProvider CreateDefaultServices(string userId)
        => CreateServices<StringKeyEntity, string>(userId);

    private static ServiceProvider CreateServices<TEntity, TKey>(string? userId)
        where TEntity : class, IHaveOwner<string>, new()
        where TKey : notnull
    {
        var services = new ServiceCollection();
        services.AddRepositoryContext()
            .AddRepository<GenericInMemoryRepo<TEntity, TKey>>(repo => repo
                .WithOwnerScoping(), ServiceLifetime.Singleton);

        var strategy = new StaticUserIdentifierStrategy<string>(userId);
        var composite = new CompositeUserIdentifierStrategy<string>();
        if (userId != null)
            composite.Add(strategy);
        services.AddSingleton(composite);
        services.AddSingleton<IUserAccessor<string>>(
            sp => new StrategyBasedUserAccessor<string>(composite, sp));

        return services.BuildServiceProvider();
    }

    // ============================================================
    // Entity Types
    // ============================================================

    public class StringKeyEntity : IHaveOwner<string>
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = string.Empty;

        [DataOwner]
        public string? OwnerId { get; set; }

        string IHaveOwner<string>.Owner => OwnerId!;
        void IHaveOwner<string>.SetOwner(string owner) => OwnerId = owner;
    }

    public class IntKeyEntity : IHaveOwner<string>
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        [DataOwner]
        public string? OwnerId { get; set; }

        string IHaveOwner<string>.Owner => OwnerId!;
        void IHaveOwner<string>.SetOwner(string owner) => OwnerId = owner;
    }

    // ============================================================
    // Repository Types
    // ============================================================

    public class StringKeyRepo : InMemoryRepository<StringKeyEntity, string>
    {
        public StringKeyRepo(IServiceProvider sp) : base(null, null, sp) { }
    }

    public class IntKeyRepo : InMemoryRepository<IntKeyEntity, int>
    {
        public IntKeyRepo(IServiceProvider sp) : base(null, null, sp) { }
    }

    public class GenericInMemoryRepo<TEntity, TKey> : InMemoryRepository<TEntity, TKey>
        where TEntity : class
        where TKey : notnull
    {
        public GenericInMemoryRepo(IServiceProvider sp) : base(null, null, sp) { }
    }

    // ============================================================
    // Helpers
    // ============================================================

    private sealed class StaticUserAccessor : IUserAccessor<string>
    {
        private readonly string _userId;
        public StaticUserAccessor(string userId) => _userId = userId;
        public string? GetUserId() => _userId;
    }
}
