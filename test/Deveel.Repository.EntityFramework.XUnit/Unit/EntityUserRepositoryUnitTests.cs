using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Deveel.Data;

[Trait("Category", "Unit")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "EntityUserRepository")]
public class EntityUserRepositoryUnitTests
{
	[Fact]
	public async Task FindAsync_ShouldReturnNull_WhenUserIdNotSet()
	{
		// Arrange
		await using var connection = new SqliteConnection("DataSource=:memory:");
		await connection.OpenAsync();

		var options = new DbContextOptionsBuilder<TestDbContext>()
			.UseSqlite(connection)
			.Options;
		await using var context = new TestDbContext(options);
		await context.Database.EnsureCreatedAsync();

		var userAccessor = new NullUserAccessor();
		var repository = new EntityUserRepository<OwnedEntity, int, string>(context, userAccessor);

		// Act
		var result = await repository.FindAsync(1);

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public async Task FindAsync_ShouldReturnNull_WhenEntityNotFound()
	{
		// Arrange
		await using var connection = new SqliteConnection("DataSource=:memory:");
		await connection.OpenAsync();

		var options = new DbContextOptionsBuilder<TestDbContext>()
			.UseSqlite(connection)
			.Options;
		await using var context = new TestDbContext(options);
		await context.Database.EnsureCreatedAsync();

		var userAccessor = new FixedUserAccessor("user1");
		var repository = new EntityUserRepository<OwnedEntity, int, string>(context, userAccessor);

		// Act
		var result = await repository.FindAsync(999);

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public async Task FindAsync_ShouldReturnNull_WhenOwnerDoesNotMatch()
	{
		// Arrange
		await using var connection = new SqliteConnection("DataSource=:memory:");
		await connection.OpenAsync();

		var options = new DbContextOptionsBuilder<TestDbContext>()
			.UseSqlite(connection)
			.Options;
		await using var context = new TestDbContext(options);
		await context.Database.EnsureCreatedAsync();

		var entity = new OwnedEntity { Id = 1, Name = "Test", Owner = "other-user" };
		context.OwnedEntities.Add(entity);
		await context.SaveChangesAsync();

		var userAccessor = new FixedUserAccessor("user1");
		var repository = new EntityUserRepository<OwnedEntity, int, string>(context, userAccessor);

		// Act
		var result = await repository.FindAsync(1);

		// Assert
		Assert.Null(result);
	}

	[Fact]
	public async Task FindAsync_ShouldReturnEntity_WhenOwnerMatches()
	{
		// Arrange
		await using var connection = new SqliteConnection("DataSource=:memory:");
		await connection.OpenAsync();

		var options = new DbContextOptionsBuilder<TestDbContext>()
			.UseSqlite(connection)
			.Options;
		await using var context = new TestDbContext(options);
		await context.Database.EnsureCreatedAsync();

		var entity = new OwnedEntity { Id = 1, Name = "Test", Owner = "user1" };
		context.OwnedEntities.Add(entity);
		await context.SaveChangesAsync();

		var userAccessor = new FixedUserAccessor("user1");
		var repository = new EntityUserRepository<OwnedEntity, int, string>(context, userAccessor);

		// Act
		var result = await repository.FindAsync(1);

		// Assert
		Assert.NotNull(result);
		Assert.Equal("Test", result.Name);
		Assert.Equal("user1", result.Owner);
	}

	[Fact]
	public async Task AddAsync_ShouldSetOwner_WhenUserIdIsSet()
	{
		// Arrange
		await using var connection = new SqliteConnection("DataSource=:memory:");
		await connection.OpenAsync();

		var options = new DbContextOptionsBuilder<TestDbContext>()
			.UseSqlite(connection)
			.Options;
		await using var context = new TestDbContext(options);
		await context.Database.EnsureCreatedAsync();

		var userAccessor = new FixedUserAccessor("user1");
		var repository = new EntityUserRepository<OwnedEntity, int, string>(context, userAccessor);
		var entity = new OwnedEntity { Id = 1, Name = "Test" };

		// Act
		await repository.AddAsync(entity);

		// Assert
		Assert.Equal("user1", entity.Owner);
	}

	[Fact]
	public async Task AddAsync_ShouldNotSetOwner_WhenUserIdNotSet()
	{
		// Arrange
		await using var connection = new SqliteConnection("DataSource=:memory:");
		await connection.OpenAsync();

		var options = new DbContextOptionsBuilder<TestDbContext>()
			.UseSqlite(connection)
			.Options;
		await using var context = new TestDbContext(options);
		await context.Database.EnsureCreatedAsync();

		var userAccessor = new NullUserAccessor();
		var repository = new EntityUserRepository<OwnedEntity, int, string>(context, userAccessor);
		var entity = new OwnedEntity { Id = 1, Name = "Test" };

		// Act
		await repository.AddAsync(entity);

		// Assert
		Assert.Null(entity.Owner);
	}

	[Fact]
	public void Constructor_ShouldThrow_WhenUserAccessorIsNull()
	{
		// Arrange
		var options = new DbContextOptionsBuilder<TestDbContext>()
			.UseSqlite("DataSource=:memory:")
			.Options;
		var context = new TestDbContext(options);

		// Act & Assert
		Assert.Throws<ArgumentNullException>(() => 
			new EntityUserRepository<OwnedEntity, int, string>(context, null!));
	}

	[Fact]
	public void EntityUserRepositoryNoKey_ShouldConstruct()
	{
		// Arrange
		var options = new DbContextOptionsBuilder<TestDbContext>()
			.UseSqlite("DataSource=:memory:")
			.Options;
		var context = new TestDbContext(options);
		var userAccessor = new FixedUserAccessor("user1");

		// Act
		var repository = new EntityUserRepository<OwnedEntity, string>(context, userAccessor);

		// Assert
		Assert.NotNull(repository);
	}

	private class TestDbContext : DbContext
	{
		public TestDbContext(DbContextOptions options) : base(options) { }

		public DbSet<OwnedEntity> OwnedEntities { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<OwnedEntity>(b =>
			{
				b.HasKey(e => e.Id);
			});
		}
	}

	private class OwnedEntity : IHaveOwner<string>
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public string? Owner { get; set; }

		public void SetOwner(string owner) => Owner = owner;
	}

	private class FixedUserAccessor : IUserAccessor<string>
	{
		private readonly string _userId;
		public FixedUserAccessor(string userId) => _userId = userId;
		public string? GetUserId() => _userId;
	}

	private class NullUserAccessor : IUserAccessor<string>
	{
		public string? GetUserId() => null;
	}
}
