using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Net.Mail;

using Bogus;

using Microsoft.Extensions.DependencyInjection;

namespace Kista;

public abstract class RepositoryTestSuite<TPerson, TKey, TRelationship> : IAsyncInitializer, IAsyncDisposable
	where TPerson : class, IPerson<TKey>
	where TKey : notnull
	where TRelationship : class, IRelationship
{
	private IServiceProvider? _services;
	private AsyncServiceScope _scope;

	protected virtual int EntitySetCount => 100;

	protected IReadOnlyList<TPerson>? People { get; private set; }

	protected int PeopleCount => People?.Count ?? 0;

	protected IServiceProvider Services => _scope.ServiceProvider;

	protected virtual IRepository<TPerson, TKey> Repository { get; private set; } = default!;

	protected abstract Faker<TPerson> PersonFaker { get; }

	protected abstract Faker<TRelationship> RelationshipFaker { get; }

	protected TPerson GeneratePerson() => PersonFaker.Generate();

	protected TRelationship GenerateRelationship() => RelationshipFaker.Generate();

	protected ISystemTime TestTime { get; } = new TestTime();

	protected IList<TPerson> GeneratePeople(int count) => PersonFaker.Generate(count);

	protected abstract TKey GeneratePersonId();

	protected virtual void ConfigureServices(IServiceCollection services) { }

	protected virtual Task<IRepository<TPerson, TKey>> GetRepositoryAsync() =>
		Task.FromResult(Services.GetRequiredService<IRepository<TPerson, TKey>>());

	private void BuildServices()
	{
		var services = new ServiceCollection();
		services.AddSystemTime(TestTime);
		ConfigureServices(services);
		_services = services.BuildServiceProvider();
		_scope = _services.CreateAsyncScope();
	}

	public async Task InitializeAsync()
	{
		BuildServices();

		People = GeneratePeople(EntitySetCount).ToImmutableList();
		Repository = await GetRepositoryAsync();

		await InitializeAsync(Repository);
	}

	protected virtual async Task InitializeAsync(IRepository<TPerson, TKey> repository)
	{
		await SeedAsync(repository);
	}

	public async ValueTask DisposeAsync()
	{
		await CleanupAsync();

		People = null;

		await _scope.DisposeAsync();
		(_services as IDisposable)?.Dispose();
	}

	protected virtual Task CleanupAsync() => Task.CompletedTask;

	protected virtual async Task SeedAsync(IRepository<TPerson, TKey> repository)
	{
		if (People != null)
			await repository.AddRangeAsync(People);
	}

	protected virtual IEnumerable<TPerson> NaturalOrder(IEnumerable<TPerson> source) => source;

	protected abstract Task AddRelationshipAsync(TPerson person, TRelationship relationship);

	protected abstract Task RemoveRelationshipAsync(TPerson person, TRelationship relationship);

	protected virtual Task<TPerson?> FindPersonAsync(object id)
	{
		var entity = People?.FirstOrDefault(x => Repository.GetEntityKey(x)?.Equals(id) ?? false);
		return Task.FromResult(entity);
	}

	protected virtual Task<TPerson> RandomPersonAsync(Expression<Func<TPerson, bool>>? predicate = null)
	{
		var result = People?.Random(predicate?.Compile());

		if (result == null)
			throw new InvalidOperationException("No person found");

		return Task.FromResult(result);
	}

	[Test]
	[Category("Integration")]
	public async Task Should_AddPerson_When_PersonIsNew()
	{
		var person = GeneratePerson();

		await Repository.AddAsync(person, CancellationToken.None);

		var id = Repository.GetEntityKey(person);
		await Assert.That((object?)id).IsNotNull();

		var found = await Repository.FindAsync(id!, CancellationToken.None);
		await Assert.That(found).IsNotNull();
		await Assert.That(found!.FirstName).IsEqualTo(person.FirstName);
		await Assert.That(found.LastName).IsEqualTo(person.LastName);
		await Assert.That(found.Email).IsEqualTo(person.Email);
	}

	[Test]
	[Category("Integration")]
	public async Task Should_AddPerson_When_CalledAsync()
	{
		var person = GeneratePerson();

		await Repository.AddAsync(person);

		var id = Repository.GetEntityKey(person);
		await Assert.That((object?)id).IsNotNull();

		var found = await Repository.FindAsync(id!, CancellationToken.None);
		await Assert.That(found).IsNotNull();
		await Assert.That(found!.FirstName).IsEqualTo(person.FirstName);
		await Assert.That(found.LastName).IsEqualTo(person.LastName);
		await Assert.That(found.Email).IsEqualTo(person.Email);
	}

	[Test]
	[Category("Integration")]
	public async Task Should_AddRange_When_PeopleAreNew()
	{
		var entities = GeneratePeople(10);

		await Repository.AddRangeAsync(entities, CancellationToken.None);

		foreach (var item in entities)
		{
			var key = Repository.GetEntityKey(item);
			await Assert.That((object?)key).IsNotNull();

			var found = await Repository.FindAsync(key!, CancellationToken.None);
			await Assert.That(found).IsNotNull();
		}
	}

	[Test]
	[Category("Integration")]
	public async Task Should_RemovePerson_When_PersonExists()
	{
		var person = await RandomPersonAsync();
		await Assert.That(person).IsNotNull();

		var result = await Repository.RemoveAsync(person, CancellationToken.None);

		await Assert.That(result).IsTrue();
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnFalse_When_RemovePersonNotFound()
	{
		var entity = GeneratePerson();
		entity.Id = GeneratePersonId();

		var result = await Repository.RemoveAsync(entity, CancellationToken.None);

		await Assert.That(result).IsFalse();
	}

	[Test]
	[Category("Integration")]
	public async Task Should_RemovePerson_When_CalledSync()
	{
		var person = People!.Random();
		await Assert.That(person).IsNotNull();

		var result = await Repository.RemoveAsync(person!);

		await Assert.That(result).IsTrue();
	}

	[Test]
	[Category("Integration")]
	public async Task Should_RemoveByKey_When_KeyExists()
	{
		var key = Repository.GetEntityKey(People!.Random()!);
		await Assert.That((object?)key).IsNotNull();

		var result = await Repository.RemoveByKeyAsync(key!, CancellationToken.None);

		await Assert.That(result).IsTrue();
	}

	[Test]
	[Category("Integration")]
	public async Task Should_RemoveByKey_When_CalledSync()
	{
		var key = Repository.GetEntityKey(People!.Random()!);
		await Assert.That((object?)key).IsNotNull();

		var result = await Repository.RemoveByKeyAsync(key!);

		await Assert.That(result).IsTrue();
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnFalse_When_RemoveByKeyNotFound()
	{
		var id = GeneratePersonId();

		var result = await Repository.RemoveByKeyAsync(id, CancellationToken.None);

		await Assert.That(result).IsFalse();
	}

	[Test]
	[Category("Integration")]
	public async Task Should_RemoveRange_When_PeopleExist()
	{
		var peopleCount = PeopleCount;
		var people = People!.Take(10).ToList();

		await Repository.RemoveRangeAsync(people, CancellationToken.None);

		var result = await Repository.FindAllAsync(CancellationToken.None);
		await Assert.That(result).IsNotNull();
		await Assert.That(result.Any()).IsTrue();
		await Assert.That(result.Count).IsEqualTo(peopleCount - 10);
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ThrowRepositoryException_When_RemoveRangeWithOneNotExisting()
	{
		var peopleCount = PeopleCount;
		var people = People!.Take(9).ToList();

		var entity = GeneratePerson();
		entity.Id = GeneratePersonId();
		people.Add(entity);

		await Assert.That(async () => await Repository.RemoveRangeAsync(people, CancellationToken.None))
			.Throws<RepositoryException>();

		var result = await Repository.FindAllAsync();
		await Assert.That(result).IsNotNull();
		await Assert.That(result.Any()).IsTrue();
		await Assert.That(result.Count).IsEqualTo(peopleCount);
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnTotalCount_When_CountAll()
	{
		var result = await Repository.CountAllAsync(CancellationToken.None);

		await Assert.That(result).IsNotEqualTo(0);
		await Assert.That(result).IsEqualTo(PeopleCount);
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnTotalCount_When_CountAllAsync()
	{
		var result = await Repository.CountAllAsync();

		await Assert.That(result).IsNotEqualTo(0);
		await Assert.That(result).IsEqualTo(PeopleCount);
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnFilteredCount_When_FilterApplied()
	{
		var person = await RandomPersonAsync();
		var firstName = person.FirstName;
		var peopleCount = People?.Count(x => x.FirstName == firstName) ?? 0;

		var count = await Repository.CountAsync(p => p.FirstName == firstName, CancellationToken.None);

		await Assert.That(count).IsEqualTo(peopleCount);
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnFilteredCount_When_FilterAppliedSync()
	{
		var person = await RandomPersonAsync();
		var firstName = person.FirstName;
		var peopleCount = People?.Count(x => x.FirstName == firstName) ?? 0;

		var count = await Repository.CountAsync(p => p.FirstName == firstName);

		await Assert.That(count).IsEqualTo(peopleCount);
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnPerson_When_FindByKey()
	{
		var person = await RandomPersonAsync();
		var id = person.Id!;

		var result = await Repository.FindAsync(id, CancellationToken.None);

		await Assert.That(result).IsNotNull();
		await Assert.That(result!.Id).IsEqualTo(id);
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnFirstMatch_When_FilterApplied()
	{
		var person = await RandomPersonAsync();
		var firstName = person.FirstName;

		var result = await Repository.FindFirstAsync(x => x.FirstName == firstName, CancellationToken.None);

		await Assert.That(result).IsNotNull();
		await Assert.That(result!.FirstName).IsEqualTo(firstName);
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnFirstMatchBySort_When_FilterAndSortApplied()
	{
		var person = await RandomPersonAsync(x => x.LastName != null);
		var firstName = person.FirstName;

		var expected = People?.Where(x => x.FirstName == firstName)
			.OrderBy(x => x.LastName)
			.FirstOrDefault();

		await Assert.That(expected).IsNotNull();

		var query = new QueryBuilder<TPerson>()
			.Where(x => x.FirstName == firstName)
			.OrderBy(x => x.LastName)
			.Query;

		var result = await Repository.FindFirstAsync(query, CancellationToken.None);

		await Assert.That(result).IsNotNull();
		await Assert.That(result!.FirstName).IsEqualTo(expected!.FirstName);
		await Assert.That(result.LastName).IsEqualTo(expected.LastName);
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnFirstPerson_When_FindFirstAsync()
	{
		var result = await Repository.FindFirstAsync();

		await Assert.That(result).IsNotNull();
		await Assert.That((object?)result!.Id).IsNotNull();
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnTrue_When_PersonExists()
	{
		var person = await RandomPersonAsync();
		var firstName = person.FirstName;

		var result = await Repository.ExistsAsync(x => x.FirstName == firstName, CancellationToken.None);

		await Assert.That(result).IsTrue();
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnTrue_When_PersonExistsSync()
	{
		var person = await RandomPersonAsync();
		var firstName = person.FirstName;

		var result = await Repository.ExistsAsync(x => x.FirstName == firstName);

		await Assert.That(result).IsTrue();
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnPerson_When_KeyExists()
	{
		var person = await RandomPersonAsync();

		var result = await Repository.FindAsync(person.Id!, CancellationToken.None);

		await Assert.That(result).IsNotNull();
		await Assert.That(result!.Id).IsEqualTo(person.Id);
		await Assert.That(result.FirstName).IsEqualTo(person.FirstName);
		await Assert.That(result.LastName).IsEqualTo(person.LastName);
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnNull_When_KeyNotFound()
	{
		var id = GeneratePersonId();

		var result = await Repository.FindAsync(id);

		await Assert.That(result).IsNull();
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnPerson_When_FindByKeySync()
	{
		var person = await RandomPersonAsync();

		var result = Repository.Find(person.Id!);

		await Assert.That(result).IsNotNull();
		await Assert.That(result!.Id).IsEqualTo(person.Id);
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnPersonWithRelationships_When_KeyExists()
	{
		var person = await RandomPersonAsync(x => x.Relationships != null && x.Relationships.Any());
		await Assert.That(person).IsNotNull();

		var result = await Repository.FindAsync(person.Id!, CancellationToken.None);

		await Assert.That(result).IsNotNull();
		await Assert.That(result!.Relationships).IsNotNull();
		await Assert.That(result.Relationships.Any()).IsTrue();
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnFirstPerson_When_FindFirst()
	{
		var ordered = NaturalOrder(People!).ToList();

		var result = await Repository.FindFirstAsync(CancellationToken.None);

		await Assert.That(result).IsNotNull();
		await Assert.That(result!.FirstName).IsEqualTo(ordered[0].FirstName);
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnFirstMatch_When_FilterAppliedSync()
	{
		var person = await RandomPersonAsync(x => x.FirstName != null);
		var ordered = NaturalOrder(People!.Where(x => x.FirstName == person.FirstName)).ToList();

		var result = await Repository.FindFirstAsync(QueryFilter.Where<TPerson>(x => x.FirstName == person.FirstName));

		await Assert.That(result).IsNotNull();
		await Assert.That(result!.Id).IsEqualTo(ordered[0].Id);
		await Assert.That(result.FirstName).IsEqualTo(ordered[0].FirstName);
		await Assert.That(result.LastName).IsEqualTo(ordered[0].LastName);
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnAllPeople_When_FindAll()
	{
		var result = await Repository.FindAllAsync(CancellationToken.None);

		await Assert.That(result).IsNotNull();
		await Assert.That(result.Any()).IsTrue();
		await Assert.That(result.Count).IsEqualTo(PeopleCount);
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnAllPeople_When_FindAllAsync()
	{
		var result = await Repository.FindAllAsync();

		await Assert.That(result).IsNotNull();
		await Assert.That(result.Any()).IsTrue();
		await Assert.That(result.Count).IsEqualTo(PeopleCount);
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnFilteredPeople_When_FilterApplied()
	{
		var person = await RandomPersonAsync();
		var firstName = person.FirstName;
		var peopleCount = People?.Count(x => x.FirstName == firstName) ?? 0;

		var result = await Repository.FindAllAsync(x => x.FirstName == firstName, CancellationToken.None);

		await Assert.That(result).IsNotNull();
		await Assert.That(result.Any()).IsTrue();
		await Assert.That(result.Count).IsEqualTo(peopleCount);
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnFilteredAndSortedPeople_When_FilterAndSortApplied()
	{
		var person = await RandomPersonAsync();
		var firstName = person.FirstName;

		var query = new QueryBuilder<TPerson>()
			.Where(x => x.FirstName == firstName)
			.OrderBy(x => x.FirstName);

		var result = await Repository.FindAllAsync(query, CancellationToken.None);

		await Assert.That(result).IsNotNull();
		await Assert.That(result.Any()).IsTrue();
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ThrowRepositoryException_When_FilterTypeIsInvalid()
	{
		var person = await RandomPersonAsync();

		await Assert.That(async () => await Repository.FindAllAsync(
			QueryFilter.Where<MailAddress>(m => m.Address == null),
			CancellationToken.None))
			.Throws<RepositoryException>();
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnPage_When_NoFilterApplied()
	{
		var totalItems = PeopleCount;
		var totalPages = (int)Math.Ceiling((double)totalItems / 10);

		var result = await Repository.GetPageAsync(1, 10, CancellationToken.None);

		await Assert.That(result).IsNotNull();
		await Assert.That(result.TotalPages).IsEqualTo(totalPages);
		await Assert.That(result.TotalItems).IsEqualTo(totalItems);
		await Assert.That(result.Items).IsNotNull();
		await Assert.That(result.Items.Any()).IsTrue();
		await Assert.That(result.Items.Count).IsEqualTo(10);
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnPage_When_PageParametersProvided()
	{
		var totalItems = PeopleCount;
		var totalPages = (int)Math.Ceiling((double)totalItems / 10);

		var result = await Repository.GetPageAsync(1, 10, CancellationToken.None);

		await Assert.That(result).IsNotNull();
		await Assert.That(result.TotalPages).IsEqualTo(totalPages);
		await Assert.That(result.TotalItems).IsEqualTo(totalItems);
		await Assert.That(result.Items).IsNotNull();
		await Assert.That(result.Items.Any()).IsTrue();
		await Assert.That(result.Items.Count).IsEqualTo(10);
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnFilteredPage_When_FilterApplied()
	{
		var person = await RandomPersonAsync();
		var firstName = person.FirstName;
		var peopleCount = People?.Count(x => x.FirstName == firstName) ?? 0;
		var totalPages = (int)Math.Ceiling((double)peopleCount / 10);
		var perPage = Math.Min(peopleCount, 10);

		var request = new PageQuery<TPerson>(1, 10)
			.Where(x => x.FirstName == firstName);

		var result = await Repository.GetPageAsync(request, CancellationToken.None);

		await Assert.That(result).IsNotNull();
		await Assert.That(result.TotalPages).IsEqualTo(totalPages);
		await Assert.That(result.TotalItems).IsEqualTo(peopleCount);
		await Assert.That(result.Items).IsNotNull();
		await Assert.That(result.Items.Any()).IsTrue();
		await Assert.That(result.Items.Count()).IsEqualTo(perPage);
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnFilteredPage_When_MultipleFiltersApplied()
	{
		var person = await RandomPersonAsync(x => x.LastName != null);
		var firstName = person.FirstName;
		var lastName = person.LastName;

		var peopleCount = People?.Count(x => x.FirstName == firstName && x.LastName == lastName) ?? 0;
		var totalPages = (int)Math.Ceiling((double)peopleCount / 10);
		var perPage = Math.Min(peopleCount, 10);

		var request = new PageQuery<TPerson>(1, 10)
			.Where(x => x.FirstName == firstName && x.LastName == lastName);

		var result = await Repository.GetPageAsync(request, CancellationToken.None);

		await Assert.That(result).IsNotNull();
		await Assert.That(result.TotalPages).IsEqualTo(totalPages);
		await Assert.That(result.TotalItems).IsEqualTo(peopleCount);
		await Assert.That(result.Items).IsNotNull();
		await Assert.That(result.Items.Any()).IsTrue();
		await Assert.That(result.Items.Count).IsEqualTo(perPage);
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnFilteredPage_When_FiltersChained()
	{
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

		var result = await Repository.GetPageAsync(request, CancellationToken.None);

		await Assert.That(result).IsNotNull();
		await Assert.That(result.TotalPages).IsEqualTo(totalPages);
		await Assert.That(result.TotalItems).IsEqualTo(peopleCount);
		await Assert.That(result.Items).IsNotNull();
		await Assert.That(result.Items.Any()).IsTrue();
		await Assert.That(result.Items.Count).IsEqualTo(perPage);
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnDescendingSortedPage_When_SortApplied()
	{
		var sorted = People!.Where(x => x.LastName != null)
			.OrderByDescending(x => x.LastName).Skip(0).Take(10).ToList();

		var request = new PageQuery<TPerson>(1, 10)
			.OrderByDescending(x => x.LastName);

		var result = await Repository.GetPageAsync(request, CancellationToken.None);

		await Assert.That(result).IsNotNull();
		await Assert.That(result.TotalPages).IsEqualTo(10);
		await Assert.That(result.TotalItems).IsEqualTo(100);
		await Assert.That(result.Items).IsNotNull();
		await Assert.That(result.Items.Any()).IsTrue();
		await Assert.That(result.Items.Count()).IsEqualTo(10);

		for (int i = 0; i < sorted.Count; i++)
		{
			await Assert.That(result.Items.ElementAt(i).LastName).IsEqualTo(sorted[i].LastName);
		}
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnSortedPage_When_SortApplied()
	{
		var totalPages = (int)Math.Ceiling((double)PeopleCount / 10);

		var request = new PageQuery<TPerson>(1, 10)
			.OrderBy(x => x.FirstName);

		var result = await Repository.GetPageAsync(request, CancellationToken.None);

		await Assert.That(result).IsNotNull();
		await Assert.That(result.TotalPages).IsEqualTo(totalPages);
		await Assert.That(result.TotalItems).IsEqualTo(PeopleCount);
		await Assert.That(result.Items).IsNotNull();
		await Assert.That(result.Items.Any()).IsTrue();
		await Assert.That(result.Items.Count).IsEqualTo(10);
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnPage_When_GetPageSync()
	{
		var totalPages = (int)Math.Ceiling((double)PeopleCount / 10);
		var request = new PageQuery<TPerson>(1, 10);

		var result = await Repository.GetPageAsync(request);

		await Assert.That(result).IsNotNull();
		await Assert.That(result.TotalPages).IsEqualTo(totalPages);
		await Assert.That(result.TotalItems).IsEqualTo(PeopleCount);
		await Assert.That(result.Items).IsNotNull();
		await Assert.That(result.Items.Any()).IsTrue();
		await Assert.That(result.Items.Count).IsEqualTo(10);
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnEntityKey_When_PersonExists()
	{
		var person = await RandomPersonAsync();

		var id = Repository.GetEntityKey(person);

		await Assert.That((object?)id).IsNotNull();
		await Assert.That(id).IsEqualTo(person.Id);
	}

	[Test]
	[Category("Integration")]
	public async Task Should_UpdatePerson_When_PersonExists()
	{
		var person = await RandomPersonAsync(x => x.FirstName != "John");
		var toUpdate = await Repository.FindAsync(person.Id!, CancellationToken.None);
		await Assert.That(toUpdate).IsNotNull();
		toUpdate!.FirstName = "John";

		var result = await Repository.UpdateAsync(toUpdate, CancellationToken.None);

		await Assert.That(result).IsTrue();

		var updated = await Repository.FindAsync(person.Id!, CancellationToken.None);
		await Assert.That(updated).IsNotNull();
		await Assert.That(updated!.FirstName).IsEqualTo(toUpdate.FirstName);
		await Assert.That(updated.LastName).IsEqualTo(toUpdate.LastName);
		await Assert.That(updated.Email).IsEqualTo(toUpdate.Email);
		await Assert.That(updated.DateOfBirth).IsEqualTo(toUpdate.DateOfBirth);
	}

	[Test]
	[Category("Integration")]
	public async Task Should_UpdatePerson_When_CalledSync()
	{
		var person = await RandomPersonAsync(x => x.FirstName != "John");
		var toUpdate = await Repository.FindAsync(person.Id!, CancellationToken.None);
		await Assert.That(toUpdate).IsNotNull();
		toUpdate!.FirstName = "John";

		var result = await Repository.UpdateAsync(toUpdate);

		await Assert.That(result).IsTrue();

		var updated = await Repository.FindAsync(person.Id!, CancellationToken.None);
		await Assert.That(updated).IsNotNull();
		await Assert.That(updated!.FirstName).IsEqualTo(toUpdate.FirstName);
	}

	[Test]
	[Category("Integration")]
	public async Task Should_ReturnFalse_When_UpdatePersonNotFound()
	{
		var person = GeneratePerson();
		person.Id = GeneratePersonId();

		var result = await Repository.UpdateAsync(person, CancellationToken.None);

		await Assert.That(result).IsFalse();
	}

	[Test]
	[Category("Integration")]
	public async Task Should_KeepEntityUnchanged_When_UpdateWithNoChanges()
	{
		var person = await RandomPersonAsync();
		var toUpdate = await Repository.FindAsync(person.Id!, CancellationToken.None);
		await Assert.That(toUpdate).IsNotNull();

		await Repository.UpdateAsync(toUpdate!);

		var updated = await Repository.FindAsync(person.Id!, CancellationToken.None);
		await Assert.That(updated).IsNotNull();
		await Assert.That(updated).IsEqualTo(toUpdate);
	}

	[Test]
	[Category("Integration")]
	public async Task Should_AddRelationship_When_UpdatePersonWithNewRelationship()
	{
		var person = People!.Random(x => x.Relationships == null || !x.Relationships.Any());
		await Assert.That(person).IsNotNull();

		var relationship = GenerateRelationship();
		var toUpdate = await Repository.FindAsync(person!.Id!, CancellationToken.None);
		await Assert.That(toUpdate).IsNotNull();

		await AddRelationshipAsync(toUpdate!, relationship);

		var result = await Repository.UpdateAsync(toUpdate!, CancellationToken.None);

		await Assert.That(result).IsTrue();

		var updated = await Repository.FindAsync(person.Id!, CancellationToken.None);
		await Assert.That(updated).IsNotNull();
		await Assert.That(updated!.Relationships).IsNotNull();
		await Assert.That(updated.Relationships.Any()).IsTrue();
		await Assert.That(updated.Relationships).HasSingleItem();
	}

	[Test]
	[Category("Integration")]
	public async Task Should_RemoveRelationship_When_UpdatePersonWithRelationshipRemoved()
	{
		var person = People!.Random(x => x.Relationships?.Any() ?? false);
		await Assert.That(person).IsNotNull();

		var toUpdate = await Repository.FindAsync(person!.Id!, CancellationToken.None);
		await Assert.That(toUpdate).IsNotNull();

		var relCount = toUpdate!.Relationships.Count();

		await RemoveRelationshipAsync(toUpdate, (TRelationship)toUpdate.Relationships!.First());

		var result = await Repository.UpdateAsync(toUpdate, CancellationToken.None);

		await Assert.That(result).IsTrue();

		var updated = await Repository.FindAsync(person.Id!, CancellationToken.None);
		await Assert.That(updated).IsNotNull();
		await Assert.That(updated!.Relationships).IsNotNull();
		await Assert.That(updated.Relationships.Count()).IsEqualTo(relCount - 1);
	}
}

