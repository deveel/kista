using Bogus;

using Microsoft.Extensions.DependencyInjection;

using System.Linq.Expressions;

namespace Kista;

public abstract class NoKeyRepositoryTestSuite<TPerson, TRelationship> : RepositoryTestSuiteBase<TPerson>
	where TPerson : class, IPerson
	where TRelationship : class, IRelationship {
	private IRepository<TPerson>? _repository;

	protected NoKeyRepositoryTestSuite(ITestOutputHelper? testOutput) : base(testOutput) {
	}

	protected abstract Faker<TRelationship> RelationshipFaker { get; }

	protected TRelationship GenerateRelationship() => RelationshipFaker.Generate();

	protected abstract string GeneratePersonId();

	protected abstract Task AddRelationshipAsync(TPerson person, TRelationship relationship);

	protected abstract Task RemoveRelationshipAsync(TPerson person, TRelationship relationship);

	protected virtual IRepository<TPerson> Repository {
		get {
			if (_repository == null)
				_repository = Services.GetRequiredService<IRepository<TPerson>>();

			return _repository;
		}
	}

	protected override object GeneratePersonKey() => GeneratePersonId();

	protected override void AssignPersonKey(TPerson person, object key) {
		person.Id = (string)key;
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
		=> Repository.RemoveByKeyAsync(key, cancellationToken);

	protected override ValueTask<TPerson?> FindAsync(object key, CancellationToken cancellationToken)
		=> Repository.FindAsync(key, cancellationToken);

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

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_AddPerson_When_CalledSync() {
		// Arrange
		var person = GeneratePerson();

		// Act
		Repository.Add(person);

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
	public async Task Should_ThrowArgumentNullException_When_AddNullPerson() {
		// Act & Assert
		await Assert.ThrowsAsync<ArgumentNullException>(async () => await Repository.AddAsync(null!, TestContext.Current.CancellationToken));
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ThrowArgumentNullException_When_AddRangeNullList() {
		// Act & Assert
		await Assert.ThrowsAsync<ArgumentNullException>(async () => await Repository.AddRangeAsync(null!, TestContext.Current.CancellationToken));
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ThrowArgumentNullException_When_RemoveNullPerson() {
		// Act & Assert
		await Assert.ThrowsAsync<ArgumentNullException>(async () => await Repository.RemoveAsync(null!, TestContext.Current.CancellationToken));
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ThrowArgumentNullException_When_RemoveRangeNullList() {
		// Act & Assert
		await Assert.ThrowsAsync<ArgumentNullException>(async () => await Repository.RemoveRangeAsync(null!, TestContext.Current.CancellationToken));
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ThrowArgumentNullException_When_FindWithNullKey() {
		// Act & Assert
		await Assert.ThrowsAsync<ArgumentNullException>(async () => await Repository.FindAsync(default!, TestContext.Current.CancellationToken));
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ThrowRepositoryException_When_AddDuplicatePerson() {
		// Arrange
		var person = GeneratePerson();
		await Repository.AddAsync(person, TestContext.Current.CancellationToken);

		// Act & Assert
		await Assert.ThrowsAsync<RepositoryException>(async () => await Repository.AddAsync(person, TestContext.Current.CancellationToken));
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public void Should_RemovePerson_When_CalledSync() {
		// Arrange
		var person = People!.Random();
		Assert.NotNull(person);

		// Act
		var result = Repository.Remove(person);

		// Assert
		Assert.True(result);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public void Should_RemoveByKey_When_CalledSync() {
		// Arrange
		var key = Repository.GetEntityKey(People!.Random()!);
		Assert.NotNull(key);

		// Act
		var result = Repository.RemoveByKey(key);

		// Assert
		Assert.True(result);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public void Should_ReturnTotalCount_When_CountAllSync() {
		// Act
		var result = Repository.GetPageAsync(new PageRequest(1, int.MaxValue)).GetAwaiter().GetResult().Items.AsQueryable().LongCount();

		// Assert
		Assert.NotEqual(0, result);
		Assert.Equal(PeopleCount, result);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public void Should_ReturnFirstPerson_When_FindFirstSync() {
		// Act
		var result = Repository.GetPageAsync(new PageRequest(1, int.MaxValue)).GetAwaiter().GetResult().Items.AsQueryable().FirstOrDefault();

		// Assert
		Assert.NotNull(result);
		Assert.NotNull(result.Id);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ReturnPerson_When_FindByKeySync() {
		// Arrange
		var person = await RandomPersonAsync();

		// Act
		var result = Repository.Find(person.Id!);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(person.Id, result.Id);
	}

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
	public void Should_ReturnAllPeople_When_FindAllSync() {
		// Act
		var result = Repository.GetPageAsync(new PageRequest(1, int.MaxValue)).GetAwaiter().GetResult().Items.AsQueryable().ToList();

		// Assert
		Assert.NotNull(result);
		Assert.NotEmpty(result);
		Assert.Equal(PeopleCount, result.Count);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_UpdatePerson_When_CalledSync() {
		// Arrange
		var person = await RandomPersonAsync(x => x.FirstName != "John");
		var toUpdate = await Repository.FindAsync(person.Id!, TestContext.Current.CancellationToken);
		Assert.NotNull(toUpdate);
		toUpdate.FirstName = "John";

		// Act
		var result = Repository.Update(toUpdate);

		// Assert
		Assert.True(result);

		var updated = await Repository.FindAsync(person.Id!, TestContext.Current.CancellationToken);
		Assert.NotNull(updated);
		Assert.Equal(toUpdate.FirstName, updated!.FirstName);
		Assert.Equal(toUpdate.LastName, updated!.LastName);
		Assert.Equal(toUpdate.Email, updated!.Email);
		Assert.Equal(toUpdate.DateOfBirth, updated!.DateOfBirth);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ThrowArgumentNullException_When_UpdateNullPerson() {
		// Act & Assert
		await Assert.ThrowsAsync<ArgumentNullException>(async () => await Repository.UpdateAsync(null!, TestContext.Current.CancellationToken));
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ThrowOperationCanceledException_When_CancellationRequested() {
		// Arrange
		var person = await RandomPersonAsync();
		using var cts = new CancellationTokenSource();
		await cts.CancelAsync();

		// Act & Assert
		await Assert.ThrowsAsync<OperationCanceledException>(async () => await Repository.UpdateAsync(person, cts.Token));
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_NotThrow_When_AddRangeEmptyList() {
		// Arrange
		var emptyList = new List<TPerson>();

		// Act
		await Repository.AddRangeAsync(emptyList, TestContext.Current.CancellationToken);

		// Assert
		var count = await Task.FromResult(Repository.GetPageAsync(new PageRequest(1, int.MaxValue)).GetAwaiter().GetResult().Items.AsQueryable().LongCount());
		Assert.Equal(PeopleCount, count);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_NotThrow_When_RemoveRangeEmptyList() {
		// Arrange
		var emptyList = new List<TPerson>();

		// Act
		await Repository.RemoveRangeAsync(emptyList, TestContext.Current.CancellationToken);

		// Assert
		var count = await Task.FromResult(Repository.GetPageAsync(new PageRequest(1, int.MaxValue)).GetAwaiter().GetResult().Items.AsQueryable().LongCount());
		Assert.Equal(PeopleCount, count);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_AddRelationship_When_UpdatePersonWithNewRelationship()
		=> await RelationshipTestCases.AddRelationshipOnUpdateAsync<TPerson, string, TRelationship>(
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
		=> await RelationshipTestCases.RemoveRelationshipOnUpdateAsync<TPerson, string, TRelationship>(
			People!,
			p => Repository.FindAsync(p.Id!, TestContext.Current.CancellationToken).AsTask(),
			RemoveRelationshipAsync,
			toUpdate => Repository.UpdateAsync(toUpdate, TestContext.Current.CancellationToken).AsTask());
}