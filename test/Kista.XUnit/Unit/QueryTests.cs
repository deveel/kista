namespace Kista;

/// <summary>
/// Tests for the <see cref="Query"/> class and <see cref="QueryBuilder{T}"/>,
/// verifying construction from filters, chaining of <c>Where</c>/<c>OrderBy</c>
/// clauses, and <see cref="Query.Apply{T}"/> behavior on <see cref="IQueryable{T}"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Core")]
[Trait("Feature", "Query")]
public class QueryTests
{
    private readonly PersonFaker _faker = new PersonFaker();

    #region Query construction

    [Fact]
    public void Should_HaveFilterAndNoOrder_When_QueryHasFilterOnly()
    {
        // Arrange
        var person = _faker.Generate();

        // Act
        var query = new Query(QueryFilter.Where<Person>(p => p.FirstName == person.FirstName));

        // Assert
        Assert.NotNull(query.Filter);
        Assert.Null(query.Order);
        Assert.IsType<ExpressionQueryFilter<Person>>(query.Filter);
    }

    [Fact]
    public void Should_HaveExpressionFilter_When_BuiltWithWhereFactory()
    {
        // Arrange
        var person = _faker.Generate();

        // Act
        var query = Query.Where<Person>(p => p.FirstName == person.FirstName);

        // Assert
        Assert.NotNull(query.Filter);
        Assert.Null(query.Order);
        Assert.IsType<ExpressionQueryFilter<Person>>(query.Filter);
    }

    [Fact]
    public void Should_HaveCombinedFilter_When_TwoWhereClausesAreChained()
    {
        // Arrange
        var person = _faker.Generate();

        // Act
        var query = new QueryBuilder<Person>()
            .Where(p => p.FirstName == person.FirstName)
            .Where(p => p.LastName == person.LastName)
            .Query;

        // Assert
        Assert.NotNull(query.Filter);
        Assert.Null(query.Order);
        var combined = Assert.IsType<CombinedQueryFilter>(query.Filter);
        Assert.Equal(2, combined.Count());
        Assert.IsType<ExpressionQueryFilter<Person>>(combined.ElementAt(0));
        Assert.IsType<ExpressionQueryFilter<Person>>(combined.ElementAt(1));
    }

    [Fact]
    public void Should_HaveFilterAndExpressionSort_When_WhereAndOrderByExpressionAreChained()
    {
        // Arrange
        var person = _faker.Generate();

        // Act
        var query = new QueryBuilder<Person>()
            .Where(p => p.FirstName == person.FirstName)
            .OrderBy(p => p.LastName)
            .Query;

        // Assert
        Assert.IsType<ExpressionQueryFilter<Person>>(query.Filter);
        Assert.IsType<ExpressionSort<Person>>(query.Order);
    }

    [Fact]
    public void Should_HaveFilterAndFieldSort_When_WhereAndOrderByFieldNameAreChained()
    {
        // Arrange
        var person = _faker.Generate();

        // Act
        var query = new QueryBuilder<Person>()
            .Where(p => p.FirstName == person.FirstName)
            .OrderBy("LastName")
            .Query;

        // Assert
        Assert.IsType<ExpressionQueryFilter<Person>>(query.Filter);
        Assert.IsType<FieldOrder>(query.Order);
    }

    [Fact]
    public void Should_HaveDescendingFieldSort_When_WhereAndOrderByDescendingFieldNameAreChained()
    {
        // Arrange
        var person = _faker.Generate();

        // Act
        var query = new QueryBuilder<Person>()
            .Where(p => p.FirstName == person.FirstName)
            .OrderByDescending("LastName")
            .Query;

        // Assert
        Assert.IsType<ExpressionQueryFilter<Person>>(query.Filter);
        var sort = Assert.IsType<FieldOrder>(query.Order);
        Assert.True(sort.IsDescending());
    }

    [Fact]
    public void Should_HaveDescendingExpressionSort_When_WhereAndOrderByDescendingExpressionAreChained()
    {
        // Arrange
        var person = _faker.Generate();

        // Act
        var query = new QueryBuilder<Person>()
            .Where(p => p.FirstName == person.FirstName)
            .OrderByDescending(p => p.LastName)
            .Query;

        // Assert
        Assert.IsType<ExpressionQueryFilter<Person>>(query.Filter);
        var sort = Assert.IsType<ExpressionSort<Person>>(query.Order);
        Assert.True(sort.IsDescending());
    }

    #endregion

    #region Apply

    [Fact]
    public void Should_FilterQueryable_When_ApplyingQueryWithWhereOnly()
    {
        // Arrange
        var people = _faker.Generate(10);
        var matches = people.Take(3).ToList();
        var sharedName = Guid.NewGuid().ToString("N")[..8]; // unique surname to avoid collision
        foreach (var p in matches)
            p.LastName = sharedName;

        var query = Query.Where<Person>(p => p.LastName == sharedName);

        // Act
        var result = query.Apply(people.AsQueryable());

        // Assert
        Assert.Equal(matches.Count, result.Count());
    }

    [Fact]
    public void Should_FilterAndSortQueryable_When_ApplyingQueryWithWhereAndOrderBy()
    {
        // Arrange
        var people = _faker.Generate(10);
        var matches = people.Take(3).ToList();
        var sharedName = Guid.NewGuid().ToString("N")[..8];
        foreach (var p in matches)
            p.LastName = sharedName;

        var query = new QueryBuilder<Person>()
            .Where(p => p.LastName == sharedName)
            .OrderBy(p => p.FirstName);

        // Act
        var result = query.Apply(people.AsQueryable());

        // Assert
        Assert.Equal(matches.Count, result.Count());
        Assert.Equal(matches.OrderBy(p => p.FirstName).First().FirstName, result.First().FirstName);
    }

    #endregion
}
