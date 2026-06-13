using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Core")]
[Trait("Feature", "Lifecycle")]
public class RepositoryLifecycleServiceSeedTests
{
    private readonly PersonFaker _faker = new();

    [Fact]
    public void Should_HaveExpectedDefaults()
    {
        var options = new RepositoryLifecycleOptions();

        Assert.True(options.DeleteIfExists);
        Assert.True(options.DontCreateExisting);
        Assert.False(options.FailFast);
        Assert.Equal(SeedStrategy.Never, options.SeedStrategy);
        Assert.Null(options.EnvironmentName);
        Assert.Null(options.SeedAction);
    }

    [Fact]
    public async Task Should_UseSeedAction_When_Configured()
    {
        var seedActionInvoked = false;
        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(new RepositoryLifecycleOptions
        {
            SeedStrategy = SeedStrategy.Always,
            SeedAction = (sp, type, seedData) =>
            {
                seedActionInvoked = true;
            }
        }));
        services.AddSingleton<ILogger<RepositoryLifecycleService>>(NullLogger<RepositoryLifecycleService>.Instance);
        services.AddSingleton<IRepositoryLifecycleHandler<Person>>(new TestHandler());
        var provider = services.BuildServiceProvider();
        var orchestrator = new RepositoryLifecycleService(
            provider.GetRequiredService<IOptions<RepositoryLifecycleOptions>>(),
            provider,
            NullLogger<RepositoryLifecycleService>.Instance);

        await orchestrator.SeedRepositoryAsync<Person>(new List<Person>(), TestContext.Current.CancellationToken);

        Assert.True(seedActionInvoked);
    }

    [Fact]
    public async Task Should_ResolveSeedStrategy_When_ByEnvironmentAndProfile()
    {
        var services = new ServiceCollection();
        var profile = new DelegateProfile(env => SeedStrategy.IfMissing);
        services.AddSingleton<IRepositoryLifecycleProfile>(profile);
        services.AddSingleton(Options.Create(new RepositoryLifecycleOptions
        {
            SeedStrategy = SeedStrategy.ByEnvironment,
            EnvironmentName = "Staging"
        }));
        services.AddSingleton<ILogger<RepositoryLifecycleService>>(NullLogger<RepositoryLifecycleService>.Instance);
        var provider = services.BuildServiceProvider();

        var orchestrator = new TestableLifecycleService(
            provider.GetRequiredService<IOptions<RepositoryLifecycleOptions>>(),
            provider,
            NullLogger<RepositoryLifecycleService>.Instance);

        var strategy = orchestrator.ExposeResolveSeedStrategy();

        Assert.Equal(SeedStrategy.IfMissing, strategy);
        Assert.Equal("Staging", profile.LastEnvName);
    }

    [Fact]
    public void Should_ReturnProductionEnvironment_When_NoOptionsOrHostEnv()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(new RepositoryLifecycleOptions()));
        services.AddSingleton<ILogger<RepositoryLifecycleService>>(NullLogger<RepositoryLifecycleService>.Instance);
        var provider = services.BuildServiceProvider();

        var orchestrator = new TestableLifecycleService(
            provider.GetRequiredService<IOptions<RepositoryLifecycleOptions>>(),
            provider,
            NullLogger<RepositoryLifecycleService>.Instance);

        var envName = orchestrator.ExposeResolveEnvironmentName();

        Assert.Equal("Production", envName);
    }

    [Fact]
    public void Should_ReturnOptionsEnvironmentName_When_Configured()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(new RepositoryLifecycleOptions
        {
            EnvironmentName = "CustomEnv"
        }));
        services.AddSingleton<ILogger<RepositoryLifecycleService>>(NullLogger<RepositoryLifecycleService>.Instance);
        var provider = services.BuildServiceProvider();

        var orchestrator = new TestableLifecycleService(
            provider.GetRequiredService<IOptions<RepositoryLifecycleOptions>>(),
            provider,
            NullLogger<RepositoryLifecycleService>.Instance);

        var envName = orchestrator.ExposeResolveEnvironmentName();

        Assert.Equal("CustomEnv", envName);
    }

    [Fact]
    public async Task Should_ResolveSeedDataFromProfile_When_NoProviderRegistered()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(new RepositoryLifecycleOptions()));
        services.AddSingleton<ILogger<RepositoryLifecycleService>>(NullLogger<RepositoryLifecycleService>.Instance);
        services.AddSingleton<IRepositoryLifecycleProfile>(new ProfileWithSeedData());
        services.AddSingleton<IRepositoryLifecycleHandler<Person>>(new TestHandler());
        var provider = services.BuildServiceProvider();

        var orchestrator = new TestableLifecycleService(
            provider.GetRequiredService<IOptions<RepositoryLifecycleOptions>>(),
            provider,
            NullLogger<RepositoryLifecycleService>.Instance);

        var seedData = orchestrator.ExposeResolveSeedDataFromProvider<Person>();

        Assert.NotNull(seedData);
    }

    [Fact]
    public async Task Should_ReturnNull_When_NoSeedProviderOrProfile()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(new RepositoryLifecycleOptions()));
        services.AddSingleton<ILogger<RepositoryLifecycleService>>(NullLogger<RepositoryLifecycleService>.Instance);
        var provider = services.BuildServiceProvider();

        var orchestrator = new TestableLifecycleService(
            provider.GetRequiredService<IOptions<RepositoryLifecycleOptions>>(),
            provider,
            NullLogger<RepositoryLifecycleService>.Instance);

        var seedData = orchestrator.ExposeResolveSeedDataFromProvider<Person>();

        Assert.Null(seedData);
    }

    private sealed class TestHandler : IRepositoryLifecycleHandler<Person>
    {
        public ValueTask<bool> ExistsAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(false);
        public ValueTask CreateAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask DropAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SeedAsync(object? seedData = null, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }

    private sealed class DelegateProfile : IRepositoryLifecycleProfile
    {
        private readonly Func<string?, SeedStrategy> _getSeedStrategy;
        public string? LastEnvName { get; private set; }

        public DelegateProfile(Func<string?, SeedStrategy> getSeedStrategy)
        {
            _getSeedStrategy = getSeedStrategy;
        }

        public SeedStrategy GetSeedStrategy(string? environmentName = null)
        {
            LastEnvName = environmentName;
            return _getSeedStrategy(environmentName);
        }

        public object? GetSeedData(Type entityType) => null;
    }

    private sealed class ProfileWithSeedData : IRepositoryLifecycleProfile
    {
        public SeedStrategy GetSeedStrategy(string? environmentName = null) => SeedStrategy.Always;
        public object? GetSeedData(Type entityType) => new List<object>();
    }

    private sealed class TestableLifecycleService : RepositoryLifecycleService
    {
        public TestableLifecycleService(
            IOptions<RepositoryLifecycleOptions> options,
            IServiceProvider serviceProvider,
            ILogger? logger = null)
            : base(options, serviceProvider, logger) { }

        public SeedStrategy ExposeResolveSeedStrategy() => base.ResolveSeedStrategy();
        public string ExposeResolveEnvironmentName() => base.ResolveEnvironmentName();
        public object? ExposeResolveSeedDataFromProvider<TEntity>() where TEntity : class
            => base.ResolveSeedDataFromProvider<TEntity>();
    }
}
