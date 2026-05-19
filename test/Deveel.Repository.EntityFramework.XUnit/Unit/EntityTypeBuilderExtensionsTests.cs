using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Deveel.Data;

[Trait("Category", "Unit")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "EntityTypeBuilderExtensions")]
public class EntityTypeBuilderExtensionsTests
{
	[Fact]
	public async Task HasOwnerFilter_WithExplicitPropertyName_ShouldConfigureQueryFilter()
	{
		// Arrange
		await using var connection = new SqliteConnection("DataSource=:memory:");
		await connection.OpenAsync();

		var options = new DbContextOptionsBuilder<TestDbContext>()
			.UseSqlite(connection)
			.Options;

		var userAccessor = new FixedUserAccessor("user1");

		// Act
		await using var context = new TestDbContext(options, userAccessor, "UserId");
		await context.Database.EnsureCreatedAsync();
		var model = context.Model;
		var entityType = model.FindEntityType(typeof(OwnedEntityWithExplicitProperty))!;
		var queryFilter = entityType.GetQueryFilter();

		// Assert
		Assert.NotNull(queryFilter);
	}

	[Fact]
	public async Task HasOwnerFilter_WithDataOwnerAttribute_ShouldConfigureQueryFilter()
	{
		// Arrange
		await using var connection = new SqliteConnection("DataSource=:memory:");
		await connection.OpenAsync();

		var options = new DbContextOptionsBuilder<TestDbContext2>()
			.UseSqlite(connection)
			.Options;

		var userAccessor = new FixedUserAccessor("user1");

		// Act
		await using var context = new TestDbContext2(options, userAccessor);
		await context.Database.EnsureCreatedAsync();
		var model = context.Model;
		var entityType = model.FindEntityType(typeof(OwnedEntityWithAttribute))!;
		var queryFilter = entityType.GetQueryFilter();

		// Assert
		Assert.NotNull(queryFilter);
	}

	private class FixedUserAccessor : IUserAccessor<string>
	{
		private readonly string _userId;
		public FixedUserAccessor(string userId) => _userId = userId;
		public string? GetUserId() => _userId;
	}

	private class OwnedEntityWithExplicitProperty : IHaveOwner<string>
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public string Owner { get; set; } = string.Empty;
		public string? UserId { get; set; }

		public void SetOwner(string owner) => Owner = owner;
	}

	private class OwnedEntityWithAttribute : IHaveOwner<string>
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public string Owner { get; set; } = string.Empty;

		[DataOwner]
		public string? OwnerId { get; set; }

		public void SetOwner(string owner) => Owner = owner;
	}

	private class TestDbContext : DbContext
	{
		private readonly IUserAccessor<string> _userAccessor;
		private readonly string _propertyName;

		public TestDbContext(DbContextOptions options, IUserAccessor<string> userAccessor, string propertyName) : base(options)
		{
			_userAccessor = userAccessor;
			_propertyName = propertyName;
		}

		public DbSet<OwnedEntityWithExplicitProperty> Entities { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<OwnedEntityWithExplicitProperty>(b =>
			{
				b.HasKey(e => e.Id);
				b.HasOwnerFilter(_propertyName, _userAccessor);
			});
		}
	}

	private class TestDbContext2 : DbContext
	{
		private readonly IUserAccessor<string> _userAccessor;

		public TestDbContext2(DbContextOptions options, IUserAccessor<string> userAccessor) : base(options)
		{
			_userAccessor = userAccessor;
		}

		public DbSet<OwnedEntityWithAttribute> Entities { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<OwnedEntityWithAttribute>(b =>
			{
				b.HasKey(e => e.Id);
				b.HasOwnerFilter("", _userAccessor);
			});
		}
	}
}
