namespace Deveel.Data;

/// <summary>
/// Tests for <see cref="ExpressionQueryFilter{T}"/> and <see cref="QueryFilter"/>,
/// verifying lambda conversion, type compatibility, <see cref="IQueryFilter.IsEmpty"/>,
/// and LINQ extension method integration.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Core")]
[Trait("Feature", "QueryFilter")]
public class QueryFilterTests
{
    private readonly PersonFaker _faker = new PersonFaker();
    /// <summary>
    /// A simple entity type used to test <see cref="IQueryFilter.AsLambda{TEntity}"/>
    /// type-incompatibility error paths when converting between unrelated entity types.
    /// </summary>
    private class Company
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }

    /// <summary>
    /// A subclass of <see cref="Person"/> used to test that
    /// <see cref="IQueryFilter.AsLambda{TEntity}"/> accepts target types
    /// that are subtypes of the filter's declared entity type.
    /// </summary>
    private class Employee : Person
    {
        public string CompanyName { get; set; } = string.Empty;
        public string EmployeeNumber { get; set; } = string.Empty;
    }

    #region AsLambda

    [Fact]
    public void Should_ProduceLambdaExpression_When_ConvertingExpressionFilter()
    {
        // Arrange
        var expr = new ExpressionQueryFilter<Person>(x => x.FirstName == "John");

        // Act
        var lambda = expr.AsLambda<Person>();

        // Assert
        Assert.NotNull(lambda);
        Assert.Equal("x => (x.FirstName == \"John\")", lambda.ToString());
    }

    [Fact]
    public void Should_ProduceTrueLambda_When_ConvertingEmptyFilter()
    {
        // Arrange & Act
        var lambda = QueryFilter.Empty.AsLambda<Person>();

        // Assert
        Assert.NotNull(lambda);
        Assert.Equal("x => True", lambda.ToString());
    }

    [Fact]
    public void Should_ThrowArgumentException_When_ConvertingFilterToIncompatibleTargetType()
    {
        // Arrange
        var expr = new ExpressionQueryFilter<Person>(x => x.FirstName == "John");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => expr.AsLambda<Company>());
    }

    [Fact]
    public void Should_ProduceCompatibleLambda_When_TargetIsSubtypeOfFilterType()
    {
        // Arrange
        var expr = new ExpressionQueryFilter<Person>(x => x.FirstName == "John");

        // Act
        var result = expr.AsLambda<Employee>();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("x => (x.FirstName == \"John\")", result.ToString());
    }

    #endregion

    #region IsEmpty

    [Fact]
    public void Should_ReturnTrue_When_FilterIsEmpty()
    {
        // Arrange & Act & Assert
        Assert.True(QueryFilter.Empty.IsEmpty());
    }

    [Fact]
    public void Should_ReturnFalse_When_FilterHasExpressionCondition()
    {
        // Arrange
        var filter = new ExpressionQueryFilter<Person>(x => x.FirstName == "John");

        // Act & Assert
        Assert.False(filter.IsEmpty());
    }

    #endregion

    #region LINQ extension methods

    [Fact]
    public void Should_ReturnMatchingEntity_When_CallingFirstOrDefaultWithFilter()
    {
        // Arrange
        var people = _faker.Generate(10);
        var target = people[Random.Shared.Next(0, people.Count - 1)];
        var filter = new ExpressionQueryFilter<Person>(x => x.FirstName == target.FirstName);

        // Act
        var result = people.AsQueryable().FirstOrDefault(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(target.FirstName, result.FirstName);
    }

    [Fact]
    public void Should_ReturnMatchingList_When_CallingToListWithFilter()
    {
        // Arrange
        var people = _faker.Generate(100);
        var target = people[Random.Shared.Next(0, people.Count - 1)];
        var expected = people.Where(x => x.FirstName == target.FirstName).ToList();
        var filter = new ExpressionQueryFilter<Person>(x => x.FirstName == target.FirstName);

        // Act
        var result = people.AsQueryable().ToList(filter);

        // Assert
        Assert.Equal(expected.Count, result.Count);
        Assert.Equal(expected.First().FirstName, result.First().FirstName);
    }

    [Fact]
    public void Should_ReturnTrue_When_AnyMatchesFilter()
    {
        // Arrange
        var people = _faker.Generate(100);
        var target = people[Random.Shared.Next(0, people.Count - 1)];
        var filter = new ExpressionQueryFilter<Person>(x => x.FirstName == target.FirstName);

        // Act
        var result = people.AsQueryable().Any(filter);

        // Assert
        Assert.True(result);
    }

    #endregion
}
