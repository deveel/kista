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
	public class EntityFrameworkSoftDeleteTests : SoftDeleteRepositoryTestSuite<SoftDeletableDbPerson, Guid, SoftDeletableDbRelationship> {
		private readonly SqlTestConnection sql;

		public EntityFrameworkSoftDeleteTests(SqlTestConnection sql, ITestOutputHelper? testOutput) : base(testOutput) {
			this.sql = sql;
		}

		protected override Faker<SoftDeletableDbPerson> PersonFaker => new SoftDeletableDbPersonFaker();

		protected override Faker<SoftDeletableDbRelationship> RelationshipFaker => new SoftDeletableDbRelationshipFaker();

		protected override Guid GeneratePersonId() => Guid.NewGuid();

		protected override Task AddRelationshipAsync(SoftDeletableDbPerson person, SoftDeletableDbRelationship relationship) {
			if (person.Relationships == null)
				person.Relationships = new List<SoftDeletableDbRelationship>();

			person.Relationships.Add(relationship);

			return Task.CompletedTask;
		}

		protected override Task RemoveRelationshipAsync(SoftDeletableDbPerson person, SoftDeletableDbRelationship relationship) {
			if (person.Relationships != null)
				person.Relationships.Remove(relationship);

			return Task.CompletedTask;
		}

		protected override void ConfigureServices(IServiceCollection services) {
			services.AddDbContext<SoftDeletablePersonDbContext>(builder => {
				builder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
				builder.UseSqlite(sql.Connection);
			});

			services.AddRepository<SoftDeletableDbPersonRepository>();

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
	public class EntityFrameworkSoftDeleteRestoreTests : SoftDeleteRepositoryTestSuite<SoftDeletableDbPerson, Guid, SoftDeletableDbRelationship> {
		private readonly SqlTestConnection sql;

		public EntityFrameworkSoftDeleteRestoreTests(SqlTestConnection sql, ITestOutputHelper? testOutput) : base(testOutput) {
			this.sql = sql;
		}

		protected override Faker<SoftDeletableDbPerson> PersonFaker => new SoftDeletableDbPersonFaker();

		protected override Faker<SoftDeletableDbRelationship> RelationshipFaker => new SoftDeletableDbRelationshipFaker();

		protected override Guid GeneratePersonId() => Guid.NewGuid();

		protected override Task AddRelationshipAsync(SoftDeletableDbPerson person, SoftDeletableDbRelationship relationship) {
			if (person.Relationships == null)
				person.Relationships = new List<SoftDeletableDbRelationship>();

			person.Relationships.Add(relationship);

			return Task.CompletedTask;
		}

		protected override Task RemoveRelationshipAsync(SoftDeletableDbPerson person, SoftDeletableDbRelationship relationship) {
			if (person.Relationships != null)
				person.Relationships.Remove(relationship);

			return Task.CompletedTask;
		}

		protected override void ConfigureServices(IServiceCollection services) {
			services.AddDbContext<SoftDeletablePersonDbContext>(builder => {
				builder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
				builder.UseSqlite(sql.Connection);
			});

			services.AddRepository<SoftDeletableDbPersonRepository>();

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

		[Fact]
		public async Task Should_RestoreEntity_ThroughEntityManager_BringsEntityBackToQueries() {
			var person = await RandomPersonAsync();
			var personId = Repository.GetEntityKey(person)!;

			var services = new ServiceCollection();
			services.AddSingleton<IUserAccessor<string>>(new StaticUserAccessor("user-42"));
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

		private sealed class StaticUserAccessor : IUserAccessor<string> {
			private readonly string _userId;

			public StaticUserAccessor(string userId) {
				_userId = userId;
			}

			public string? GetUserId() => _userId;
		}
	}
}