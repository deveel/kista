// Copyright 2023-2026 Antonello Provenzano
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Net.Mail;

using Bogus;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kista;

/// <summary>
/// Shared base class for <see cref="NoKeyRepositoryTestSuite{TPerson, TRelationship}"/>
/// and <see cref="RepositoryTestSuite{TPerson, TKey}"/> that hosts the test
/// methods whose bodies are identical between the two suites, so SonarQube
/// no longer reports them as duplicated blocks.
/// </summary>
/// <typeparam name="TPerson">
/// The person entity type used by the test suite. The base class is
/// agnostic of the key shape: subclasses adapt it to either
/// <see cref="IRepository{TEntity}"/> (TKey=object) or
/// <see cref="IRepository{TEntity, TKey}"/> by overriding the
/// <c>AddAsync</c>/<c>FindAsync</c>/<c>GetEntityKey</c>/<c>GetPageAsync</c>
/// helpers and the person property hooks.
/// </typeparam>
public abstract class RepositoryTestSuiteBase<TPerson> : IAsyncLifetime
	where TPerson : class {
	private IServiceProvider? rootServiceProvider;
	private AsyncServiceScope scope;

	protected RepositoryTestSuiteBase(ITestOutputHelper? testOutput) {
		TestOutput = testOutput;
	}

	protected ITestOutputHelper? TestOutput { get; }

	protected virtual int EntitySetCount => 100;

	protected IReadOnlyList<TPerson>? People { get; private set; }

	protected int PeopleCount => People?.Count ?? 0;

	protected IServiceProvider Services => scope.ServiceProvider;

	protected abstract Faker<TPerson> PersonFaker { get; }

	protected ISystemTime TestTime { get; } = new TestTime();

	protected TPerson GeneratePerson() => PersonFaker.Generate();

	protected IList<TPerson> GeneratePeople(int count) => PersonFaker.Generate(count);

	// ---- Person property / key hooks (key-agnostic) ----

	protected abstract object GeneratePersonKey();
	protected abstract void AssignPersonKey(TPerson person, object key);
	protected abstract object? GetPersonKey(TPerson person);
	protected abstract string GetFirstName(TPerson person);
	protected abstract string GetLastName(TPerson person);
	protected abstract string? GetEmail(TPerson person);
	protected abstract DateTime? GetDateOfBirth(TPerson person);
	protected abstract void SetFirstName(TPerson person, string firstName);

	/// <summary>
	/// Expression-based selectors for the person properties, used to build
	/// LINQ predicates that can be translated by server-side query providers
	/// (e.g. Entity Framework). Subclasses return property-access expressions
	/// such as <c>x => x.FirstName</c> so that the composed predicates do not
	/// contain method calls that the provider cannot translate.
	/// </summary>
	protected abstract Expression<Func<TPerson, string>> FirstNameSelector { get; }
	protected abstract Expression<Func<TPerson, string>> LastNameSelector { get; }
	protected abstract Expression<Func<TPerson, string?>> EmailSelector { get; }
	protected abstract Expression<Func<TPerson, DateTime?>> DateOfBirthSelector { get; }

	/// <summary>
	/// Builds a predicate that compares the selected property to a constant
	/// value, returning an expression tree composed of property accesses
	/// (no method calls) so that server-side query providers can translate it.
	/// </summary>
	protected static Expression<Func<TPerson, bool>> EqualTo<TValue>(
		Expression<Func<TPerson, TValue>> selector, TValue value) {
		var param = selector.Parameters[0];
		var body = Expression.Equal(selector.Body, Expression.Constant(value, typeof(TValue)));
		return Expression.Lambda<Func<TPerson, bool>>(body, param);
	}

	/// <summary>
	/// Builds a predicate that compares the selected property to a constant
	/// value using the greater-than-or-equal operator, returning an
	/// expression tree composed of property accesses so that server-side
	/// query providers can translate it.
	/// </summary>
	protected static Expression<Func<TPerson, bool>> GreaterThanOrEqual<TValue>(
		Expression<Func<TPerson, TValue>> selector, TValue value) {
		var param = selector.Parameters[0];
		var body = Expression.GreaterThanOrEqual(selector.Body, Expression.Constant(value, typeof(TValue)));
		return Expression.Lambda<Func<TPerson, bool>>(body, param);
	}

	/// <summary>
	/// Converts a typed selector to an <c>object?</c>-returning selector
	/// suitable for the <see cref="PageQuery{TEntity}.OrderBy"/> /
	/// <see cref="PageQuery{TEntity}.OrderByDescending"/> overloads, while
	/// keeping the underlying property access so server-side query
	/// providers can translate the sort.
	/// </summary>
	protected static Expression<Func<TPerson, object?>> ToObjectSelector<TValue>(
		Expression<Func<TPerson, TValue>> selector) {
		var param = selector.Parameters[0];
		UnaryExpression body;
		if (typeof(TValue).IsValueType)
			body = Expression.Convert(selector.Body, typeof(object));
		else
			body = Expression.TypeAs(selector.Body, typeof(object));
		return Expression.Lambda<Func<TPerson, object?>>(body, param);
	}

	// ---- Repository operation hooks (key-agnostic) ----
	//
	// These wrap the underlying IRepository<TPerson, object> (T2) or
	// IRepository<TPerson, TKey> (T3) so the shared tests can call them
	// without knowing the key type. Subclasses forward each call to their
	// own typed Repository, casting the key where needed.

	protected abstract ValueTask AddAsync(TPerson entity, CancellationToken cancellationToken);
	protected abstract ValueTask AddRangeAsync(IEnumerable<TPerson> entities, CancellationToken cancellationToken);
	protected abstract ValueTask<bool> RemoveAsync(TPerson entity, CancellationToken cancellationToken);
	protected abstract ValueTask RemoveRangeAsync(IEnumerable<TPerson> entities, CancellationToken cancellationToken);
	protected abstract ValueTask<bool> RemoveByKeyAsync(object key, CancellationToken cancellationToken);
	protected abstract ValueTask<TPerson?> FindAsync(object key, CancellationToken cancellationToken);
	protected abstract ValueTask<bool> UpdateAsync(TPerson entity, CancellationToken cancellationToken);
	protected abstract object? GetEntityKey(TPerson entity);
	protected abstract ValueTask<PageResult<TPerson>> GetPageAsync(PageRequest request, CancellationToken cancellationToken);
	protected abstract PageResult<TPerson> GetPage(PageRequest request);

	protected virtual ValueTask<PageResult<TPerson>> GetPageAsync(int page, int size, CancellationToken cancellationToken)
		=> GetPageAsync(new PageRequest(page, size), cancellationToken);

	protected virtual void ConfigureServices(IServiceCollection services) {
		if (TestOutput != null)
			services.AddLogging(logging => { logging.ClearProviders(); logging.AddXUnit(TestOutput); });
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

		People = GeneratePeople(EntitySetCount).ToImmutableList();
		await InitializeAsync();
	}

	protected virtual async ValueTask InitializeAsync() {
		await SeedAsync();
	}

	async ValueTask IAsyncDisposable.DisposeAsync() {
		await DisposeAsync();

		People = null;

		await scope.DisposeAsync();
		(rootServiceProvider as IDisposable)?.Dispose();
	}

	protected virtual ValueTask DisposeAsync() {
		return ValueTask.CompletedTask;
	}

	/// <summary>
	/// Seeds the resolved repository with the generated <see cref="People"/>.
	/// Subclasses forward to their own typed repository.
	/// </summary>
	protected abstract Task SeedAsync();

	protected virtual IEnumerable<TPerson> NaturalOrder(IEnumerable<TPerson> source) {
		return source;
	}

	protected virtual void ClearRelationships(TPerson person) {
	}

	protected virtual Task<TPerson?> FindPersonAsync(object id) {
		var entity = People?.FirstOrDefault(x => GetPersonKey(x)?.Equals(id) ?? false);
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
		await AddAsync(person, TestContext.Current.CancellationToken);

		// Assert
		var id = GetEntityKey(person);
		Assert.NotNull(id);

		var found = await FindAsync(id, TestContext.Current.CancellationToken);
		Assert.NotNull(found);
		Assert.Equal(GetFirstName(person), GetFirstName(found));
		Assert.Equal(GetLastName(person), GetLastName(found));
		Assert.Equal(GetEmail(person), GetEmail(found));
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_AddRange_When_PeopleAreNew() {
		// Arrange
		var entities = GeneratePeople(10);

		// Act
		await AddRangeAsync(entities, TestContext.Current.CancellationToken);

		// Assert
		foreach (var item in entities) {
			var key = GetEntityKey(item);
			Assert.NotNull(key);

			var found = await FindAsync(key, TestContext.Current.CancellationToken);
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
		var result = await RemoveAsync(person, TestContext.Current.CancellationToken);

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
		AssignPersonKey(entity, GeneratePersonKey());

		// Act
		var result = await RemoveAsync(entity, TestContext.Current.CancellationToken);

		// Assert
		Assert.False(result);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_RemoveByKey_When_KeyExists() {
		// Arrange
		var key = GetEntityKey(People!.Random()!);
		Assert.NotNull(key);

		// Act
		var result = await RemoveByKeyAsync(key, TestContext.Current.CancellationToken);

		// Assert
		Assert.True(result);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ReturnFalse_When_RemoveByKeyNotFound() {
		// Arrange
		var id = GeneratePersonKey();

		// Act
		var result = await RemoveByKeyAsync(id, TestContext.Current.CancellationToken);

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
		await RemoveRangeAsync(people, TestContext.Current.CancellationToken);

		// Assert
		var result = await Task.FromResult((IReadOnlyList<TPerson>)GetPageAsync(new PageRequest(1, int.MaxValue), CancellationToken.None).GetAwaiter().GetResult().Items.AsQueryable().ToList());
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
		AssignPersonKey(entity, GeneratePersonKey());
		people.Add(entity);

		// Act & Assert
		await Assert.ThrowsAsync<RepositoryException>(async () => await RemoveRangeAsync(people, TestContext.Current.CancellationToken));

		var result = await Task.FromResult((IReadOnlyList<TPerson>)GetPageAsync(new PageRequest(1, int.MaxValue), CancellationToken.None).GetAwaiter().GetResult().Items.AsQueryable().ToList());
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
		var result = await Task.FromResult(GetPageAsync(new PageRequest(1, int.MaxValue), CancellationToken.None).GetAwaiter().GetResult().Items.AsQueryable().LongCount());

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
		var firstName = GetFirstName(person);
		var peopleCount = People?.Count(x => GetFirstName(x) == firstName) ?? 0;

		// Act
		var count = await Task.FromResult(QueryFilter.Where<TPerson>(p => GetFirstName(p) == firstName).Apply<TPerson>(GetPageAsync(new PageRequest(1, int.MaxValue), CancellationToken.None).GetAwaiter().GetResult().Items.AsQueryable()).LongCount());

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
		var id = GetPersonKey(person)!;

		// Act
		var result = await FindAsync(id, TestContext.Current.CancellationToken);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(id, GetPersonKey(result));
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ReturnFirstMatch_When_FilterApplied() {
		// Arrange
		var person = await RandomPersonAsync();
		var firstName = GetFirstName(person);

		// Act
		var result = await Task.FromResult(QueryFilter.Where<TPerson>(x => GetFirstName(x) == firstName).Apply<TPerson>(GetPageAsync(new PageRequest(1, int.MaxValue), CancellationToken.None).GetAwaiter().GetResult().Items.AsQueryable()).FirstOrDefault());

		// Assert
		Assert.NotNull(result);
		Assert.Equal(firstName, GetFirstName(result));
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ReturnFirstMatchBySort_When_FilterAndSortApplied() {
		// Arrange
		var person = await RandomPersonAsync(x => GetLastName(x) != null);
		var firstName = GetFirstName(person);

		var expected = People?.Where(x => GetFirstName(x) == firstName)
			.OrderBy(x => GetLastName(x))
			.FirstOrDefault();

		Assert.NotNull(expected);

		var query = new QueryBuilder<TPerson>()
			.Where(x => GetFirstName(x) == firstName)
			.OrderBy(x => GetLastName(x))
			.Query;

		// Act
		var result = await Task.FromResult(query.Apply<TPerson>(GetPageAsync(new PageRequest(1, int.MaxValue), CancellationToken.None).GetAwaiter().GetResult().Items.AsQueryable()).FirstOrDefault());

		// Assert
		Assert.NotNull(result);
		Assert.Equal(GetFirstName(expected), GetFirstName(result));
		Assert.Equal(GetLastName(expected), GetLastName(result));
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ReturnTrue_When_PersonExists() {
		// Arrange
		var person = await RandomPersonAsync();
		var firstName = GetFirstName(person);

		// Act
		var result = await Task.FromResult(QueryFilter.Where<TPerson>(x => GetFirstName(x) == firstName).Apply<TPerson>(GetPageAsync(new PageRequest(1, int.MaxValue), CancellationToken.None).GetAwaiter().GetResult().Items.AsQueryable()).Any());

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
		var result = await FindAsync(GetPersonKey(person)!, TestContext.Current.CancellationToken);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(GetPersonKey(person), GetPersonKey(result));
		Assert.Equal(GetFirstName(person), GetFirstName(result));
		Assert.Equal(GetLastName(person), GetLastName(result));
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ReturnNull_When_KeyNotFound() {
		// Arrange
		var id = GeneratePersonKey();

		// Act
		var result = await FindAsync(id, TestContext.Current.CancellationToken);

		// Assert
		Assert.Null(result);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ReturnFirstPerson_When_FindFirst() {
		// Arrange
		var ordered = NaturalOrder(People!).ToList();

		// Act
		var result = await Task.FromResult(GetPageAsync(new PageRequest(1, int.MaxValue), CancellationToken.None).GetAwaiter().GetResult().Items.AsQueryable().FirstOrDefault());

		// Assert
		Assert.NotNull(result);
		Assert.Equal(GetFirstName(ordered[0]), GetFirstName(result));
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ReturnFirstMatch_When_FilterAppliedAsync() {
		// Arrange
		var person = await RandomPersonAsync(x => GetFirstName(x) != null);
		var ordered = NaturalOrder(People!.Where(x => GetFirstName(x) == GetFirstName(person))).ToList();

		// Act
		var result = await Task.FromResult(QueryFilter.Where<TPerson>(x => GetFirstName(x) == GetFirstName(person)).Apply<TPerson>(GetPageAsync(new PageRequest(1, int.MaxValue), CancellationToken.None).GetAwaiter().GetResult().Items.AsQueryable()).FirstOrDefault());

		// Assert
		Assert.NotNull(result);
		Assert.Equal(GetPersonKey(ordered[0]), GetPersonKey(result));
		Assert.Equal(GetFirstName(ordered[0]), GetFirstName(result));
		Assert.Equal(GetLastName(ordered[0]), GetLastName(result));
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ReturnAllPeople_When_FindAll() {
		// Act
		var result = await Task.FromResult((IReadOnlyList<TPerson>)GetPageAsync(new PageRequest(1, int.MaxValue), CancellationToken.None).GetAwaiter().GetResult().Items.AsQueryable().ToList());

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
		var firstName = GetFirstName(person);
		var peopleCount = People?.Count(x => GetFirstName(x) == firstName) ?? 0;

		// Act
		var result = await Task.FromResult((IReadOnlyList<TPerson>)QueryFilter.Where<TPerson>(x => GetFirstName(x) == firstName).Apply<TPerson>(GetPageAsync(new PageRequest(1, int.MaxValue), CancellationToken.None).GetAwaiter().GetResult().Items.AsQueryable()).ToList());

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
		var firstName = GetFirstName(person);
		var expected = People?.Where(x => GetFirstName(x) == firstName)
			.OrderBy(x => GetFirstName(x))
			.ToList();
		Assert.NotNull(expected);

		var query = new QueryBuilder<TPerson>()
			.Where(x => GetFirstName(x) == firstName)
			.OrderBy(x => GetFirstName(x));

		// Act
		var result = await Task.FromResult((IReadOnlyList<TPerson>)query.Apply<TPerson>(GetPageAsync(new PageRequest(1, int.MaxValue), CancellationToken.None).GetAwaiter().GetResult().Items.AsQueryable()).ToList());

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
		await RandomPersonAsync();

		// Act & Assert
		Assert.Throws<ArgumentException>(
			() => QueryFilter.Where<MailAddress>(m => m.Address == null).Apply<TPerson>(GetPageAsync(new PageRequest(1, int.MaxValue), CancellationToken.None).GetAwaiter().GetResult().Items.AsQueryable()));
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
		var result = await GetPageAsync(1, 10, TestContext.Current.CancellationToken);

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
		var firstName = GetFirstName(person);
		var peopleCount = People?.Count(x => GetFirstName(x) == firstName) ?? 0;
		var totalPages = (int)Math.Ceiling((double)peopleCount / 10);
		var perPage = Math.Min(peopleCount, 10);

		var request = new PageQuery<TPerson>(1, 10)
			.Where(EqualTo(FirstNameSelector, firstName));

		// Act
		var result = await GetPageAsync(request, TestContext.Current.CancellationToken);

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
		var person = await RandomPersonAsync(x => GetLastName(x) != null);
		var firstName = GetFirstName(person);
		var lastName = GetLastName(person);

		var peopleCount = People?.Count(x => GetFirstName(x) == firstName && GetLastName(x) == lastName) ?? 0;
		var totalPages = (int)Math.Ceiling((double)peopleCount / 10);
		var perPage = Math.Min(peopleCount, 10);

		var request = new PageQuery<TPerson>(1, 10)
			.Where(EqualTo(FirstNameSelector, firstName))
			.Where(EqualTo(LastNameSelector, lastName));

		// Act
		var result = await GetPageAsync(request, TestContext.Current.CancellationToken);

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
		var person = await RandomPersonAsync(x => GetDateOfBirth(x) != null);
		var firstName = GetFirstName(person);
		var birthDate = GetDateOfBirth(person)!.Value;

		var peopleCount = People?
			.Count(x => GetFirstName(x) == firstName && GetDateOfBirth(x) >= birthDate) ?? 0;

		var totalPages = (int)Math.Ceiling((double)peopleCount / 10);
		var perPage = Math.Min(peopleCount, 10);

		var request = new PageQuery<TPerson>(1, 10)
			.Where(EqualTo(FirstNameSelector, firstName))
			.Where(GreaterThanOrEqual(DateOfBirthSelector, birthDate));

		// Act
		var result = await GetPageAsync(request, TestContext.Current.CancellationToken);

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
		var sorted = People!.Where(x => GetLastName(x) != null)
			.OrderByDescending(x => GetLastName(x)).Skip(0).Take(10).ToList();

		var request = new PageQuery<TPerson>(1, 10)
			.OrderByDescending(ToObjectSelector(LastNameSelector));

		// Act
		var result = await GetPageAsync(request, TestContext.Current.CancellationToken);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(10, result.TotalPages);
		Assert.Equal(100, result.TotalItems);
		Assert.NotNull(result.Items);
		Assert.NotEmpty(result.Items);
		Assert.Equal(10, result.Items.Count());

		for (int i = 0; i < sorted.Count; i++) {
			Assert.Equal(GetLastName(sorted[i]), GetLastName(result.Items[i]));
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
			.OrderBy(ToObjectSelector(FirstNameSelector));

		// Act
		var result = await GetPageAsync(request, TestContext.Current.CancellationToken);

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
		var result = GetPage(request);

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
		var id = GetEntityKey(person);

		// Assert
		Assert.NotNull(id);
		// Compare via ToString() to handle entities that expose the key
		// both as a strongly-typed value (e.g. ObjectId) and as a string
		// through an explicit interface implementation (e.g. IPerson.Id).
		Assert.Equal(GetPersonKey(person)?.ToString(), id.ToString());
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_UpdatePerson_When_PersonExists() {
		// Arrange
		var person = await RandomPersonAsync(x => GetFirstName(x) != "John");
		var toUpdate = await FindAsync(GetPersonKey(person)!, TestContext.Current.CancellationToken);
		Assert.NotNull(toUpdate);
		SetFirstName(toUpdate, "John");

		// Act
		var result = await UpdateAsync(toUpdate, TestContext.Current.CancellationToken);

		// Assert
		Assert.True(result);

		var updated = await FindAsync(GetPersonKey(person)!, TestContext.Current.CancellationToken);
		Assert.NotNull(updated);
		Assert.Equal(GetFirstName(toUpdate), GetFirstName(updated!));
		Assert.Equal(GetLastName(toUpdate), GetLastName(updated!));
		Assert.Equal(GetEmail(toUpdate), GetEmail(updated!));
		Assert.Equal(GetDateOfBirth(toUpdate), GetDateOfBirth(updated!));
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Infrastructure")]
	[Trait("Feature", "Repository")]
	public async Task Should_ReturnFalse_When_UpdatePersonNotFound() {
		// Arrange
		var person = GeneratePerson();
		AssignPersonKey(person, GeneratePersonKey());
		ClearRelationships(person);

		// Act
		var result = await UpdateAsync(person, TestContext.Current.CancellationToken);

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
		var toUpdate = await FindAsync(GetPersonKey(person)!, TestContext.Current.CancellationToken);
		Assert.NotNull(toUpdate);

		// Act
		await UpdateAsync(toUpdate, TestContext.Current.CancellationToken);

		// Assert
		var updated = await FindAsync(GetPersonKey(person)!, TestContext.Current.CancellationToken);
		Assert.NotNull(updated);
		Assert.Equal(toUpdate, updated);
	}
}