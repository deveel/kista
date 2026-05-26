using Bogus;
using Kista.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kista;

[Collection(nameof(SqlUserConnectionCollection))]
[Trait("Category", "Integration")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "UserRepository")]
	public class UserEntityRepositoryTestSuite : UserRepositoryTestSuite<DbBookWithOwner, Guid, string>
	{
		private readonly SqlUserTestConnection sql;

		public UserEntityRepositoryTestSuite(ITestOutputHelper? outputHelper, SqlUserTestConnection sql) 
			: base(outputHelper)
		{
			this.sql = sql;
			BookFaker = new DbBookFaker(UserId);
		}

	protected override Faker<DbBookWithOwner> BookFaker { get; }
    
	protected override Guid GenerateBookId() => Guid.NewGuid();

	protected override string GenerateUserId() => Guid.NewGuid().ToString("N");

	protected string ConnectionString => sql.Connection.ConnectionString;

	protected override void ConfigureServices(IServiceCollection services)
	{
		services.AddDbContext<DbContext, BookDbContext>(builder => {
			builder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
			builder.UseSqlite(sql.Connection, sqlite => {
				if (sql.SpatialiteAvailable)
					sqlite.UseNetTopologySuite();
			});
		})
		.AddRepository<DbBookRepository>();

		base.ConfigureServices(services);
	}

	protected override async ValueTask InitializeAsync()
	{
		var userAccessor = Services.GetRequiredService<IUserAccessor<string>>();
		var options = Services.GetRequiredService<DbContextOptions<BookDbContext>>();
		await using var dbContext = new BookDbContext(options, userAccessor);

		await dbContext.Database.EnsureDeletedAsync(TestContext.Current.CancellationToken);
		await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

		await base.InitializeAsync();
	}
    
	protected override async ValueTask DisposeAsync()
	{
		var userAccessor = Services.GetRequiredService<IUserAccessor<string>>();
		var options = Services.GetRequiredService<DbContextOptions<BookDbContext>>();
		await using var dbContext = new BookDbContext(options, userAccessor);

		dbContext.Books!.RemoveRange(dbContext.Books);
		await dbContext.SaveChangesAsync(true, TestContext.Current.CancellationToken);

		await dbContext.Database.EnsureDeletedAsync();
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "UserRepository")]
	public async Task Should_ReturnNull_When_UserNotSet()
	{
		// Arrange
		var userAccessor = new NullUserAccessor();
		var options = Services.GetRequiredService<DbContextOptions<BookDbContext>>();
		await using var dbContext = new BookDbContext(options, userAccessor);

		var repository = new EntityUserRepository<DbBookWithOwner, Guid, string>(dbContext, userAccessor);

		// Act
		var result = await repository.FindAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

		// Assert
		Assert.Null(result);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "UserRepository")]
	public async Task Should_ReturnNull_When_OwnerMismatch()
	{
		// Arrange
		var book = GenerateUserBook();
		var repository = await GetRepositoryAsync();
		await repository.AddAsync(book, TestContext.Current.CancellationToken);

		var differentUserAccessor = new FixedUserAccessor("different-user");
		var options = Services.GetRequiredService<DbContextOptions<BookDbContext>>();
		await using var dbContext = new BookDbContext(options, differentUserAccessor);

		var differentUserRepo = new EntityUserRepository<DbBookWithOwner, Guid, string>(dbContext, differentUserAccessor);

		// Act
		var result = await differentUserRepo.FindAsync(book.Id, TestContext.Current.CancellationToken);

		// Assert
		Assert.Null(result);
	}

	private class NullUserAccessor : IUserAccessor<string>
	{
		public string? GetUserId() => null;
	}

	private class FixedUserAccessor : IUserAccessor<string>
	{
		private readonly string _userId;
		public FixedUserAccessor(string userId) => _userId = userId;
		public string? GetUserId() => _userId;
	}
}
