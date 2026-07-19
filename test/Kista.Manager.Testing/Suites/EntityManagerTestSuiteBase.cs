using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Xunit;

namespace Kista;

/// <summary>
/// Shared base class for the two <see cref="EntityManagerTestSuite{,}"/>
/// and <see cref="EntityManagerTestSuite{,,}"/> test suites, hosting the
/// test bodies that are identical regardless of whether the entity manager
/// is keyed by a custom <typeparamref name="TKey"/> or by the default
/// <c>object</c> key.
/// </summary>
/// <typeparam name="TPerson">
/// The person entity type managed by the suite, constrained to the
/// <see cref="IPerson{TKey}"/> contract so that the shared test bodies
/// can access <see cref="IPerson{TKey}.Id"/>, <see cref="IPerson{TKey}.Email"/>,
/// <see cref="IHaveTimeStamp.CreatedAtUtc"/> and
/// <see cref="IHaveTimeStamp.UpdatedAtUtc"/> uniformly.
/// </typeparam>
/// <typeparam name="TKey">
/// The key type of <see cref="IPerson{TKey}"/> implemented by
/// <typeparamref name="TPerson"/>. Note that for the no-key suite
/// (<see cref="EntityManagerTestSuite{,}"/>) this is the key of the
/// <see cref="IPerson{TKey}"/> contract (e.g. <see cref="string"/>),
/// which may differ from the <c>object</c> key used by the manager;
/// the abstract hooks on this class bridge that gap by accepting
/// boxed <c>object</c> keys.
/// </typeparam>
public abstract class EntityManagerTestSuiteBase<TPerson, TKey>
	where TPerson : class, IPerson<TKey>, new()
	where TKey : notnull {
	private AsyncServiceScope scope;

	protected EntityManagerTestSuiteBase(ITestOutputHelper testOutput) {
		TestOutput = testOutput;

		CreateServices();
	}

	protected IServiceProvider Services => scope.ServiceProvider ?? throw new InvalidOperationException();

	protected ITestOutputHelper TestOutput { get; }

	protected IQueryable<TPerson> People => GetPageAsync(new PageRequest(1, int.MaxValue)).GetAwaiter().GetResult().Items.AsQueryable();

	protected ISystemTime TestTime { get; } = new TestSystemTime();

	protected abstract Faker<TPerson> PersonFaker { get; }

	/// <summary>
	/// Adds a single entity through the manager under test.
	/// </summary>
	protected abstract ValueTask<OperationResult> AddAsync(TPerson person);

	/// <summary>
	/// Adds a range of entities through the manager under test.
	/// </summary>
	protected abstract ValueTask<OperationResult> AddRangeAsync(IEnumerable<TPerson> people);

	/// <summary>
	/// Updates an entity through the manager under test.
	/// </summary>
	protected abstract ValueTask<OperationResult> UpdateAsync(TPerson person);

	/// <summary>
	/// Removes an entity through the manager under test.
	/// </summary>
	protected abstract ValueTask<OperationResult> RemoveAsync(TPerson person);

	/// <summary>
	/// Removes a range of entities through the manager under test.
	/// </summary>
	protected abstract ValueTask<OperationResult> RemoveRangeAsync(IEnumerable<TPerson> people);

	/// <summary>
	/// Finds an entity by its key through the manager under test,
	/// returning an <see cref="OperationResult{TPerson}"/>. The given
	/// <paramref name="key"/> is the boxed entity key (of type
	/// <typeparamref name="TKey"/>) as exposed by <see cref="IPerson{TKey}.Id"/>.
	/// </summary>
	protected abstract ValueTask<OperationResult<TPerson>> FindByKeyAsync(object key);

	/// <summary>
	/// Finds an entity by its key directly in the underlying repository,
	/// returning the raw entity or <c>null</c>. The given <paramref name="key"/>
	/// is the boxed entity key (of type <typeparamref name="TKey"/>) as
	/// exposed by <see cref="IPerson{TKey}.Id"/>.
	/// </summary>
	protected abstract ValueTask<TPerson?> FindInRepositoryAsync(object key);

	/// <summary>
	/// Adds a range of entities directly to the underlying repository,
	/// bypassing the manager (used to seed the suite).
	/// </summary>
	protected abstract ValueTask AddRangeToRepositoryAsync(IEnumerable<TPerson> people);

	/// <summary>
	/// Returns a page of entities from the underlying repository.
	/// </summary>
	protected abstract ValueTask<PageResult<TPerson>> GetPageAsync(PageRequest request);

	/// <summary>
	/// Returns a filtered/sorted page of entities from the underlying repository.
	/// </summary>
	protected abstract ValueTask<PageResult<TPerson>> GetPageAsync(PageQuery<TPerson> query);

	/// <summary>
	/// Produces a person carrying a key that is guaranteed not to exist in
	/// the repository, used by the not-found update/remove tests.
	/// </summary>
	protected abstract TPerson CreatePersonWithUnknownKey();

	/// <summary>
	/// Generates a key value that is guaranteed not to match any entity in
	/// the repository, used by the find-by-not-found-key test.
	/// </summary>
	protected abstract object GenerateUnknownKey();

	private void CreateServices() {
		var services = new ServiceCollection();

		services.AddLogging(logging => logging.AddXUnit(TestOutput));
		services.AddSingleton<IOperationCancellationSource>(new TestCancellationTokenSource());
		services.AddSystemTime(TestTime);
		services.AddOperationErrorFactory<TPerson, PersonErrorFactory>();

		ConfigureServices(services);

		scope = services.BuildServiceProvider().CreateAsyncScope();
	}

	protected virtual void ConfigureServices(IServiceCollection services) {
	}

	/// <summary>
	/// Seeds the underlying repository with the initial set of persons.
	/// Called by the suite's <c>InitializeAsync</c> implementation before
	/// any test runs.
	/// </summary>
	protected virtual async ValueTask InitializeCoreAsync() {
		var people = PersonFaker.Generate(100);
		await AddRangeToRepositoryAsync(people);
	}

	/// <summary>
	/// Disposes the DI scope created for this suite instance. Called by
	/// the suite's <c>DisposeAsync</c> implementation after all tests have run.
	/// </summary>
	protected virtual async ValueTask DisposeCoreAsync() {
		await scope.DisposeAsync();
		(Services as IDisposable)?.Dispose();
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Application")]
	[Trait("Feature", "Manager")]
	public async Task Should_AddEntity_When_EntityIsValid() {
		// Arrange
		var person = PersonFaker.Generate();

		// Act
		var result = await AddAsync(person);

		// Assert
		Assert.True(result.IsSuccess());
		Assert.NotNull(person.Id);
		Assert.NotNull(person.CreatedAtUtc);
		Assert.Null(person.UpdatedAtUtc);
		Assert.Equal(TestTime.UtcNow, person.CreatedAtUtc.Value);

		var found = await FindInRepositoryAsync(person.Id!);
		Assert.NotNull(found);
		Assert.Equal(person.Id, found.Id);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Application")]
	[Trait("Feature", "Manager")]
	public async Task Should_ReturnValidationError_When_EmailIsInvalid() {
		// Arrange
		var person = PersonFaker.Generate();
		person.Email = "invalid";

		// Act
		var result = await AddAsync(person);

		// Assert
		Assert.True(result.HasValidationErrors());
		Assert.NotNull(result.Error);

		var validationError = Assert.IsAssignableFrom<IValidationError>(result.Error);
		Assert.NotNull(validationError);
		Assert.Single(validationError.ValidationResults);
		Assert.Equal(nameof(Person.Email), validationError.ValidationResults[0].MemberNames.First());
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Application")]
	[Trait("Feature", "Manager")]
	public async Task Should_AddRangeOfEntities_When_EntitiesAreValid() {
		// Arrange
		var people = PersonFaker.Generate(10);
		var peopleCount = People.Count();

		// Act
		var result = await AddRangeAsync(people);

		// Assert
		Assert.True(result.IsSuccess());
		Assert.Equal(peopleCount + 10, People.Count());

		var found = People.ToList();
		Assert.NotNull(found);
		Assert.Equal(peopleCount + 10, found.Count);

		foreach (var person in people) {
			Assert.NotNull(person.Id);
			Assert.Contains(found, x => x.Id?.Equals(person.Id) ?? false);
		}
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Application")]
	[Trait("Feature", "Manager")]
	public async Task Should_ReturnValidationError_When_RangeContainsInvalidEmail() {
		// Arrange
		var people = PersonFaker.Generate(10);
		people[Random.Shared.Next(0, 9)].Email = "invalid";

		// Act
		var result = await AddRangeAsync(people);

		// Assert
		Assert.True(result.HasValidationErrors());
		Assert.NotNull(result.Error);

		var validationError = Assert.IsAssignableFrom<IValidationError>(result.Error);
		Assert.NotNull(validationError);
		Assert.Single(validationError.ValidationResults);

		var validationResult = validationError.ValidationResults[0];
		Assert.NotNull(validationResult);
		Assert.Equal(nameof(Person.Email), validationResult.MemberNames.First());
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Application")]
	[Trait("Feature", "Manager")]
	public virtual async Task Should_UpdateEntity_When_EntityIsValid() {
		// Arrange
		var person = People.Random();
		Assert.NotNull(person);
		Assert.NotNull(person.Id);

		person.Email = new Bogus.Faker().Internet.Email();

		// Act
		var result = await UpdateAsync(person);

		// Assert
		Assert.False(result.HasValidationErrors());
		Assert.True(result.IsSuccess());
		Assert.NotNull(person.UpdatedAtUtc);
		Assert.Equal(TestTime.UtcNow, person.UpdatedAtUtc.Value);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Application")]
	[Trait("Feature", "Manager")]
	public virtual async Task Should_ReturnUnchanged_When_UpdateWithNoChanges() {
		// Arrange
		var person = People.Random();
		Assert.NotNull(person);
		Assert.NotNull(person.Id);

		var toUpdate = await FindInRepositoryAsync(person.Id!);
		Assert.NotNull(toUpdate);

		// Act
		var result = await UpdateAsync(toUpdate);

		// Assert
		Assert.True(result.IsUnchanged());
		Assert.False(result.IsSuccess());
		Assert.Null(result.Error);
		Assert.Null(person.UpdatedAtUtc);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Application")]
	[Trait("Feature", "Manager")]
	public async Task Should_ReturnNotFoundError_When_UpdateEntityNotFound() {
		// Arrange
		var person = CreatePersonWithUnknownKey();

		// Act
		var result = await UpdateAsync(person);

		// Assert
		Assert.True(result.IsError());
		Assert.False(result.IsSuccess());
		Assert.NotNull(result.Error);
		Assert.Equal(PersonErrorCodes.NotFound, result.Error.Code);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Application")]
	[Trait("Feature", "Manager")]
	public async Task Should_ReturnValidationError_When_UpdateEntityHasNoKey() {
		// Arrange
		var person = PersonFaker
			.RuleFor(x => x.Id, f => default)
			.Generate();

		// Act
		var result = await UpdateAsync(person);

		// Assert
		Assert.True(result.IsError());
		Assert.False(result.IsSuccess());
		Assert.NotNull(result.Error);
		Assert.Equal(PersonErrorCodes.NotValid, result.Error.Code);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Application")]
	[Trait("Feature", "Manager")]
	public async Task Should_RemoveEntity_When_EntityExists() {
		// Arrange
		var person = People.Random()!;

		// Act
		var result = await RemoveAsync(person);

		// Assert
		Assert.True(result.IsSuccess());
		Assert.False(result.IsError());
		Assert.Null(result.Error);

		var found = await FindInRepositoryAsync(person.Id!);
		Assert.Null(found);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Application")]
	[Trait("Feature", "Manager")]
	public async Task Should_ReturnNotFoundError_When_RemoveEntityNotFound() {
		// Arrange
		var person = CreatePersonWithUnknownKey();

		// Act
		var result = await RemoveAsync(person);

		// Assert
		Assert.True(result.IsError());
		Assert.False(result.IsSuccess());
		Assert.NotNull(result.Error);
		Assert.Equal(PersonErrorCodes.NotFound, result.Error.Code);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Application")]
	[Trait("Feature", "Manager")]
	public async Task Should_ReturnValidationError_When_RemoveEntityHasNoKey() {
		// Arrange
		var person = PersonFaker.Generate();

		// Act
		var result = await RemoveAsync(person);

		// Assert
		Assert.True(result.IsError());
		Assert.False(result.IsSuccess());
		Assert.NotNull(result.Error);
		Assert.Equal(PersonErrorCodes.NotValid, result.Error.Code);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Application")]
	[Trait("Feature", "Manager")]
	public async Task Should_RemoveRange_When_EntitiesExist() {
		// Arrange
		var peopleCount = People.Count();
		var people = People
			.Where(x => x.FirstName.StartsWith("A"))
			.ToList();

		// Act
		var result = await RemoveRangeAsync(people);

		// Assert
		Assert.True(result.IsSuccess());
		Assert.False(result.IsError());
		Assert.Null(result.Error);

		var found = People.ToList();
		Assert.NotNull(found);
		Assert.Equal(peopleCount - people.Count, found.Count);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Application")]
	[Trait("Feature", "Manager")]
	public async Task Should_ReturnEntity_When_FindByKey() {
		// Arrange
		var person = People.Random()!;

		// Act
		var found = await FindByKeyAsync(person.Id!);

		// Assert
		Assert.NotNull(found);
		Assert.True(found.IsSuccess());
		Assert.Equal(person.Id, found.Value!.Id);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Application")]
	[Trait("Feature", "Manager")]
	public async Task Should_ReturnNull_When_FindByKeyNotFound() {
		// Arrange
		var personId = GenerateUnknownKey();

		// Act
		var found = await FindByKeyAsync(personId);

		// Assert
		Assert.NotNull(found);
		Assert.True(found.IsError());
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Application")]
	[Trait("Feature", "Manager")]
	public async Task Should_ReturnPage_When_NoFilterApplied() {
		// Arrange
		var totalPeople = People.Count();
		var totalPages = (int)Math.Ceiling((double)totalPeople / 10);
		var perPage = Math.Min(10, totalPeople);

		var query = new PageQuery<TPerson>(1, 10);

		// Act
		var page = (PageQueryResult<TPerson>) await GetPageAsync(query);

		// Assert
		Assert.NotNull(page);
		Assert.Equal(1, page.Request.Page);
		Assert.Equal(10, page.Request.Size);
		Assert.Equal(totalPages, page.TotalPages);
		Assert.Equal(totalPeople, page.TotalItems);
		Assert.NotNull(page.Items);
		Assert.Equal(perPage, page.Items.Count);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Application")]
	[Trait("Feature", "Manager")]
	public async Task Should_ReturnFilteredPage_When_DynamicLinqFilterApplied() {
		// Arrange
		var totalPeople = People.Count(x => x.FirstName.StartsWith("A"));
		var totalPages = (int)Math.Ceiling((double)totalPeople / 10);
		var perPage = Math.Min(10, totalPeople);

		var query = new PageQuery<TPerson>(1, 10)
			.Where("FirstName.StartsWith(\"A\")");

		// Act
		var page = (PageQueryResult<TPerson>) await GetPageAsync(query);

		// Assert
		Assert.NotNull(page);
		Assert.Equal(1, page.Request.Page);
		Assert.Equal(10, page.Request.Size);
		Assert.Equal(totalPages, page.TotalPages);
		Assert.Equal(totalPeople, page.TotalItems);
		Assert.NotNull(page.Items);
		Assert.Equal(perPage, page.Items.Count);
	}

	private class TestSystemTime : ISystemTime {
		public TestSystemTime() {
			UtcNow = DateTimeOffset.UtcNow;
			Now = DateTimeOffset.Now;
		}

		public DateTimeOffset UtcNow { get; }

		public DateTimeOffset Now { get; }
	}
}