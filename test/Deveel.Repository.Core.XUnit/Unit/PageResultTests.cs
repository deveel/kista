namespace Deveel.Data;

/// <summary>
/// Tests for <see cref="PageResult{T}"/> construction and pagination state,
/// verifying <see cref="PageResult{T}.TotalItems"/>, <see cref="PageResult{T}.TotalPages"/>,
/// <see cref="PageResult{T}.IsFirstPage"/>, <see cref="PageResult{T}.IsLastPage"/>,
/// and edge cases such as null request or negative total items.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Core")]
[Trait("Feature", "PageResult")]
public class PageResultTests
{
    private readonly PersonFaker _faker = new PersonFaker();

    #region PageResult construction

    [Fact]
    public void Should_HaveCorrectPaginationState_When_OnFirstPageOfManyPages()
    {
        // Arrange
        var query = new PageQuery<Person>(1, 10);
        var items = _faker.Generate(10);

        // Act
        var result = new PageResult<Person>(query, 100, items);

        // Assert
        Assert.Equal(100, result.TotalItems);
        Assert.Equal(10, result.TotalPages);
        Assert.Equal(1, result.Request.Page);
        Assert.Equal(10, result.Request.Size);
        Assert.Equal(10, result.Items.Count);
        Assert.True(result.IsFirstPage);
        Assert.False(result.IsLastPage);
        Assert.True(result.HasNextPage);
        Assert.False(result.HasPreviousPage);
        Assert.Equal(2, result.NextPage);
        Assert.Null(result.PreviousPage);
    }

    [Fact]
    public void Should_HaveCorrectPaginationState_When_OnLastPageOfManyPages()
    {
        // Arrange
        var query = new PageQuery<Person>(10, 10);
        var items = _faker.Generate(8);

        // Act
        var result = new PageResult<Person>(query, 100, items);

        // Assert
        Assert.Equal(100, result.TotalItems);
        Assert.Equal(10, result.TotalPages);
        Assert.Equal(10, result.Request.Page);
        Assert.Equal(8, result.Items.Count);
        Assert.False(result.IsFirstPage);
        Assert.True(result.IsLastPage);
        Assert.False(result.HasNextPage);
        Assert.True(result.HasPreviousPage);
        Assert.Null(result.NextPage);
        Assert.Equal(9, result.PreviousPage);
    }

    [Fact]
    public void Should_ThrowArgumentOutOfRangeException_When_TotalItemsIsNegative()
    {
        // Arrange
        var query = new PageQuery<Person>(1, 10);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new PageResult<Person>(query, -1));
    }

    [Fact]
    public void Should_ThrowArgumentNullException_When_RequestIsNull()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PageResult<Person>(null!, 0));
    }

    [Fact]
    public void Should_ReturnZeroItemsAndPages_When_ResultIsEmpty()
    {
        // Arrange
        var query = new PageQuery<Person>(1, 10);

        // Act
        var result = PageResult<Person>.Empty(query);

        // Assert
        Assert.Equal(0, result.TotalItems);
        Assert.Equal(0, result.TotalPages);
        Assert.Equal(1, result.Request.Page);
        Assert.Equal(10, result.Request.Size);
    }

    #endregion
}
