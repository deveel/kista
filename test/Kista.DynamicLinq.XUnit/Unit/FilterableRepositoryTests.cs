#pragma warning disable CS0618

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "FilterableRepository")]
public class FilterableRepositoryTests {
    private static readonly Faker<Person> PersonFaker = new Faker<Person>("en")
        .RuleFor(x => x.Id, f => f.Random.Guid().ToString())
        .RuleFor(x => x.FirstName, f => f.Name.FirstName())
        .RuleFor(x => x.LastName, f => f.Name.LastName())
        .RuleFor(x => x.DateOfBirth, f => f.Date.Past(20))
        .RuleFor(x => x.Email, f => f.Internet.Email().OrNull(f))
        .RuleFor(x => x.Phone, f => f.Phone.PhoneNumber().OrNull(f));

    private readonly IList<Person> _persons;
    private readonly IRepository<Person> _repository;

    public FilterableRepositoryTests() {
        _persons = PersonFaker.Generate(100).ToList();
        _repository = _persons.AsRepository();
    }

    private Person RandomPerson() => _persons[Random.Shared.Next(0, _persons.Count - 1)];

    #region CountAsync

    [Fact]
    public async Task Should_ReturnFilteredCount_When_ParameterNameProvided() {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var person = RandomPerson();
        var expected = _persons.Count(x => x.FirstName == person.FirstName);

        // Act
        var count = await _repository.CountAsync("p", $"p.FirstName == \"{person.FirstName}\"", cancellationToken);

        // Assert
        Assert.Equal(expected, count);
    }

    [Fact]
    public async Task Should_ReturnFilteredCount_When_NoParameterName() {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var person = RandomPerson();
        var expected = _persons.Count(x => x.FirstName == person.FirstName);

        // Act
        var count = await _repository.CountAsync($"x.FirstName == \"{person.FirstName}\"", cancellationToken);

        // Assert
        Assert.Equal(expected, count);
    }

    [Fact]
    public async Task Should_ThrowInvalidOperationException_When_CountExpressionInvalid() {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _repository.CountAsync("x.FirstName", TestContext.Current.CancellationToken));
    }

    #endregion

    #region ExistsAsync

    [Fact]
    public async Task Should_ReturnTrue_When_EntityMatchesExpressionWithParameterName() {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var person = RandomPerson();
        var expected = _persons.Any(x => x.FirstName == person.FirstName);

        // Act
        var result = await _repository.ExistsAsync("p", $"p.FirstName == \"{person.FirstName}\"", cancellationToken);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task Should_ReturnTrue_When_EntityMatchesExpressionWithoutParameterName() {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var person = RandomPerson();
        var expected = _persons.Any(x => x.FirstName == person.FirstName);

        // Act
        var result = await _repository.ExistsAsync($"x.FirstName == \"{person.FirstName}\"", cancellationToken);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task Should_ThrowInvalidOperationException_When_ExistsExpressionInvalid() {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _repository.ExistsAsync("x.FirstName", TestContext.Current.CancellationToken));
    }

    #endregion

    #region FindFirstAsync

    [Fact]
    public async Task Should_ReturnFirstMatch_When_ExpressionWithParameterName() {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var person = RandomPerson();
        var expected = _persons.FirstOrDefault(x => x.FirstName == person.FirstName);

        // Act
        var result = await _repository.FindFirstAsync("p", $"p.FirstName == \"{person.FirstName}\"", cancellationToken);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task Should_ReturnFirstMatch_When_ExpressionWithoutParameterName() {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var person = RandomPerson();
        var expected = _persons.FirstOrDefault(x => x.FirstName == person.FirstName);

        // Act
        var result = await _repository.FindFirstAsync<Person>($"x.FirstName == \"{person.FirstName}\"", cancellationToken);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task Should_ThrowInvalidOperationException_When_FindFirstExpressionInvalid() {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _repository.FindFirstAsync<Person>("x.FirstName", TestContext.Current.CancellationToken));
    }

    #endregion

    #region FindAllAsync

    [Fact]
    public async Task Should_ReturnAllMatches_When_ExpressionWithParameterName() {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var person = RandomPerson();
        var expected = _persons.Where(x => x.FirstName == person.FirstName).ToList();

        // Act
        var result = await _repository.FindAllAsync("p", $"p.FirstName == \"{person.FirstName}\"", cancellationToken);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task Should_ReturnAllMatches_When_ExpressionWithoutParameterName() {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var person = RandomPerson();
        var expected = _persons.Where(x => x.FirstName == person.FirstName).ToList();

        // Act
        var result = await _repository.FindAllAsync($"x.FirstName == \"{person.FirstName}\"", cancellationToken);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task Should_ThrowInvalidOperationException_When_FindAllExpressionInvalid() {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _repository.FindAllAsync("x.FirstName", TestContext.Current.CancellationToken));
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
