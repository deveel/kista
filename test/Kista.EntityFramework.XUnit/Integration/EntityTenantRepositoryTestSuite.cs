using Bogus;
using Kista.Entities;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Kista
{
	[Collection(nameof(SqlTenantConnectionCollection))]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "MultiTenantRepository")]
	public class EntityTenantRepositoryTestSuite : MultiTenantRepositoryTestSuite<DbTenantInfo, DbTenantPerson, Guid>
	{
		private readonly SqlTestConnection sql;

		public EntityTenantRepositoryTestSuite(SqlTenantTestConnection sql, ITestOutputHelper? testOutput) : base(testOutput)
		{
			this.sql = sql;
		}

		protected override Faker<DbTenantPerson> CreatePersonFaker(string tenantId) => new DbTenantPersonFaker(tenantId);

		protected override DbTenantInfo CreateTenantInfo(string tenantId) => new DbTenantInfo
		{
			Id = tenantId,
			Identifier = tenantId,
			ConnectionString = sql.Connection.ConnectionString
		};

		protected override Guid GeneratePersonId() => Guid.NewGuid();

		protected override void ConfigureServices(IServiceCollection services)
		{
			AddDbContext(services);

			services.AddRepositoryContext().AddRepository<DbTenantPersonRepository>(_ => { });

			base.ConfigureServices(services);
		}

		protected virtual void AddDbContext(IServiceCollection services)
		{
			services.AddDbContext<PersonTenantDbContext>(builder => {
				builder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
				builder.UseSqlite(sql.Connection, sqlite => {
					if (sql.SpatialiteAvailable)
						sqlite.UseNetTopologySuite();
				});
				// PersonTenantDbContext.OnModelCreating references DbRelationship which
				// navigates back to DbPerson (with a Point? Location).  When SpatiaLite is
				// not available we must strip those geometry properties via the customizer.
				if (!sql.SpatialiteAvailable)
					builder.ReplaceService<IModelCustomizer, NonSpatialModelCustomizer>();
			});
		}

		protected override async ValueTask InitializeAsync()
		{
			foreach (var tenantId in TenantIds)
			{
				await ExecuteInTenantScopeAsync(tenantId, async (IMultiTenantContextAccessor tenantContextAccessor, DbContextOptions<PersonTenantDbContext> options) =>
				{
					await using var dbContext = new PersonTenantDbContext(tenantContextAccessor, options);

					await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
				});
			}


			await base.InitializeAsync();
		}

		protected override async ValueTask DisposeAsync()
		{
			foreach (var tenantId in TenantIds)
			{
				await ExecuteInTenantScopeAsync(tenantId, async (IMultiTenantContextAccessor tenantContextAccessor, DbContextOptions<PersonTenantDbContext> options) =>
				{
					await using var dbContext = new PersonTenantDbContext(tenantContextAccessor, options);
					dbContext.Persons!.RemoveRange(dbContext.Persons);
					await dbContext.SaveChangesAsync(true, TestContext.Current.CancellationToken);
				});
			}
		}

	}
}
