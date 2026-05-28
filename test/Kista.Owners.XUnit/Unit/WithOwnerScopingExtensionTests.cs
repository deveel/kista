using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;

namespace Kista.Owners.XUnit.Unit;

[Trait("Category", "Unit")]
[Trait("Layer", "Domain")]
[Trait("Feature", "OwnerScoping")]
public class WithOwnerScopingExtensionTests
{
    [Fact]
    public void Should_RegisterDecorator_When_WithOwnerScopingIsCalled()
    {
        var services = new ServiceCollection();
        services.AddRepositoryContext()
            .AddRepository<TestUserRepository>(repo => repo
                .WithOwnerScoping(),
                ServiceLifetime.Singleton);

        RegisterStaticUserAccessor(services, "test");
        var sp = services.BuildServiceProvider();
        var repo = sp.GetRequiredService<IRepository<TestUserEntity, Guid>>();

        Assert.IsType<UserScopedRepositoryDecorator<TestUserEntity, Guid, string>>(repo);
    }

    [Fact]
    public void Should_RegisterOptions_When_ConfigureIsProvided()
    {
        var services = new ServiceCollection();
        services.AddRepositoryContext()
            .AddRepository<TestUserRepository>(repo => repo
                .WithOwnerScoping(opts =>
                {
                    opts.ThrowWhenUserNotSet = true;
                    opts.OwnerPropertyName = "CustomOwner";
                }),
                ServiceLifetime.Singleton);

        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<UserScopingOptions>();

        Assert.True(options.ThrowWhenUserNotSet);
        Assert.Equal("CustomOwner", options.OwnerPropertyName);
    }

    [Fact]
    public void Should_Throw_When_EntityDoesNotImplementIHaveOwner()
    {
        var services = new ServiceCollection();
        services.AddRepositoryContext();

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            services.AddRepositoryContext()
                .AddRepository<BadOwnerRepository>(repo => repo
                    .WithOwnerScoping(),
                    ServiceLifetime.Singleton);
        });
        Assert.Contains("does not implement IHaveOwner", ex.Message);
    }

    [Fact]
    public void Should_ReturnRepositoryBuilder_When_ChainedAfterAddRepository()
    {
        var services = new ServiceCollection();
        var ctx = services.AddRepositoryContext();
        var repo = ctx.AddRepository<TestUserRepository>(ServiceLifetime.Singleton);

        Assert.IsType<RepositoryBuilder>(repo);
        Assert.Equal(typeof(TestUserEntity), repo.EntityType);
        Assert.Equal(typeof(Guid), repo.EntityKeyType);
        Assert.Equal(typeof(TestUserRepository), repo.RepositoryType);

        repo.WithOwnerScoping();

        RegisterStaticUserAccessor(services, "test");
        var sp = services.BuildServiceProvider();
        var resolved = sp.GetRequiredService<IRepository<TestUserEntity, Guid>>();
        Assert.IsType<UserScopedRepositoryDecorator<TestUserEntity, Guid, string>>(resolved);
    }

    // ============================================================
    // Entity & Repo for tests
    // ============================================================

    public class TestUserEntity : IHaveOwner<string>
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;

        string IHaveOwner<string>.Owner => Name;
        void IHaveOwner<string>.SetOwner(string owner) => Name = owner;
    }

    public class TestUserRepository : InMemoryRepository<TestUserEntity, Guid>
    {
        public TestUserRepository(IServiceProvider sp) : base(null, null, sp) { }
    }

    public class BadOwnerEntity
    {
        [Key]
        public Guid Id { get; set; }
    }

    public class BadOwnerRepository : InMemoryRepository<BadOwnerEntity, Guid>
    {
        public BadOwnerRepository(IServiceProvider sp) : base(null, null, sp) { }
    }

    private static void RegisterStaticUserAccessor(IServiceCollection services, string userId)
    {
        var strategy = new StaticUserIdentifierStrategy<string>(userId);
        var composite = new CompositeUserIdentifierStrategy<string>();
        composite.Add(strategy);
        services.AddSingleton(composite);
        services.AddSingleton<IUserAccessor<string>>(
            sp => new StrategyBasedUserAccessor<string>(composite, sp));
    }
}
