namespace Kista;

/// <summary>
/// Unit tests for the synchronous extension methods in <see cref="RepositoryExtensions"/>,
/// verifying full async/sync parity across all operation groups.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Core")]
[Trait("Feature", "RepositoryExtensions")]
public class RepositoryExtensionsSyncTests : IClassFixture<PersonFixture>
{
    private readonly PersonFixture _fixture;
    private readonly List<Person> _people;
    private readonly IRepository<Person> _repository;

    public RepositoryExtensionsSyncTests(PersonFixture fixture)
    {
        _fixture = fixture;
        _people = fixture.BuildPeople(50).ToList();
        _repository = _people.AsRepository();
    }

    private Person RandomPerson() => _people[Random.Shared.Next(0, _people.Count - 1)];

    // -------------------------------------------------------------------------
    // AddRange
    // -------------------------------------------------------------------------

    #region AddRange<TEntity> / AddRange<TEntity, TKey>

    [Fact]
    public void Should_IncrementCount_When_AddRangeCalledOnSingleTypeParamRepository()
    {
        // Arrange
        var newPeople = _fixture.PersonFaker.Generate(5);
        var initialCount = _people.Count;

        // Act
        _repository.AddRange(newPeople);

        // Assert
        Assert.Equal(initialCount + 5, _repository.CountAll());
    }

    [Fact]
    public void Should_IncrementCount_When_AddRangeCalledOnTwoTypeParamRepository()
    {
        // Arrange
        IRepository<Person, object> typedRepo = _repository;
        var newPeople = _fixture.PersonFaker.Generate(3);
        var initialCount = _people.Count;

        // Act
        typedRepo.AddRange(newPeople);

        // Assert
        Assert.Equal(initialCount + 3, _repository.CountAll());
    }

    #endregion

    // -------------------------------------------------------------------------
    // RemoveRange
    // -------------------------------------------------------------------------

    #region RemoveRange<TEntity> / RemoveRange<TEntity, TKey>

    [Fact]
    public void Should_DecrementCount_When_RemoveRangeCalledOnSingleTypeParamRepository()
    {
        // Arrange
        var toRemove = _people.Take(5).ToList();
        var initialCount = _people.Count;

        // Act
        _repository.RemoveRange(toRemove);

        // Assert
        Assert.Equal(initialCount - 5, _repository.CountAll());
    }

    [Fact]
    public void Should_DecrementCount_When_RemoveRangeCalledOnTwoTypeParamRepository()
    {
        // Arrange
        IRepository<Person, object> typedRepo = _repository;
        var toRemove = _people.Take(3).ToList();
        var initialCount = _people.Count;

        // Act
        typedRepo.RemoveRange(toRemove);

        // Assert
        Assert.Equal(initialCount - 3, _repository.CountAll());
    }

    #endregion

    // -------------------------------------------------------------------------
    // GetPage
    // -------------------------------------------------------------------------

    #region GetPage<TEntity, TKey>(int page, int size)

    [Fact]
    public void Should_ReturnFirstPage_When_GetPageCalledWithPageNumberAndSize()
    {
        // Arrange
        IRepository<Person, object> typedRepo = _repository;
        var expectedTotalItems = _people.Count;

        // Act
        var page = typedRepo.GetPage(1, 10);

        // Assert
        Assert.NotNull(page);
        Assert.Equal(10, page.Items.Count);
        Assert.Equal(expectedTotalItems, page.TotalItems);
        Assert.Equal(1, page.Request.Page);
        Assert.Equal(10, page.Request.Size);
    }

    [Fact]
    public void Should_ReturnCorrectPage_When_GetPageCalledForSecondPage()
    {
        // Arrange
        IRepository<Person, object> typedRepo = _repository;

        // Act
        var page = typedRepo.GetPage(2, 10);

        // Assert
        Assert.NotNull(page);
        Assert.Equal(10, page.Items.Count);
        Assert.Equal(2, page.Request.Page);
    }

    #endregion

    // -------------------------------------------------------------------------
    // Exists
    // -------------------------------------------------------------------------

    #region Exists<TEntity>(IRepository<TEntity>, IQueryFilter)

    [Fact]
    public void Should_ReturnTrue_When_ExistsCalledWithMatchingIQueryFilter()
    {
        // Arrange
        var target = RandomPerson();
        var filter = QueryFilter.Where<Person>(x => x.Id == target.Id);

        // Act
        var result = _repository.Exists(filter);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Should_ReturnFalse_When_ExistsCalledWithNonMatchingIQueryFilter()
    {
        // Arrange
        var filter = QueryFilter.Where<Person>(x => x.FirstName == "__no_match__");

        // Act
        var result = _repository.Exists(filter);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Exists<TEntity>(IRepository<TEntity>, Expression)

    [Fact]
    public void Should_ReturnTrue_When_ExistsCalledWithMatchingExpression()
    {
        // Arrange
        var target = RandomPerson();

        // Act
        var result = _repository.Exists(x => x.Id == target.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Should_ReturnFalse_When_ExistsCalledWithNonMatchingExpression()
    {
        // Act
        var result = _repository.Exists(x => x.FirstName == "__no_match__");

        // Assert
        Assert.False(result);
    }

    #endregion

    // -------------------------------------------------------------------------
    // Count
    // -------------------------------------------------------------------------

    #region FindFirst<TEntity>(IRepository<TEntity>, IQueryFilter)

    [Fact]
    public void Should_ReturnMatchingEntity_When_FindFirstCalledWithMatchingIQueryFilter()
    {
        // Arrange
        var target = RandomPerson();
        var filter = QueryFilter.Where<Person>(x => x.Id == target.Id);

        // Act
        var result = _repository.FindFirst(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(target.Id, result.Id);
        Assert.Equal(target.FirstName, result.FirstName);
        Assert.Equal(target.LastName, result.LastName);
    }

    [Fact]
    public void Should_ReturnNull_When_FindFirstCalledWithNonMatchingIQueryFilter()
    {
        // Arrange
        var filter = QueryFilter.Where<Person>(x => x.FirstName == "__no_match__");

        // Act
        var result = _repository.FindFirst(filter);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Should_ReturnFirstByFirstName_When_FindFirstCalledWithFirstNameFilter()
    {
        // Arrange
        var target = RandomPerson();
        var filter = QueryFilter.Where<Person>(x => x.FirstName == target.FirstName);
        var expected = _people.First(x => x.FirstName == target.FirstName);

        // Act
        var result = _repository.FindFirst(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expected.Id, result.Id);
    }

    #endregion

    #region Count<TEntity>(IRepository<TEntity>, IQueryFilter)

    [Fact]
    public void Should_ReturnMatchingCount_When_CountCalledWithMatchingIQueryFilter()
    {
        // Arrange
        var target = RandomPerson();
        var filter = QueryFilter.Where<Person>(x => x.FirstName == target.FirstName);
        var expectedCount = _people.LongCount(x => x.FirstName == target.FirstName);

        // Act
        var count = _repository.Count(filter);

        // Assert
        Assert.Equal(expectedCount, count);
    }

    [Fact]
    public void Should_ReturnZero_When_CountCalledWithNonMatchingIQueryFilter()
    {
        // Arrange
        var filter = QueryFilter.Where<Person>(x => x.FirstName == "__no_match__");

        // Act
        var count = _repository.Count(filter);

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public void Should_ReturnTotalCount_When_CountCalledWithEmptyFilter()
    {
        // Arrange
        var filter = QueryFilter.Empty;

        // Act
        var count = _repository.Count(filter);

        // Assert
        Assert.Equal(_people.Count, count);
    }

    [Fact]
    public void Should_ReturnCountByIdFilter_When_UniqueEntityExists()
    {
        // Arrange
        var target = RandomPerson();
        var filter = QueryFilter.Where<Person>(x => x.Id == target.Id);

        // Act
        var count = _repository.Count(filter);

        // Assert
        Assert.Equal(1L, count);
    }

    #endregion

    #region Count<TEntity>(IRepository<TEntity>, Expression)

    [Fact]
    public void Should_ReturnMatchingCount_When_CountCalledWithMatchingExpression()
    {
        // Arrange
        var target = RandomPerson();
        var expectedCount = _people.LongCount(x => x.FirstName == target.FirstName);

        // Act
        var count = _repository.Count(x => x.FirstName == target.FirstName);

        // Assert
        Assert.Equal(expectedCount, count);
    }

    [Fact]
    public void Should_ReturnZero_When_CountCalledWithNonMatchingExpression()
    {
        // Act
        var count = _repository.Count(x => x.FirstName == "__no_match__");

        // Assert
        Assert.Equal(0L, count);
    }

    #endregion

    #region CountAll<TEntity>(IRepository<TEntity>)

    [Fact]
    public void Should_ReturnTotalCount_When_CountAllCalled()
    {
        // Act
        var count = _repository.CountAll();

        // Assert
        Assert.Equal(_people.Count, count);
    }

    #endregion

    #region Count<TEntity, TKey>(IRepository<TEntity, TKey>, IQueryFilter)

    [Fact]
    public void Should_ReturnMatchingCount_When_CountWithKeyCalledWithMatchingIQueryFilter()
    {
        // Arrange
        var target = RandomPerson();
        IRepository<Person, object> typedRepository = _repository;
        var filter = QueryFilter.Where<Person>(x => x.FirstName == target.FirstName);
        var expectedCount = _people.LongCount(x => x.FirstName == target.FirstName);

        // Act
        var count = typedRepository.Count(filter);

        // Assert
        Assert.Equal(expectedCount, count);
    }

    [Fact]
    public void Should_ReturnZero_When_CountWithKeyCalledWithNonMatchingIQueryFilter()
    {
        // Arrange
        IRepository<Person, object> typedRepository = _repository;
        var filter = QueryFilter.Where<Person>(x => x.LastName == "__no_match__");

        // Act
        var count = typedRepository.Count(filter);

        // Assert
        Assert.Equal(0L, count);
    }

    #endregion

    // -------------------------------------------------------------------------
    // Find (by key)
    // -------------------------------------------------------------------------

    #region Find<TEntity>(IRepository<TEntity>, object)

    [Fact]
    public void Should_ReturnEntity_When_FindCalledWithExistingKey()
    {
        // Arrange
        var target = RandomPerson();

        // Act
        var result = _repository.Find(target.Id!);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(target.Id, result.Id);
    }

    [Fact]
    public void Should_ReturnNull_When_FindCalledWithNonExistingKey()
    {
        // Arrange
        var missingKey = Guid.NewGuid().ToString();

        // Act
        var result = _repository.Find(missingKey);

        // Assert
        Assert.Null(result);
    }

    #endregion

    // -------------------------------------------------------------------------
    // FindFirst
    // -------------------------------------------------------------------------

    #region FindFirst<TEntity, TKey>(IRepository<TEntity, TKey>, IQueryFilter)

    [Fact]
    public void Should_ReturnMatchingEntity_When_FindFirstWithKeyCalledWithMatchingIQueryFilter()
    {
        // Arrange
        var target = RandomPerson();
        IRepository<Person, object> typedRepository = _repository;
        var filter = QueryFilter.Where<Person>(x => x.Id == target.Id);

        // Act
        var result = typedRepository.FindFirst(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(target.Id, result.Id);
        Assert.Equal(target.FirstName, result.FirstName);
    }

    [Fact]
    public void Should_ReturnNull_When_FindFirstWithKeyCalledWithNonMatchingIQueryFilter()
    {
        // Arrange
        IRepository<Person, object> typedRepository = _repository;
        var filter = QueryFilter.Where<Person>(x => x.LastName == "__no_match__");

        // Act
        var result = typedRepository.FindFirst(filter);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region FindFirst<TEntity, TKey>(IRepository<TEntity, TKey>, IQuery)

    [Fact]
    public void Should_ReturnMatchingEntity_When_FindFirstWithKeyCalledWithIQuery()
    {
        // Arrange
        var target = RandomPerson();
        IRepository<Person, object> typedRepository = _repository;
        var query = Query.Where<Person>(x => x.Id == target.Id);

        // Act
        var result = typedRepository.FindFirst(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(target.Id, result.Id);
    }

    [Fact]
    public void Should_ReturnNull_When_FindFirstWithKeyCalledWithNonMatchingIQuery()
    {
        // Arrange
        IRepository<Person, object> typedRepository = _repository;
        var query = Query.Where<Person>(x => x.FirstName == "__no_match__");

        // Act
        var result = typedRepository.FindFirst(query);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region FindFirst<TEntity, TKey>(IRepository<TEntity, TKey>, Expression)

    [Fact]
    public void Should_ReturnMatchingEntity_When_FindFirstWithKeyCalledWithExpression()
    {
        // Arrange
        var target = RandomPerson();
        IRepository<Person, object> typedRepository = _repository;

        // Act
        var result = typedRepository.FindFirst(x => x.Id == target.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(target.Id, result.Id);
    }

    [Fact]
    public void Should_ReturnNull_When_FindFirstWithKeyCalledWithNonMatchingExpression()
    {
        // Arrange
        IRepository<Person, object> typedRepository = _repository;

        // Act
        var result = typedRepository.FindFirst(x => x.FirstName == "__no_match__");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region FindFirst<TEntity, TKey>(IRepository<TEntity, TKey>) — no filter

    [Fact]
    public void Should_ReturnAnyEntity_When_FindFirstWithKeyCalledWithNoFilter()
    {
        // Arrange
        IRepository<Person, object> typedRepository = _repository;

        // Act
        var result = typedRepository.FindFirst();

        // Assert
        Assert.NotNull(result);
    }

    #endregion

    #region FindFirst<TEntity>(IRepository<TEntity>, IQuery)

    [Fact]
    public void Should_ReturnMatchingEntity_When_FindFirstCalledWithIQuery()
    {
        // Arrange
        var target = RandomPerson();
        var query = Query.Where<Person>(x => x.Id == target.Id);

        // Act
        var result = _repository.FindFirst(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(target.Id, result.Id);
    }

    [Fact]
    public void Should_ReturnNull_When_FindFirstCalledWithNonMatchingIQuery()
    {
        // Arrange
        var query = Query.Where<Person>(x => x.FirstName == "__no_match__");

        // Act
        var result = _repository.FindFirst(query);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region FindFirst<TEntity>(IRepository<TEntity>, Expression)

    [Fact]
    public void Should_ReturnMatchingEntity_When_FindFirstCalledWithExpression()
    {
        // Arrange
        var target = RandomPerson();

        // Act
        var result = _repository.FindFirst(x => x.Id == target.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(target.Id, result.Id);
    }

    [Fact]
    public void Should_ReturnNull_When_FindFirstCalledWithNonMatchingExpression()
    {
        // Act
        var result = _repository.FindFirst(x => x.FirstName == "__no_match__");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region FindFirst<TEntity>(IRepository<TEntity>) — no filter

    [Fact]
    public void Should_ReturnAnyEntity_When_FindFirstCalledWithNoFilter()
    {
        // Act
        var result = _repository.FindFirst();

        // Assert
        Assert.NotNull(result);
    }

    #endregion

    // -------------------------------------------------------------------------
    // FindAll
    // -------------------------------------------------------------------------

    #region FindAll<TEntity>(IRepository<TEntity>, IQueryFilter)

    [Fact]
    public void Should_ReturnMatchingEntities_When_FindAllCalledWithMatchingIQueryFilter()
    {
        // Arrange
        var target = RandomPerson();
        var filter = QueryFilter.Where<Person>(x => x.FirstName == target.FirstName);
        var expectedCount = _people.Count(x => x.FirstName == target.FirstName);

        // Act
        var result = _repository.FindAll(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedCount, result.Count);
        Assert.All(result, p => Assert.Equal(target.FirstName, p.FirstName));
    }

    [Fact]
    public void Should_ReturnEmpty_When_FindAllCalledWithNonMatchingIQueryFilter()
    {
        // Arrange
        var filter = QueryFilter.Where<Person>(x => x.FirstName == "__no_match__");

        // Act
        var result = _repository.FindAll(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void Should_ReturnAllEntities_When_FindAllCalledWithEmptyFilter()
    {
        // Act
        var result = _repository.FindAll(QueryFilter.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_people.Count, result.Count);
    }

    #endregion

    #region FindAll<TEntity>(IRepository<TEntity>, IQuery)

    [Fact]
    public void Should_ReturnMatchingEntities_When_FindAllCalledWithIQuery()
    {
        // Arrange
        var target = RandomPerson();
        var query = Query.Where<Person>(x => x.FirstName == target.FirstName);
        var expectedCount = _people.Count(x => x.FirstName == target.FirstName);

        // Act
        var result = _repository.FindAll(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedCount, result.Count);
    }

    [Fact]
    public void Should_ReturnEmpty_When_FindAllCalledWithNonMatchingIQuery()
    {
        // Arrange
        var query = Query.Where<Person>(x => x.FirstName == "__no_match__");

        // Act
        var result = _repository.FindAll(query);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region FindAll<TEntity>(IRepository<TEntity>, Expression)

    [Fact]
    public void Should_ReturnMatchingEntities_When_FindAllCalledWithExpression()
    {
        // Arrange
        var target = RandomPerson();
        var expectedCount = _people.Count(x => x.FirstName == target.FirstName);

        // Act
        var result = _repository.FindAll(x => x.FirstName == target.FirstName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedCount, result.Count);
    }

    [Fact]
    public void Should_ReturnEmpty_When_FindAllCalledWithNonMatchingExpression()
    {
        // Act
        var result = _repository.FindAll(x => x.FirstName == "__no_match__");

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region FindAll<TEntity>(IRepository<TEntity>) — no filter

    [Fact]
    public void Should_ReturnAllEntities_When_FindAllCalledWithNoFilter()
    {
        // Act
        var result = _repository.FindAll();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_people.Count, result.Count);
    }

    #endregion

    #region FindAll<TEntity, TKey>(IRepository<TEntity, TKey>, IQuery)

    [Fact]
    public void Should_ReturnMatchingEntities_When_FindAllWithKeyCalledWithIQuery()
    {
        // Arrange
        var target = RandomPerson();
        IRepository<Person, object> typedRepository = _repository;
        var query = Query.Where<Person>(x => x.FirstName == target.FirstName);
        var expectedCount = _people.Count(x => x.FirstName == target.FirstName);

        // Act
        var result = typedRepository.FindAll(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedCount, result.Count);
    }

    [Fact]
    public void Should_ReturnEmpty_When_FindAllWithKeyCalledWithNonMatchingIQuery()
    {
        // Arrange
        IRepository<Person, object> typedRepository = _repository;
        var query = Query.Where<Person>(x => x.FirstName == "__no_match__");

        // Act
        var result = typedRepository.FindAll(query);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region FindAll<TEntity, TKey>(IRepository<TEntity, TKey>, Expression)

    [Fact]
    public void Should_ReturnMatchingEntities_When_FindAllWithKeyCalledWithExpression()
    {
        // Arrange
        var target = RandomPerson();
        IRepository<Person, object> typedRepository = _repository;
        var expectedCount = _people.Count(x => x.FirstName == target.FirstName);

        // Act
        var result = typedRepository.FindAll(x => x.FirstName == target.FirstName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedCount, result.Count);
    }

    [Fact]
    public void Should_ReturnEmpty_When_FindAllWithKeyCalledWithNonMatchingExpression()
    {
        // Arrange
        IRepository<Person, object> typedRepository = _repository;

        // Act
        var result = typedRepository.FindAll(x => x.FirstName == "__no_match__");

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region FindAll<TEntity, TKey>(IRepository<TEntity, TKey>) — no filter

    [Fact]
    public void Should_ReturnAllEntities_When_FindAllWithKeyCalledWithNoFilter()
    {
        // Arrange
        IRepository<Person, object> typedRepository = _repository;

        // Act
        var result = typedRepository.FindAll();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_people.Count, result.Count);
    }

    #endregion
}

