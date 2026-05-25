using Bogus;

using Kista.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kista {
	[Collection(nameof(PostgresConnectionCollection))]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Application")]
	[Trait("Feature", "EntityManager")]
	[Trait("Database", "PostgreSQL")]
	public class EntityManagementPostgresTestSuite : EntityManagerTestSuite<EntityManager<DbPerson, Guid>, DbPerson, Guid> {
		private readonly PostgresTestConnection postgres;

		public EntityManagementPostgresTestSuite(PostgresTestConnection postgres, ITestOutputHelper testOutput) : base(testOutput) {
			this.postgres = postgres;
		}

		protected override Faker<DbPerson> PersonFaker { get; } = new DbPersonFaker();

		protected override Guid GenerateKey() => Guid.NewGuid();

		protected override void SetKey(DbPerson person, Guid key) {
			person.Id = key;
		}

		protected override void ConfigureServices(IServiceCollection services) {
			services.AddDbContext<DbContext, PersonDbContext>(builder => {
				builder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
				builder.UseNpgsql(postgres.ConnectionString, npgsql => {
					npgsql.UseNetTopologySuite();
				});
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
