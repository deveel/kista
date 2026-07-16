using Bogus;

using Deveel;

using Kista.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kista {
	[Collection(nameof(SqlConnectionCollection))]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "SoftDelete")]
	public class EntityFrameworkSoftDeleteTests : SoftDeleteRepositoryTestSuite<SoftDeletableDbPerson, Guid> {
		private readonly SqlTestConnection sql;

		public EntityFrameworkSoftDeleteTests(SqlTestConnection sql, ITestOutputHelper? testOutput) : base(testOutput) {
			this.sql = sql;
		}

		protected SqlTestConnection Sql => sql;

		protected override Faker<SoftDeletableDbPerson> PersonFaker => new SoftDeletableDbPersonFaker();

		protected override Guid GeneratePersonId() => Guid.NewGuid();

		protected override void ConfigureServices(IServiceCollection services) {
			services.AddDbContext<SoftDeletablePersonDbContext>(builder => {
				builder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
				builder.UseSqlite(sql.Connection);
			});

			services.AddRepositoryContext().AddRepository<SoftDeletableDbPersonRepository>(_ => { });

			base.ConfigureServices(services);
		}

		protected override async ValueTask InitializeAsync() {
			var options = Services.GetRequiredService<DbContextOptions<SoftDeletablePersonDbContext>>();
			await using var dbContext = new SoftDeletablePersonDbContext(options);

			await dbContext.Database.EnsureDeletedAsync(TestContext.Current.CancellationToken);
			await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

			await base.InitializeAsync();
		}

		protected override async ValueTask DisposeAsync() {
			var options = Services.GetRequiredService<DbContextOptions<SoftDeletablePersonDbContext>>();
			await using var dbContext = new SoftDeletablePersonDbContext(options);

			await dbContext.Database.EnsureDeletedAsync(TestContext.Current.CancellationToken);
		}

		protected override IEnumerable<SoftDeletableDbPerson> NaturalOrder(IEnumerable<SoftDeletableDbPerson> source) => source.OrderBy(x => x.Id);
	}

	[Collection(nameof(SqlConnectionCollection))]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "SoftDelete")]
	public class EntityFrameworkSoftDeleteRestoreTests : EntityFrameworkSoftDeleteTests {
		public EntityFrameworkSoftDeleteRestoreTests(SqlTestConnection sql, ITestOutputHelper? testOutput) : base(sql, testOutput) {
		}

		[Fact]
		public async Task Should_RestoreEntity_ThroughEntityManager_BringsEntityBackToQueries() {
			var person = await RandomPersonAsync();
			var personId = Repository.GetEntityKey(person)!;

			var services = new ServiceCollection();
			services.AddSingleton<IUserAccessor<string>>(new StaticUserAccessor<string>("user-42"));
			services.AddLogging();
			var provider = services.BuildServiceProvider();

			var manager = new EntityManager<SoftDeletableDbPerson, Guid>(Repository, services: provider);

			var removed = await manager.RemoveAsync(person, TestContext.Current.CancellationToken);
			Assert.True(removed.IsSuccess());

			var foundDefault = await Repository.FindAsync(personId, TestContext.Current.CancellationToken);
			Assert.Null(foundDefault);

			var restored = await manager.RestoreAsync(person, TestContext.Current.CancellationToken);
			Assert.True(restored.IsSuccess());

			var foundRestored = await Repository.FindAsync(personId, TestContext.Current.CancellationToken);
			Assert.NotNull(foundRestored);
			Assert.False(foundRestored!.IsDeleted);
			Assert.Null(foundRestored.DeletedAtUtc);
			Assert.Null(foundRestored.DeletedBy);
		}

	}
}