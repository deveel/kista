using Deveel.Data.Entities;

using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Deveel.Data;

[Trait("Category", "Integration")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "EntityFrameworkMultiTenancy")]
public class EntityFrameworkMultiTenancyTests {
	private static readonly string[] TenantIds = ["tenant-a", "tenant-b"];

	[Fact]
	public void WithDatabasePerTenant_RegistersOptions() {
		var services = new ServiceCollection();
		services.AddRepositoryContext()
			.UseEntityFramework<DatabasePerTenantDbContext>()
			.WithDatabasePerTenant<DbTenantInfo>("Data Source=default.db")
			.Build();

		var hasOptions = services.Any(d => 
			d.ServiceType == typeof(IConfigureOptions<EntityFrameworkTenantConnectionOptions>));
		Assert.True(hasOptions);
	}

#if !NET10_0_OR_GREATER
	[Fact]
	public async Task WithDatabasePerTenant_ResolvesTenantConnection() {
		var services = new ServiceCollection();

		services.AddMultiTenant<DbTenantInfo>()
			.WithInMemoryStore()
			.WithContextStrategy();

		services.AddRepositoryContext()
			.UseEntityFramework<DatabasePerTenantDbContext>()
			.WithDatabasePerTenant<DbTenantInfo>("Data Source=default.db")
			.Build();

		var provider = services.BuildServiceProvider();

		using var scope = provider.CreateScope();
		var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<DbTenantInfo>>();

		foreach (var tenantId in TenantIds) {
			await tenantStore.TryAddAsync(new DbTenantInfo(tenantId, tenantId, "Data Source=:memory:"));
		}

		var executionContext = scope.ServiceProvider.GetRequiredService<TenantExecutionContext<DbTenantInfo>>();

		await executionContext.ExecuteInScopeAsync(TenantIds[0], async (IServiceProvider sp) => {
			var dbContext = sp.GetRequiredService<DatabasePerTenantDbContext>();
			await dbContext.Database.EnsureCreatedAsync();
			var conn = dbContext.Database.GetDbConnection();
			Assert.Equal("Data Source=:memory:", conn.ConnectionString);
		});
	}

	[Fact]
	public async Task WithDatabasePerTenant_FallsBackToDefaultConnection() {
		var services = new ServiceCollection();

		services.AddMultiTenant<DbTenantInfo>()
			.WithInMemoryStore()
			.WithContextStrategy();

		services.AddRepositoryContext()
			.UseEntityFramework<DatabasePerTenantDbContext>()
			.WithDatabasePerTenant<DbTenantInfo>("Data Source=fallback.db")
			.Build();

		var provider = services.BuildServiceProvider();

		using var scope = provider.CreateScope();
		var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<DbTenantInfo>>();

		await tenantStore.TryAddAsync(new DbTenantInfo("no-conn-tenant", "no-conn-tenant", ""));

		var executionContext = scope.ServiceProvider.GetRequiredService<TenantExecutionContext<DbTenantInfo>>();

		await executionContext.ExecuteInScopeAsync("no-conn-tenant", async (IServiceProvider sp) => {
			var dbContext = sp.GetRequiredService<DatabasePerTenantDbContext>();
			var conn = dbContext.Database.GetDbConnection();
			Assert.Equal("Data Source=fallback.db", conn.ConnectionString);
		});
	}

	[Fact]
	public async Task WithDatabasePerTenant_ThrowsWhenNoConnection() {
		var services = new ServiceCollection();

		services.AddMultiTenant<DbTenantInfo>()
			.WithInMemoryStore()
			.WithContextStrategy();

		services.AddRepositoryContext()
			.UseEntityFramework<DatabasePerTenantDbContext>()
			.WithDatabasePerTenant<DbTenantInfo>()
			.Build();

		var provider = services.BuildServiceProvider();

		using var scope = provider.CreateScope();
		var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<DbTenantInfo>>();

		await tenantStore.TryAddAsync(new DbTenantInfo("empty-tenant", "empty-tenant", ""));

		var executionContext = scope.ServiceProvider.GetRequiredService<TenantExecutionContext<DbTenantInfo>>();

		await Assert.ThrowsAsync<InvalidOperationException>(() =>
			executionContext.ExecuteInScopeAsync("empty-tenant", async (IServiceProvider sp) => {
				var dbContext = sp.GetRequiredService<DatabasePerTenantDbContext>();
				await dbContext.Database.EnsureCreatedAsync();
			}));
	}

	[Fact]
	public void WithSharedTenantDatabase_PassesForMultiTenantDbContext() {
		var services = new ServiceCollection();

		services.AddMultiTenant<DbTenantInfo>()
			.WithInMemoryStore();

		services.AddRepositoryContext()
			.UseEntityFramework<MultiTenantTestDbContext>(b => b
				.ConfigureDbContext(opts => opts.UseSqlite("Data Source=:memory:"))
				.WithSharedTenantDatabase());

		var provider = services.BuildServiceProvider();
		var context = provider.GetService<MultiTenantTestDbContext>();
		Assert.NotNull(context);
	}
#endif

	[Fact]
	public void WithSharedTenantDatabase_RequiresMultiTenantDbContext() {
		var services = new ServiceCollection();

		Assert.Throws<InvalidOperationException>(() => {
			services.AddRepositoryContext()
				.UseEntityFramework<TestDbContext>()
				.WithSharedTenantDatabase()
				.Build();
		});
	}

	#region Support Types

	private class TestDbContext : DbContext {
		public TestDbContext(DbContextOptions options) : base(options) {
		}

		public DbSet<DbPerson>? Persons { get; set; }
	}

	private class DatabasePerTenantDbContext : DbContext {
		private readonly IMultiTenantContextAccessor<DbTenantInfo> _tenantAccessor;
		private readonly IOptions<EntityFrameworkTenantConnectionOptions> _options;

		public DatabasePerTenantDbContext(
			DbContextOptions<DatabasePerTenantDbContext> dbContextOptions,
			IMultiTenantContextAccessor<DbTenantInfo> tenantAccessor,
			IOptions<EntityFrameworkTenantConnectionOptions> options) : base(dbContextOptions) {
			_tenantAccessor = tenantAccessor;
			_options = options;
		}

		public DbSet<DbTenantPerson>? Persons { get; set; }

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
			var tenantInfo = _tenantAccessor.MultiTenantContext?.TenantInfo;
			var connectionString = tenantInfo?.ConnectionString;

			if (string.IsNullOrEmpty(connectionString)) {
				connectionString = _options.Value.DefaultConnectionString;
			}

			if (string.IsNullOrEmpty(connectionString))
				throw new InvalidOperationException("Connection string is not provided in the tenant info or options.");

			optionsBuilder.UseSqlite(connectionString);
		}
	}

	private class MultiTenantTestDbContext : Finbuckle.MultiTenant.EntityFrameworkCore.MultiTenantDbContext {
		public MultiTenantTestDbContext(
			IMultiTenantContextAccessor<DbTenantInfo> multiTenantContextAccessor,
			DbContextOptions<MultiTenantTestDbContext> options) : base(multiTenantContextAccessor, options) {
		}

		public DbSet<DbPerson>? Persons { get; set; }
	}

	#endregion
}
