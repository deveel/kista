using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;

namespace Kista.Owners.XUnit.Unit;

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
[Trait("Feature", "Security")]
public class UserScopedRepositorySecurityTests {
    private const string AliceUserId = "alice";
    private const string OwnershipViolationSnippet = "does not belong";

    #region C1 — Secure default chain (claim-only)

    [Fact]
    public void Should_RegisterOnlyClaimStrategy_When_AddHttpUserAccessorDefaultCalled() {
        // Arrange — the default AddHttpUserAccessor must register only the claim strategy,
        // not the query-string or route fallbacks (C1 fix).
        var services = new ServiceCollection();
        services.AddHttpUserAccessor<string>();
        var provider = services.BuildServiceProvider();

        // Act — resolve the composite strategy and inspect its chain.
        var composite = provider.GetRequiredService<CompositeUserIdentifierStrategy<string>>();

        // Assert — only one strategy (claim) is registered by default.
        Assert.Single(composite.Strategies);
        Assert.IsType<ClaimUserIdentifierStrategy<string>>(composite.Strategies[0]);
    }

    [Fact]
    public void Should_AllowOptInQueryStringFallback_When_ExplicitlyConfigured() {
        // Arrange — a consumer who understands the risk can opt back into the query-string fallback.
        var services = new ServiceCollection();
        services.AddHttpUserAccessor<string>(b => b.AddClaim().AddQueryString("user_id"));
        var provider = services.BuildServiceProvider();

        // Act
        var composite = provider.GetRequiredService<CompositeUserIdentifierStrategy<string>>();

        // Assert — two strategies: claim + query-string.
        Assert.Equal(2, composite.Strategies.Count);
        Assert.IsType<ClaimUserIdentifierStrategy<string>>(composite.Strategies[0]);
        Assert.IsType<QueryStringUserIdentifierStrategy<string>>(composite.Strategies[1]);
    }

    #endregion

    #region C2 — Write-path ownership verification

    [Fact]
    public async Task Should_ThrowOnUpdate_When_EntityOwnedByAnotherUser() {
        // Arrange — alice creates an entity, then bob tries to update it.
        var aliceServices = CreateDefaultServices(AliceUserId);
        var aliceRepo = aliceServices.GetRequiredService<IRepository<SecNoteEntity, Guid>>();

        var entity = new SecNoteEntity { Title = "Alice's Note" };
        await aliceRepo.AddAsync(entity);

        var bobServices = CreateDefaultServices("bob");
        var bobRepo = bobServices.GetRequiredService<IRepository<SecNoteEntity, Guid>>();

        // Act + Assert — bob must not be able to update alice's entity.
        entity.Title = "Hacked by Bob";
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            bobRepo.UpdateAsync(entity).AsTask());
        Assert.Contains(OwnershipViolationSnippet, ex.Message);
    }

    [Fact]
    public async Task Should_ThrowOnRemove_When_EntityOwnedByAnotherUser() {
        // Arrange — alice creates an entity, then bob tries to delete it.
        var aliceServices = CreateDefaultServices(AliceUserId);
        var aliceRepo = aliceServices.GetRequiredService<IRepository<SecNoteEntity, Guid>>();

        var entity = new SecNoteEntity { Title = "Alice's Note" };
        await aliceRepo.AddAsync(entity);

        var bobServices = CreateDefaultServices("bob");
        var bobRepo = bobServices.GetRequiredService<IRepository<SecNoteEntity, Guid>>();

        // Act + Assert — bob must not be able to remove alice's entity.
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            bobRepo.RemoveAsync(entity).AsTask());
        Assert.Contains(OwnershipViolationSnippet, ex.Message);
    }

    [Fact]
    public async Task Should_ThrowOnRemoveRange_When_AnyEntityOwnedByAnotherUser() {
        // Arrange — alice owns two entities, bob owns one; bob tries to remove all three.
        var aliceServices = CreateDefaultServices(AliceUserId);
        var aliceRepo = aliceServices.GetRequiredService<IRepository<SecNoteEntity, Guid>>();

        var aliceEntity1 = new SecNoteEntity { Title = "Alice 1" };
        var aliceEntity2 = new SecNoteEntity { Title = "Alice 2" };
        await aliceRepo.AddAsync(aliceEntity1);
        await aliceRepo.AddAsync(aliceEntity2);

        var bobServices = CreateDefaultServices("bob");
        var bobRepo = bobServices.GetRequiredService<IRepository<SecNoteEntity, Guid>>();
        var bobEntity = new SecNoteEntity { Title = "Bob's" };
        await bobRepo.AddAsync(bobEntity);

        // Act + Assert — bob must not be able to remove a range containing alice's entities.
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            bobRepo.RemoveRangeAsync(new[] { bobEntity, aliceEntity1 }).AsTask());
        Assert.Contains(OwnershipViolationSnippet, ex.Message);
    }

    [Fact]
    public async Task Should_AllowUpdate_When_EntityOwnedByCurrentUser() {
        // Arrange — alice creates and then updates her own entity.
        var services = CreateDefaultServices(AliceUserId);
        var repo = services.GetRequiredService<IRepository<SecNoteEntity, Guid>>();

        var entity = new SecNoteEntity { Title = "Original" };
        await repo.AddAsync(entity);

        // Act
        entity.Title = "Updated";
        var result = await repo.UpdateAsync(entity);

        // Assert
        Assert.True(result);
        var found = await repo.FindAsync(entity.Id);
        Assert.NotNull(found);
        Assert.Equal("Updated", found.Title);
    }

    [Fact]
    public async Task Should_AllowRemove_When_EntityOwnedByCurrentUser() {
        // Arrange — alice creates and then deletes her own entity.
        var services = CreateDefaultServices(AliceUserId);
        var repo = services.GetRequiredService<IRepository<SecNoteEntity, Guid>>();

        var entity = new SecNoteEntity { Title = "To Delete" };
        await repo.AddAsync(entity);

        // Act
        var result = await repo.RemoveAsync(entity);

        // Assert
        Assert.True(result);
        var found = await repo.FindAsync(entity.Id);
        Assert.Null(found);
    }

    [Fact]
    public async Task Should_AllowRemoveRange_When_AllEntitiesOwnedByCurrentUser() {
        // Arrange — alice owns all entities in the range.
        var services = CreateDefaultServices(AliceUserId);
        var repo = services.GetRequiredService<IRepository<SecNoteEntity, Guid>>();

        var e1 = new SecNoteEntity { Title = "A" };
        var e2 = new SecNoteEntity { Title = "B" };
        await repo.AddAsync(e1);
        await repo.AddAsync(e2);

        // Act
        await repo.RemoveRangeAsync(new[] { e1, e2 });

        // Assert
        Assert.Null(await repo.FindAsync(e1.Id));
        Assert.Null(await repo.FindAsync(e2.Id));
    }

    [Fact]
    public async Task Should_ThrowOnUpdate_When_EntityNotFound() {
        // Arrange — an entity that was never persisted; the ownership check must reject it.
        var services = CreateDefaultServices(AliceUserId);
        var repo = services.GetRequiredService<IRepository<SecNoteEntity, Guid>>();

        var phantom = new SecNoteEntity { Title = "Phantom" };

        // Act + Assert — cannot verify ownership of a non-existent entity.
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            repo.UpdateAsync(phantom).AsTask());
        Assert.Contains(OwnershipViolationSnippet, ex.Message);
    }

    #endregion

    #region C3 — Fail-closed default

    [Fact]
    public void Should_DefaultThrowWhenUserNotSet_ToTrue() {
        // Arrange & Act
        var options = new UserScopingOptions();

        // Assert — the default must be fail-closed (true).
        Assert.True(options.ThrowWhenUserNotSet);
    }

    [Fact]
    public async Task Should_ThrowOnFind_When_NoUserAndDefaultOptions() {
        // Arrange — no user accessor strategy, default options (ThrowWhenUserNotSet = true).
        var repo = BuildNoUserServices();

        // Act + Assert — must throw, not return empty.
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.FindAsync(Guid.NewGuid()).AsTask());
        Assert.Contains("User context is not set", ex.Message);
    }

    [Fact]
    public async Task Should_ThrowOnUpdate_When_NoUserAndDefaultOptions() {
        // Arrange — no user, default options (fail-closed).
        var repo = BuildNoUserServices();

        // Act + Assert — update must throw, not silently proceed.
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.UpdateAsync(new SecNoteEntity { Title = "x" }).AsTask());
        Assert.Contains("User context is not set", ex.Message);
    }

    #endregion

    // ============================================================
    // Helpers
    // ============================================================

    private static ServiceProvider CreateDefaultServices(string userId) {
        var services = new ServiceCollection();
        services.AddRepositoryContext()
            .AddRepository<SecNoteRepository>(repo => repo
                .WithOwnerScoping(), ServiceLifetime.Singleton);

        var strategy = new StaticUserIdentifierStrategy<string>(userId);
        var composite = new CompositeUserIdentifierStrategy<string>();
        composite.Add(strategy);
        services.AddSingleton(composite);
        services.AddSingleton<IUserAccessor<string>>(
            sp => new StrategyBasedUserAccessor<string>(composite, sp));

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Builds a service provider with owner scoping enabled but NO user
    /// accessor strategies registered, so the composite returns null and the
    /// default <c>ThrowWhenUserNotSet</c> behaviour is exercised.
    /// </summary>
    private static IRepository<SecNoteEntity, Guid> BuildNoUserServices() {
        var services = new ServiceCollection();
        services.AddRepositoryContext()
            .AddRepository<SecNoteRepository>(repo => repo
                .WithOwnerScoping(), ServiceLifetime.Singleton);

        // No strategies registered — composite returns null.
        var composite = new CompositeUserIdentifierStrategy<string>();
        services.AddSingleton(composite);
        services.AddSingleton<IUserAccessor<string>>(
            sp => new StrategyBasedUserAccessor<string>(composite, sp));

        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IRepository<SecNoteEntity, Guid>>();
    }

    // ============================================================
    // Entity & Repo
    // ============================================================

    public class SecNoteEntity : IHaveOwner<string> {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;

        [DataOwner]
        public string? OwnerId { get; set; }

        string IHaveOwner<string>.Owner => OwnerId;
        void IHaveOwner<string>.SetOwner(string owner) => OwnerId = owner;
    }

    public class SecNoteRepository : InMemoryRepository<SecNoteEntity, Guid> {
        public SecNoteRepository(IServiceProvider sp) : base(null, null, sp) { }
    }
}