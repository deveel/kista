using Bogus;

using Deveel.Data.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Deveel.Data {
	[Collection(nameof(SqlConnectionCollection))]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Application")]
	[Trait("Feature", "EntityManager")]
	public class EntityManagementTestSuite : EntityManagerTestSuite<EntityManager<DbPerson, Guid>, DbPerson, Guid> {
		private readonly SqlTestConnection sql;

		public EntityManagementTestSuite(SqlTestConnection sql, ITestOutputHelper testOutput) : base(testOutput) {
			this.sql = sql;
		}

		protected override Faker<DbPerson> PersonFaker { get; } = new DbPersonFaker();

		protected override Guid GenerateKey() => Guid.NewGuid();

		protected override void SetKey(DbPerson person, Guid key) {
			person.Id = key;
		}

		protected override void ConfigureServices(IServiceCollection services) {
			services.AddDbContext<DbContext, PersonDbContext>(builder => {
				builder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
				builder.UseSqlite(sql.Connection, sqlite => {
					if (sql.SpatialiteAvailable)
						sqlite.UseNetTopologySuite();
				});
				if (!sql.SpatialiteAvailable)
					builder.ReplaceService<IModelCustomizer, NonSpatialModelCustomizer>();
				builder.EnableSensitiveDataLogging();
			})
			.AddRepository<DbPersonRepository>();

			base.ConfigureServices(services);
		}

		public override async ValueTask InitializeAsync() {
			var options = Services.GetRequiredService<DbContextOptions<PersonDbContext>>();
			using var dbContext = new PersonDbContext(options);

			await dbContext.Database.EnsureDeletedAsync(TestContext.Current.CancellationToken);
			await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

			await base.InitializeAsync();
		}

		public override async Task DisposeAsync() {
			var options = Services.GetRequiredService<DbContextOptions<PersonDbContext>>();
			await using var dbContext = new PersonDbContext(options);

			dbContext.People!.RemoveRange(dbContext.People);
			await dbContext.SaveChangesAsync(true, TestContext.Current.CancellationToken);

			await dbContext.Database.EnsureDeletedAsync(TestContext.Current.CancellationToken);
		}
	}
}
