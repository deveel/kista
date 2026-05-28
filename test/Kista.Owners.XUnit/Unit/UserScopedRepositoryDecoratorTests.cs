using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Kista;
using Microsoft.Extensions.DependencyInjection;

namespace Kista.Owners.XUnit.Unit;

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
[Trait("Feature", "OwnerScoping")]
public class UserScopedRepositoryDecoratorTests
{
    #region Property Discovery — DataOwnerAttribute

    [Fact]
    public void Should_DiscoverDataOwnerProperty_When_PropertyHasDataOwnerAttribute()
    {
        var services = CreateDefaultServices("alice");
        var repo = services.GetRequiredService<IRepository<NoteEntity, Guid>>();

        var entity = new NoteEntity { Title = "Test" };
        repo.AddAsync(entity).GetAwaiter().GetResult();

        Assert.Equal("alice", entity.OwnerId);
    }

    [Fact]
    public void Should_DiscoverOwnerProperty_When_NoDataOwnerAttributeAndPropertyNamedOwner()
    {
        var services = new ServiceCollection();
        services.AddRepositoryContext()
            .AddRepository<TestTaskEntityRepository>(repo => repo
                .WithOwnerScoping(), ServiceLifetime.Singleton);

        var strategy = new StaticUserIdentifierStrategy<string>("bob");
        var composite = new CompositeUserIdentifierStrategy<string>();
        composite.Add(strategy);
        services.AddSingleton(composite);
        services.AddSingleton<IUserAccessor<string>>(
            sp => new StrategyBasedUserAccessor<string>(composite, sp));

        var sp = services.BuildServiceProvider();
        var repo = sp.GetRequiredService<IRepository<TaskEntity, Guid>>();

        var entity = new TaskEntity { Title = "Test" };
        repo.AddAsync(entity).GetAwaiter().GetResult();

        Assert.Equal("bob", entity.Owner);
    }

    [Fact]
    public void Should_Throw_When_EntityHasNoDataOwnerAndNoOwnerProperty()
    {
        var services = new ServiceCollection();
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            services.AddRepositoryContext()
                .AddRepository<TestBadEntityRepository>(repo => repo
                    .WithOwnerScoping(), ServiceLifetime.Singleton);
        });
        Assert.Contains("does not implement IHaveOwner", ex.Message);
    }

    #endregion

    #region Owner Assignment — Add

    [Fact]
    public void Should_AssignOwner_When_EntityIsAdded()
    {
        var services = CreateDefaultServices("alice");
        var repo = services.GetRequiredService<IRepository<NoteEntity, Guid>>();

        var entity = new NoteEntity { Title = "My Note" };
        repo.AddAsync(entity).GetAwaiter().GetResult();

        Assert.Equal("alice", entity.OwnerId);
    }

    [Fact]
    public void Should_AssignOwnerToAll_When_RangeIsAdded()
    {
        var services = CreateDefaultServices("alice");
        var repo = services.GetRequiredService<IRepository<NoteEntity, Guid>>();

        var entities = new[]
        {
            new NoteEntity { Title = "A" },
            new NoteEntity { Title = "B" }
        };
        repo.AddRangeAsync(entities).GetAwaiter().GetResult();

        Assert.All(entities, e => Assert.Equal("alice", e.OwnerId));
    }

    [Fact]
    public void Should_SkipOwnerAssignment_When_NoUserAndNotThrowing()
    {
        var services = CreateServices<NoteEntity, Guid, string>(null, false);
        var repo = services.GetRequiredService<IRepository<NoteEntity, Guid>>();

        var entity = new NoteEntity { Title = "Test" };
        repo.AddAsync(entity).GetAwaiter().GetResult();

        Assert.Null(entity.OwnerId);
    }

    #endregion

    #region Owner Filtering — Find

    [Fact]
    public void Should_ReturnEntity_When_OwnerMatches()
    {
        var services = CreateDefaultServices("alice");
        var repo = services.GetRequiredService<IRepository<NoteEntity, Guid>>();

        var entity = new NoteEntity { Title = "Alice's Note" };
        repo.AddAsync(entity).GetAwaiter().GetResult();

        var found = repo.FindAsync(entity.Id).GetAwaiter().GetResult();
        Assert.NotNull(found);
        Assert.Equal("Alice's Note", found.Title);
    }

    [Fact]
    public void Should_ReturnNull_When_OwnerDoesNotMatch()
    {
        var aliceServices = CreateDefaultServices("alice");
        var aliceRepo = aliceServices.GetRequiredService<IRepository<NoteEntity, Guid>>();

        var entity = new NoteEntity { Title = "Alice's Note" };
        aliceRepo.AddAsync(entity).GetAwaiter().GetResult();

        var bobServices = CreateDefaultServices("bob");
        var bobRepo = bobServices.GetRequiredService<IRepository<NoteEntity, Guid>>();

        var found = bobRepo.FindAsync(entity.Id).GetAwaiter().GetResult();
        Assert.Null(found);
    }

    [Fact]
    public void Should_ReturnNull_When_EntityNotFound()
    {
        var services = CreateDefaultServices("alice");
        var repo = services.GetRequiredService<IRepository<NoteEntity, Guid>>();

        var found = repo.FindAsync(Guid.NewGuid()).GetAwaiter().GetResult();
        Assert.Null(found);
    }

    #endregion

    #region Owner Filtering — FindAll

    [Fact]
    public void Should_ReturnOnlyOwnedEntities_When_FindAllIsCalled()
    {
        var aliceServices = CreateDefaultServices("alice");
        var aliceRepo = aliceServices.GetRequiredService<IRepository<NoteEntity, Guid>>();

        aliceRepo.AddAsync(new NoteEntity { Title = "Alice's" }).GetAwaiter().GetResult();

        var bobServices = CreateDefaultServices("bob");
        var bobRepo = bobServices.GetRequiredService<IRepository<NoteEntity, Guid>>();

        bobRepo.AddAsync(new NoteEntity { Title = "Bob's" }).GetAwaiter().GetResult();

        var aliceNotes = aliceRepo.FindAllAsync().GetAwaiter().GetResult();
        Assert.Single(aliceNotes);
        Assert.Equal("Alice's", aliceNotes[0].Title);
    }

    [Fact]
    public void Should_CombineOwnerFilter_When_CustomFilterIsProvided()
    {
        var services = CreateDefaultServices("alice");
        var repo = services.GetRequiredService<IRepository<NoteEntity, Guid>>();

        repo.AddAsync(new NoteEntity { Title = "Alpha" }).GetAwaiter().GetResult();
        repo.AddAsync(new NoteEntity { Title = "Beta" }).GetAwaiter().GetResult();

        var filter = new ExpressionQueryFilter<NoteEntity>(x => x.Title == "Alpha");
        var result = repo.FindAllAsync(filter).GetAwaiter().GetResult();

        Assert.Single(result);
        Assert.Equal("Alpha", result[0].Title);
    }

    [Fact]
    public void Should_ReturnEmpty_When_NoUserAndNotThrowing()
    {
        var services = CreateServices<NoteEntity, Guid, string>(null, false);
        var repo = services.GetRequiredService<IRepository<NoteEntity, Guid>>();

        var result = repo.FindAllAsync().GetAwaiter().GetResult();
        Assert.Empty(result);
    }

    #endregion

    #region Owner Filtering — Exists / Count

    [Fact]
    public void Should_CountOnlyOwnedEntities()
    {
        var aliceServices = CreateDefaultServices("alice");
        var aliceRepo = aliceServices.GetRequiredService<IRepository<NoteEntity, Guid>>();

        aliceRepo.AddAsync(new NoteEntity { Title = "A" }).GetAwaiter().GetResult();

        var bobServices = CreateDefaultServices("bob");
        var bobRepo = bobServices.GetRequiredService<IRepository<NoteEntity, Guid>>();

        bobRepo.AddAsync(new NoteEntity { Title = "B" }).GetAwaiter().GetResult();

        var count = aliceRepo.CountAllAsync().GetAwaiter().GetResult();
        Assert.Equal(1, count);
    }

    [Fact]
    public void Should_ReturnTrueForExists_When_OwnedEntityMatches()
    {
        var services = CreateDefaultServices("alice");
        var repo = services.GetRequiredService<IRepository<NoteEntity, Guid>>();

        repo.AddAsync(new NoteEntity { Title = "Test" }).GetAwaiter().GetResult();

        var exists = repo.AsFilterable().ExistsAsync(
            new ExpressionQueryFilter<NoteEntity>(x => x.Title == "Test")).GetAwaiter().GetResult();
        Assert.True(exists);
    }

    [Fact]
    public void Should_ReturnFalseForExists_When_OnlyOtherOwnersEntityMatches()
    {
        var aliceServices = CreateDefaultServices("alice");
        var aliceRepo = aliceServices.GetRequiredService<IRepository<NoteEntity, Guid>>();

        aliceRepo.AddAsync(new NoteEntity { Title = "Shared" }).GetAwaiter().GetResult();

        var bobServices = CreateDefaultServices("bob");
        var bobRepo = bobServices.GetRequiredService<IRepository<NoteEntity, Guid>>();

        var exists = bobRepo.AsFilterable().ExistsAsync(
            new ExpressionQueryFilter<NoteEntity>(x => x.Title == "Shared")).GetAwaiter().GetResult();
        Assert.False(exists);
    }

    #endregion

    #region Pageable

    [Fact]
    public void Should_FilterPageByOwner()
    {
        var services = CreateDefaultServices("alice");
        var repo = services.GetRequiredService<IRepository<NoteEntity, Guid>>();

        for (int i = 0; i < 5; i++)
            repo.AddAsync(new NoteEntity { Title = $"Item {i}" }).GetAwaiter().GetResult();

        var pageable = Assert.IsAssignableFrom<IPageableRepository<NoteEntity, Guid>>(repo);
        var page = pageable.GetPageAsync(new PageQuery<NoteEntity>(1, 10)).GetAwaiter().GetResult();
        Assert.Equal(5, page.TotalItems);
        Assert.Equal(5, page.Items.Count);
    }

    #endregion

    #region Update / Remove

    [Fact]
    public void Should_UpdateEntity_When_AlreadyOwned()
    {
        var services = CreateDefaultServices("alice");
        var repo = services.GetRequiredService<IRepository<NoteEntity, Guid>>();

        var entity = new NoteEntity { Title = "Original" };
        repo.AddAsync(entity).GetAwaiter().GetResult();

        entity.Title = "Updated";
        repo.UpdateAsync(entity).GetAwaiter().GetResult();

        var found = repo.FindAsync(entity.Id).GetAwaiter().GetResult();
        Assert.NotNull(found);
        Assert.Equal("Updated", found.Title);
    }

    [Fact]
    public void Should_RemoveEntity_When_Owned()
    {
        var services = CreateDefaultServices("alice");
        var repo = services.GetRequiredService<IRepository<NoteEntity, Guid>>();

        var entity = new NoteEntity { Title = "To Delete" };
        repo.AddAsync(entity).GetAwaiter().GetResult();
        repo.RemoveAsync(entity).GetAwaiter().GetResult();

        var found = repo.FindAsync(entity.Id).GetAwaiter().GetResult();
        Assert.Null(found);
    }

    #endregion

    #region Options — ThrowWhenUserNotSet

    [Fact]
    public void Should_Throw_When_ThrowWhenUserNotSetAndNoUser()
    {
        var services = CreateServices<NoteEntity, Guid, string>(null, true);

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            var repo = services.GetRequiredService<IRepository<NoteEntity, Guid>>();
            repo.FindAllAsync().GetAwaiter().GetResult();
        });
        Assert.Contains("User context is not set", ex.Message);
    }

    #endregion

    #region StaticUserIdentifierStrategy

    [Fact]
    public void Should_WorkWith_StaticUserIdentifierStrategy()
    {
        var services = new ServiceCollection();
        services.AddRepositoryContext()
            .AddRepository<NoteRepository>(repo => repo
                .WithOwnerScoping(), ServiceLifetime.Singleton);

        var strategy = new StaticUserIdentifierStrategy<string>("static-user");
        var composite = new CompositeUserIdentifierStrategy<string>();
        composite.Add(strategy);
        services.AddSingleton(composite);
        services.AddSingleton<IUserAccessor<string>>(
            sp => new StrategyBasedUserAccessor<string>(composite, sp));

        var sp = services.BuildServiceProvider();
        var repo = sp.GetRequiredService<IRepository<NoteEntity, Guid>>();

        var entity = new NoteEntity { Title = "Static" };
        repo.AddAsync(entity).GetAwaiter().GetResult();

        Assert.Equal("static-user", entity.OwnerId);
    }

    #endregion

    // ============================================================
    // Helpers
    // ============================================================

    private static ServiceProvider CreateDefaultServices(string userId)
        => CreateServices<NoteEntity, Guid, string>(userId, false);

    private static ServiceProvider CreateServices<TEntity, TKey, TUserKey>(
        TUserKey? userId, bool throwWhenNotSet)
        where TEntity : class, IHaveOwner<TUserKey>, new()
        where TKey : notnull
    {
        var services = new ServiceCollection();
        services.AddRepositoryContext()
            .AddRepository<TestInMemoryRepository<TEntity, TKey>>(repo => repo
                .WithOwnerScoping(opts =>
                {
                    opts.ThrowWhenUserNotSet = throwWhenNotSet;
                }), ServiceLifetime.Singleton);

        var strategy = new StaticUserIdentifierStrategy<TUserKey>(userId);
        var composite = new CompositeUserIdentifierStrategy<TUserKey>();
        if (!EqualityComparer<TUserKey>.Default.Equals(userId, default))
            composite.Add(strategy);
        services.AddSingleton(composite);
        services.AddSingleton<IUserAccessor<TUserKey>>(
            sp => new StrategyBasedUserAccessor<TUserKey>(composite, sp));

        return services.BuildServiceProvider();
    }

    // ============================================================
    // Entity types
    // ============================================================

    public class NoteEntity : IHaveOwner<string>
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;

        [DataOwner]
        public string? OwnerId { get; set; }

        string IHaveOwner<string>.Owner => OwnerId!;
        void IHaveOwner<string>.SetOwner(string owner) => OwnerId = owner;
    }

    public class TaskEntity : IHaveOwner<string>
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Owner { get; set; } = string.Empty;

        string IHaveOwner<string>.Owner => Owner;
        void IHaveOwner<string>.SetOwner(string owner) => Owner = owner;
    }

    public class BadEntity
    {
        [Key]
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class NoteRepository : InMemoryRepository<NoteEntity, Guid>
    {
        public NoteRepository(IServiceProvider sp) : base(null, null, sp) { }
    }

    public class TestInMemoryRepository<TEntity, TKey> : InMemoryRepository<TEntity, TKey>
        where TEntity : class
        where TKey : notnull
    {
        public TestInMemoryRepository(IServiceProvider sp) : base(null, null, sp) { }
    }

    public class TestBadEntityRepository : InMemoryRepository<BadEntity, Guid>
    {
        public TestBadEntityRepository(IServiceProvider sp) : base(null, null, sp) { }
    }

    public class TestTaskEntityRepository : InMemoryRepository<TaskEntity, Guid>
    {
        public TestTaskEntityRepository(IServiceProvider sp) : base(null, null, sp) { }
    }
}
