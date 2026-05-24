using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Net.Mail;

using Bogus;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Deveel.Data;

public abstract class RepositoryTestSuite<TPerson, TKey, TRelationship> : IAsyncLifetime, IAsyncDisposable
	where TPerson : class, IPerson<TKey>
	where TKey : notnull
	where TRelationship : class, IRelationship {
	private IServiceProvider? _services;
	private AsyncServiceScope scope;

	protected RepositoryTestSuite(ITestOutputHelper? testOutput) {
		TestOutput = testOutput;
	}

	protected ITestOutputHelper? TestOutput { get; }

	protected virtual int EntitySetCount => 100;

	protected IReadOnlyList<TPerson>? People { get; private set; }

	protected int PeopleCount => People?.Count ?? 0;

	protected IServiceProvider Services => scope.ServiceProvider;

	protected virtual IRepository<TPerson, TKey> Repository { get; private set; }

	protected abstract Faker<TPerson> PersonFaker { get; }

	protected abstract Faker<TRelationship> RelationshipFaker { get; }

	protected TPerson GeneratePerson() => PersonFaker.Generate();

	protected TRelationship GenerateRelationship() => RelationshipFaker.Generate();

	protected ISystemTime TestTime { get; } = new TestTime();

	protected IList<TPerson> GeneratePeople(int count) => PersonFaker.Generate(count);

	protected abstract TKey GeneratePersonId();

	protected virtual void ConfigureServices(IServiceCollection services) {
		if (TestOutput != null)
			services.AddLogging(logging => { logging.ClearProviders(); logging.AddXUnit(TestOutput); });
	}

	protected virtual Task<IRepository<TPerson, TKey>> GetRepositoryAsync() {
		return Task.FromResult(Services.GetRequiredService<IRepository<TPerson, TKey>>());
	}

	private void BuildServices() {
		var services = new ServiceCollection();
		services.AddSystemTime(TestTime);

		ConfigureServices(services);

		_services = services.BuildServiceProvider();
		scope = _services.CreateAsyncScope();
	}

	async ValueTask IAsyncLifetime.InitializeAsync() {
		BuildServices();

		People = GeneratePeople(EntitySetCount).ToImmutableList();
		Repository = await GetRepositoryAsync();

		await InitializeAsync();
	}

	protected virtual async ValueTask InitializeAsync() {
		await SeedAsync(Repository);
	}

	async ValueTask IAsyncDisposable.DisposeAsync() {
		await DisposeAsync();

		People = null;

		await scope.DisposeAsync();
		(_services as IDisposable)?.Dispose();
	}

	protected virtual ValueTask DisposeAsync() {
		return ValueTask.CompletedTask;
	}

	protected virtual async Task SeedAsync(IRepository<TPerson, TKey> repository) {
		if (People != null)
			await repository.AddRangeAsync(People);
	}

	protected virtual IEnumerable<TPerson> NaturalOrder(IEnumerable<TPerson> source) {
		return source;
	}

	protected abstract Task AddRelationshipAsync(TPerson person, TRelationship relationship);

	protected abstract Task RemoveRelationshipAsync(TPerson person, TRelationship relationship);

	protected virtual void ClearRelationships(TPerson person) {
	}

	protected virtual Task<TPerson?> FindPersonAsync(object id) {
		var entity = People?.FirstOrDefault(x => Repository.GetEntityKey(x)?.Equals(id) ?? false);
		return Task.FromResult(entity);
	}

	protected virtual Task<TPerson> RandomPersonAsync(Expression<Func<TPerson, bool>>? predicate = null) {
		var result = People?.Random(predicate?.Compile());

		if (result == null)
			throw new InvalidOperationException("No person found");

		return Task.FromResult(result);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_AddPerson_When_PersonIsNew() {
		// Arrange
		var person = GeneratePerson();

		// Act
		await Repository.AddAsync(person, TestContext.Current.CancellationToken);

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
	public async Task Should_AddRange_When_PeopleAreNew() {
		// Arrange
		var entities = GeneratePeople(10);

		// Act
		await Repository.AddRangeAsync(entities, TestContext.Current.CancellationToken);

		// Assert
		foreach (var item in entities) {
			var key = Repository.GetEntityKey(item);
			Assert.NotNull(key);

			var found = await Repository.FindAsync(key, TestContext.Current.CancellationToken);
			Assert.NotNull(found);
		}
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_RemovePerson_When_PersonExists() {
		// Arrange
		var person = await RandomPersonAsync();
		Assert.NotNull(person);

		// Act
		var result = await Repository.RemoveAsync(person, TestContext.Current.CancellationToken);

		// Assert
		Assert.True(result);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ReturnFalse_When_RemovePersonNotFound() {
		// Arrange
		var entity = GeneratePerson();
		entity.Id = GeneratePersonId();

		// Act
		var result = await Repository.RemoveAsync(entity, TestContext.Current.CancellationToken);

		// Assert
		Assert.False(result);
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
	public async Task Should_RemoveByKey_When_KeyExists() {
		// Arrange
		var key = Repository.GetEntityKey(People!.Random()!);
		Assert.NotNull(key);

		// Act
		var result = await Repository.RemoveByKeyAsync(key, TestContext.Current.CancellationToken);

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
	public async Task Should_ReturnFalse_When_RemoveByKeyNotFound() {
		// Arrange
		var id = GeneratePersonId();

		// Act
		var result = await Repository.RemoveByKeyAsync(id, TestContext.Current.CancellationToken);

		// Assert
		Assert.False(result);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_RemoveRange_When_PeopleExist() {
		// Arrange
		var peopleCount = PeopleCount;
		var people = People!.Take(10).ToList();

		// Act
		await Repository.RemoveRangeAsync(people);

		// Assert
		var result = await Repository.FindAllAsync(TestContext.Current.CancellationToken);
		Assert.NotNull(result);
		Assert.NotEmpty(result);
		Assert.Equal(peopleCount - 10, result.Count);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ThrowRepositoryException_When_RemoveRangeWithOneNotExisting() {
		// Arrange
		var peopleCount = PeopleCount;
		var people = People!.Take(9).ToList();

		var entity = GeneratePerson();
		entity.Id = GeneratePersonId();
		people.Add(entity);

		// Act & Assert
		await Assert.ThrowsAsync<RepositoryException>(async () => await Repository.RemoveRangeAsync(people, TestContext.Current.CancellationToken));

		var result = await Repository.FindAllAsync();
		Assert.NotNull(result);
		Assert.NotEmpty(result);
		Assert.Equal(peopleCount, result.Count);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ReturnTotalCount_When_CountAll() {
		// Act
		var result = await Repository.CountAllAsync(TestContext.Current.CancellationToken);

		// Assert
		Assert.NotEqual(0, result);
		Assert.Equal(PeopleCount, result);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public void Should_ReturnTotalCount_When_CountAllSync() {
		// Act
		var result = Repository.CountAll();

		// Assert
		Assert.NotEqual(0, result);
		Assert.Equal(PeopleCount, result);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ReturnFilteredCount_When_FilterApplied() {
		// Arrange
		var person = await RandomPersonAsync();
		var firstName = person.FirstName;
		var peopleCount = People?.Count(x => x.FirstName == firstName) ?? 0;

		// Act
		var count = await Repository.CountAsync(p => p.FirstName == firstName, TestContext.Current.CancellationToken);

		// Assert
		Assert.Equal(peopleCount, count);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ReturnFilteredCount_When_FilterAppliedSync() {
		// Arrange
		var person = await RandomPersonAsync();
		var firstName = person.FirstName;
		var peopleCount = People?.Count(x => x.FirstName == firstName) ?? 0;

		// Act
		var count = Repository.Count(p => p.FirstName == firstName);

		// Assert
		Assert.Equal(peopleCount, count);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ReturnPerson_When_FindByKey() {
		// Arrange
		var person = await RandomPersonAsync();
		var id = person.Id!;

		// Act
		var result = await Repository.FindAsync(id, TestContext.Current.CancellationToken);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(id, result.Id);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ReturnFirstMatch_When_FilterApplied() {
		// Arrange
		var person = await RandomPersonAsync();
		var firstName = person.FirstName;

		// Act
		var result = await Repository.FindFirstAsync(x => x.FirstName == firstName, TestContext.Current.CancellationToken);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(firstName, result.FirstName);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ReturnFirstMatchBySort_When_FilterAndSortApplied() {
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
		var result = await Repository.FindFirstAsync(query, TestContext.Current.CancellationToken);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(expected.FirstName, result.FirstName);
		Assert.Equal(expected.LastName, result.LastName);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public void Should_ReturnFirstPerson_When_FindFirstSync() {
		// Act
		var result = Repository.FindFirst();

		// Assert
		Assert.NotNull(result);
		Assert.NotNull(result.Id);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ReturnTrue_When_PersonExists() {
		// Arrange
		var person = await RandomPersonAsync();
		var firstName = person.FirstName;

		// Act
		var result = await Repository.ExistsAsync(x => x.FirstName == firstName, TestContext.Current.CancellationToken);

		// Assert
		Assert.True(result);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ReturnTrue_When_PersonExistsSync() {
		// Arrange
		var person = await RandomPersonAsync();
		var firstName = person.FirstName;

		// Act
		var result = Repository.Exists(x => x.FirstName == firstName);

		// Assert
		Assert.True(result);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ReturnPerson_When_KeyExists() {
		// Arrange
		var person = await RandomPersonAsync();

		// Act
		var result = await Repository.FindAsync(person.Id!, TestContext.Current.CancellationToken);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(person.Id, result.Id);
		Assert.Equal(person.FirstName, result.FirstName);
		Assert.Equal(person.LastName, result.LastName);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ReturnNull_When_KeyNotFound() {
		// Arrange
		var id = GeneratePersonId();

		// Act
		var result = await Repository.FindAsync(id);

		// Assert
		Assert.Null(result);
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
	public async Task Should_ReturnFirstPerson_When_FindFirst() {
		// Arrange
		var ordered = NaturalOrder(People!).ToList();

		// Act
		var result = await Repository.FindFirstAsync(TestContext.Current.CancellationToken);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(ordered[0].FirstName, result.FirstName);
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
		var result = Repository.FindFirst(QueryFilter.Where<TPerson>(x => x.FirstName == person.FirstName));

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
	public async Task Should_ReturnAllPeople_When_FindAll() {
		// Act
		var result = await Repository.FindAllAsync(TestContext.Current.CancellationToken);

		// Assert
		Assert.NotNull(result);
		Assert.NotEmpty(result);
		Assert.Equal(PeopleCount, result.Count);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public void Should_ReturnAllPeople_When_FindAllSync() {
		// Act
		var result = Repository.FindAll();

		// Assert
		Assert.NotNull(result);
		Assert.NotEmpty(result);
		Assert.Equal(PeopleCount, result.Count);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ReturnFilteredPeople_When_FilterApplied() {
		// Arrange
		var person = await RandomPersonAsync();
		var firstName = person.FirstName;
		var peopleCount = People?.Count(x => x.FirstName == firstName) ?? 0;

		// Act
		var result = await Repository.FindAllAsync(x => x.FirstName == firstName, 
			TestContext.Current.CancellationToken);

		// Assert
		Assert.NotNull(result);
		Assert.NotEmpty(result);
		Assert.Equal(peopleCount, result.Count);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ReturnFilteredAndSortedPeople_When_FilterAndSortApplied() {
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
		var result = await Repository.FindAllAsync(query, TestContext.Current.CancellationToken);

		// Assert
		Assert.NotNull(result);
		Assert.NotEmpty(result);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ThrowRepositoryException_When_FilterTypeIsInvalid() {
		// Arrange
		var person = await RandomPersonAsync();

		// Act & Assert
		await Assert.ThrowsAsync<RepositoryException>(
			async () => await Repository.FindAllAsync(QueryFilter.Where<MailAddress>(m => m.Address == null), 
				TestContext.Current.CancellationToken));
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ReturnPage_When_NoFilterApplied() {
		// Arrange
		var totalItems = PeopleCount;
		var totalPages = (int)Math.Ceiling((double)totalItems / 10);

		// Act
		var result = await Repository.GetPageAsync(1, 10, TestContext.Current.CancellationToken);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(totalPages, result.TotalPages);
		Assert.Equal(totalItems, result.TotalItems);
		Assert.NotNull(result.Items);
		Assert.NotEmpty(result.Items);
		Assert.Equal(10, result.Items.Count);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ReturnPage_When_PageParametersProvided() {
		// Arrange
		var totalItems = PeopleCount;
		var totalPages = (int)Math.Ceiling((double)totalItems / 10);

		// Act
		var result = await Repository.GetPageAsync(1, 10, TestContext.Current.CancellationToken);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(totalPages, result.TotalPages);
		Assert.Equal(totalItems, result.TotalItems);
		Assert.NotNull(result.Items);
		Assert.NotEmpty(result.Items);
		Assert.Equal(10, result.Items.Count);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ReturnFilteredPage_When_FilterApplied() {
		// Arrange
		var person = await RandomPersonAsync();
		var firstName = person.FirstName;
		var peopleCount = People?.Count(x => x.FirstName == firstName) ?? 0;
		var totalPages = (int)Math.Ceiling((double)peopleCount / 10);
		var perPage = Math.Min(peopleCount, 10);

		var request = new PageQuery<TPerson>(1, 10)
			.Where(x => x.FirstName == firstName);

		// Act
		var result = await Repository.GetPageAsync(request, TestContext.Current.CancellationToken);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(totalPages, result.TotalPages);
		Assert.Equal(peopleCount, result.TotalItems);
		Assert.NotNull(result.Items);
		Assert.NotEmpty(result.Items);
		Assert.Equal(perPage, result.Items.Count());
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ReturnFilteredPage_When_MultipleFiltersApplied() {
		// Arrange
		var person = await RandomPersonAsync(x => x.LastName != null);
		var firstName = person.FirstName;
		var lastName = person.LastName;

		var peopleCount = People?.Count(x => x.FirstName == firstName && x.LastName == lastName) ?? 0;
		var totalPages = (int)Math.Ceiling((double)peopleCount / 10);
		var perPage = Math.Min(peopleCount, 10);

		var request = new PageQuery<TPerson>(1, 10)
			.Where(x => x.FirstName == firstName && x.LastName == lastName);

		// Act
		var result = await Repository.GetPageAsync(request, TestContext.Current.CancellationToken);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(totalPages, result.TotalPages);
		Assert.Equal(peopleCount, result.TotalItems);
		Assert.NotNull(result.Items);
		Assert.NotEmpty(result.Items);
		Assert.Equal(perPage, result.Items.Count);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ReturnFilteredPage_When_FiltersChained() {
		// Arrange
		var person = await RandomPersonAsync(x => x.DateOfBirth != null);
		var firstName = person.FirstName;
		var birthDate = person.DateOfBirth!.Value;

		var peopleCount = People?
			.Where(x => x.FirstName == firstName)
			.Where(x => x.DateOfBirth >= birthDate)
			.Count() ?? 0;

		var totalPages = (int)Math.Ceiling((double)peopleCount / 10);
		var perPage = Math.Min(peopleCount, 10);

		var request = new PageQuery<TPerson>(1, 10)
			.Where(x => x.FirstName == firstName)
			.Where(x => x.DateOfBirth >= birthDate);

		// Act
		var result = await Repository.GetPageAsync(request, TestContext.Current.CancellationToken);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(totalPages, result.TotalPages);
		Assert.Equal(peopleCount, result.TotalItems);
		Assert.NotNull(result.Items);
		Assert.NotEmpty(result.Items);
		Assert.Equal(perPage, result.Items.Count);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ReturnDescendingSortedPage_When_SortApplied() {
		// Arrange
		var sorted = People!.Where(x => x.LastName != null)
			.OrderByDescending(x => x.LastName).Skip(0).Take(10).ToList();

		var request = new PageQuery<TPerson>(1, 10)
			.OrderByDescending(x => x.LastName);

		// Act
		var result = await Repository.GetPageAsync(request, TestContext.Current.CancellationToken);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(10, result.TotalPages);
		Assert.Equal(100, result.TotalItems);
		Assert.NotNull(result.Items);
		Assert.NotEmpty(result.Items);
		Assert.Equal(10, result.Items.Count());

		for (int i = 0; i < sorted.Count; i++) {
			Assert.Equal(sorted[i].LastName, result.Items.ElementAt(i).LastName);
		}
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ReturnSortedPage_When_SortApplied() {
		// Arrange
		var totalPages = (int)Math.Ceiling((double)PeopleCount / 10);

		var request = new PageQuery<TPerson>(1, 10)
			.OrderBy(x => x.FirstName);

		// Act
		var result = await Repository.GetPageAsync(request, TestContext.Current.CancellationToken);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(totalPages, result.TotalPages);
		Assert.Equal(PeopleCount, result.TotalItems);
		Assert.NotNull(result.Items);
		Assert.NotEmpty(result.Items);
		Assert.Equal(10, result.Items.Count);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public void Should_ReturnPage_When_GetPageSync() {
		// Arrange
		var totalPages = (int)Math.Ceiling((double)PeopleCount / 10);
		var request = new PageQuery<TPerson>(1, 10);

		// Act
		var result = Repository.GetPage(request);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(totalPages, result.TotalPages);
		Assert.Equal(PeopleCount, result.TotalItems);
		Assert.NotNull(result.Items);
		Assert.NotEmpty(result.Items);
		Assert.Equal(10, result.Items.Count);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ReturnEntityKey_When_PersonExists() {
		// Arrange
		var person = await RandomPersonAsync();

		// Act
		var id = Repository.GetEntityKey(person);

		// Assert
		Assert.NotNull(id);
		Assert.Equal(person.Id, id);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_UpdatePerson_When_PersonExists() {
		// Arrange
		var person = await RandomPersonAsync(x => x.FirstName != "John");
		var toUpdate = await Repository.FindAsync(person.Id!, TestContext.Current.CancellationToken);
		Assert.NotNull(toUpdate);
		toUpdate.FirstName = "John";

		// Act
		var result = await Repository.UpdateAsync(toUpdate, TestContext.Current.CancellationToken);

		// Assert
		Assert.True(result);

		var updated = await Repository.FindAsync(person.Id!, TestContext.Current.CancellationToken);
		Assert.NotNull(updated);
		Assert.Equal(toUpdate.FirstName, updated.FirstName);
		Assert.Equal(toUpdate.LastName, updated.LastName);
		Assert.Equal(toUpdate.Email, updated.Email);
		Assert.Equal(toUpdate.DateOfBirth, updated.DateOfBirth);
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
		Assert.Equal(toUpdate.FirstName, updated.FirstName);
		Assert.Equal(toUpdate.LastName, updated.LastName);
		Assert.Equal(toUpdate.Email, updated.Email);
		Assert.Equal(toUpdate.DateOfBirth, updated.DateOfBirth);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ReturnFalse_When_UpdatePersonNotFound() {
		// Arrange
		var person = GeneratePerson();
		person.Id = GeneratePersonId();
		ClearRelationships(person);

		// Act
		var result = await Repository.UpdateAsync(person, TestContext.Current.CancellationToken);

		// Assert
		Assert.False(result);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_KeepEntityUnchanged_When_UpdateWithNoChanges() {
		// Arrange
		var person = await RandomPersonAsync();
		var toUpdate = await Repository.FindAsync(person.Id!, TestContext.Current.CancellationToken);
		Assert.NotNull(toUpdate);

		// Act
		await Repository.UpdateAsync(toUpdate);

		// Assert
		var updated = await Repository.FindAsync(person.Id!, TestContext.Current.CancellationToken);
		Assert.NotNull(updated);
		Assert.Equal(toUpdate, updated);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_AddRelationship_When_UpdatePersonWithNewRelationship() {
		// Arrange
		var person = People!.Random(x => x.Relationships == null || !x.Relationships.Any());
		Assert.NotNull(person);

		var relationship = GenerateRelationship();
		var toUpdate = await Repository.FindAsync(person.Id!, TestContext.Current.CancellationToken);
		Assert.NotNull(toUpdate);

		await AddRelationshipAsync(toUpdate, relationship);

		// Act
		var result = await Repository.UpdateAsync(toUpdate, TestContext.Current.CancellationToken);

		// Assert
		Assert.True(result);

		var updated = await Repository.FindAsync(person.Id!, TestContext.Current.CancellationToken);
		Assert.NotNull(updated);
		Assert.NotNull(updated.Relationships);
		Assert.NotEmpty(updated.Relationships);
		Assert.Single(updated.Relationships);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_RemoveRelationship_When_UpdatePersonWithRelationshipRemoved() {
		// Arrange
		var person = People!.Random(x => x.Relationships?.Any() ?? false);
		Assert.NotNull(person);

		var toUpdate = await Repository.FindAsync(person.Id!, TestContext.Current.CancellationToken);
		Assert.NotNull(toUpdate);

		var relCount = toUpdate.Relationships.Count();

		await RemoveRelationshipAsync(toUpdate, (TRelationship)toUpdate.Relationships!.First());

		// Act
		var result = await Repository.UpdateAsync(toUpdate, TestContext.Current.CancellationToken);

		// Assert
		Assert.True(result);

		var updated = await Repository.FindAsync(person.Id!, TestContext.Current.CancellationToken);
		Assert.NotNull(updated);
		Assert.NotNull(updated.Relationships);
		Assert.Equal(relCount - 1, updated.Relationships.Count());
	}
}
