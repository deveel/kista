using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Kista;

/// <summary>
/// Tests for the repository lifecycle feature, including
/// <see cref="IRepositoryLifecycleService"/> registration, option configuration,
/// <see cref="RepositoryLifecycleService"/> behavior (create, drop, seed),
/// controllable repository fallback, seed-data provider resolution, and
/// obsolete <see cref="RepositoryControllerAdapter"/> / <see cref="DefaultRepositoryController"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Core")]
[Trait("Feature", "Lifecycle")]
public class LifecycleTests {
	[Fact]
	public void AddRepositoryLifecycleOrchestrator_ShouldRegisterService() {
		var services = new ServiceCollection();
		services.AddRepositoryLifecycleOrchestrator();

		var provider = services.BuildServiceProvider();
		var orchestrator = provider.GetService<IRepositoryLifecycleService>();

		Assert.NotNull(orchestrator);
		Assert.IsType<RepositoryLifecycleService>(orchestrator);
	}

	[Fact]
	public void AddRepositoryLifecycleOrchestrator_WithOptions_ShouldConfigure() {
		var services = new ServiceCollection();
		services.AddRepositoryLifecycleOrchestrator(options => {
			options.DeleteIfExists = false;
			options.SeedStrategy = SeedStrategy.Always;
			options.FailFast = true;
		});

		var provider = services.BuildServiceProvider();
		var options = provider.GetRequiredService<IOptions<RepositoryLifecycleOptions>>();

		Assert.False(options.Value.DeleteIfExists);
		Assert.Equal(SeedStrategy.Always, options.Value.SeedStrategy);
		Assert.True(options.Value.FailFast);
	}

	[Fact]
	public void ConfigureLifecycle_OnBuilder_ShouldRegisterOrchestrator() {
		var services = new ServiceCollection();
		var builder = new RepositoryContextBuilder(services);

		builder.ConfigureLifecycle(options => {
			options.SeedStrategy = SeedStrategy.Always;
		});

		var provider = services.BuildServiceProvider();
		var orchestrator = provider.GetService<IRepositoryLifecycleService>();

		Assert.NotNull(orchestrator);
	}

	[Fact]
	public void ConfigureLifecycle_WithoutArgs_OnBuilder_ShouldRegisterOrchestrator() {
		var services = new ServiceCollection();
		var builder = new RepositoryContextBuilder(services);

		builder.ConfigureLifecycle();

		var provider = services.BuildServiceProvider();
		var orchestrator = provider.GetService<IRepositoryLifecycleService>();

		Assert.NotNull(orchestrator);
	}

	[Fact]
	public void ConfigureLifecycle_TriggersSeedProviderScan() {
		var services = new ServiceCollection();
		var builder = new RepositoryContextBuilder(services);

		// Register a seed provider from the test assembly before calling ConfigureLifecycle,
		// then verify ConfigureLifecycle doesn't break (it will have triggered EnsureSeedProvidersScanned
		// which scans the entry assembly — the test host, not this one — so no providers found, but
		// it should not throw).
		builder.WithSeedDataFrom(typeof(LifecycleTests).Assembly);
		builder.ConfigureLifecycle();

		var provider = services.BuildServiceProvider();
		var orchestrator = provider.GetService<IRepositoryLifecycleService>();
		Assert.NotNull(orchestrator);
	}

	[Fact]
	public void ConfigureLifecycle_WithArgs_TriggersSeedProviderScan() {
		var services = new ServiceCollection();
		var builder = new RepositoryContextBuilder(services);

		builder.WithSeedDataFrom(typeof(LifecycleTests).Assembly);
		builder.ConfigureLifecycle(options => {
			options.SeedStrategy = SeedStrategy.Always;
		});

		var provider = services.BuildServiceProvider();
		var orchestrator = provider.GetService<IRepositoryLifecycleService>();
		Assert.NotNull(orchestrator);
	}

	[Fact]
	public void RepositoryLifecycleOptions_Defaults() {
		var options = new RepositoryLifecycleOptions();

		Assert.True(options.DeleteIfExists);
		Assert.True(options.DontCreateExisting);
		Assert.False(options.FailFast);
		Assert.Equal(SeedStrategy.Never, options.SeedStrategy);
		Assert.Null(options.EnvironmentName);
		Assert.Null(options.SeedAction);
	}

	[Fact]
	public void RepositoryLifecycleOptions_SeedAction() {
		var options = new RepositoryLifecycleOptions();
		Action<IServiceProvider, Type, object?> action = (_, _, _) => { };
		options.SeedAction = action;
		Assert.Same(action, options.SeedAction);
	}

	[Fact]
	public void SeedStrategy_Values() {
		Assert.Equal(0, (int)SeedStrategy.Never);
		Assert.Equal(1, (int)SeedStrategy.Always);
		Assert.Equal(2, (int)SeedStrategy.IfMissing);
		Assert.Equal(3, (int)SeedStrategy.ByEnvironment);
	}

	[Fact]
	public async Task Orchestrator_CreateAsync_WithNoHandler_ShouldNotThrow() {
		var services = new ServiceCollection();
		services.AddRepositoryLifecycleOrchestrator();

		var provider = services.BuildServiceProvider();
		var orchestrator = provider.GetRequiredService<IRepositoryLifecycleService>();

		await orchestrator.CreateRepositoryAsync<Person>();
	}

	[Fact]
	public async Task Orchestrator_DropAsync_WithNoHandler_ShouldNotThrow() {
		var services = new ServiceCollection();
		services.AddRepositoryLifecycleOrchestrator();

		var provider = services.BuildServiceProvider();
		var orchestrator = provider.GetRequiredService<IRepositoryLifecycleService>();

		await orchestrator.DropRepositoryAsync<Person>();
	}

	[Fact]
	public async Task Orchestrator_SeedAsync_WithNoHandler_ShouldNotThrow() {
		var services = new ServiceCollection();
		services.AddRepositoryLifecycleOrchestrator();

		var provider = services.BuildServiceProvider();
		var orchestrator = provider.GetRequiredService<IRepositoryLifecycleService>();

		await orchestrator.SeedRepositoryAsync<Person>(new List<Person>());
	}

	[Fact]
	public async Task Orchestrator_FailFast_ShouldThrow_WhenNoHandler() {
		var services = new ServiceCollection();
		services.AddRepositoryLifecycleOrchestrator(options => {
			options.FailFast = true;
		});

		var provider = services.BuildServiceProvider();
		var orchestrator = provider.GetRequiredService<IRepositoryLifecycleService>();

		await Assert.ThrowsAsync<RepositoryException>(
			() => orchestrator.CreateRepositoryAsync<Person>().AsTask()
		);
	}

	[Fact]
	public void Adapter_WrapsOrchestrator() {
		var orchestrator = new RepositoryLifecycleService(
			Options.Create(new RepositoryLifecycleOptions()),
			new ServiceCollection().BuildServiceProvider()
		);

		var adapter = new RepositoryControllerAdapter(orchestrator);

		Assert.NotNull(adapter);
		Assert.IsAssignableFrom<IRepositoryController>(adapter);
	}

	[Fact]
	public void ControllerAdapter_IsObsolete() {
		var attr = typeof(RepositoryControllerAdapter)
			.GetCustomAttributes(typeof(ObsoleteAttribute), false);

		Assert.NotEmpty(attr);
	}

	[Fact]
	public void DefaultRepositoryController_IsObsolete() {
		var attr = typeof(DefaultRepositoryController)
			.GetCustomAttributes(typeof(ObsoleteAttribute), false);

		Assert.NotEmpty(attr);
	}

	[Fact]
	public void RepositoryControllerOptions_IsObsolete() {
		var attr = typeof(RepositoryControllerOptions)
			.GetCustomAttributes(typeof(ObsoleteAttribute), false);

		Assert.NotEmpty(attr);
	}

	[Fact]
	public void IRepositorySeedDataProvider_IsRegistered() {
		var services = new ServiceCollection();
		services.AddTransient<IRepositorySeedDataProvider<Person>, TestSeedDataProvider>();

		var provider = services.BuildServiceProvider();
		var seedProvider = provider.GetService<IRepositorySeedDataProvider<Person>>();

		Assert.NotNull(seedProvider);
	}

	[Fact]
	public async Task Constructor_WithExplicitLogger() {
		var logger = NullLogger<RepositoryLifecycleService>.Instance;
		var options = Options.Create(new RepositoryLifecycleOptions());
		var sp = new ServiceCollection().BuildServiceProvider();

		var orchestrator = new RepositoryLifecycleService(options, sp, logger);

		await orchestrator.CreateRepositoryAsync<Person>();
	}

	[Fact]
	public async Task Constructor_WithNonGenericLogger() {
		var logger = NullLogger.Instance;
		var options = Options.Create(new RepositoryLifecycleOptions());
		var sp = new ServiceCollection().BuildServiceProvider();

		var orchestrator = new TestableOrchestrator(options, sp, logger);

		await orchestrator.CreateRepositoryAsync<Person>();
	}

	[Fact]
	public async Task ResolveHandler_WithRegisteredHandler_ReturnsHandler() {
		var handler = new MockHandler<Person>();
		var services = new ServiceCollection();
		services.AddSingleton<IRepositoryLifecycleHandler<Person>>(handler);
		services.AddRepositoryLifecycleOrchestrator();

		var provider = services.BuildServiceProvider();
		var orchestrator = provider.GetRequiredService<IRepositoryLifecycleService>();

		await orchestrator.CreateRepositoryAsync<Person>();

		Assert.True(handler.CreateCalled);
	}

	[Fact]
	public async Task ResolveHandler_WithControllableRepository_FallsBack() {
		var repo = new ControllableRepo<Person>();
		var services = new ServiceCollection();
		services.AddSingleton<IRepository<Person>>(repo);
		services.AddRepositoryLifecycleOrchestrator();

		var provider = services.BuildServiceProvider();
		var orchestrator = provider.GetRequiredService<IRepositoryLifecycleService>();

		await orchestrator.CreateRepositoryAsync<Person>();

		Assert.True(repo.CreateCalled);
	}

	[Fact]
	public async Task ResolveHandler_NoHandler_NoFailFast_ReturnsNull() {
		var services = new ServiceCollection();
		services.AddRepositoryLifecycleOrchestrator(options => {
			options.FailFast = false;
		});

		var provider = services.BuildServiceProvider();
		var orchestrator = provider.GetRequiredService<IRepositoryLifecycleService>();

		await orchestrator.CreateRepositoryAsync<Person>();
	}

	[Fact]
	public async Task ResolveHandler_TKey_WithRegisteredHandler_ReturnsHandler() {
		var handler = new MockHandler<Person>();
		var services = new ServiceCollection();
		services.AddSingleton<IRepositoryLifecycleHandler<Person>>(handler);
		services.AddRepositoryLifecycleOrchestrator();

		var provider = services.BuildServiceProvider();
		var orchestrator = provider.GetRequiredService<IRepositoryLifecycleService>();

		await orchestrator.CreateRepositoryAsync<Person, string>();

		Assert.True(handler.CreateCalled);
	}

	[Fact]
	public async Task ResolveHandler_TKey_WithControllableRepository_FallsBack() {
		var repo = new ControllableRepo<Person, string>();
		var services = new ServiceCollection();
		services.AddSingleton<IRepository<Person, string>>(repo);
		services.AddRepositoryLifecycleOrchestrator();

		var provider = services.BuildServiceProvider();
		var orchestrator = provider.GetRequiredService<IRepositoryLifecycleService>();

		await orchestrator.CreateRepositoryAsync<Person, string>();

		Assert.True(repo.CreateCalled);
	}

	[Fact]
	public async Task ResolveHandler_TKey_NoHandler_FailFast_Throws() {
		var services = new ServiceCollection();
		services.AddRepositoryLifecycleOrchestrator(options => {
			options.FailFast = true;
		});

		var provider = services.BuildServiceProvider();
		var orchestrator = provider.GetRequiredService<IRepositoryLifecycleService>();

		await Assert.ThrowsAsync<RepositoryException>(
			() => orchestrator.CreateRepositoryAsync<Person, string>().AsTask()
		);
	}

	[Fact]
	public async Task ResolveHandler_TKey_NoHandler_NoFailFast_DoesNotThrow() {
		var services = new ServiceCollection();
		services.AddRepositoryLifecycleOrchestrator(options => {
			options.FailFast = false;
		});

		var provider = services.BuildServiceProvider();
		var orchestrator = provider.GetRequiredService<IRepositoryLifecycleService>();

		await orchestrator.CreateRepositoryAsync<Person, string>();
	}

	[Fact]
	public async Task DropRepositoryAsync_TKey_WithHandler_CallsDrop() {
		var handler = new MockHandler<Person> { Exists = true };
		var services = new ServiceCollection();
		services.AddSingleton<IRepositoryLifecycleHandler<Person>>(handler);
		services.AddRepositoryLifecycleOrchestrator();

		var provider = services.BuildServiceProvider();
		var orchestrator = provider.GetRequiredService<IRepositoryLifecycleService>();

		await orchestrator.DropRepositoryAsync<Person, string>();

		Assert.True(handler.DropCalled);
	}

	[Fact]
	public async Task SeedRepositoryAsync_TKey_WithHandler_CallsSeed() {
		var handler = new MockHandler<Person>();
		var services = new ServiceCollection();
		services.AddSingleton<IRepositoryLifecycleHandler<Person>>(handler);
		services.AddRepositoryLifecycleOrchestrator(options => {
			options.SeedStrategy = SeedStrategy.Always;
		});

		var provider = services.BuildServiceProvider();
		var orchestrator = provider.GetRequiredService<IRepositoryLifecycleService>();

		await orchestrator.SeedRepositoryAsync<Person, string>(new List<Person>());

		Assert.True(handler.SeedCalled);
	}

	[Fact]
	public async Task CreateRepository_Exists_DeleteIfExists_DropsAndCreates() {
		var handler = new MockHandler<Person> { Exists = true };
		var opts = Options.Create(new RepositoryLifecycleOptions {
			DeleteIfExists = true,
			DontCreateExisting = false
		});
		var sp = new ServiceCollection().BuildServiceProvider();
		var orchestrator = new TestableOrchestrator(opts, sp);

		await orchestrator.CreateRepository(handler, default);

		Assert.True(handler.DropCalled);
		Assert.True(handler.CreateCalled);
	}

	[Fact]
	public async Task CreateRepository_Exists_DontCreateExisting_Skips() {
		var handler = new MockHandler<Person> { Exists = true };
		var opts = Options.Create(new RepositoryLifecycleOptions {
			DeleteIfExists = false,
			DontCreateExisting = true
		});
		var sp = new ServiceCollection().BuildServiceProvider();
		var orchestrator = new TestableOrchestrator(opts, sp);

		await orchestrator.CreateRepository(handler, default);

		Assert.False(handler.DropCalled);
		Assert.False(handler.CreateCalled);
	}

	[Fact]
	public async Task CreateRepository_Exists_Neither_Throws() {
		var handler = new MockHandler<Person> { Exists = true };
		var opts = Options.Create(new RepositoryLifecycleOptions {
			DeleteIfExists = false,
			DontCreateExisting = false
		});
		var sp = new ServiceCollection().BuildServiceProvider();
		var orchestrator = new TestableOrchestrator(opts, sp);

		await Assert.ThrowsAsync<RepositoryException>(
			() => orchestrator.CreateRepository(handler, default).AsTask()
		);
	}

	[Fact]
	public async Task CreateRepository_NotSupportedException_Rethrows() {
		var handler = new MockHandler<Person> { CreateException = new NotSupportedException() };
		var opts = Options.Create(new RepositoryLifecycleOptions());
		var sp = new ServiceCollection().BuildServiceProvider();
		var orchestrator = new TestableOrchestrator(opts, sp);

		await Assert.ThrowsAsync<NotSupportedException>(
			() => orchestrator.CreateRepository(handler, default).AsTask()
		);
	}

	[Fact]
	public async Task CreateRepository_RepositoryException_Rethrows() {
		var handler = new MockHandler<Person> { CreateException = new RepositoryException("fail") };
		var opts = Options.Create(new RepositoryLifecycleOptions());
		var sp = new ServiceCollection().BuildServiceProvider();
		var orchestrator = new TestableOrchestrator(opts, sp);

		await Assert.ThrowsAsync<RepositoryException>(
			() => orchestrator.CreateRepository(handler, default).AsTask()
		);
	}

	[Fact]
	public async Task CreateRepository_GeneralException_WrapsInRepositoryException() {
		var handler = new MockHandler<Person> { CreateException = new InvalidOperationException("bad") };
		var opts = Options.Create(new RepositoryLifecycleOptions());
		var sp = new ServiceCollection().BuildServiceProvider();
		var orchestrator = new TestableOrchestrator(opts, sp);

		var ex = await Assert.ThrowsAsync<RepositoryException>(
			() => orchestrator.CreateRepository(handler, default).AsTask()
		);
		Assert.IsType<InvalidOperationException>(ex.InnerException);
	}

	[Fact]
	public async Task DropRepository_HandlerNull_DoesNothing() {
		var orchestrator = CreateTestOrchestrator(new RepositoryLifecycleOptions());
		await orchestrator.DropRepository<Person>(null, default);
	}

	[Fact]
	public async Task DropRepository_DoesNotExist_Skips() {
		var handler = new MockHandler<Person> { Exists = false };
		var orchestrator = CreateTestOrchestrator(new RepositoryLifecycleOptions());

		await orchestrator.DropRepository(handler, default);

		Assert.False(handler.DropCalled);
	}

	[Fact]
	public async Task DropRepository_Exists_Drops() {
		var handler = new MockHandler<Person> { Exists = true };
		var orchestrator = CreateTestOrchestrator(new RepositoryLifecycleOptions());

		await orchestrator.DropRepository(handler, default);

		Assert.True(handler.DropCalled);
	}

	[Fact]
	public async Task DropRepository_NotSupportedException_Rethrows() {
		var handler = new MockHandler<Person> {
			Exists = true,
			DropException = new NotSupportedException()
		};
		var orchestrator = CreateTestOrchestrator(new RepositoryLifecycleOptions());

		await Assert.ThrowsAsync<NotSupportedException>(
			() => orchestrator.DropRepository(handler, default).AsTask()
		);
	}

	[Fact]
	public async Task DropRepository_RepositoryException_Rethrows() {
		var handler = new MockHandler<Person> {
			Exists = true,
			DropException = new RepositoryException("fail")
		};
		var orchestrator = CreateTestOrchestrator(new RepositoryLifecycleOptions());

		await Assert.ThrowsAsync<RepositoryException>(
			() => orchestrator.DropRepository(handler, default).AsTask()
		);
	}

	[Fact]
	public async Task DropRepository_GeneralException_WrapsInRepositoryException() {
		var handler = new MockHandler<Person> {
			Exists = true,
			DropException = new InvalidOperationException("bad")
		};
		var orchestrator = CreateTestOrchestrator(new RepositoryLifecycleOptions());

		var ex = await Assert.ThrowsAsync<RepositoryException>(
			() => orchestrator.DropRepository(handler, default).AsTask()
		);
		Assert.IsType<InvalidOperationException>(ex.InnerException);
	}

	[Fact]
	public async Task SeedRepository_HandlerNull_DoesNothing() {
		var orchestrator = CreateTestOrchestrator(new RepositoryLifecycleOptions());
		await orchestrator.SeedRepository<Person>(null, null, default);
	}

	[Fact]
	public async Task SeedRepository_StrategyNever_DoesNothing() {
		var handler = new MockHandler<Person>();
		var orchestrator = CreateTestOrchestrator(new RepositoryLifecycleOptions {
			SeedStrategy = SeedStrategy.Never
		});

		await orchestrator.SeedRepository(handler, new List<Person>(), default);

		Assert.False(handler.SeedCalled);
	}

	[Fact]
	public async Task SeedRepository_StrategyIfMissing_RepoExists_Skips() {
		var handler = new MockHandler<Person> { Exists = true };
		var orchestrator = CreateTestOrchestrator(new RepositoryLifecycleOptions {
			SeedStrategy = SeedStrategy.IfMissing
		});

		await orchestrator.SeedRepository(handler, new List<Person>(), default);

		Assert.False(handler.SeedCalled);
	}

	[Fact]
	public async Task SeedRepository_SeedAction_InvokesAction() {
		var handler = new MockHandler<Person>();
		var invoked = false;
		var opts = new RepositoryLifecycleOptions {
			SeedStrategy = SeedStrategy.Always,
			SeedAction = (_, _, _) => { invoked = true; }
		};
		var orchestrator = CreateTestOrchestrator(opts);

		await orchestrator.SeedRepository(handler, "seed-data", default);

		Assert.True(invoked);
		Assert.False(handler.SeedCalled);
	}

	[Fact]
	public async Task SeedRepository_WithExplicitSeedData_Seeds() {
		var handler = new MockHandler<Person>();
		var orchestrator = CreateTestOrchestrator(new RepositoryLifecycleOptions {
			SeedStrategy = SeedStrategy.Always
		});

		await orchestrator.SeedRepository(handler, new List<Person>(), default);

		Assert.True(handler.SeedCalled);
		Assert.NotNull(handler.SeedDataReceived);
	}

	[Fact]
	public async Task SeedRepository_WithSeedDataProvider_Seeds() {
		var handler = new MockHandler<Person>();
		var services = new ServiceCollection();
		services.AddSingleton<IRepositorySeedDataProvider<Person>>(new TestSeedDataProvider());
		var sp = services.BuildServiceProvider();
		var opts = Options.Create(new RepositoryLifecycleOptions {
			SeedStrategy = SeedStrategy.Always
		});
		var orchestrator = new TestableOrchestrator(opts, sp);

		await orchestrator.SeedRepository(handler, null, default);

		Assert.True(handler.SeedCalled);
		Assert.NotNull(handler.SeedDataReceived);
	}

	[Fact]
	public async Task SeedRepository_WithoutSeedData_NoProvider_DoesNothing() {
		var handler = new MockHandler<Person>();
		var orchestrator = CreateTestOrchestrator(new RepositoryLifecycleOptions {
			SeedStrategy = SeedStrategy.Always
		});

		await orchestrator.SeedRepository(handler, null, default);

		Assert.False(handler.SeedCalled);
	}

	[Fact]
	public async Task SeedRepository_NotSupportedException_Rethrows() {
		var handler = new MockHandler<Person> {
			SeedException = new NotSupportedException()
		};
		var orchestrator = CreateTestOrchestrator(new RepositoryLifecycleOptions {
			SeedStrategy = SeedStrategy.Always
		});

		await Assert.ThrowsAsync<NotSupportedException>(
			() => orchestrator.SeedRepository(handler, new List<Person>(), default).AsTask()
		);
	}

	[Fact]
	public async Task SeedRepository_RepositoryException_Rethrows() {
		var handler = new MockHandler<Person> {
			SeedException = new RepositoryException("fail")
		};
		var orchestrator = CreateTestOrchestrator(new RepositoryLifecycleOptions {
			SeedStrategy = SeedStrategy.Always
		});

		await Assert.ThrowsAsync<RepositoryException>(
			() => orchestrator.SeedRepository(handler, new List<Person>(), default).AsTask()
		);
	}

	[Fact]
	public async Task SeedRepository_GeneralException_WrapsInRepositoryException() {
		var handler = new MockHandler<Person> {
			SeedException = new InvalidOperationException("bad")
		};
		var orchestrator = CreateTestOrchestrator(new RepositoryLifecycleOptions {
			SeedStrategy = SeedStrategy.Always
		});

		var ex = await Assert.ThrowsAsync<RepositoryException>(
			() => orchestrator.SeedRepository(handler, new List<Person>(), default).AsTask()
		);
		Assert.IsType<InvalidOperationException>(ex.InnerException);
	}

	[Fact]
	public void ResolveSeedStrategy_ReturnsDefault() {
		var orchestrator = CreateTestOrchestrator(new RepositoryLifecycleOptions {
			SeedStrategy = SeedStrategy.Always
		});

		Assert.Equal(SeedStrategy.Always, orchestrator.ResolveSeedStrategy());
	}

	[Fact]
	public void ResolveSeedStrategy_ByEnvironment_WithProfile_ReturnsProfileStrategy() {
		var services = new ServiceCollection();
		services.AddSingleton<IRepositoryLifecycleProfile>(new MockLifecycleProfile(SeedStrategy.IfMissing));
		var orchestrator = new TestableOrchestrator(
			Options.Create(new RepositoryLifecycleOptions {
				SeedStrategy = SeedStrategy.ByEnvironment,
				EnvironmentName = "Staging"
			}),
			services.BuildServiceProvider()
		);

		Assert.Equal(SeedStrategy.IfMissing, orchestrator.ResolveSeedStrategy());
	}

	[Fact]
	public void ResolveSeedStrategy_ByEnvironment_WithoutProfile_ReturnsAlways() {
		var orchestrator = CreateTestOrchestrator(new RepositoryLifecycleOptions {
			SeedStrategy = SeedStrategy.ByEnvironment,
			EnvironmentName = "Staging"
		});

		Assert.Equal(SeedStrategy.Always, orchestrator.ResolveSeedStrategy());
	}

	[Fact]
	public void ResolveEnvironmentName_ReturnsProduction() {
		var orchestrator = CreateTestOrchestrator(new RepositoryLifecycleOptions());

		Assert.Equal(RepositoryLifecycleService.ProductionEnvironment, orchestrator.ResolveEnvironmentName());
	}

	[Fact]
	public void ResolveEnvironmentName_FromOptions() {
		var orchestrator = CreateTestOrchestrator(new RepositoryLifecycleOptions {
			EnvironmentName = "Staging"
		});

		Assert.Equal("Staging", orchestrator.ResolveEnvironmentName());
	}

	[Fact]
	public void ResolveEnvironmentName_EmptyOptions_FallsBack() {
		var orchestrator = CreateTestOrchestrator(new RepositoryLifecycleOptions {
			EnvironmentName = ""
		});

		Assert.Equal(RepositoryLifecycleService.ProductionEnvironment, orchestrator.ResolveEnvironmentName());
	}

	[Fact]
	public void ResolveEnvironmentName_WhitespaceOptions_FallsBack() {
		var orchestrator = CreateTestOrchestrator(new RepositoryLifecycleOptions {
			EnvironmentName = "   "
		});

		Assert.Equal(RepositoryLifecycleService.ProductionEnvironment, orchestrator.ResolveEnvironmentName());
	}

	// -----------------------------------------------------------------------
	// DefaultRepositoryLifecycleProfile tests
	// -----------------------------------------------------------------------

	[Theory]
	[InlineData("Development", SeedStrategy.Always)]
	[InlineData("development", SeedStrategy.Always)]
	[InlineData("Dev", SeedStrategy.Always)]
	[InlineData("dev", SeedStrategy.Always)]
	[InlineData("Staging", SeedStrategy.IfMissing)]
	[InlineData("staging", SeedStrategy.IfMissing)]
	[InlineData("Stage", SeedStrategy.IfMissing)]
	[InlineData("stage", SeedStrategy.IfMissing)]
	[InlineData("Testing", SeedStrategy.Always)]
	[InlineData("testing", SeedStrategy.Always)]
	[InlineData("Test", SeedStrategy.Always)]
	[InlineData("test", SeedStrategy.Always)]
	[InlineData("Production", SeedStrategy.Never)]
	[InlineData("production", SeedStrategy.Never)]
	[InlineData("Prod", SeedStrategy.Never)]
	[InlineData("prod", SeedStrategy.Never)]
	[InlineData("Unknown", SeedStrategy.Always)]
	[InlineData(null, SeedStrategy.Always)]
	[InlineData("", SeedStrategy.Always)]
	public void DefaultProfile_GetSeedStrategy_ReturnsCorrectStrategy(string? envName, SeedStrategy expected) {
		var sp = new ServiceCollection().BuildServiceProvider();
		var profile = new DefaultRepositoryLifecycleProfile(sp);

		var result = profile.GetSeedStrategy(envName);

		Assert.Equal(expected, result);
	}

	[Fact]
	public void DefaultProfile_GetSeedData_WithProvider_ReturnsData() {
		var services = new ServiceCollection();
		services.AddSingleton<IRepositorySeedDataProvider<Person>, TestSeedDataProvider>();
		var sp = services.BuildServiceProvider();
		var profile = new DefaultRepositoryLifecycleProfile(sp);

		var result = profile.GetSeedData<Person>();

		Assert.NotNull(result);
		var data = Assert.IsAssignableFrom<IEnumerable<Person>>(result);
		Assert.Single(data);
	}

	[Fact]
	public void DefaultProfile_GetSeedData_WithoutProvider_ReturnsNull() {
		var sp = new ServiceCollection().BuildServiceProvider();
		var profile = new DefaultRepositoryLifecycleProfile(sp);

		var result = profile.GetSeedData<Person>();

		Assert.Null(result);
	}

	[Fact]
	public void DefaultProfile_GetSeedData_ByType_ReturnsData() {
		var services = new ServiceCollection();
		services.AddSingleton<IRepositorySeedDataProvider<Person>, TestSeedDataProvider>();
		var sp = services.BuildServiceProvider();
		var profile = new DefaultRepositoryLifecycleProfile(sp);

		var result = profile.GetSeedData(typeof(Person));

		Assert.NotNull(result);
	}

	[Fact]
	public void DefaultProfile_GetSeedData_ByType_Null_Throws() {
		var sp = new ServiceCollection().BuildServiceProvider();
		var profile = new DefaultRepositoryLifecycleProfile(sp);

		Assert.Throws<ArgumentNullException>(() => profile.GetSeedData(null!));
	}

	[Fact]
	public void DefaultProfile_GetSeedData_CachesResults() {
		var services = new ServiceCollection();
		services.AddSingleton<IRepositorySeedDataProvider<Person>, TestSeedDataProvider>();
		var sp = services.BuildServiceProvider();
		var profile = new DefaultRepositoryLifecycleProfile(sp);

		var result1 = profile.GetSeedData<Person>();
		var result2 = profile.GetSeedData<Person>();

		Assert.Same(result1, result2);
	}

	// -----------------------------------------------------------------------
	// WithLifecycleProfile builder method tests
	// -----------------------------------------------------------------------

	[Fact]
	public void WithLifecycleProfile_Type_RegistersService() {
		var services = new ServiceCollection();
		var builder = new RepositoryContextBuilder(services);

		builder.WithLifecycleProfile<MockLifecycleProfile>();

		var provider = services.BuildServiceProvider();
		var profile = provider.GetService<IRepositoryLifecycleProfile>();

		Assert.NotNull(profile);
		Assert.IsType<MockLifecycleProfile>(profile);
	}

	[Fact]
	public void WithLifecycleProfile_Type_SupportsCustomLifetime() {
		var services = new ServiceCollection();
		var builder = new RepositoryContextBuilder(services);

		builder.WithLifecycleProfile<MockLifecycleProfile>(ServiceLifetime.Transient);

		var provider = services.BuildServiceProvider();
		var profile1 = provider.GetRequiredService<IRepositoryLifecycleProfile>();
		var profile2 = provider.GetRequiredService<IRepositoryLifecycleProfile>();

		Assert.NotSame(profile1, profile2);
	}

	[Fact]
	public void WithLifecycleProfile_Instance_RegistersSingleton() {
		var services = new ServiceCollection();
		var builder = new RepositoryContextBuilder(services);
		var instance = new MockLifecycleProfile(SeedStrategy.IfMissing);

		builder.WithLifecycleProfile(instance);

		var provider = services.BuildServiceProvider();
		var profile = provider.GetService<IRepositoryLifecycleProfile>();

		Assert.Same(instance, profile);
	}

	[Fact]
	public void ConfigureLifecycle_AutoRegistersDefaultProfile() {
		var services = new ServiceCollection();
		var builder = new RepositoryContextBuilder(services);

		builder.ConfigureLifecycle();

		var provider = services.BuildServiceProvider();
		var profile = provider.GetService<IRepositoryLifecycleProfile>();

		Assert.NotNull(profile);
		Assert.IsType<DefaultRepositoryLifecycleProfile>(profile);
	}

	[Fact]
	public void ConfigureLifecycle_CustomProfileOverridesDefault() {
		var services = new ServiceCollection();
		var builder = new RepositoryContextBuilder(services);
		var customProfile = new MockLifecycleProfile(SeedStrategy.IfMissing);

		builder
			.WithLifecycleProfile(customProfile)
			.ConfigureLifecycle();

		var provider = services.BuildServiceProvider();
		var profile = provider.GetService<IRepositoryLifecycleProfile>();

		Assert.Same(customProfile, profile);
	}

	[Fact]
	public void WithLifecycleProfile_ReturnsBuilderForChaining() {
		var services = new ServiceCollection();
		var builder = new RepositoryContextBuilder(services);

		var result = builder.WithLifecycleProfile<MockLifecycleProfile>();

		Assert.Same(builder, result);
	}

	[Fact]
	public void WithLifecycleProfile_Instance_ReturnsBuilderForChaining() {
		var services = new ServiceCollection();
		var builder = new RepositoryContextBuilder(services);
		var instance = new MockLifecycleProfile(SeedStrategy.Never);

		var result = builder.WithLifecycleProfile(instance);

		Assert.Same(builder, result);
	}

	// -----------------------------------------------------------------------
	// Orchestrator seed data resolution with profile fallback tests
	// -----------------------------------------------------------------------

	[Fact]
	public void ResolveSeedDataFromProvider_WithProfile_ReturnsProfileData() {
		var services = new ServiceCollection();
		services.AddSingleton<IRepositoryLifecycleProfile>(new ProfileWithSeedData());
		var orchestrator = new TestableOrchestrator(
			Options.Create(new RepositoryLifecycleOptions()),
			services.BuildServiceProvider()
		);

		var result = orchestrator.ResolveSeedDataFromProvider<Person>();

		Assert.NotNull(result);
		var data = Assert.IsAssignableFrom<IEnumerable<Person>>(result);
		Assert.Single(data);
	}

	[Fact]
	public void ResolveSeedDataFromProvider_PreferProviderOverProfile() {
		var services = new ServiceCollection();
		services.AddSingleton<IRepositorySeedDataProvider<Person>>(new TestSeedDataProvider());
		services.AddSingleton<IRepositoryLifecycleProfile>(new ProfileWithDifferentData());
		var orchestrator = new TestableOrchestrator(
			Options.Create(new RepositoryLifecycleOptions()),
			services.BuildServiceProvider()
		);

		var result = orchestrator.ResolveSeedDataFromProvider<Person>();

		Assert.NotNull(result);
		var data = Assert.IsAssignableFrom<IEnumerable<Person>>(result);
		var person = Assert.Single(data);
		Assert.Equal("Test", person.FirstName);
	}

	[Fact]
	public async Task SeedRepository_UsesProfileData_WhenNoProviderRegistered() {
		var handler = new MockHandler<Person>();
		var services = new ServiceCollection();
		services.AddSingleton<IRepositoryLifecycleProfile>(new ProfileWithSeedData());
		var sp = services.BuildServiceProvider();
		var opts = Options.Create(new RepositoryLifecycleOptions {
			SeedStrategy = SeedStrategy.Always
		});
		var orchestrator = new TestableOrchestrator(opts, sp);

		await orchestrator.SeedRepository(handler, null, default);

		Assert.True(handler.SeedCalled);
		Assert.NotNull(handler.SeedDataReceived);
		var data = Assert.IsAssignableFrom<IEnumerable<Person>>(handler.SeedDataReceived);
		var person = Assert.Single(data);
		Assert.Equal("ProfilePerson", person.FirstName);
	}

	[Fact]
	public async Task SeedRepository_ByEnvironment_UsesProfileStrategy() {
		var handler = new MockHandler<Person>();
		var services = new ServiceCollection();
		services.AddSingleton<IRepositoryLifecycleProfile>(new EnvironmentAwareProfile());
		services.AddSingleton<IRepositorySeedDataProvider<Person>>(new TestSeedDataProvider());
		var sp = services.BuildServiceProvider();
		var opts = Options.Create(new RepositoryLifecycleOptions {
			SeedStrategy = SeedStrategy.ByEnvironment,
			EnvironmentName = "Production"
		});
		var orchestrator = new TestableOrchestrator(opts, sp);

		await orchestrator.SeedRepository(handler, null, default);

		Assert.False(handler.SeedCalled);
	}

	[Fact]
	public async Task SeedRepository_ByEnvironment_Development_SeedsWithProfile() {
		var handler = new MockHandler<Person>();
		var services = new ServiceCollection();
		services.AddSingleton<IRepositoryLifecycleProfile>(new ProfileWithSeedData());
		var sp = services.BuildServiceProvider();
		var opts = Options.Create(new RepositoryLifecycleOptions {
			SeedStrategy = SeedStrategy.ByEnvironment,
			EnvironmentName = "Development"
		});
		var orchestrator = new TestableOrchestrator(opts, sp);

		await orchestrator.SeedRepository(handler, null, default);

		Assert.True(handler.SeedCalled);
	}

	[Fact]
	public void ResolveSeedStrategy_ByEnvironment_WithDefaultProfile_ReturnsEnvironmentStrategy() {
		var services = new ServiceCollection();
		services.AddSingleton<IRepositoryLifecycleProfile, DefaultRepositoryLifecycleProfile>();
		var sp = services.BuildServiceProvider();
		var opts = Options.Create(new RepositoryLifecycleOptions {
			SeedStrategy = SeedStrategy.ByEnvironment,
			EnvironmentName = "Production"
		});
		var orchestrator = new TestableOrchestrator(opts, sp);

		var strategy = orchestrator.ResolveSeedStrategy();

		Assert.Equal(SeedStrategy.Never, strategy);
	}

	[Fact]
	public void ResolveSeedStrategy_ByEnvironment_WithDefaultProfile_Development_ReturnsAlways() {
		var services = new ServiceCollection();
		services.AddSingleton<IRepositoryLifecycleProfile, DefaultRepositoryLifecycleProfile>();
		var sp = services.BuildServiceProvider();
		var opts = Options.Create(new RepositoryLifecycleOptions {
			SeedStrategy = SeedStrategy.ByEnvironment,
			EnvironmentName = "Development"
		});
		var orchestrator = new TestableOrchestrator(opts, sp);

		var strategy = orchestrator.ResolveSeedStrategy();

		Assert.Equal(SeedStrategy.Always, strategy);
	}

	[Fact]
	public void ResolveSeedDataFromProvider_WithoutProviderOrProfile_ReturnsNull() {
		var orchestrator = CreateTestOrchestrator(new RepositoryLifecycleOptions());

		var result = orchestrator.ResolveSeedDataFromProvider<Person>();

		Assert.Null(result);
	}

	[Fact]
	public void ResolveSeedDataFromProvider_WithProvider_ReturnsData() {
		var services = new ServiceCollection();
		services.AddSingleton<IRepositorySeedDataProvider<Person>>(new TestSeedDataProvider());
		var orchestrator = new TestableOrchestrator(
			Options.Create(new RepositoryLifecycleOptions()),
			services.BuildServiceProvider()
		);

		var result = orchestrator.ResolveSeedDataFromProvider<Person>();

		Assert.NotNull(result);
	}

	[Fact]
	public void ResolveSeedDataFromProvider_WithoutProvider_ReturnsNull() {
		var orchestrator = CreateTestOrchestrator(new RepositoryLifecycleOptions());

		Assert.Null(orchestrator.ResolveSeedDataFromProvider<Person>());
	}

	[Fact]
	public async Task ControllableRepositoryHandler_Exists() {
		var repo = new ControllableRepo<Person>();
		var handler = new ControllableHandler<Person>(repo);

		Assert.False(await handler.ExistsAsync(default));
	}

	[Fact]
	public async Task ControllableRepositoryHandler_Create() {
		var repo = new ControllableRepo<Person>();
		var handler = new ControllableHandler<Person>(repo);

		await handler.CreateAsync(default);

		Assert.True(repo.CreateCalled);
	}

	[Fact]
	public async Task ControllableRepositoryHandler_Drop() {
		var repo = new ControllableRepo<Person>();
		var handler = new ControllableHandler<Person>(repo);

		await handler.DropAsync(default);

		Assert.True(repo.DropCalled);
	}

	[Fact]
	public async Task ControllableRepositoryHandler_Seed() {
		var repo = new ControllableRepo<Person>();
		var handler = new ControllableHandler<Person>(repo);

		await handler.SeedAsync("data", default);
	}

	private static TestableOrchestrator CreateTestOrchestrator(RepositoryLifecycleOptions opts) {
		return new TestableOrchestrator(
			Options.Create(opts),
			new ServiceCollection().BuildServiceProvider()
		);
	}

	[Fact]
	public void ResolveSeedStrategy_ByEnvironment_WithHostEnv() {
		var services = new ServiceCollection();
		services.AddRepositoryLifecycleOrchestrator();
		var sp = services.BuildServiceProvider();
		var orchestrator = new LifecycleEnvOrchestrator(
			Options.Create(new RepositoryLifecycleOptions {
				SeedStrategy = SeedStrategy.ByEnvironment,
				EnvironmentName = null
			}),
			sp
		);
		var strategy = orchestrator.PublicResolveSeedStrategy();
		Assert.Equal(SeedStrategy.Always, strategy);
	}

	/// <summary>
	/// A stub <see cref="IRepositorySeedDataProvider{Person}"/> that returns a single
	/// test <see cref="Person"/> entity.
	/// </summary>
	private class TestSeedDataProvider : IRepositorySeedDataProvider<Person> {
		public IEnumerable<Person> GetSeedData() {
			yield return new Person { FirstName = "Test" };
		}

		IEnumerable<object> IRepositorySeedDataProvider.GetSeedData()
			=> GetSeedData().Cast<object>();
	}

	/// <summary>
	/// A testable subclass of <see cref="RepositoryLifecycleService"/> that
	/// exposes all internal and protected members as public, enabling direct
	/// unit-testing of orchestration logic (create, drop, seed, strategy resolution).
	/// </summary>
	private class TestableOrchestrator : RepositoryLifecycleService {
		/// <summary>
		/// Creates a new instance with the given options, service provider, and optional logger.
		/// </summary>
		/// <param name="options">The lifecycle configuration options.</param>
		/// <param name="serviceProvider">The service provider for dependency resolution.</param>
		/// <param name="logger">An optional logger instance.</param>
		public TestableOrchestrator(
			IOptions<RepositoryLifecycleOptions> options,
			IServiceProvider serviceProvider,
			ILogger? logger = null)
			: base(options, serviceProvider, logger) {
		}

		/// <summary>
		/// Resolves the lifecycle handler for the given entity type (no key).
		/// </summary>
		public new IRepositoryLifecycleHandler<TEntity>? ResolveHandler<TEntity>()
			where TEntity : class
			=> base.ResolveHandler<TEntity>();

		/// <summary>
		/// Resolves the lifecycle handler for the given entity type with a key.
		/// </summary>
		public new IRepositoryLifecycleHandler<TEntity>? ResolveHandler<TEntity, TKey>()
			where TEntity : class
			=> base.ResolveHandler<TEntity, TKey>();

		/// <summary>
		/// Creates the repository via the given handler.
		/// </summary>
		public new ValueTask CreateRepository<TEntity>(IRepositoryLifecycleHandler<TEntity>? handler, CancellationToken cancellationToken)
			where TEntity : class
			=> base.CreateRepository(handler, cancellationToken);

		/// <summary>
		/// Drops the repository via the given handler.
		/// </summary>
		public new ValueTask DropRepository<TEntity>(IRepositoryLifecycleHandler<TEntity>? handler, CancellationToken cancellationToken)
			where TEntity : class
			=> base.DropRepository(handler, cancellationToken);

		/// <summary>
		/// Seeds the repository via the given handler.
		/// </summary>
		public new ValueTask SeedRepository<TEntity>(IRepositoryLifecycleHandler<TEntity>? handler, object? seedData, CancellationToken cancellationToken)
			where TEntity : class
			=> base.SeedRepository(handler, seedData, cancellationToken);

		/// <summary>
		/// Resolves the effective <see cref="SeedStrategy"/> from options.
		/// </summary>
		public new SeedStrategy ResolveSeedStrategy()
			=> base.ResolveSeedStrategy();

		/// <summary>
		/// Resolves the effective environment name.
		/// </summary>
		public new string ResolveEnvironmentName()
			=> base.ResolveEnvironmentName();

		/// <summary>
		/// Resolves seed data from a registered <see cref="IRepositorySeedDataProvider{TEntity}"/>.
		/// </summary>
		public new object? ResolveSeedDataFromProvider<TEntity>()
			where TEntity : class
			=> base.ResolveSeedDataFromProvider<TEntity>();
	}

	/// <summary>
	/// A configurable mock implementation of <see cref="IRepositoryLifecycleHandler{TEntity}"/>
	/// that tracks calls and can throw configurable exceptions for each lifecycle operation.
	/// </summary>
	/// <typeparam name="TEntity">The entity type handled by this mock.</typeparam>
	private class MockHandler<TEntity> : IRepositoryLifecycleHandler<TEntity> where TEntity : class {
		public bool Exists { get; set; }
		public bool CreateCalled { get; set; }
		public bool DropCalled { get; set; }
		public bool SeedCalled { get; set; }
		public object? SeedDataReceived { get; set; }

		public Exception? ExistsException { get; set; }
		public Exception? CreateException { get; set; }
		public Exception? DropException { get; set; }
		public Exception? SeedException { get; set; }

		public ValueTask<bool> ExistsAsync(CancellationToken cancellationToken = default) {
			if (ExistsException != null)
				throw ExistsException;
			return new ValueTask<bool>(Exists);
		}

		public ValueTask CreateAsync(CancellationToken cancellationToken = default) {
			CreateCalled = true;
			if (CreateException != null)
				throw CreateException;
			return ValueTask.CompletedTask;
		}

		public ValueTask DropAsync(CancellationToken cancellationToken = default) {
			DropCalled = true;
			if (DropException != null)
				throw DropException;
			return ValueTask.CompletedTask;
		}

		public ValueTask SeedAsync(object? seedData = null, CancellationToken cancellationToken = default) {
			SeedCalled = true;
			SeedDataReceived = seedData;
			if (SeedException != null)
				throw SeedException;
			return ValueTask.CompletedTask;
		}
	}

	/// <summary>
	/// A repository stub that implements both <see cref="IRepository{TEntity}"/> and
	/// <see cref="IControllableRepository"/>, allowing lifecycle tests to exercise
	/// the controllable-repository fallback path for entity types without a key.
	/// </summary>
	/// <typeparam name="TEntity">The entity type.</typeparam>
	private class ControllableRepo<TEntity> : IRepository<TEntity>, IControllableRepository where TEntity : class {
		public bool CreateCalled { get; set; }
		public bool DropCalled { get; set; }

		public IServiceProvider? Services => null;

		public ValueTask<bool> ExistsAsync(CancellationToken cancellationToken = default)
			=> new(false);

		public ValueTask CreateAsync(CancellationToken cancellationToken = default) {
			CreateCalled = true;
			return ValueTask.CompletedTask;
		}

		public ValueTask DropAsync(CancellationToken cancellationToken = default) {
			DropCalled = true;
			return ValueTask.CompletedTask;
		}

		public object? GetEntityKey(TEntity entity) => null;

		public ValueTask AddAsync(TEntity entity, CancellationToken cancellationToken = default)
			=> throw new NotSupportedException();

		public ValueTask AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
			=> throw new NotSupportedException();

		public ValueTask<bool> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
			=> throw new NotSupportedException();

		public ValueTask<bool> RemoveAsync(TEntity entity, CancellationToken cancellationToken = default)
			=> throw new NotSupportedException();

		public ValueTask RemoveRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
			=> throw new NotSupportedException();

		public ValueTask<TEntity?> FindAsync(object key, CancellationToken cancellationToken = default)
			=> throw new NotSupportedException();
	}

	/// <summary>
	/// A repository stub that implements both <see cref="IRepository{TEntity, TKey}"/> and
	/// <see cref="IControllableRepository"/>, allowing lifecycle tests to exercise
	/// the controllable-repository fallback path for entity types with a key.
	/// </summary>
	/// <typeparam name="TEntity">The entity type.</typeparam>
	/// <typeparam name="TKey">The key type.</typeparam>
	private class ControllableRepo<TEntity, TKey> : IRepository<TEntity, TKey>, IControllableRepository where TEntity : class {
		public bool CreateCalled { get; set; }
		public bool DropCalled { get; set; }

		public IServiceProvider? Services => null;

		public ValueTask<bool> ExistsAsync(CancellationToken cancellationToken = default)
			=> new(false);

		public ValueTask CreateAsync(CancellationToken cancellationToken = default) {
			CreateCalled = true;
			return ValueTask.CompletedTask;
		}

		public ValueTask DropAsync(CancellationToken cancellationToken = default) {
			DropCalled = true;
			return ValueTask.CompletedTask;
		}

		public TKey? GetEntityKey(TEntity entity) => default;

		public ValueTask AddAsync(TEntity entity, CancellationToken cancellationToken = default)
			=> throw new NotSupportedException();

		public ValueTask AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
			=> throw new NotSupportedException();

		public ValueTask<bool> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
			=> throw new NotSupportedException();

		public ValueTask<bool> RemoveAsync(TEntity entity, CancellationToken cancellationToken = default)
			=> throw new NotSupportedException();

		public ValueTask RemoveRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
			=> throw new NotSupportedException();

		public ValueTask<TEntity?> FindAsync(TKey key, CancellationToken cancellationToken = default)
			=> throw new NotSupportedException();
	}

	private sealed class LifecycleEnvOrchestrator : RepositoryLifecycleService {
		public LifecycleEnvOrchestrator(IOptions<RepositoryLifecycleOptions> options, IServiceProvider sp)
			: base(options, sp, NullLogger.Instance) { }

		public string PublicResolveEnvironmentName() => base.ResolveEnvironmentName();
		public SeedStrategy PublicResolveSeedStrategy() => base.ResolveSeedStrategy();
	}

	/// <summary>
	/// An adapter that implements <see cref="IRepositoryLifecycleHandler{TEntity}"/>
	/// by delegating to an <see cref="IControllableRepository"/>, allowing lifecycle
	/// tests to exercise controllable-repository-backed handlers.
	/// </summary>
	/// <typeparam name="TEntity">The entity type.</typeparam>
	private class ControllableHandler<TEntity> : IRepositoryLifecycleHandler<TEntity> where TEntity : class {
		private readonly IControllableRepository repository;

		public ControllableHandler(IControllableRepository repository) {
			this.repository = repository;
		}

		public ValueTask<bool> ExistsAsync(CancellationToken cancellationToken)
			=> repository.ExistsAsync(cancellationToken);

		public ValueTask CreateAsync(CancellationToken cancellationToken)
			=> repository.CreateAsync(cancellationToken);

		public ValueTask DropAsync(CancellationToken cancellationToken)
			=> repository.DropAsync(cancellationToken);

		public ValueTask SeedAsync(object? seedData = null, CancellationToken cancellationToken = default)
			=> ValueTask.CompletedTask;
	}

	/// <summary>
	/// A stub <see cref="IRepositoryLifecycleProfile"/> that returns a fixed
	/// <see cref="SeedStrategy"/> regardless of the environment name.
	/// </summary>
	private class MockLifecycleProfile : IRepositoryLifecycleProfile {
		private readonly SeedStrategy strategy;

		public MockLifecycleProfile() : this(SeedStrategy.Always) { }

		public MockLifecycleProfile(SeedStrategy strategy) {
			this.strategy = strategy;
		}

		public SeedStrategy GetSeedStrategy(string? environmentName = null)
			=> strategy;

		public object? GetSeedData(Type entityType)
			=> null;
	}

	/// <summary>
	/// A lifecycle profile that provides seed data for <see cref="Person"/> entities.
	/// </summary>
	private class ProfileWithSeedData : IRepositoryLifecycleProfile {
		public SeedStrategy GetSeedStrategy(string? environmentName = null)
			=> SeedStrategy.Always;

		public object? GetSeedData(Type entityType) {
			if (entityType == typeof(Person)) {
				return new List<Person> { new Person { FirstName = "ProfilePerson" } };
			}
			return null;
		}
	}

	/// <summary>
	/// A lifecycle profile that provides different seed data to distinguish from providers.
	/// </summary>
	private class ProfileWithDifferentData : IRepositoryLifecycleProfile {
		public SeedStrategy GetSeedStrategy(string? environmentName = null)
			=> SeedStrategy.Always;

		public object? GetSeedData(Type entityType) {
			if (entityType == typeof(Person)) {
				return new List<Person> { new Person { FirstName = "ProfilePerson" } };
			}
			return null;
		}
	}

	/// <summary>
	/// A lifecycle profile that provides environment-aware seed strategies.
	/// </summary>
	private class EnvironmentAwareProfile : IRepositoryLifecycleProfile {
		public SeedStrategy GetSeedStrategy(string? environmentName = null) {
			return environmentName?.ToLowerInvariant() switch {
				"development" or "dev" => SeedStrategy.Always,
				"production" or "prod" => SeedStrategy.Never,
				_ => SeedStrategy.IfMissing
			};
		}

		public object? GetSeedData(Type entityType) {
			if (entityType == typeof(Person)) {
				return new List<Person> { new Person { FirstName = "EnvProfilePerson" } };
			}
			return null;
		}
	}
}
