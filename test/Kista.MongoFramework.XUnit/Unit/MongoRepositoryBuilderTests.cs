using Microsoft.Extensions.DependencyInjection;

using MongoFramework;

namespace Kista;

/// <summary>
/// Tests for <see cref="MongoRepositoryBuilder"/> configuration methods
/// (<see cref="MongoRepositoryBuilder.WithLifetime"/>,
/// <see cref="MongoRepositoryBuilder.WithLifecycle"/>,
/// <see cref="MongoRepositoryBuilder.WithoutLifecycle"/>,
/// <see cref="MongoRepositoryBuilder.WithConnectionString"/>,
/// <see cref="MongoRepositoryBuilder.WithConnection"/>)
/// and their effect on service registration.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "DependencyInjection")]
public class MongoRepositoryBuilderTests {
    #region WithLifetime

    [Fact]
    public void WithLifetime_Scoped_RegistersContextAsScoped() {
        var services = new ServiceCollection();

        services.AddRepositoryContext()
            .UseMongoDB<MongoDbContext>(b => b
                .WithConnectionString("mongodb://localhost:27017/testdb")
                .WithLifetime(ServiceLifetime.Scoped));

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(MongoDbContext));

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void WithLifetime_Transient_RegistersContextAsScoped() {
        var services = new ServiceCollection();

        services.AddRepositoryContext()
            .UseMongoDB<MongoDbContext>(b => b
                .WithConnectionString("mongodb://localhost:27017/testdb")
                .WithLifetime(ServiceLifetime.Transient));

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(MongoDbContext));

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void WithLifetime_Singleton_RegistersContextAsScoped() {
        var services = new ServiceCollection();

        services.AddRepositoryContext()
            .UseMongoDB<MongoDbContext>(b => b
                .WithConnectionString("mongodb://localhost:27017/testdb")
                .WithLifetime(ServiceLifetime.Singleton));

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(MongoDbContext));

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void DefaultLifetime_IsScoped() {
        var services = new ServiceCollection();

        services.AddRepositoryContext()
            .UseMongoDB<MongoDbContext>(b => b
                .WithConnectionString("mongodb://localhost:27017/testdb"));

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(MongoDbContext));

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void WithLifetime_RegistersOpenGenericRepositories() {
        var services = new ServiceCollection();

        services.AddRepositoryContext()
            .UseMongoDB<MongoDbContext>(b => b
                .WithConnectionString("mongodb://localhost:27017/testdb")
                .WithLifetime(ServiceLifetime.Scoped));

        var singleParam = services.FirstOrDefault(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IRepository<>) &&
            d.ImplementationType.IsGenericType &&
            d.ImplementationType.GetGenericTypeDefinition() == typeof(MongoRepository<>));

        var twoParam = services.FirstOrDefault(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IRepository<,>) &&
            d.ImplementationType.IsGenericType &&
            d.ImplementationType.GetGenericTypeDefinition() == typeof(MongoRepository<,>));

        Assert.NotNull(singleParam);
        Assert.NotNull(twoParam);
    }

    [Fact]
    public void WithLifetime_AppliesSameLifetimeToOpenGenericRepositories() {
        var services = new ServiceCollection();

        services.AddRepositoryContext()
            .UseMongoDB<MongoDbContext>(b => b
                .WithConnectionString("mongodb://localhost:27017/testdb")
                .WithLifetime(ServiceLifetime.Singleton));

        var singleParam = services.FirstOrDefault(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IRepository<>) &&
            d.ImplementationType.IsGenericType &&
            d.ImplementationType.GetGenericTypeDefinition() == typeof(MongoRepository<>));

        var twoParam = services.FirstOrDefault(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IRepository<,>) &&
            d.ImplementationType.IsGenericType &&
            d.ImplementationType.GetGenericTypeDefinition() == typeof(MongoRepository<,>));

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
            .UseMongoDB<MongoDbContext>(b => b
                .WithConnectionString("mongodb://localhost:27017/testdb")
                .WithLifecycle());

        var handlerDescriptor = services.FirstOrDefault(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IRepositoryLifecycleHandler<>) &&
            d.ImplementationType == typeof(MongoRepositoryLifecycleHandler<>));

        Assert.NotNull(handlerDescriptor);
    }

    [Fact]
    public void WithLifecycle_HandlerIsTransient() {
        var services = new ServiceCollection();

        services.AddRepositoryContext()
            .UseMongoDB<MongoDbContext>(b => b
                .WithConnectionString("mongodb://localhost:27017/testdb")
                .WithLifecycle());

        var handlerDescriptor = services.FirstOrDefault(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IRepositoryLifecycleHandler<>) &&
            d.ImplementationType == typeof(MongoRepositoryLifecycleHandler<>));

        Assert.NotNull(handlerDescriptor);
        Assert.Equal(ServiceLifetime.Transient, handlerDescriptor.Lifetime);
    }

    [Fact]
    public void Default_EnablesLifecycle() {
        var services = new ServiceCollection();

        services.AddRepositoryContext()
            .UseMongoDB<MongoDbContext>(b => b
                .WithConnectionString("mongodb://localhost:27017/testdb"));

        var handlerDescriptor = services.FirstOrDefault(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IRepositoryLifecycleHandler<>) &&
            d.ImplementationType == typeof(MongoRepositoryLifecycleHandler<>));

        Assert.NotNull(handlerDescriptor);
    }

    #endregion

    #region WithoutLifecycle

    [Fact]
    public void WithoutLifecycle_DoesNotRegisterHandler() {
        var services = new ServiceCollection();

        services.AddRepositoryContext()
            .UseMongoDB<MongoDbContext>(b => b
                .WithConnectionString("mongodb://localhost:27017/testdb")
                .WithoutLifecycle());

        var handlerDescriptor = services.FirstOrDefault(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IRepositoryLifecycleHandler<>));

        Assert.Null(handlerDescriptor);
    }

    [Fact]
    public void WithoutLifecycle_StillRegistersContext() {
        var services = new ServiceCollection();

        services.AddRepositoryContext()
            .UseMongoDB<MongoDbContext>(b => b
                .WithConnectionString("mongodb://localhost:27017/testdb")
                .WithoutLifecycle());

        var contextDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(MongoDbContext));

        Assert.NotNull(contextDescriptor);
    }

    [Fact]
    public void WithoutLifecycle_StillRegistersRepositories() {
        var services = new ServiceCollection();

        services.AddRepositoryContext()
            .UseMongoDB<MongoDbContext>(b => b
                .WithConnectionString("mongodb://localhost:27017/testdb")
                .WithoutLifecycle());

        var singleParam = services.FirstOrDefault(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IRepository<>) &&
            d.ImplementationType.IsGenericType &&
            d.ImplementationType.GetGenericTypeDefinition() == typeof(MongoRepository<>));

        Assert.NotNull(singleParam);
    }

    #endregion

    #region Combined Configuration

    [Fact]
    public void WithLifetime_Transient_WithLifecycle_RegistersAll() {
        var services = new ServiceCollection();

        services.AddRepositoryContext()
            .UseMongoDB<MongoDbContext>(b => b
                .WithConnectionString("mongodb://localhost:27017/testdb")
                .WithLifetime(ServiceLifetime.Transient)
                .WithLifecycle());

        var contextDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(MongoDbContext));
        var handlerDescriptor = services.FirstOrDefault(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IRepositoryLifecycleHandler<>) &&
            d.ImplementationType == typeof(MongoRepositoryLifecycleHandler<>));
        var repoDescriptor = services.FirstOrDefault(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IRepository<>) &&
            d.ImplementationType.IsGenericType &&
            d.ImplementationType.GetGenericTypeDefinition() == typeof(MongoRepository<>));

        Assert.NotNull(contextDescriptor);
        Assert.NotNull(handlerDescriptor);
        Assert.NotNull(repoDescriptor);
    }

    [Fact]
    public void WithLifetime_Singleton_WithoutLifecycle_RegistersAllExceptHandler() {
        var services = new ServiceCollection();

        services.AddRepositoryContext()
            .UseMongoDB<MongoDbContext>(b => b
                .WithConnectionString("mongodb://localhost:27017/testdb")
                .WithLifetime(ServiceLifetime.Singleton)
                .WithoutLifecycle());

        var contextDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(MongoDbContext));
        var handlerDescriptor = services.FirstOrDefault(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IRepositoryLifecycleHandler<>));
        var repoDescriptor = services.FirstOrDefault(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IRepository<>) &&
            d.ImplementationType.IsGenericType &&
            d.ImplementationType.GetGenericTypeDefinition() == typeof(MongoRepository<>));

        Assert.NotNull(contextDescriptor);
        Assert.Null(handlerDescriptor);
        Assert.NotNull(repoDescriptor);
    }

    [Fact]
    public void WithLifetime_Singleton_RegistersRepositoriesAsSingleton() {
        var services = new ServiceCollection();

        services.AddRepositoryContext()
            .UseMongoDB<MongoDbContext>(b => b
                .WithConnectionString("mongodb://localhost:27017/testdb")
                .WithLifetime(ServiceLifetime.Singleton));

        var singleParam = services.FirstOrDefault(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IRepository<>) &&
            d.ImplementationType.IsGenericType &&
            d.ImplementationType.GetGenericTypeDefinition() == typeof(MongoRepository<>));

        var twoParam = services.FirstOrDefault(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IRepository<,>) &&
            d.ImplementationType.IsGenericType &&
            d.ImplementationType.GetGenericTypeDefinition() == typeof(MongoRepository<,>));

        Assert.NotNull(singleParam);
        Assert.Equal(ServiceLifetime.Singleton, singleParam.Lifetime);
        Assert.NotNull(twoParam);
        Assert.Equal(ServiceLifetime.Singleton, twoParam.Lifetime);
    }

    [Fact]
    public void WithLifetime_Transient_RegistersRepositoriesAsTransient() {
        var services = new ServiceCollection();

        services.AddRepositoryContext()
            .UseMongoDB<MongoDbContext>(b => b
                .WithConnectionString("mongodb://localhost:27017/testdb")
                .WithLifetime(ServiceLifetime.Transient));

        var singleParam = services.FirstOrDefault(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IRepository<>) &&
            d.ImplementationType.IsGenericType &&
            d.ImplementationType.GetGenericTypeDefinition() == typeof(MongoRepository<>));

        Assert.NotNull(singleParam);
        Assert.Equal(ServiceLifetime.Transient, singleParam.Lifetime);
    }

    #endregion

    #region WithConnectionString

    [Fact]
    public void WithConnectionString_RegistersContext() {
        var services = new ServiceCollection();

        services.AddRepositoryContext()
            .UseMongoDB<MongoDbContext>(b => b
                .WithConnectionString("mongodb://localhost:27017/testdb"));

        var contextDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(MongoDbContext));

        Assert.NotNull(contextDescriptor);
    }

    [Fact]
    public void WithConnectionString_RegistersIMongoDbContext() {
        var services = new ServiceCollection();

        services.AddRepositoryContext()
            .UseMongoDB<MongoDbContext>(b => b
                .WithConnectionString("mongodb://localhost:27017/testdb"));

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IMongoDbContext));

        Assert.NotNull(descriptor);
    }

    #endregion

    #region WithConnection

    [Fact]
    public void WithConnection_RegistersContext() {
        var services = new ServiceCollection();

        services.AddRepositoryContext()
            .UseMongoDB<MongoDbContext>(b => b
                .WithConnection(conn => conn.UseConnection("mongodb://localhost:27017/testdb")));

        var contextDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(MongoDbContext));

        Assert.NotNull(contextDescriptor);
    }

    [Fact]
    public void WithConnection_RegistersIMongoDbContext() {
        var services = new ServiceCollection();

        services.AddRepositoryContext()
            .UseMongoDB<MongoDbContext>(b => b
                .WithConnection(conn => conn.UseConnection("mongodb://localhost:27017/testdb")));

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IMongoDbContext));

        Assert.NotNull(descriptor);
    }

    #endregion

    #region Resolution (end-to-end)

    [Fact]
    public void WithoutLifecycle_HandlerNotResolvable() {
        var services = new ServiceCollection();

        services.AddRepositoryContext()
            .UseMongoDB<MongoDbContext>(b => b
                .WithConnectionString("mongodb://localhost:27017/testdb")
                .WithLifetime(ServiceLifetime.Scoped)
                .WithoutLifecycle());

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var handler = scope.ServiceProvider.GetService<IRepositoryLifecycleHandler<MongoPerson>>();

        Assert.Null(handler);
    }

    [Fact]
    public void WithLifetime_Scoped_ResolvesContextAsScoped() {
        var services = new ServiceCollection();

        services.AddRepositoryContext()
            .UseMongoDB<MongoDbContext>(b => b
                .WithConnectionString("mongodb://localhost:27017/testdb")
                .WithLifetime(ServiceLifetime.Scoped));

        var provider = services.BuildServiceProvider();
        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var context1 = scope1.ServiceProvider.GetService<MongoDbContext>();
        var context2 = scope2.ServiceProvider.GetService<MongoDbContext>();

        Assert.NotNull(context1);
        Assert.NotNull(context2);
        Assert.NotSame(context1, context2);
    }

    [Fact]
    public void WithLifetime_Transient_ResolvesContextAsDifferentInstances() {
        var services = new ServiceCollection();

        services.AddRepositoryContext()
            .UseMongoDB<MongoDbContext>(b => b
                .WithConnectionString("mongodb://localhost:27017/testdb")
                .WithLifetime(ServiceLifetime.Transient));

        var provider = services.BuildServiceProvider();
        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var context1 = scope1.ServiceProvider.GetService<MongoDbContext>();
        var context2 = scope2.ServiceProvider.GetService<MongoDbContext>();

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
            .UseMongoDB<MongoDbContext>(b => b
                .WithConnectionString("mongodb://localhost:27017/testdb")
                .WithLifetime(ServiceLifetime.Transient)
                .WithoutLifecycle());

        var provider = services.BuildServiceProvider();
        var context = provider.CreateScope().ServiceProvider.GetService<MongoDbContext>();

        Assert.NotNull(context);
    }

    #endregion

    #region Builder Properties

    [Fact]
    public void Services_ReturnsParentServices() {
        var services = new ServiceCollection();
        var parent = services.AddRepositoryContext();
        var builder = parent.UseMongoDB<MongoDbContext>();

        Assert.Same(services, builder.Services);
    }

    #endregion
}
