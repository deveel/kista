using Kista.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Kista;

/// <summary>
/// Integration tests for <see cref="Repository{TEntity,TKey}"/> when
/// hosted on top of an Entity Framework Core provider. The tests verify that
/// the new translation pipeline binds <see cref="IQuery"/> and
/// <see cref="PageQuery{TEntity}"/> parameters correctly through the protected
/// <c>Queryable()</c> hatch without breaking the underlying ORM provider's
/// change-tracking behaviour.
/// </summary>
[Collection(nameof(SqlConnectionCollection))]
[Trait("Category", "Integration")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "Repository")]
public class RepositoryBaseEfTests {
	private readonly SqlTestConnection sql;

	public RepositoryBaseEfTests(SqlTestConnection sql) {
		this.sql = sql;
	}

	#region Tracking

	[Fact]
	public async Task GetPageAsync_Default_TracksEntities() {
		// Arrange
		await using var fixture = await EfFixture.CreateAsync(sql);
		var repo = fixture.Repository;

		// Act
		var result = await repo.PublicQueryPageAsync(new PageQuery<DbPerson>(1, 10), TestContext.Current.CancellationToken);

		// Assert
		Assert.NotNull(result);
		Assert.NotEmpty(result.Items!);
		// The default EF tracking behaviour must remain untouched: the returned
		// entities are still attached to the change tracker.
		foreach (var person in result.Items!) {
			var state = fixture.Context.Entry(person).State;
			Assert.Equal(EntityState.Unchanged, state);
		}
	}

	[Fact]
	public async Task GetPageAsync_AsNoTrackingInQueryHatch_DoesNotAttach() {
		// Arrange — this fixture's repository overrides Query() to apply
		// AsNoTracking() and we verify that the change tracker does not pick
		// up the returned entities.
		await using var fixture = await NoTrackingEfFixture.CreateAsync(sql);
		var repo = fixture.Repository;

		// Act
		var result = await repo.PublicQueryPageAsync(new PageQuery<DbPerson>(1, 10), TestContext.Current.CancellationToken);

		// Assert
		Assert.NotNull(result);
		Assert.NotEmpty(result.Items!);
		foreach (var person in result.Items!) {
			var state = fixture.Context.Entry(person).State;
			Assert.Equal(EntityState.Detached, state);
		}
	}

	#endregion

	#region Query translation

	[Fact]
	public async Task GetPageAsync_AppliesFilterAndOrder() {
		await using var fixture = await EfFixture.CreateAsync(sql);
		var repo = fixture.Repository;

		var request = new PageQuery<DbPerson>(1, 5)
			.Where(p => p.FirstName!.StartsWith("A"))
			.OrderBy(p => p.LastName);

		var result = await repo.PublicQueryPageAsync(request, TestContext.Current.CancellationToken);

		Assert.NotNull(result);
		Assert.NotNull(result.Items);
		Assert.All(result.Items!, p => Assert.StartsWith("A", p.FirstName));

		// Verify the order is applied to the page slice.
		for (var i = 1; i < result.Items!.Count; i++) {
			Assert.True(string.CompareOrdinal(result.Items[i - 1].LastName, result.Items[i].LastName) <= 0,
				$"Items at index {i - 1} and {i} are not in ascending LastName order.");
		}
	}

	[Fact]
	public async Task GetPageAsync_TotalCount_ReflectsFilter() {
		await using var fixture = await EfFixture.CreateAsync(sql);
		var repo = fixture.Repository;

		var aliceCount = fixture.Seed.Count(p => p.FirstName == "Alice");
		var request = new PageQuery<DbPerson>(1, 100)
			.Where(p => p.FirstName == "Alice");

		var result = await repo.PublicQueryPageAsync(request, TestContext.Current.CancellationToken);

		Assert.Equal(aliceCount, result.TotalItems);
	}

	#endregion

	#region FindAsync(IQuery)

	[Fact]
	public async Task FindAsync_Filter_ReturnsMatchingSet() {
		await using var fixture = await EfFixture.CreateAsync(sql);
		var repo = fixture.Repository;

		var query = Query.Where<DbPerson>(p => p.FirstName == "Alice");
		var result = await repo.PublicFindAsync(query, TestContext.Current.CancellationToken);

		Assert.NotEmpty(result);
		Assert.All(result, p => Assert.Equal("Alice", p.FirstName));
	}

	#endregion

	#region Fixtures

	/// <summary>
	/// A thin <see cref="Repository{TEntity,TKey}"/> that
	/// implements the abstract <c>Queryable()</c> hatch by returning the EF
	/// <see cref="DbSet{TEntity}"/> as <see cref="IQueryable{T}"/>.
	/// </summary>
	private class TestEfRepository : Repository<DbPerson, Guid> {
		private readonly PersonDbContext context;

		public TestEfRepository(PersonDbContext context) {
			this.context = context;
		}

		public PersonDbContext Context => context;

		protected override IServiceProvider? Services => null;

		protected override Guid GetEntityKey(DbPerson entity) {
			ArgumentNullException.ThrowIfNull(entity);
			return entity.Id;
		}

		protected override IQueryable<DbPerson> Queryable() => context.Set<DbPerson>();

		protected override bool IsQueryable => true;

		public override async ValueTask AddAsync(DbPerson entity, CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(entity);
			context.Set<DbPerson>().Add(entity);
			await context.SaveChangesAsync(cancellationToken);
		}

		public override async ValueTask AddRangeAsync(IEnumerable<DbPerson> entities, CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(entities);
			context.Set<DbPerson>().AddRange(entities);
			await context.SaveChangesAsync(cancellationToken);
		}

		public override ValueTask<bool> UpdateAsync(DbPerson entity, CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(entity);
			context.Set<DbPerson>().Update(entity);
			return new ValueTask<bool>(context.SaveChangesAsync(cancellationToken).ContinueWith(t => t.Result > 0, cancellationToken));
		}

		public override ValueTask<bool> RemoveAsync(DbPerson entity, CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(entity);
			context.Set<DbPerson>().Remove(entity);
			return new ValueTask<bool>(context.SaveChangesAsync(cancellationToken).ContinueWith(t => t.Result > 0, cancellationToken));
		}

		public override async ValueTask RemoveRangeAsync(IEnumerable<DbPerson> entities, CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(entities);
			context.Set<DbPerson>().RemoveRange(entities);
			await context.SaveChangesAsync(cancellationToken);
		}

		public override async ValueTask<DbPerson?> FindAsync(Guid key, CancellationToken cancellationToken = default) {
			ArgumentNullException.ThrowIfNull(key);
			return await context.Set<DbPerson>().FindAsync(new object?[] { key }, cancellationToken);
		}

		/// <summary>
		/// Public wrapper around the protected <c>QueryPageAsync</c> from the base
		/// class so that tests can exercise the pageable query pipeline.
		/// </summary>
		public ValueTask<PageQueryResult<DbPerson>> PublicQueryPageAsync(PageQuery<DbPerson> request, CancellationToken ct = default) =>
			base.QueryPageAsync(request, ct);

		/// <summary>
		/// Public wrapper around the protected <c>FindAsync(IQuery)</c> from the
		/// base class so that tests can exercise the filtered query pipeline.
		/// </summary>
		public ValueTask<IReadOnlyList<DbPerson>> PublicFindAsync(IQuery query, CancellationToken ct = default) =>
			base.FindAsync(query, ct);
	}

	/// <summary>
	/// Variant that disables EF change tracking by applying
	/// <c>AsNoTracking()</c> inside the <c>Queryable()</c> hatch. This is the
	/// pattern engine authors should follow when they need read-only
	/// materialisation.
	/// </summary>
	private sealed class NoTrackingEfRepository : TestEfRepository {
		public NoTrackingEfRepository(PersonDbContext context) : base(context) { }

		protected override IQueryable<DbPerson> Queryable() => Context.Set<DbPerson>().AsNoTracking();
	}

	private sealed class EfFixture : IAsyncDisposable {
		public PersonDbContext Context { get; }
		public TestEfRepository Repository { get; }
		public IList<DbPerson> Seed { get; }

		private EfFixture(PersonDbContext context, TestEfRepository repository, IList<DbPerson> seed) {
			Context = context;
			Repository = repository;
			Seed = seed;
		}

		public static async Task<EfFixture> CreateAsync(SqlTestConnection sql) {
			var options = new DbContextOptionsBuilder<PersonDbContext>()
				.UseSqlite(sql.Connection, sqlite => {
					if (sql.SpatialiteAvailable)
						sqlite.UseNetTopologySuite();
				})
				.Options;

			if (!sql.SpatialiteAvailable) {
				// Rebuild options with NonSpatialModelCustomizer
				var builder = new DbContextOptionsBuilder<PersonDbContext>()
					.UseSqlite(sql.Connection);
				builder.ReplaceService<IModelCustomizer, NonSpatialModelCustomizer>();
				options = builder.Options;
			}

			var context = new PersonDbContext(options);
			await context.Database.EnsureDeletedAsync();
			await context.Database.EnsureCreatedAsync();

			var seed = new List<DbPerson>();
			for (var i = 0; i < 12; i++) {
				seed.Add(new DbPerson {
					Id = Guid.NewGuid(),
					FirstName = i % 3 == 0 ? "Alice" : $"First{i}",
					LastName = $"Last{i:D2}",
				});
			}
			context.People!.AddRange(seed);
			await context.SaveChangesAsync();

			// Detach the seed so the change tracker starts empty before the
			// tested repository's GetPageAsync runs.
			foreach (var entry in context.ChangeTracker.Entries<DbPerson>().ToList()) {
				entry.State = EntityState.Detached;
			}

			return new EfFixture(context, new TestEfRepository(context), seed);
		}

		public async ValueTask DisposeAsync() {
			await Context.DisposeAsync();
		}
	}

	private sealed class NoTrackingEfFixture : IAsyncDisposable {
		public PersonDbContext Context { get; }
		public TestEfRepository Repository { get; }
		public IList<DbPerson> Seed { get; }

		private NoTrackingEfFixture(PersonDbContext context, TestEfRepository repository, IList<DbPerson> seed) {
			Context = context;
			Repository = repository;
			Seed = seed;
		}

		public static async Task<NoTrackingEfFixture> CreateAsync(SqlTestConnection sql) {
			var options = new DbContextOptionsBuilder<PersonDbContext>()
				.UseSqlite(sql.Connection, sqlite => {
					if (sql.SpatialiteAvailable)
						sqlite.UseNetTopologySuite();
				})
				.Options;

			if (!sql.SpatialiteAvailable) {
				var builder = new DbContextOptionsBuilder<PersonDbContext>()
					.UseSqlite(sql.Connection);
				builder.ReplaceService<IModelCustomizer, NonSpatialModelCustomizer>();
				options = builder.Options;
			}

			var context = new PersonDbContext(options);
			await context.Database.EnsureDeletedAsync();
			await context.Database.EnsureCreatedAsync();

			var seed = new List<DbPerson>();
			for (var i = 0; i < 8; i++) {
				seed.Add(new DbPerson {
					Id = Guid.NewGuid(),
					FirstName = i % 2 == 0 ? "Alice" : $"First{i}",
					LastName = $"Last{i:D2}",
				});
			}
			context.People!.AddRange(seed);
			await context.SaveChangesAsync();

			foreach (var entry in context.ChangeTracker.Entries<DbPerson>().ToList()) {
				entry.State = EntityState.Detached;
			}

			return new NoTrackingEfFixture(context, new NoTrackingEfRepository(context), seed);
		}

		public async ValueTask DisposeAsync() {
			await Context.DisposeAsync();
		}
	}

	#endregion
}
