using System.Reflection;

using Bogus;

using Microsoft.Extensions.DependencyInjection;

namespace Kista;

public abstract class EntityManagerTestSuite<TManager, TPerson> : IAsyncInitializer, IAsyncDisposable
	where TManager : EntityManager<TPerson>
	where TPerson : class, IPerson, new()
{
	private AsyncServiceScope _scope;

	protected IServiceProvider Services => _scope.ServiceProvider ?? throw new InvalidOperationException();

	protected IRepository<TPerson> Repository => Services.GetRequiredService<IRepository<TPerson>>();

	protected IQueryable<TPerson> People => Repository.AsQueryable().AsQueryable();

	protected TManager Manager => Services.GetRequiredService<TManager>();

	protected ISystemTime TestTime { get; } = new TestSystemTime();

	protected abstract Faker<TPerson> PersonFaker { get; }

	private void CreateServices()
	{
		var services = new ServiceCollection();
		services.AddSingleton<IOperationCancellationSource>(new TestCancellationTokenSource());
		services.AddSystemTime(TestTime);
		services.AddOperationErrorFactory<TPerson, PersonErrorFactory>();

		ConfigureServices(services);

		_scope = services.BuildServiceProvider().CreateAsyncScope();
	}

	protected virtual void ConfigureServices(IServiceCollection services)
	{
		services.AddEntityValidator<PersonValidator<TPerson>>();
		services.AddEntityManager<TManager>();
	}

	public async Task InitializeAsync()
	{
		CreateServices();

		var people = PersonFaker.Generate(100);
		await Repository.AddRangeAsync(people);
	}

	public async ValueTask DisposeAsync()
	{
		try
		{
			await Repository.RemoveRangeAsync(People);
		}
		catch { /* best effort */ }

		await _scope.DisposeAsync();
	}

	protected abstract string GenerateKey();

	[Test]
	[Category("Integration")]
	public async Task Should_AddEntity_When_EntityIsValid()
	{
		var person = PersonFaker.Generate();

		var result = await Manager.AddAsync(person);

		await Assert.That(result.IsSuccess()).IsTrue();
		await Assert.That(person.Id).IsNotNull();
		await Assert.That(person.CreatedAtUtc).IsNotNull();
		await Assert.That(person.UpdatedAtUtc).IsNull();
		await Assert.That(person.CreatedAtUtc!.Value).IsEqualTo(TestTime.UtcNow);

		var found = await Repository.FindAsync(person.Id);
		await Assert.That(found).IsNotNull();
		await Assert.That(found!.Id).IsEqualTo(person.Id);
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnValidationError_When_EmailIsInvalid()
	{
		var person = PersonFaker.Generate();
		person.Email = "invalid";

		var result = await Manager.AddAsync(person);

		await Assert.That(result.HasValidationErrors()).IsTrue();
		await Assert.That(result.Error).IsNotNull();

		var validationError = result.Error as IValidationError;
		await Assert.That(validationError).IsNotNull();
		await Assert.That(validationError!.ValidationResults.Count).IsEqualTo(1);
		await Assert.That(validationError.ValidationResults[0].MemberNames.First()).IsEqualTo(nameof(Person.Email));
	}

	[Test]
	[Category("Integration")]
	public async Task Should_AddRangeOfEntities_When_EntitiesAreValid()
	{
		var people = PersonFaker.Generate(10);
		var peopleCount = People.Count();

		var result = await Manager.AddRangeAsync(people);

		await Assert.That(result.IsSuccess()).IsTrue();
		await Assert.That(People.Count()).IsEqualTo(peopleCount + 10);

		var found = await Repository.FindAllAsync();
		await Assert.That(found).IsNotNull();

		foreach (var person in people)
			await Assert.That(person.Id).IsNotNull();
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnValidationError_When_RangeContainsInvalidEmail()
	{
		var people = PersonFaker.Generate(10);
		people[Random.Shared.Next(0, 9)].Email = "invalid";

		var result = await Manager.AddRangeAsync(people);

		await Assert.That(result.HasValidationErrors()).IsTrue();
		await Assert.That(result.Error).IsNotNull();

		var validationError = result.Error as IValidationError;
		await Assert.That(validationError).IsNotNull();
		await Assert.That(validationError!.ValidationResults.Count).IsEqualTo(1);
	}

	[Test]
	[Category("Integration")]
	public virtual async Task Should_UpdateEntity_When_EntityIsValid()
	{
		var person = People.Random();
		await Assert.That(person).IsNotNull();
		await Assert.That(person!.Id).IsNotNull();

		var copy = (TPerson)(typeof(TPerson)
			.GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)!
			.Invoke(person, Array.Empty<object>())!);

		copy.Email = new Faker().Internet.Email();

		var result = await Manager.UpdateAsync(copy);

		await Assert.That(result.HasValidationErrors()).IsFalse();
		await Assert.That(result.IsSuccess()).IsTrue();
		await Assert.That(copy.UpdatedAtUtc).IsNotNull();
		await Assert.That(copy.UpdatedAtUtc!.Value).IsEqualTo(TestTime.UtcNow);
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnUnchanged_When_UpdateWithNoChanges()
	{
		var person = People.Random();
		await Assert.That(person).IsNotNull();

		var toUpdate = await Repository.FindAsync(person!.Id!);
		await Assert.That(toUpdate).IsNotNull();

		var result = await Manager.UpdateAsync(toUpdate!);

		await Assert.That(result.IsUnchanged()).IsTrue();
		await Assert.That(result.IsSuccess()).IsFalse();
		await Assert.That(result.Error).IsNull();
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnNotFoundError_When_UpdateEntityNotFound()
	{
		var person = PersonFaker.RuleFor(x => x.Id, f => GenerateKey()).Generate();

		var result = await Manager.UpdateAsync(person);

		await Assert.That(result.IsError()).IsTrue();
		await Assert.That(result.IsSuccess()).IsFalse();
		await Assert.That(result.Error).IsNotNull();
		await Assert.That(result.Error!.Code).IsEqualTo(PersonErrorCodes.NotFound);
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnValidationError_When_UpdateEntityHasNoKey()
	{
		var person = PersonFaker.RuleFor(x => x.Id, f => default).Generate();

		var result = await Manager.UpdateAsync(person);

		await Assert.That(result.IsError()).IsTrue();
		await Assert.That(result.IsSuccess()).IsFalse();
		await Assert.That(result.Error).IsNotNull();
		await Assert.That(result.Error!.Code).IsEqualTo(PersonErrorCodes.NotValid);
	}

	[Test]
	[Category("Integration")]
	public async Task Should_RemoveEntity_When_EntityExists()
	{
		var person = People.Random()!;

		var result = await Manager.RemoveAsync(person);

		await Assert.That(result.IsSuccess()).IsTrue();
		await Assert.That(result.IsError()).IsFalse();
		await Assert.That(result.Error).IsNull();

		var found = await Repository.FindAsync(person.Id!);
		await Assert.That(found).IsNull();
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnNotFoundError_When_RemoveEntityNotFound()
	{
		var person = PersonFaker.RuleFor(x => x.Id, f => GenerateKey()).Generate();

		var result = await Manager.RemoveAsync(person);

		await Assert.That(result.IsError()).IsTrue();
		await Assert.That(result.Error!.Code).IsEqualTo(PersonErrorCodes.NotFound);
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnValidationError_When_RemoveEntityHasNoKey()
	{
		var person = PersonFaker.Generate();

		var result = await Manager.RemoveAsync(person);

		await Assert.That(result.IsError()).IsTrue();
		await Assert.That(result.Error!.Code).IsEqualTo(PersonErrorCodes.NotValid);
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnEntity_When_FindByKey()
	{
		var person = People.Random()!;

		var found = await Manager.FindAsync(person.Id!);

		await Assert.That(found.IsSuccess()).IsTrue();
		await Assert.That(found.Value!.Id).IsEqualTo(person.Id);
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnNull_When_FindByKeyNotFound()
	{
		var personId = GenerateKey();

		var found = await Manager.FindAsync(personId);

		await Assert.That(found.IsError()).IsTrue();
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnFirstMatch_When_FilterApplied()
	{
		var person = People.Where(x => x.FirstName.StartsWith("A")).OrderBy(x => x.Id).FirstOrDefault();
		await Assert.That(person).IsNotNull();

		var found = await Manager.FindFirstAsync(x => x.FirstName.StartsWith("A"));

		await Assert.That(found.IsSuccess()).IsTrue();
		await Assert.That(found.Value!.Id).IsEqualTo(person!.Id);
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnAllEntities_When_FindAll()
	{
		var people = People.ToList();

		var found = await Manager.FindAllAsync();

		await Assert.That(found).IsNotNull();
		await Assert.That(found.Count).IsEqualTo(people.Count);
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnTotalCount_When_CountAll()
	{
		var count = await Manager.CountAsync();

		await Assert.That(count).IsEqualTo(People.Count());
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnPage_When_NoFilterApplied()
	{
		var totalPeople = People.Count();
		var totalPages = (int)Math.Ceiling((double)totalPeople / 10);
		var perPage = Math.Min(10, totalPeople);

		var query = new PageQuery<TPerson>(1, 10);

		var page = await Manager.GetPageAsync(query);

		await Assert.That(page).IsNotNull();
		await Assert.That(page.TotalPages).IsEqualTo(totalPages);
		await Assert.That(page.TotalItems).IsEqualTo(totalPeople);
		await Assert.That(page.Items).IsNotNull();
		await Assert.That(page.Items.Count).IsEqualTo(perPage);
	}

	private sealed class TestSystemTime : ISystemTime
	{
		public TestSystemTime()
		{
			UtcNow = DateTimeOffset.UtcNow;
			Now = DateTimeOffset.Now;
		}

		public DateTimeOffset UtcNow { get; }
		public DateTimeOffset Now { get; }
	}
}

