using Kista.Entities;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Kista;

/// <summary>
/// Tests for <see cref="EntityFrameworkRepositoryBuilder"/> configuration methods
/// (<see cref="EntityFrameworkRepositoryBuilder.WithLifetime"/>,
/// <see cref="EntityFrameworkRepositoryBuilder.WithLifecycle"/>,
/// <see cref="EntityFrameworkRepositoryBuilder.WithoutLifecycle"/>)
/// and their effect on service registration.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "DependencyInjection")]
public class EntityFrameworkRepositoryBuilderTests {
    private static bool IsSpatialiteAvailable() {
        try {
            using var conn = new SqliteConnection("Data Source=:memory:");
            conn.Open();
            conn.EnableExtensions();
            SpatialiteLoader.Load(conn);
            return true;
        } catch {
            return false;
        }
    }

    private static void ConfigurePersonDbContext(DbContextOptionsBuilder builder) {
        if (IsSpatialiteAvailable()) {
            builder.UseSqlite("Data Source=:memory:", x => x.UseNetTopologySuite());
        } else {
            builder.UseSqlite("Data Source=:memory:");
            builder.ReplaceService<IModelCustomizer, NonSpatialModelCustomizer>();
        }
    }

    #region WithLifetime

    [Fact]
    public void WithLifetime_Scoped_RegistersDbContextAsScoped() {
        var services = new ServiceCollection();

        services.AddRepositoryContext()
            .UseEntityFramework<PersonDbContext>(b => b
                .ConfigureDbContext(ConfigurePersonDbContext)
                .WithLifetime(ServiceLifetime.Scoped));

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(PersonDbContext));

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void WithLifetime_Transient_RegistersDbContextAsTransient() {
        var services = new ServiceCollection();

        services.AddRepositoryContext()
            .UseEntityFramework<PersonDbContext>(b => b
                .ConfigureDbContext(ConfigurePersonDbContext)
                .WithLifetime(ServiceLifetime.Transient));

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(PersonDbContext));

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
    }

    [Fact]
    public void WithLifetime_Singleton_RegistersDbContextAsSingleton() {
        var services = new ServiceCollection();

        services.AddRepositoryContext()
            .UseEntityFramework<PersonDbContext>(b => b
                .ConfigureDbContext(ConfigurePersonDbContext)
                .WithLifetime(ServiceLifetime.Singleton));

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(PersonDbContext));

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void DefaultLifetime_IsScoped() {
        var services = new ServiceCollection();

        services.AddRepositoryContext()
            .UseEntityFramework<PersonDbContext>(b => b
                .ConfigureDbContext(ConfigurePersonDbContext));

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(PersonDbContext));

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void WithLifetime_RegistersOpenGenericRepositories() {
        var services = new ServiceCollection();

        services.AddRepositoryContext()
            .UseEntityFramework<PersonDbContext>(b => b
                .ConfigureDbContext(ConfigurePersonDbContext)
                .WithLifetime(ServiceLifetime.Scoped));

        var singleParam = services.FirstOrDefault(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IRepository<>) &&
            d.ImplementationType.IsGenericType &&
            d.ImplementationType.GetGenericTypeDefinition() == typeof(EntityRepository<>));

        var twoParam = services.FirstOrDefault(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IRepository<,>) &&
            d.ImplementationType.IsGenericType &&
            d.ImplementationType.GetGenericTypeDefinition() == typeof(EntityRepository<,>));

        Assert.NotNull(singleParam);
        Assert.NotNull(twoParam);
    }

    [Fact]
    public void WithLifetime_AppliesSameLifetimeToOpenGenericRepositories() {
        var services = new ServiceCollection();

        services.AddRepositoryContext()
            .UseEntityFramework<PersonDbContext>(b => b
                .ConfigureDbContext(ConfigurePersonDbContext)
                .WithLifetime(ServiceLifetime.Singleton));

        var singleParam = services.FirstOrDefault(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IRepository<>) &&
            d.ImplementationType.IsGenericType &&
            d.ImplementationType.GetGenericTypeDefinition() == typeof(EntityRepository<>));

        var twoParam = services.FirstOrDefault(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IRepository<,>) &&
            d.ImplementationType.IsGenericType &&
            d.ImplementationType.GetGenericTypeDefinition() == typeof(EntityRepository<,>));

        Assert.NotNull(singleParam);
        Assert.Equal(ServiceLifetime.Singleton, singleParam.Lifetime);
        Assert.NotNull(twoParam);
        Assert.Equal(ServiceLifetime.Singleton, twoParam.Lifetime);
    }

    #endregion

    #region WithLifecycle

    [Fact]
    public void WithLifecycle_RegistersLifecycleHandler() {
        var services = new ServiceCollection();

        services.AddRepositoryContext()
            .UseEntityFramework<PersonDbContext>(b => b
                .ConfigureDbContext(ConfigurePersonDbContext)
                .WithLifecycle());

        var handlerDescriptor = services.FirstOrDefault(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IRepositoryLifecycleHandler<>) &&
            d.ImplementationType == typeof(EntityFrameworkRepositoryLifecycleHandler<>));

        Assert.NotNull(handlerDescriptor);
    }

    [Fact]
    public void WithLifecycle_HandlerIsTransient() {
        var services = new ServiceCollection();

        services.AddRepositoryContext()
            .UseEntityFramework<PersonDbContext>(b => b
                .ConfigureDbContext(ConfigurePersonDbContext)
                .WithLifecycle());

        var handlerDescriptor = services.FirstOrDefault(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IRepositoryLifecycleHandler<>) &&
            d.ImplementationType == typeof(EntityFrameworkRepositoryLifecycleHandler<>));

        Assert.NotNull(handlerDescriptor);
        Assert.Equal(ServiceLifetime.Transient, handlerDescriptor.Lifetime);
    }

    [Fact]
    public void Default_EnablesLifecycle() {
        var services = new ServiceCollection();

        services.AddRepositoryContext()
            .UseEntityFramework<PersonDbContext>(b => b
                .ConfigureDbContext(ConfigurePersonDbContext));

        var handlerDescriptor = services.FirstOrDefault(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IRepositoryLifecycleHandler<>) &&
            d.ImplementationType == typeof(EntityFrameworkRepositoryLifecycleHandler<>));

        Assert.NotNull(handlerDescriptor);
    }

    #endregion

    #region WithoutLifecycle

    [Fact]
    public void WithoutLifecycle_DoesNotRegisterHandler() {
        var services = new ServiceCollection();

        services.AddRepositoryContext()
            .UseEntityFramework<PersonDbContext>(b => b
                .ConfigureDbContext(ConfigurePersonDbContext)
                .WithoutLifecycle());

        var handlerDescriptor = services.FirstOrDefault(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IRepositoryLifecycleHandler<>));

        Assert.Null(handlerDescriptor);
    }

    [Fact]
    public void WithoutLifecycle_StillRegistersDbContext() {
        var services = new ServiceCollection();

        services.AddRepositoryContext()
            .UseEntityFramework<PersonDbContext>(b => b
                .ConfigureDbContext(ConfigurePersonDbContext)
                .WithoutLifecycle());

        var contextDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(PersonDbContext));

        Assert.NotNull(contextDescriptor);
    }

    [Fact]
    public void WithoutLifecycle_StillRegistersRepositories() {
        var services = new ServiceCollection();

        services.AddRepositoryContext()
            .UseEntityFramework<PersonDbContext>(b => b
                .ConfigureDbContext(ConfigurePersonDbContext)
                .WithoutLifecycle());

        var singleParam = services.FirstOrDefault(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IRepository<>) &&
            d.ImplementationType.IsGenericType &&
            d.ImplementationType.GetGenericTypeDefinition() == typeof(EntityRepository<>));

        Assert.NotNull(singleParam);
    }

    #endregion

    #region Combined Configuration

    [Fact]
    public void WithLifetime_Transient_WithLifecycle_RegistersAll() {
        var services = new ServiceCollection();

        services.AddRepositoryContext()
            .UseEntityFramework<PersonDbContext>(b => b
                .ConfigureDbContext(ConfigurePersonDbContext)
                .WithLifetime(ServiceLifetime.Transient)
                .WithLifecycle());

        var contextDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(PersonDbContext));
        var handlerDescriptor = services.FirstOrDefault(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IRepositoryLifecycleHandler<>) &&
            d.ImplementationType == typeof(EntityFrameworkRepositoryLifecycleHandler<>));
        var repoDescriptor = services.FirstOrDefault(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IRepository<>) &&
            d.ImplementationType.IsGenericType &&
            d.ImplementationType.GetGenericTypeDefinition() == typeof(EntityRepository<>));

        Assert.NotNull(contextDescriptor);
        Assert.Equal(ServiceLifetime.Transient, contextDescriptor.Lifetime);
        Assert.NotNull(handlerDescriptor);
        Assert.NotNull(repoDescriptor);
    }

    [Fact]
    public void WithLifetime_Singleton_WithoutLifecycle_RegistersAllExceptHandler() {
        var services = new ServiceCollection();

        services.AddRepositoryContext()
            .UseEntityFramework<PersonDbContext>(b => b
                .ConfigureDbContext(ConfigurePersonDbContext)
                .WithLifetime(ServiceLifetime.Singleton)
                .WithoutLifecycle());

        var contextDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(PersonDbContext));
        var handlerDescriptor = services.FirstOrDefault(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IRepositoryLifecycleHandler<>));
        var repoDescriptor = services.FirstOrDefault(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IRepository<>) &&
            d.ImplementationType.IsGenericType &&
            d.ImplementationType.GetGenericTypeDefinition() == typeof(EntityRepository<>));

        Assert.NotNull(contextDescriptor);
        Assert.Equal(ServiceLifetime.Singleton, contextDescriptor.Lifetime);
        Assert.Null(handlerDescriptor);
        Assert.NotNull(repoDescriptor);
    }

    [Fact]
    public void WithLifetime_Transient_RegistersRepositoriesAsTransient() {
        var services = new ServiceCollection();

        services.AddRepositoryContext()
            .UseEntityFramework<PersonDbContext>(b => b
                .ConfigureDbContext(ConfigurePersonDbContext)
                .WithLifetime(ServiceLifetime.Transient));

        var singleParam = services.FirstOrDefault(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IRepository<>) &&
            d.ImplementationType.IsGenericType &&
            d.ImplementationType.GetGenericTypeDefinition() == typeof(EntityRepository<>));

        Assert.NotNull(singleParam);
        Assert.Equal(ServiceLifetime.Transient, singleParam.Lifetime);
    }

    #endregion

    #region Resolution (end-to-end)

    [Fact]
    public void WithoutLifecycle_HandlerNotResolvable() {
        var services = new ServiceCollection();

        services.AddRepositoryContext()
            .UseEntityFramework<PersonDbContext>(b => b
                .ConfigureDbContext(ConfigurePersonDbContext)
                .WithLifetime(ServiceLifetime.Scoped)
                .WithoutLifecycle());

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var handler = scope.ServiceProvider.GetService<IRepositoryLifecycleHandler<DbPerson>>();

        Assert.Null(handler);
    }

    [Fact]
    public void WithLifetime_Singleton_ResolvesDbContextAsSingleton() {
        var services = new ServiceCollection();

        services.AddRepositoryContext()
            .UseEntityFramework<PersonDbContext>(b => b
                .ConfigureDbContext(ConfigurePersonDbContext)
                .WithLifetime(ServiceLifetime.Singleton));

        var provider = services.BuildServiceProvider();
        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var context1 = scope1.ServiceProvider.GetService<PersonDbContext>();
        var context2 = scope2.ServiceProvider.GetService<PersonDbContext>();

        Assert.NotNull(context1);
        Assert.Same(context1, context2);
    }

    [Fact]
    public void WithLifetime_Transient_ResolvesDbContextAsDifferentInstances() {
        var services = new ServiceCollection();

        services.AddRepositoryContext()
            .UseEntityFramework<PersonDbContext>(b => b
                .ConfigureDbContext(ConfigurePersonDbContext)
                .WithLifetime(ServiceLifetime.Transient));

        var provider = services.BuildServiceProvider();
        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var context1 = scope1.ServiceProvider.GetService<PersonDbContext>();
        var context2 = scope2.ServiceProvider.GetService<PersonDbContext>();

        Assert.NotNull(context1);
        Assert.NotNull(context2);
        Assert.NotSame(context1, context2);
    }

    #endregion

    #region ImplicitConversion and Build

    [Fact]
    public void ImplicitConversion_FinalizesRegistration() {
        var services = new ServiceCollection();

        services.AddRepositoryContext()
            .UseEntityFramework<PersonDbContext>(b => b
                .ConfigureDbContext(ConfigurePersonDbContext)
                .WithLifetime(ServiceLifetime.Transient)
                .WithoutLifecycle());

        var provider = services.BuildServiceProvider();
        var context = provider.CreateScope().ServiceProvider.GetService<PersonDbContext>();

        Assert.NotNull(context);
    }

    [Fact]
    public void Build_FinalizesRegistration() {
        var services = new ServiceCollection();
        var builder = services.AddRepositoryContext()
            .UseEntityFramework<PersonDbContext>();

        builder.ConfigureDbContext(ConfigurePersonDbContext)
            .WithLifetime(ServiceLifetime.Singleton)
            .WithoutLifecycle()
            .Build();

        var provider = services.BuildServiceProvider();
        var context = provider.CreateScope().ServiceProvider.GetService<PersonDbContext>();

        Assert.NotNull(context);
    }

    [Fact]
    public void Build_ReturnsParentBuilder() {
        var services = new ServiceCollection();
        var parent = services.AddRepositoryContext();
        var builder = parent.UseEntityFramework<PersonDbContext>();

        var result = builder
            .ConfigureDbContext(ConfigurePersonDbContext)
            .Build();

        Assert.Same(parent, result);
    }

    #endregion

    #region DbContextType

    [Fact]
    public void DbContextType_ReturnsConfiguredType() {
        var services = new ServiceCollection();
        var parent = services.AddRepositoryContext();
        var builder = parent.UseEntityFramework<PersonDbContext>();

        Assert.Equal(typeof(PersonDbContext), builder.DbContextType);
    }

    [Fact]
    public void Services_ReturnsParentServices() {
        var services = new ServiceCollection();
        var parent = services.AddRepositoryContext();
        var builder = parent.UseEntityFramework<PersonDbContext>();

        Assert.Same(services, builder.Services);
    }

    #endregion
}
