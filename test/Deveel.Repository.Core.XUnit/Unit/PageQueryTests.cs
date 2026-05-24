using System.ComponentModel.DataAnnotations;

namespace Deveel.Data;

/// <summary>
/// Tests for <see cref="PageQuery{T}"/> construction and fluent configuration,
/// verifying default state, <c>Where</c> clause chaining, <c>OrderBy</c> and
/// <c>OrderByDescending</c> methods with both expressions and field names.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Core")]
[Trait("Feature", "PageQuery")]
public class PageQueryTests
{
    private readonly PersonFaker _faker = new PersonFaker();

    #region PageQuery creation

    [Fact]
    public void Should_HaveDefaultEmptyState_When_CreatedWithPageAndSize()
    {
        // Arrange & Act
        var query = new PageQuery<Person>(1, 10);

        // Assert
        Assert.Equal(1, query.Page);
        Assert.Equal(10, query.Size);
        Assert.Equal(0, query.Offset);
        Assert.NotNull(query.Query);
        Assert.Equal(Query.Empty, query.Query);
        Assert.NotNull(query.Query.Filter);
        Assert.Equal(QueryFilter.Empty, query.Query.Filter);
    }

    [Fact]
    public void Should_HaveEmptyFilterAndNoOrder_When_AssignedEmptyQuery()
    {
        // Arrange & Act
        var query = new PageQuery<Person>(1, 10) { Query = Query.Empty };

        // Assert
        Assert.Equal(QueryFilter.Empty, query.Query.Filter);
        Assert.True(query.Query.Filter.IsEmpty());
        Assert.Null(query.Query.Order);
    }

    [Fact]
    public void Should_HaveExpressionFilter_When_WhereClauseIsApplied()
    {
        // Arrange
        var person = _faker.Generate();

        // Act
        var query = new PageQuery<Person>(1, 10).Where(x => x.FirstName == person.FirstName);

        // Assert
        Assert.True(query.Query.HasFilter());
        Assert.False(query.Query.Filter.IsEmpty());
        Assert.IsType<ExpressionQueryFilter<Person>>(query.Query.Filter);
        Assert.Null(query.Query.Order);
    }

    [Fact]
    public void Should_HaveCombinedFilter_When_MultipleWhereClausesAreApplied()
    {
        // Arrange
        var person = _faker.Generate();

        // Act
        var query = new PageQuery<Person>(1, 10)
            .Where(x => x.FirstName == person.FirstName)
            .Where(x => x.LastName == person.LastName);

        // Assert
        var filter = Assert.IsType<CombinedQueryFilter>(query.Query.Filter);
        Assert.Equal(2, filter.Count());
    }

    [Fact]
    public void Should_HaveExpressionSortAscending_When_OrderByExpressionIsApplied()
    {
        // Arrange & Act
        var query = new PageQuery<Person>(1, 10).OrderBy(x => x.FirstName);

        // Assert
        Assert.False(query.Query.HasFilter());
        var sort = Assert.IsType<ExpressionSort<Person>>(query.Query.Order);
        Assert.Equal("x => x.FirstName", sort.Field.ToString());
    }

    [Fact]
    public void Should_HaveCombinedSort_When_MultipleOrderByClausesAreApplied()
    {
        // Arrange & Act
        var query = new PageQuery<Person>(1, 10)
            .OrderBy(x => x.FirstName)
            .OrderByDescending(x => x.LastName);

        // Assert
        var combined = Assert.IsType<CombinedOrder>(query.Query.Order);
        Assert.Equal(2, combined.Count());
        var first = Assert.IsType<ExpressionSort<Person>>(combined.ElementAt(0));
        Assert.True(first.IsAscending());
        var second = Assert.IsType<ExpressionSort<Person>>(combined.ElementAt(1));
        Assert.False(second.IsAscending());
    }

    [Fact]
    public void Should_HaveFieldSort_When_OrderByFieldNameIsApplied()
    {
        // Arrange & Act
        var query = new PageQuery<Person>(1, 10).OrderBy("FirstName");

        // Assert
        var sort = Assert.IsType<FieldOrder>(query.Query.Order);
        Assert.Equal("FirstName", sort.FieldName);
        Assert.True(sort.IsAscending());
    }

    [Fact]
    public void Should_SupportMixedExpressionAndFieldSort_When_BothAreApplied()
    {
        // Arrange & Act — verifies no exception is thrown
        var query = new PageQuery<Person>(1, 10)
            .OrderBy(x => x.FirstName)
            .OrderByDescending("LastName");

        // Assert
        Assert.NotNull(query.Query.Order);
    }

    #endregion
}
