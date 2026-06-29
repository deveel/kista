using Bogus;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using System.Collections.Immutable;
using System.Linq.Expressions;

namespace Kista;

[Trait("Feature", "OwnerFilter")]
public abstract class UserRepositoryTestSuite<TBook, TKey, TUserKey> : IAsyncLifetime, IAsyncDisposable
	where TBook : class, IBook<TKey>, IHaveOwner<TUserKey>
	where TKey : notnull {
	private IServiceProvider? rootServiceProvider;
	private AsyncServiceScope scope;

	protected UserRepositoryTestSuite(ITestOutputHelper? outputHelper) {
		TestOutput = outputHelper;
#pragma warning disable S1699 // Abstract method call in constructor is intentional for test suite initialization
		UserId = GenerateUserId();
#pragma warning restore S1699
	}

	protected ITestOutputHelper? TestOutput { get; }

	protected TUserKey UserId { get; }

	protected virtual int EntitySetCount => 100;

	protected IReadOnlyList<TBook>? Books { get; private set; } = new List<TBook>();

	protected IServiceProvider Services => scope.ServiceProvider;

	protected virtual IRepository<TBook, TKey> Repository { get; private set; } = null!;

	protected abstract Faker<TBook> BookFaker { get; }

	protected TBook GenerateBook() => BookFaker.Generate();

	protected TBook GenerateUserBook() {
		var book = BookFaker.Generate();
		book.SetOwner(UserId);
		return book;
	}

	protected IList<TBook> GenerateBooks(int count) => BookFaker.Generate(count);

	protected ISystemTime TestTime { get; } = new TestTime();

	protected abstract TUserKey GenerateUserId();

	protected abstract TKey GenerateBookId();

	protected virtual void ConfigureServices(IServiceCollection services) {
		if (TestOutput != null)
			services.AddLogging(logging => { logging.ClearProviders(); logging.AddXUnit(TestOutput); });

		services.AddSingleton<IUserAccessor<TUserKey>>(new StaticUserAccessor(this));
	}

	protected virtual Task<IRepository<TBook, TKey>> GetRepositoryAsync() {
		return Task.FromResult(Services.GetRequiredService<IRepository<TBook, TKey>>());
	}

	private void BuildServices() {
		var services = new ServiceCollection();
		services.AddSystemTime(TestTime);

		ConfigureServices(services);

		rootServiceProvider = services.BuildServiceProvider();
		scope = rootServiceProvider.CreateAsyncScope();
	}

	async ValueTask IAsyncLifetime.InitializeAsync() {
		BuildServices();

		Books = GenerateBooks(EntitySetCount).ToImmutableList();
		Repository = await GetRepositoryAsync();

		await InitializeAsync();
	}

	protected virtual async ValueTask InitializeAsync() {
		await SeedAsync(Repository);
	}

	async ValueTask IAsyncDisposable.DisposeAsync() {
		await DisposeAsync();

		Books = null;

		await scope.DisposeAsync();
		(rootServiceProvider as IDisposable)?.Dispose();
	}

	protected virtual ValueTask DisposeAsync() {
		return ValueTask.CompletedTask;
	}

	protected virtual async ValueTask SeedAsync(IRepository<TBook, TKey> repository) {
		if (Books != null)
			await repository.AddRangeAsync(Books);
	}

	protected virtual Task<TBook?> FindBookAsync(object id) {
		var entity = Books?.FirstOrDefault(x => Repository.GetEntityKey(x)?.Equals(id) ?? false);
		return Task.FromResult(entity);
	}

	protected virtual Task<TBook> RandomBookAsync(Expression<Func<TBook, bool>>? predicate = null) {
		var result = Books?.Random(predicate?.Compile());

		if (result == null)
			throw new InvalidOperationException("No book found");

		return Task.FromResult(result);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "OwnerFilter")]
	public async Task Should_AddBook_When_BookBelongsToUser() {
		// Arrange
		var book = GenerateUserBook();

		// Act
		await Repository.AddAsync(book, TestContext.Current.CancellationToken);

		// Assert
		var id = Repository.GetEntityKey(book);
		Assert.NotNull(id);

		var found = await Repository.FindAsync(id, TestContext.Current.CancellationToken);
		Assert.NotNull(found);
		Assert.Equal(book.Title, found.Title);
		Assert.Equal(book.Author, found.Author);
		Assert.Equal(book.Synopsis, found.Synopsis);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "OwnerFilter")]
	public async Task Should_AddBook_When_BookIsNew() {
		// Arrange
		var book = GenerateBook();

		// Act
		await Repository.AddAsync(book, TestContext.Current.CancellationToken);

		// Assert
		var id = Repository.GetEntityKey(book);
		Assert.NotNull(id);

		var found = await Repository.FindAsync(id, TestContext.Current.CancellationToken);
		Assert.NotNull(found);
		Assert.Equal(book.Title, found.Title);
		Assert.Equal(book.Author, found.Author);
		Assert.Equal(book.Synopsis, found.Synopsis);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "OwnerFilter")]
	public async Task Should_SetCurrentUserAsOwner_When_BookHasNoOwner() {
		// Arrange
		var book = GenerateBook();
		book.SetOwner(default!);

		// Act
		await Repository.AddAsync(book, TestContext.Current.CancellationToken);

		// Assert
		var id = Repository.GetEntityKey(book);
		Assert.NotNull(id);

		var found = await Repository.FindAsync(id, TestContext.Current.CancellationToken);
		Assert.NotNull(found);
		Assert.Equal(book.Title, found.Title);
		Assert.Equal(book.Author, found.Author);
		Assert.Equal(book.Synopsis, found.Synopsis);
		Assert.Equal(UserId, found.Owner);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "OwnerFilter")]
	public async Task Should_ReturnBook_When_BookBelongsToUser() {
		// Arrange
		var book = await RandomBookAsync(x => Equals(x.Owner, UserId));

		// Act
		var found = await Repository.FindAsync(book.Id!, TestContext.Current.CancellationToken);

		// Assert
		Assert.NotNull(found);
		Assert.Equal(UserId, found.Owner);
		Assert.Equal(book.Title, found.Title);
		Assert.Equal(book.Author, found.Author);
		Assert.Equal(book.Synopsis, found.Synopsis);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "OwnerFilter")]
	public async Task Should_ReturnNull_When_BookNotFound() {
		// Arrange
		var id = GenerateBookId();

		// Act
		var found = await Repository.FindAsync(id, TestContext.Current.CancellationToken);

		// Assert
		Assert.Null(found);
	}

	private class StaticUserAccessor : IUserAccessor<TUserKey> {
		private readonly UserRepositoryTestSuite<TBook, TKey, TUserKey> suite;

		public StaticUserAccessor(UserRepositoryTestSuite<TBook, TKey, TUserKey> suite) {
			this.suite = suite;
		}

		public TUserKey GetUserId() => suite.UserId;
	}
}
