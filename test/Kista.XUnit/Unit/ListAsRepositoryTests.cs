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
        Assert.Equal(initialCount + 1, await _repository.CountAllAsync(cancellationToken));
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
    public async Task Should_DecrementCount_When_RemovingExistingPerson()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var initialCount = _people.Count;
        var target = RandomPerson();

        // Act
        var result = await _repository.RemoveAsync(target, cancellationToken);

        // Assert
        Assert.True(result);
	Assert.Equal(initialCount - 1, await _repository.AsFilterable().CountAllAsync());
    }

    [Fact]
    public async Task Should_DecrementCount_When_RemovingByExistingKey()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var initialCount = _people.Count;
        var target = RandomPerson();

        // Act
        var result = await _repository.RemoveByKeyAsync(target.Id!, cancellationToken);

        // Assert
        Assert.True(result);
	Assert.Equal(initialCount - 1, await _repository.AsFilterable().CountAllAsync());
    }

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
	Assert.Equal(initialCount, await _repository.AsFilterable().CountAllAsync());
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

    #region Filter

    [Fact]
    public async Task Should_ReturnTrue_When_FilterMatchesExistingPerson()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var target = RandomPerson();

        // Act
        var result = await _repository.ExistsAsync(x => x.LastName == target.LastName, cancellationToken);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task Should_ReturnTotalCount_When_CountingAllWithoutFilter()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        Assert.IsAssignableFrom<IFilterableRepository<Person>>(_repository);

        // Act
        var count = await _repository.AsFilterable().CountAllAsync(cancellationToken);

        // Assert
        Assert.Equal(_people.Count, count);
    }

    [Fact]
    public async Task Should_ReturnMatchingCount_When_CountingWithFilter()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var target = RandomPerson();
        var expected = _people.Count(x => x.FirstName == target.FirstName);

        // Act
        var count = await _repository.AsFilterable().CountAsync(x => x.FirstName == target.FirstName, cancellationToken);

        // Assert
        Assert.Equal(expected, count);
    }

    [Fact]
    public async Task Should_ReturnZero_When_CountingWithNonMatchingFilter()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var count = await _repository.AsFilterable().CountAsync(x => x.FirstName == "__no_match__", cancellationToken);

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Should_ReturnEntity_When_FindingFirstWithMatchingFilter()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var target = RandomPerson();

        // Act
        var found = await _repository.FindFirstAsync(QueryFilter.Where<Person>(x => x.Id == target.Id), cancellationToken);

        // Assert
        Assert.NotNull(found);
        Assert.Equal(target.Id, found.Id);
        Assert.Equal(target.FirstName, found.FirstName);
        Assert.Equal(target.LastName, found.LastName);
    }

    [Fact]
    public async Task Should_ReturnAllEntities_When_FindingAllWithoutFilter()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var result = await _repository.AsFilterable().FindAllAsync(cancellationToken: cancellationToken);

        // Assert
        Assert.Equal(_people.Count, result.Count);
    }

    [Fact]
    public async Task Should_ReturnMatchingEntities_When_FindingAllWithFilter()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var target = RandomPerson();
        var expected = _people.Count(x => x.FirstName == target.FirstName);

        // Act
        var result = await _repository.AsFilterable().FindAllAsync(x => x.FirstName == target.FirstName, cancellationToken: cancellationToken);

        // Assert
        Assert.Equal(expected, result.Count);
    }

    [Fact]
    public async Task Should_ReturnEmpty_When_FindingAllWithNonMatchingFilter()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var result = await _repository.AsFilterable().FindAllAsync(x => x.FirstName == "__no_match__", cancellationToken: cancellationToken);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region GetPage

    [Fact]
    public async Task Should_ReturnFirstPage_When_GettingPageWithoutFilter()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var totalPages = (int)Math.Ceiling(_people.Count / 10.0);
        var request = new PageQuery<Person>(1, 10);

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
    public async Task Should_ReturnFilteredPage_When_GettingPageWithFilter()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var target = RandomPerson();
        var subset = _people.Where(x => x.FirstName == target.FirstName).ToList();
        var expectedItemCount = Math.Min(10, subset.Count);
        var expectedTotalPages = (int)Math.Ceiling(subset.Count / 10.0);
        var request = new PageQuery<Person>(1, 10).Where(x => x.FirstName == target.FirstName);

        // Act
        var page = await _repository.GetPageAsync(request, cancellationToken);

        // Assert
        Assert.Equal(subset.Count, page.TotalItems);
        Assert.Equal(expectedTotalPages, page.TotalPages);
        Assert.Equal(expectedItemCount, page.Items.Count);
    }

    [Fact]
    public async Task Should_ReturnEmptyPage_When_GettingPageWithNonMatchingFilter()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var request = new PageQuery<Person>(1, 10).Where(x => x.FirstName == "__no_match__");

        // Act
        var page = await _repository.GetPageAsync(request, cancellationToken);

        // Assert
        Assert.Equal(0, page.TotalItems);
        Assert.Equal(0, page.TotalPages);
        Assert.Empty(page.Items);
    }

    [Fact]
    public async Task Should_ReturnSortedPage_When_GettingPageWithAscendingSort()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var request = new PageQuery<Person>(1, 10).OrderBy(x => x.FirstName);
        var expectedFirst = _people.OrderBy(x => x.FirstName).First().FirstName;

        // Act
        var page = await _repository.GetPageAsync(request, cancellationToken);

        // Assert
        Assert.Equal(10, page.Items.Count);
        Assert.Equal(expectedFirst, page.Items.First().FirstName);
    }

    [Fact]
    public async Task Should_ReturnSortedPage_When_GettingPageWithDescendingSort()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var request = new PageQuery<Person>(1, 10).OrderByDescending(x => x.FirstName);
        var expectedFirst = _people.OrderByDescending(x => x.FirstName).First().FirstName;

        // Act
        var page = await _repository.GetPageAsync(request, cancellationToken);

        // Assert
        Assert.Equal(10, page.Items.Count);
        Assert.Equal(expectedFirst, page.Items.First().FirstName);
    }

    [Fact]
    public async Task Should_ReturnSortedPage_When_GettingPageWithFieldNameSort()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var request = new PageQuery<Person>(1, 10).OrderBy("FirstName");
        var expectedFirst = _people.OrderBy(x => x.FirstName).First().FirstName;

        // Act
        var page = await _repository.GetPageAsync(request, cancellationToken);

        // Assert
        Assert.Equal(10, page.Items.Count);
        Assert.Equal(expectedFirst, page.Items.First().FirstName);
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
