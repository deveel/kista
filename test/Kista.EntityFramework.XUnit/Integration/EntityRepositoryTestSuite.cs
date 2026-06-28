using Bogus;

using Kista.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Kista {
	[Collection(nameof(SqlConnectionCollection))]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "EntityRepository")]
	public class EntityRepositoryTestSuite : RepositoryTestSuite<DbPerson, Guid, DbRelationship> {
		private readonly SqlTestConnection sql;

		public EntityRepositoryTestSuite(SqlTestConnection sql, ITestOutputHelper? testOutput) : base(testOutput) {
			this.sql = sql;
		}

		protected string ConnectionString => sql.Connection.ConnectionString;

		protected override Faker<DbPerson> PersonFaker => new DbPersonFaker();

		protected override Faker<DbRelationship> RelationshipFaker => new DbPersonRelationshipFaker();

		protected DbPersonRepository PersonRepository => (DbPersonRepository)Repository;

		protected override Guid GeneratePersonId() => Guid.NewGuid();

		protected override Task AddRelationshipAsync(DbPerson person, DbRelationship relationship) {
			if (person.Relationships == null)
				person.Relationships = new List<DbRelationship>();

			person.Relationships.Add(relationship);

			return Task.CompletedTask;
		}

		protected override Task RemoveRelationshipAsync(DbPerson person, DbRelationship relationship) {
			if (person.Relationships != null)
				person.Relationships.Remove(relationship);

			return Task.CompletedTask;
		}

		protected override void ConfigureServices(IServiceCollection services) {
			AddDbContext(services);

			services.AddRepository<DbPersonRepository>();

			base.ConfigureServices(services);
		}

		protected virtual void AddDbContext(IServiceCollection services) {
			services.AddDbContext<PersonDbContext>(builder => {
				builder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
				builder.UseSqlite(sql.Connection, sqlite => {
					if (sql.SpatialiteAvailable)
						sqlite.UseNetTopologySuite();
				});
				// When SpatiaLite is not available we cannot use the NTS type mappings,
				// so we replace the model customizer to ignore geometry-typed properties.
				if (!sql.SpatialiteAvailable)
					builder.ReplaceService<IModelCustomizer, NonSpatialModelCustomizer>();
			});
		}

		protected override async ValueTask InitializeAsync() {
			var options = Services.GetRequiredService<DbContextOptions<PersonDbContext>>();
			await using var dbContext = new PersonDbContext(options);

			await dbContext.Database.EnsureDeletedAsync(TestContext.Current.CancellationToken);
			await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            
			await base.InitializeAsync();
		}

		protected override async ValueTask DisposeAsync() {
			var options = Services.GetRequiredService<DbContextOptions<PersonDbContext>>();
			await using var dbContext = new PersonDbContext(options);

			dbContext.People!.RemoveRange(dbContext.People);
			await dbContext.SaveChangesAsync(true, TestContext.Current.CancellationToken);

			await dbContext.Database.EnsureDeletedAsync(TestContext.Current.CancellationToken);
		}

		protected override IEnumerable<DbPerson> NaturalOrder(IEnumerable<DbPerson> source) => source.OrderBy(x => x.Id);

		[Fact]
	public async Task Should_ReturnEntityWithRelationships_When_FindByKeyAndRelationshipsExist() {
		// Arrange
		var cancellationToken = TestContext.Current.CancellationToken;
		var person = await RandomPersonAsync(x => x.Relationships != null);

		// Act
		var found = await Repository.FindAsync(person.Id!, cancellationToken);

		// Assert
		Assert.NotNull(found);
		Assert.NotNull(found.Relationships);
		Assert.NotEmpty(found.Relationships);
	}

	[Fact]
	public async Task Should_PersistUpdatedEmail_When_UpdateAsyncCalled() {
		// Arrange
		var cancellationToken = TestContext.Current.CancellationToken;
		var newEmail = new Faker("en").Internet.Email();
		var person = await RandomPersonAsync(x => x.Email != newEmail);
		var toUpdate = await PersonRepository.FindAsync(person.Id!, cancellationToken);

		// Act
		await PersonRepository.SetEmailAsync(toUpdate!, newEmail, cancellationToken);
		var updated = await PersonRepository.UpdateAsync(toUpdate!, cancellationToken);

		// Assert
		Assert.True(updated);
		var result = await PersonRepository.FindAsync(person.Id!, cancellationToken);
		Assert.NotNull(result);
		Assert.Equal(newEmail, result.Email);
	}

	[Fact]
	public async Task Should_ReturnEntitiesWithinDistance_When_SpatialQueryApplied() {
		// Arrange — SpatiaLite native lib required; skip on platforms where it's unavailable
		if (!sql.SpatialiteAvailable)
			Assert.Skip("SpatiaLite is not available on this platform – skipping spatial query test.");

		var person = await RandomPersonAsync(x => x.Location != null);

		// Act
		var found = await QueryFilter.Where<DbPerson>(x => x.Location!.Distance(person.Location) <= 1000).Apply<DbPerson>(((Repository<DbPerson, Guid>)PersonRepository).Queryable()).ToListAsync();

		// Assert
		Assert.NotNull(found);
		Assert.NotEmpty(found);
		Assert.Contains(found, x => x.Id == person.Id);
	}
	}
}
