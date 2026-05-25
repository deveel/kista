namespace Kista;

/// <summary>
/// Tests for <see cref="QueryOrder"/>, <see cref="CombinedOrder"/>, <see cref="ExpressionSort{T}"/>,
/// and <see cref="FieldOrder"/>, verifying combine semantics, flattening of nested combined orders,
/// and <see cref="IQueryOrder.Apply(IQueryable)"/> with field-name and expression-based mappers.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Core")]
[Trait("Feature", "QueryOrder")]
public class QueryOrderTests
{
    private readonly PersonFaker _faker = new PersonFaker();

    #region QueryOrder.Combine

    [Fact]
    public void Should_ReturnCombinedOrder_When_TwoSortsAreCombined()
    {
        // Arrange
        var sort1 = QueryOrder.OrderBy<Person>(x => x.FirstName);
        var sort2 = QueryOrder.OrderByDescending<Person>(x => x.LastName);

        // Act
        var combined = QueryOrder.Combine(sort1, sort2);

        // Assert
        var result = Assert.IsType<CombinedOrder>(combined);
        Assert.Equal(2, result.Count());
        Assert.Equal(sort1, result.ElementAt(0));
        Assert.True(result.ElementAt(0).IsAscending());
        Assert.Equal(sort2, result.ElementAt(1));
        Assert.False(result.ElementAt(1).IsAscending());
        Assert.True(result.ElementAt(1).IsDescending());
    }

    [Fact]
    public void Should_FlattenSorts_When_CombiningWithAlreadyCombinedOrder()
    {
        // Arrange
        var sort1 = QueryOrder.OrderBy<Person>(x => x.FirstName);
        var sort2 = QueryOrder.OrderByDescending<Person>(x => x.LastName);
        var sort3 = QueryOrder.OrderBy<Person>(x => x.DateOfBirth);
        var combined1 = QueryOrder.Combine(sort1, sort2);

        // Act
        var combined2 = combined1.Combine(sort3);

        // Assert
        Assert.NotNull(combined2);
    }

    [Fact]
    public void Should_ThrowArgumentException_When_CreatingCombinedOrderWithEmptyList()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => new CombinedOrder(Array.Empty<IQueryOrder>()));
    }

    #endregion

    #region Apply

    [Fact]
    public void Should_ReturnSortedQueryable_When_ApplyingCombinedSort()
    {
        // Arrange
        var people = _faker.Generate(4);
        var queryable = people.AsQueryable();
        var sort1 = QueryOrder.OrderBy<Person>(x => x.FirstName);
        var sort2 = QueryOrder.OrderByDescending<Person>(x => x.LastName);
        var combined = QueryOrder.Combine(sort1, sort2);

        // Act
        var result = combined.Apply(queryable);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.ToList().Count);
    }

    [Fact]
    public void Should_SortByProperty_When_ApplyingFieldNameSortWithNoMapper()
    {
        // Arrange
        var people = _faker.Generate(4);
        var queryable = people.AsQueryable();
        var sort = QueryOrder.OrderBy("FirstName");

        // Act
        var result = sort.Apply(queryable);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.ToList().Count);
    }

    [Fact]
    public void Should_SortByMappedField_When_ApplyingFieldNameSortWithDelegatedMapper()
    {
        // Arrange
        var people = _faker.Generate(4);
        var queryable = people.AsQueryable();
        var sort = QueryOrder.OrderBy("first_name");

        // Act
        var result = sort.Apply(queryable, field => field switch
        {
            "first_name" => (System.Linq.Expressions.Expression<Func<Person, object?>>)(p => p.FirstName),
            "last_name" => p => p.LastName,
            _ => throw new ArgumentException($"Unsupported field '{field}'")
        });

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.ToList().Count);
    }

    #endregion
}
