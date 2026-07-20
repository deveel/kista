using Bogus;

using Microsoft.Extensions.DependencyInjection;

using System.Linq.Expressions;

namespace Kista;

public abstract class RepositoryTestSuite<TPerson, TKey> : RepositoryTestSuiteBase<TPerson>
	where TPerson : class, IPerson<TKey>
	where TKey : notnull {
	private IRepository<TPerson, TKey>? _repository;

	protected RepositoryTestSuite(ITestOutputHelper? testOutput) : base(testOutput) {
	}

	protected abstract override Faker<TPerson> PersonFaker { get; }

	protected abstract TKey GeneratePersonId();

	/// <summary>
	/// The strongly-keyed repository under test, exposed for T3-specific
	/// tests and for derived suites (e.g. SoftDelete) that need the
	/// typed key.
	/// </summary>
	protected virtual IRepository<TPerson, TKey> Repository {
		get {
			if (_repository == null)
				_repository = Services.GetRequiredService<IRepository<TPerson, TKey>>();

			return _repository;
		}
	}

	protected override object GeneratePersonKey() => GeneratePersonId();

	protected override void AssignPersonKey(TPerson person, object key) {
		person.Id = (TKey)key;
	}

	protected override object? GetPersonKey(TPerson person) => person.Id;

	protected override string GetFirstName(TPerson person) => person.FirstName;
	protected override string GetLastName(TPerson person) => person.LastName;
	protected override string? GetEmail(TPerson person) => person.Email;
	protected override DateTime? GetDateOfBirth(TPerson person) => person.DateOfBirth;
	protected override void SetFirstName(TPerson person, string firstName) => person.FirstName = firstName;

	protected override Expression<Func<TPerson, string>> FirstNameSelector => x => x.FirstName;
	protected override Expression<Func<TPerson, string>> LastNameSelector => x => x.LastName;
	protected override Expression<Func<TPerson, string?>> EmailSelector => x => x.Email;
	protected override Expression<Func<TPerson, DateTime?>> DateOfBirthSelector => x => x.DateOfBirth;

	protected override ValueTask AddAsync(TPerson entity, CancellationToken cancellationToken)
		=> Repository.AddAsync(entity, cancellationToken);

	protected override ValueTask AddRangeAsync(IEnumerable<TPerson> entities, CancellationToken cancellationToken)
		=> Repository.AddRangeAsync(entities, cancellationToken);

	protected override ValueTask<bool> RemoveAsync(TPerson entity, CancellationToken cancellationToken)
		=> Repository.RemoveAsync(entity, cancellationToken);

	protected override ValueTask RemoveRangeAsync(IEnumerable<TPerson> entities, CancellationToken cancellationToken)
		=> Repository.RemoveRangeAsync(entities, cancellationToken);

	protected override ValueTask<bool> RemoveByKeyAsync(object key, CancellationToken cancellationToken)
		=> Repository.RemoveByKeyAsync((TKey)key, cancellationToken);

	protected override ValueTask<TPerson?> FindAsync(object key, CancellationToken cancellationToken)
		=> Repository.FindAsync((TKey)key, cancellationToken);

	protected override ValueTask<bool> UpdateAsync(TPerson entity, CancellationToken cancellationToken)
		=> Repository.UpdateAsync(entity, cancellationToken);

	protected override object? GetEntityKey(TPerson entity) => Repository.GetEntityKey(entity);

	protected override ValueTask<PageResult<TPerson>> GetPageAsync(PageRequest request, CancellationToken cancellationToken)
		=> Repository.GetPageAsync(request, cancellationToken);

	protected override PageResult<TPerson> GetPage(PageRequest request) => Repository.GetPage(request);

	protected override async Task SeedAsync() {
		if (People != null)
			await Repository.AddRangeAsync(People);
	}

	private ITestRepository<TPerson, TKey> TestRepo => (ITestRepository<TPerson, TKey>)Repository;

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_AddPerson_When_CalledAsync() {
		// Arrange
		var person = GeneratePerson();

		// Act
		await Repository.AddAsync(person);

		// Assert
		var id = Repository.GetEntityKey(person);
		Assert.NotNull(id);

		var found = await Repository.FindAsync(id, TestContext.Current.CancellationToken);
		Assert.NotNull(found);
		Assert.Equal(person.FirstName, found.FirstName);
		Assert.Equal(person.LastName, found.LastName);
		Assert.Equal(person.Email, found.Email);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_RemovePerson_When_CalledAsync() {
		// Arrange
		var person = People!.Random();
		Assert.NotNull(person);

		// Act
		var result = await Repository.RemoveAsync(person);

		// Assert
		Assert.True(result);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_RemoveByKey_When_CalledAsync() {
		// Arrange
		var key = Repository.GetEntityKey(People!.Random()!);
		Assert.NotNull(key);

		// Act
		var result = await Repository.RemoveByKeyAsync(key);

		// Assert
		Assert.True(result);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public new async Task Should_ReturnTotalCount_When_CountAll() {
		// Act
		var result = await TestRepo.CountAsync(QueryFilter.Empty);

		// Assert
		Assert.NotEqual(0, result);
		Assert.Equal(PeopleCount, result);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public new async Task Should_ReturnFilteredCount_When_FilterApplied() {
		// Arrange
		var person = await RandomPersonAsync();
		var firstName = person.FirstName;
		var peopleCount = People?.Count(x => x.FirstName == firstName) ?? 0;

		// Act
		var count = await TestRepo.CountAsync(QueryFilter.Where<TPerson>(p => p.FirstName == firstName));

		// Assert
		Assert.Equal(peopleCount, count);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public new async Task Should_ReturnFirstMatch_When_FilterApplied() {
		// Arrange
		var person = await RandomPersonAsync();
		var firstName = person.FirstName;

		// Act
		var result = await TestRepo.FindFirstAsync(Query.Where<TPerson>(x => x.FirstName == firstName));

		// Assert
		Assert.NotNull(result);
		Assert.Equal(firstName, result.FirstName);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public new async Task Should_ReturnFirstMatchBySort_When_FilterAndSortApplied() {
		// Arrange
		var person = await RandomPersonAsync(x => x.LastName != null);
		var firstName = person.FirstName;

		var expected = People?.Where(x => x.FirstName == firstName)
			.OrderBy(x => x.LastName)
			.FirstOrDefault();

		Assert.NotNull(expected);

		var query = new QueryBuilder<TPerson>()
			.Where(x => x.FirstName == firstName)
			.OrderBy(x => x.LastName)
			.Query;

		// Act
		var result = await TestRepo.FindFirstAsync(query);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(expected.FirstName, result.FirstName);
		Assert.Equal(expected.LastName, result.LastName);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ReturnFirstPerson_When_FindFirstAsync() {
		// Act
		var result = await TestRepo.FindFirstAsync(Kista.Query.Empty);

		// Assert
		Assert.NotNull(result);
		Assert.NotNull(result.Id);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public new async Task Should_ReturnTrue_When_PersonExists() {
		// Arrange
		var person = await RandomPersonAsync();
		var firstName = person.FirstName;

		// Act
		var result = await TestRepo.ExistsAsync(QueryFilter.Where<TPerson>(x => x.FirstName == firstName));

		// Assert
		Assert.True(result);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ReturnPerson_When_FindByKeySync() {
		// Arrange
		var person = await RandomPersonAsync();

		// Act
		var result = await Repository.FindAsync(person.Id!, TestContext.Current.CancellationToken);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(person.Id, result.Id);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ReturnFirstMatch_When_FilterAppliedSync() {
		// Arrange
		var person = await RandomPersonAsync(x => x.FirstName != null);
		var ordered = NaturalOrder(People!.Where(x => x.FirstName == person.FirstName)).ToList();

		// Act
		var result = await TestRepo.FindFirstAsync(new Query(QueryFilter.Where<TPerson>(x => x.FirstName == person.FirstName), null));

		// Assert
		Assert.NotNull(result);
		Assert.Equal(ordered[0].Id, result.Id);
		Assert.Equal(ordered[0].FirstName, result.FirstName);
		Assert.Equal(ordered[0].LastName, result.LastName);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public new async Task Should_ReturnAllPeople_When_FindAll() {
		// Act
		var result = await TestRepo.FindAllAsync(Kista.Query.Empty);

		// Assert
		Assert.NotNull(result);
		Assert.NotEmpty(result);
		Assert.Equal(PeopleCount, result.Count);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public new async Task Should_ReturnFilteredPeople_When_FilterApplied() {
		// Arrange
		var person = await RandomPersonAsync();
		var firstName = person.FirstName;
		var peopleCount = People?.Count(x => x.FirstName == firstName) ?? 0;

		// Act
		var result = await TestRepo.FindAllAsync(new Query(QueryFilter.Where<TPerson>(x => x.FirstName == firstName), null));

		// Assert
		Assert.NotNull(result);
		Assert.NotEmpty(result);
		Assert.Equal(peopleCount, result.Count);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public new async Task Should_ReturnFilteredAndSortedPeople_When_FilterAndSortApplied() {
		// Arrange
		var person = await RandomPersonAsync();
		var firstName = person.FirstName;

		var expected = People?.Where(x => x.FirstName == firstName)
			.OrderBy(x => x.FirstName)
			.ToList();

		Assert.NotNull(expected);

		var query = new QueryBuilder<TPerson>()
			.Where(x => x.FirstName == firstName)
			.OrderBy(x => x.FirstName);

		// Act
		var result = await TestRepo.FindAllAsync(query.Query);

		// Assert
		Assert.NotNull(result);
		Assert.NotEmpty(result);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_UpdatePerson_When_CalledAsync() {
		// Arrange
		var person = await RandomPersonAsync(x => x.FirstName != "John");
		var toUpdate = await Repository.FindAsync(person.Id!, TestContext.Current.CancellationToken);
		Assert.NotNull(toUpdate);
		toUpdate.FirstName = "John";

		// Act
		var result = await Repository.UpdateAsync(toUpdate);

		// Assert
		Assert.True(result);

		var updated = await Repository.FindAsync(person.Id!, TestContext.Current.CancellationToken);
		Assert.NotNull(updated);
		Assert.Equal(toUpdate.FirstName, updated.FirstName);
		Assert.Equal(toUpdate.LastName, updated.LastName);
		Assert.Equal(toUpdate.Email, updated.Email);
		Assert.Equal(toUpdate.DateOfBirth, updated.DateOfBirth);
	}
}

/// <summary>
/// Extends <see cref="RepositoryTestSuite{TPerson, TKey}"/> with relationship-specific
/// fixtures and tests for entities whose <see cref="IPerson.Relationships"/> collection
/// is typed by <typeparamref name="TRelationship"/>.
/// </summary>
public abstract class RepositoryTestSuite<TPerson, TKey, TRelationship> : RepositoryTestSuite<TPerson, TKey>
	where TPerson : class, IPerson<TKey>
	where TKey : notnull
	where TRelationship : class, IRelationship {

	protected RepositoryTestSuite(ITestOutputHelper? testOutput) : base(testOutput) {
	}

	protected abstract Faker<TRelationship> RelationshipFaker { get; }

	protected TRelationship GenerateRelationship() => RelationshipFaker.Generate();

	protected abstract Task AddRelationshipAsync(TPerson person, TRelationship relationship);

	protected abstract Task RemoveRelationshipAsync(TPerson person, TRelationship relationship);

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ReturnPersonWithRelationships_When_KeyExists() {
		// Arrange
		var person = await RandomPersonAsync(x => x.Relationships != null && x.Relationships.Any());
		Assert.NotNull(person);

		// Act
		var result = await Repository.FindAsync(person.Id!, TestContext.Current.CancellationToken);

		// Assert
		Assert.NotNull(result);
		Assert.NotNull(result.Relationships);
		Assert.NotEmpty(result.Relationships);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_AddRelationship_When_UpdatePersonWithNewRelationship()
		=> await RelationshipTestCases.AddRelationshipOnUpdateAsync<TPerson, TKey, TRelationship>(
			People!,
			p => Repository.FindAsync(p.Id!, TestContext.Current.CancellationToken).AsTask(),
			AddRelationshipAsync,
			GenerateRelationship,
			toUpdate => Repository.UpdateAsync(toUpdate, TestContext.Current.CancellationToken).AsTask());

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_RemoveRelationship_When_UpdatePersonWithRelationshipRemoved()
		=> await RelationshipTestCases.RemoveRelationshipOnUpdateAsync<TPerson, TKey, TRelationship>(
			People!,
			p => Repository.FindAsync(p.Id!, TestContext.Current.CancellationToken).AsTask(),
			RemoveRelationshipAsync,
			toUpdate => Repository.UpdateAsync(toUpdate, TestContext.Current.CancellationToken).AsTask());
}