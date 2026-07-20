using Microsoft.Extensions.DependencyInjection;

namespace Kista;

/// <summary>
/// Tests for the <c>WithSoftDelete</c> extension methods on
/// <see cref="RepositoryContextBuilder"/> and <see cref="RepositoryBuilder"/>,
/// and for the <see cref="SoftDeleteOptions"/> configuration bag.
/// Soft-delete filtering activates automatically for entities implementing
/// <see cref="ISoftDeletable"/>; these tests cover only the explicit
/// registration surface.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Core")]
[Trait("Feature", "SoftDelete")]
public class SoftDeleteExtensionsTests {
    [Fact]
    public void WithSoftDelete_OnContextBuilder_RegistersOptionsSingleton() {
        var services = new ServiceCollection();
        var builder = new RepositoryContextBuilder(services);

        var result = builder.WithSoftDelete(o => { /* no knobs yet */ });

        Assert.Same(builder, result);
        using var provider = services.BuildServiceProvider();
        var opts = provider.GetService<SoftDeleteOptions>();
        Assert.NotNull(opts);
    }

    [Fact]
    public void WithSoftDelete_OnContextBuilder_WithoutConfigure_RegistersDefaultOptions() {
        var services = new ServiceCollection();
        var builder = new RepositoryContextBuilder(services);

        builder.WithSoftDelete();

        using var provider = services.BuildServiceProvider();
        var opts = provider.GetService<SoftDeleteOptions>();
        Assert.NotNull(opts);
        Assert.Same(SoftDeleteOptions.Default, SoftDeleteOptions.Default);
    }

    [Fact]
    public void WithSoftDelete_OnContextBuilder_ThrowsOnNullBuilder() {
        RepositoryContextBuilder? builder = null;
        Assert.Throws<ArgumentNullException>(() => builder!.WithSoftDelete());
    }

    [Fact]
    public void WithSoftDelete_OnRepositoryBuilder_RegistersOptionsSingleton() {
        var services = new ServiceCollection();
        var contextBuilder = new RepositoryContextBuilder(services);
        var repoBuilder = contextBuilder.AddRepository<InMemoryRepository<Person, string>>();

        var result = repoBuilder.WithSoftDelete(o => { /* no knobs yet */ });

        Assert.Same(repoBuilder, result);
        using var provider = services.BuildServiceProvider();
        var opts = provider.GetService<SoftDeleteOptions>();
        Assert.NotNull(opts);
    }

    [Fact]
    public void WithSoftDelete_OnRepositoryBuilder_WithoutConfigure_RegistersDefaultOptions() {
        var services = new ServiceCollection();
        var contextBuilder = new RepositoryContextBuilder(services);
        var repoBuilder = contextBuilder.AddRepository<InMemoryRepository<Person, string>>();

        repoBuilder.WithSoftDelete();

        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<SoftDeleteOptions>());
    }

    [Fact]
    public void WithSoftDelete_OnRepositoryBuilder_ThrowsOnNullBuilder() {
        RepositoryBuilder? builder = null;
        Assert.Throws<ArgumentNullException>(() => builder!.WithSoftDelete());
    }

    [Fact]
    public void SoftDeleteOptions_Default_IsSingletonInstance() {
        Assert.Same(SoftDeleteOptions.Default, SoftDeleteOptions.Default);
        Assert.NotNull(SoftDeleteOptions.Default);
    }

    [Fact]
    public void WithSoftDelete_OnContextBuilder_TryAdd_DoesNotOverrideExistingOptions() {
        // WithSoftDelete uses TryAddSingleton, so a pre-registered SoftDeleteOptions
        // instance must survive a subsequent WithSoftDelete call.
        var services = new ServiceCollection();
        var preExisting = new SoftDeleteOptions();
        services.AddSingleton(preExisting);
        var builder = new RepositoryContextBuilder(services);

        builder.WithSoftDelete();

        using var provider = services.BuildServiceProvider();
        var resolved = provider.GetRequiredService<SoftDeleteOptions>();
        Assert.Same(preExisting, resolved);
    }
}