namespace Kista;

/// <summary>
/// Integration-style tests for <see cref="RepositoryExtensions.AsRepository{T}(List{T})"/> and
/// the resulting <see cref="RepositoryWrapper{TEntity}"/> behavior, covering add, remove, update,
/// filter, and pagination operations against a list-backed repository.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Core")]
[Trait("Feature", "Repository")]
public class ListAsRepositoryTests : IClassFixture<PersonFixture>
{
    private readonly PersonFixture _fixture;
    private readonly List<Person> _people;
    private readonly IRepository<Person> _repository;

    public ListAsRepositoryTests(PersonFixture fixture)
    {
        _fixture = fixture;
        _people = fixture.BuildPeople(100).ToList();
        _repository = _people.AsRepository();
    }

    private Person RandomPerson() => _people[Random.Shared.Next(0, _people.Count - 1)];

    #region Add

    [Fact]
    public async Task Should_ThrowNotSupportedException_When_AddingToReadOnlyRepository()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var readOnly = _people.AsReadOnly().AsRepository();
        var newPerson = _fixture.PersonFaker.Generate();

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(async () => await readOnly.AddAsync(newPerson, cancellationToken));
    }

    [Fact]
    public async Task Should_IncrementCount_When_AddingToMutableRepository()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var initialCount = _people.Count;
        var newPerson = _fixture.PersonFaker.Generate();

        // Act
        await _repository.AddAsync(newPerson, cancellationToken);

        // Assert
        Assert.Equal(initialCount + 1, _people.Count);
        Assert.NotNull(newPerson.Id);
    }

    [Fact]
    public async Task Should_ThrowNotSupportedException_When_AddingRangeToReadOnlyRepository()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var readOnly = _people.AsReadOnly().AsRepository();
        var newPeople = _fixture.PersonFaker.Generate(10);

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(async () => await readOnly.AddRangeAsync(newPeople, cancellationToken));
    }

    #endregion

    #region Remove

    [Fact]
    public async Task Should_ReturnFalse_When_RemovingByKeyThatDoesNotExist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var initialCount = _people.Count;
        var missingId = Guid.NewGuid().ToString();

        // Act
        var result = await _repository.RemoveByKeyAsync(missingId, cancellationToken);

        // Assert
        Assert.False(result);
	Assert.Equal(initialCount, _people.Count);
    }

    [Fact]
    public async Task Should_RemoveAllSpecified_When_RemovingRange()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var initialCount = _people.Count;
        var toRemove = _people.Take(Math.Min(10, _people.Count)).ToList();

        // Act
        await _repository.RemoveRangeAsync(toRemove, cancellationToken);

        // Assert
        Assert.All(toRemove, p => Assert.Null(_repository.Find(p.Id!)));
        Assert.Equal(initialCount - toRemove.Count, _people.Count);
    }

    [Fact]
    public async Task Should_ThrowNotSupportedException_When_RemovingFromReadOnlyRepository()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var readOnly = _people.AsReadOnly().AsRepository();
        var target = RandomPerson();

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(async () => await readOnly.RemoveAsync(target, cancellationToken));
    }

    #endregion

    #region GetPage

    [Fact]
    public async Task Should_ReturnFirstPage_When_GettingPageWithoutFilter()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var totalPages = (int)Math.Ceiling(_people.Count / 10.0);
        var request = new PageRequest(1, 10);

        // Act
        var page = await _repository.GetPageAsync(request, cancellationToken);

        // Assert
        Assert.Equal(10, page.Request.Size);
        Assert.Equal(1, page.Request.Page);
        Assert.Equal(_people.Count, page.TotalItems);
        Assert.Equal(totalPages, page.TotalPages);
        Assert.NotNull(page.Items);
        Assert.Equal(10, page.Items.Count);
    }

    [Fact]
    public async Task Should_ReturnSecondPage_When_GettingPage()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var request = new PageRequest(2, 10);

        // Act
        var page = await _repository.GetPageAsync(request, cancellationToken);

        // Assert
        Assert.Equal(10, page.Request.Size);
        Assert.Equal(2, page.Request.Page);
        Assert.Equal(_people.Count, page.TotalItems);
        Assert.NotNull(page.Items);
        Assert.Equal(10, page.Items.Count);
    }

    [Fact]
    public async Task Should_ReturnLastPage_When_GettingPageBeyondTotal()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var request = new PageRequest(999, 10);

        // Act
        var page = await _repository.GetPageAsync(request, cancellationToken);

        // Assert
        Assert.Equal(10, page.Request.Size);
        Assert.Equal(999, page.Request.Page);
        Assert.Equal(_people.Count, page.TotalItems);
        Assert.NotNull(page.Items);
        Assert.Empty(page.Items);
    }

    #endregion

    #region Update

    [Fact]
    public async Task Should_ReturnTrue_When_UpdatingExistingPerson()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var target = RandomPerson();
        var originalId = target.Id;
        var newFirstName = _fixture.PersonFaker.Generate().FirstName;
        target.FirstName = newFirstName;

        // Act
        var result = await _repository.UpdateAsync(target, cancellationToken);

        // Assert
        Assert.True(result);
        Assert.Equal(originalId, target.Id);
        Assert.Equal(newFirstName, target.FirstName);
    }

    [Fact]
    public async Task Should_ReturnFalse_When_UpdatingPersonThatDoesNotExist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var nonExistent = _fixture.PersonFaker.Generate();

        // Act
        var result = await _repository.UpdateAsync(nonExistent, cancellationToken);

        // Assert
        Assert.False(result);
    }

    #endregion
}
