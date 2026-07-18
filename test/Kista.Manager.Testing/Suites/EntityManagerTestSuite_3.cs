using System.Reflection;

using Bogus;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Xunit;

namespace Kista;

public abstract class EntityManagerTestSuite<TManager, TPerson, TKey> : IAsyncLifetime, IAsyncDisposable
	where TManager : EntityManager<TPerson, TKey>
	where TPerson : class, IPerson<TKey>, new()
	where TKey : notnull {
	private AsyncServiceScope scope;

	protected EntityManagerTestSuite(ITestOutputHelper testOutput) {
		TestOutput = testOutput;

		CreateServices();
	}

	protected IServiceProvider Services => scope.ServiceProvider ?? throw new InvalidOperationException();

	protected ITestOutputHelper TestOutput { get; }

	protected IRepository<TPerson, TKey> Repository => Services.GetRequiredService<IRepository<TPerson, TKey>>();

	protected IQueryable<TPerson> People => Repository.GetPageAsync(new PageRequest(1, int.MaxValue)).GetAwaiter().GetResult().Items.AsQueryable();

	protected TManager Manager => Services.GetRequiredService<TManager>();

	protected ISystemTime TestTime { get; } = new TestSystemTime();

	protected abstract Faker<TPerson> PersonFaker { get; }

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
		services.AddSingleton<IEqualityComparer<TPerson>, PersonComparer<TPerson, TKey>>();
		TestServiceRegistration.RegisterManager(services, typeof(TManager));
		TestServiceRegistration.RegisterValidator(services, typeof(PersonValidator<TPerson, TKey>));
	}

	public virtual async ValueTask InitializeAsync() {
		var people = PersonFaker.Generate(100);
		await Repository.AddRangeAsync(people);
	}

	public virtual async Task DisposeAsync() {
	}

	async ValueTask IAsyncDisposable.DisposeAsync() {
		await scope.DisposeAsync();
		(Services as IDisposable)?.Dispose();
	}

	protected abstract TKey GenerateKey();

	protected abstract void SetKey(TPerson person, TKey key);

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Application")]
	[Trait("Feature", "Manager")]
	public async Task Should_AddEntity_When_EntityIsValid() {
		// Arrange
		var person = PersonFaker.Generate();

		// Act
		var result = await Manager.AddAsync(person);

		// Assert
		Assert.True(result.IsSuccess());
		Assert.NotNull(person.Id);
		Assert.NotNull(person.CreatedAtUtc);
		Assert.Null(person.UpdatedAtUtc);
		Assert.Equal(TestTime.UtcNow, person.CreatedAtUtc.Value);

		var found = await Repository.FindAsync(person.Id);
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
		var result = await Manager.AddAsync(person);

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
		var result = await Manager.AddRangeAsync(people);

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
		var result = await Manager.AddRangeAsync(people);

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
	public async Task Should_UpdateEntity_When_EntityIsValid() {
		// Arrange
		var person = People.Random();
		Assert.NotNull(person);
		Assert.NotNull(person.Id);

		person.Email = new Bogus.Faker().Internet.Email();

		// Act
		var result = await Manager.UpdateAsync(person);

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
	public async Task Should_ReturnUnchanged_When_UpdateWithNoChanges() {
		// Arrange
		var person = People.Random();
		Assert.NotNull(person);
		Assert.NotNull(person.Id);

		var toUpdate = await Repository.FindAsync(person.Id);
		Assert.NotNull(toUpdate);

		// Act
		var result = await Manager.UpdateAsync(toUpdate);

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
		var person = PersonFaker.Generate();
		SetKey(person, GenerateKey());

		// Act
		var result = await Manager.UpdateAsync(person);

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
		var result = await Manager.UpdateAsync(person);

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
		var result = await Manager.RemoveAsync(person);

		// Assert
		Assert.True(result.IsSuccess());
		Assert.False(result.IsError());
		Assert.Null(result.Error);

		var found = await Repository.FindAsync(person.Id!);
		Assert.Null(found);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Application")]
	[Trait("Feature", "Manager")]
	public async Task Should_ReturnNotFoundError_When_RemoveEntityNotFound() {
		// Arrange
		var person = PersonFaker.Generate();
		SetKey(person, GenerateKey());

		// Act
		var result = await Manager.RemoveAsync(person);

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
		var result = await Manager.RemoveAsync(person);

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
		var result = await Manager.RemoveRangeAsync(people);

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
		var found = await Manager.FindAsync(person.Id!);

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
		var personId = GenerateKey();

		// Act
		var found = await Manager.FindAsync(personId);

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
		var page = (PageQueryResult<TPerson>) await Repository.GetPageAsync(query);

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
		var page = (PageQueryResult<TPerson>) await Repository.GetPageAsync(query);

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
