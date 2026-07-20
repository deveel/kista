namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "FilterableRepository")]
public class FilterableRepositoryTests {
    private const string FirstNameExpression = "x.FirstName";
    private static readonly Faker<Person> PersonFaker = new Faker<Person>("en")
        .RuleFor(x => x.Id, f => f.Random.Guid().ToString())
        .RuleFor(x => x.FirstName, f => f.Name.FirstName())
        .RuleFor(x => x.LastName, f => f.Name.LastName())
        .RuleFor(x => x.DateOfBirth, f => f.Date.Past(20))
        .RuleFor(x => x.Email, f => f.Internet.Email().OrNull(f))
        .RuleFor(x => x.Phone, f => f.Phone.PhoneNumber().OrNull(f));

    private readonly IList<Person> _persons;
    private readonly IRepository<Person, string> _repository;

    public FilterableRepositoryTests() {
        _persons = PersonFaker.Generate(100).ToList();
        _repository = new InMemoryRepository<Person, string>(_persons);
    }

    private Person RandomPerson() => _persons[Random.Shared.Next(0, _persons.Count - 1)];

    #region CountAsync

    [Fact]
    public void Should_ReturnFilteredCount_When_ParameterNameProvided() {
        var person = RandomPerson();
        var expected = _persons.Count(x => x.FirstName == person.FirstName);

        var count = new DynamicLinqFilter("p", $"p.FirstName == \"{person.FirstName}\"").Apply<Person>(_persons.AsQueryable()).LongCount();

        Assert.Equal(expected, count);
    }

    [Fact]
    public void Should_ReturnFilteredCount_When_NoParameterName() {
        var person = RandomPerson();
        var expected = _persons.Count(x => x.FirstName == person.FirstName);

        var count = new DynamicLinqFilter($"x.FirstName == \"{person.FirstName}\"").Apply<Person>(_persons.AsQueryable()).LongCount();

        Assert.Equal(expected, count);
    }

    [Fact]
    public void Should_ThrowInvalidOperationException_When_CountExpressionInvalid() {
        Assert.Throws<InvalidOperationException>(
            () => new DynamicLinqFilter(FirstNameExpression).Apply<Person>(_persons.AsQueryable()).LongCount());
    }

    #endregion

    #region ExistsAsync

    [Fact]
    public void Should_ReturnTrue_When_EntityMatchesExpressionWithParameterName() {
        var person = RandomPerson();
        var expected = _persons.Any(x => x.FirstName == person.FirstName);

        var result = new DynamicLinqFilter("p", $"p.FirstName == \"{person.FirstName}\"").Apply<Person>(_persons.AsQueryable()).Any();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Should_ReturnTrue_When_EntityMatchesExpressionWithoutParameterName() {
        var person = RandomPerson();
        var expected = _persons.Any(x => x.FirstName == person.FirstName);

        var result = new DynamicLinqFilter($"x.FirstName == \"{person.FirstName}\"").Apply<Person>(_persons.AsQueryable()).Any();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Should_ThrowInvalidOperationException_When_ExistsExpressionInvalid() {
        Assert.Throws<InvalidOperationException>(
            () => new DynamicLinqFilter(FirstNameExpression).Apply<Person>(_persons.AsQueryable()).Any());
    }

    #endregion

    #region FindFirstAsync

    [Fact]
    public void Should_ReturnFirstMatch_When_ExpressionWithParameterName() {
        var person = RandomPerson();
        var expected = _persons.FirstOrDefault(x => x.FirstName == person.FirstName);

        var result = new DynamicLinqFilter("p", $"p.FirstName == \"{person.FirstName}\"").Apply<Person>(_persons.AsQueryable()).FirstOrDefault();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Should_ReturnFirstMatch_When_ExpressionWithoutParameterName() {
        var person = RandomPerson();
        var expected = _persons.FirstOrDefault(x => x.FirstName == person.FirstName);

        var result = new DynamicLinqFilter($"x.FirstName == \"{person.FirstName}\"").Apply<Person>(_persons.AsQueryable()).FirstOrDefault();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Should_ThrowInvalidOperationException_When_FindFirstExpressionInvalid() {
        Assert.Throws<InvalidOperationException>(
            () => new DynamicLinqFilter(FirstNameExpression).Apply<Person>(_persons.AsQueryable()).FirstOrDefault());
    }

    #endregion

    #region FindAllAsync

    [Fact]
    public void Should_ReturnAllMatches_When_ExpressionWithParameterName() {
        var person = RandomPerson();
        var expected = _persons.Where(x => x.FirstName == person.FirstName).ToList();

        var result = (IReadOnlyList<Person>)new DynamicLinqFilter("p", $"p.FirstName == \"{person.FirstName}\"").Apply<Person>(_persons.AsQueryable()).ToList();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Should_ReturnAllMatches_When_ExpressionWithoutParameterName() {
        var person = RandomPerson();
        var expected = _persons.Where(x => x.FirstName == person.FirstName).ToList();

        var result = (IReadOnlyList<Person>)new DynamicLinqFilter($"x.FirstName == \"{person.FirstName}\"").Apply<Person>(_persons.AsQueryable()).ToList();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Should_ThrowInvalidOperationException_When_FindAllExpressionInvalid() {
        Assert.Throws<InvalidOperationException>(
            () => new DynamicLinqFilter(FirstNameExpression).Apply<Person>(_persons.AsQueryable()).ToList());
    }

    #endregion

    #region GetPageAsync

    [Fact]
    public async Task Should_ReturnFilteredPage_When_ParameterNameInPageQuery() {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var person = RandomPerson();
        var list = _persons.Where(x => x.FirstName == person.FirstName).ToList();
        var totalPages = (int)Math.Ceiling((double)list.Count / 10);
        var pageRequest = new PageQuery<Person>(1, 10)
            .Where("p", $"p.FirstName == \"{person.FirstName}\"");

        // Act
        var result = await _repository.GetPageAsync(pageRequest, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(totalPages, result.TotalPages);
        Assert.Equal(list.Count, result.TotalItems);
        Assert.NotNull(result.Items);
        Assert.NotEmpty(result.Items);
    }

    [Fact]
    public async Task Should_ReturnFilteredPage_When_DefaultParameterNameInPageQuery() {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var person = RandomPerson();
        var list = _persons.Where(x => x.FirstName == person.FirstName).ToList();
        var totalPages = (int)Math.Ceiling((double)list.Count / 10);
        var pageRequest = new PageQuery<Person>(1, 10)
            .Where($"x.FirstName == \"{person.FirstName}\"");

        // Act
        var result = await _repository.GetPageAsync(pageRequest, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(totalPages, result.TotalPages);
        Assert.Equal(list.Count, result.TotalItems);
        Assert.NotNull(result.Items);
        Assert.NotEmpty(result.Items);
    }

    #endregion
}
