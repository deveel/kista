using Microsoft.Extensions.DependencyInjection;

namespace Kista;

/// <summary>
/// Tests for <see cref="CombinedQueryFilter"/> and <see cref="QueryFilter.Combine"/>,
/// verifying flattening of nested combined filters, empty-filter exclusion,
/// and <see cref="IExpressionQueryFilter.AsLambda{T}"/> producing correct
/// <c>AndAlso</c> expression trees.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Core")]
[Trait("Feature", "QueryFilter")]
public class CombinedFilterTests
{

    #region QueryFilter.Combine

    [Fact]
    public void Should_FlattenFilters_When_CombiningAlreadyCombinedFilter()
    {
        // Arrange
        var filter1 = new ExpressionQueryFilter<Person>(x => x.FirstName == "John");
        var filter2 = new ExpressionQueryFilter<Person>(x => x.LastName == "Doe");
        var combined1 = QueryFilter.Combine(filter1, filter2);

        // Act
        var combined2 = combined1.Combine(QueryFilter.Empty);

        // Assert
        var result = Assert.IsType<CombinedQueryFilter>(combined2);
        Assert.Equal(3, result.Count());
        Assert.Equal(filter1, result.ElementAt(0));
        Assert.Equal(filter2, result.ElementAt(1));
        Assert.Equal(QueryFilter.Empty, result.ElementAt(2));
    }

    [Fact]
    public void Should_ReturnCombinedFilter_When_TwoExpressionFiltersAreCombined()
    {
        // Arrange
        var filter1 = new ExpressionQueryFilter<Person>(x => x.FirstName == "John");
        var filter2 = new ExpressionQueryFilter<Person>(x => x.LastName == "Doe");

        // Act
        var combined = QueryFilter.Combine(filter1, filter2);

        // Assert
        var result = Assert.IsType<CombinedQueryFilter>(combined);
        Assert.Equal(2, result.Count());
        Assert.Equal(filter1, result.ElementAt(0));
        Assert.Equal(filter2, result.ElementAt(1));
    }

    [Fact]
    public void Should_ExcludeEmptyFilter_When_CombiningWithEmpty()
    {
        // Arrange
        var filter1 = new ExpressionQueryFilter<Person>(x => x.FirstName == "John");

        // Act
        var combined = QueryFilter.Combine(filter1, QueryFilter.Empty);

        // Assert
        var result = Assert.IsType<CombinedQueryFilter>(combined);
        Assert.Single(result);
        Assert.Equal(filter1, result.ElementAt(0));
    }

    [Fact]
    public void Should_ThrowArgumentException_When_AllFiltersAreEmpty()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => QueryFilter.Combine(QueryFilter.Empty, QueryFilter.Empty));
    }

    [Fact]
    public void Should_ProduceAndAlsoLambda_When_ConvertingCombinedFilter()
    {
        // Arrange
        var filter1 = new ExpressionQueryFilter<Person>(x => x.FirstName == "John");
        var filter2 = new ExpressionQueryFilter<Person>(x => x.LastName == "Doe");
        var combined = QueryFilter.Combine(filter1, filter2);

        // Act
        var lambda = combined.AsLambda<Person>();

        // Assert
        Assert.NotNull(lambda);
        Assert.Equal("x => ((x.FirstName == \"John\") AndAlso (x.LastName == \"Doe\"))", lambda.ToString());
    }

    [Fact]
    public void Should_ProduceSingleConditionLambda_When_CombinedFilterHasOnlyOneNonEmptyFilter()
    {
        // Arrange
        var filter1 = new ExpressionQueryFilter<Person>(x => x.FirstName == "John");
        var combined = QueryFilter.Combine(filter1, QueryFilter.Empty);

        // Act
        var lambda = combined.AsLambda<Person>();

        // Assert
        Assert.NotNull(lambda);
        Assert.Equal("x => (x.FirstName == \"John\")", lambda.ToString());
    }

    [Fact]
    public void Should_ProduceTripleAndAlsoLambda_When_ThreeFiltersAreCombined()
    {
        // Arrange
        var filter1 = new ExpressionQueryFilter<Person>(x => x.FirstName == "John");
        var filter2 = new ExpressionQueryFilter<Person>(x => x.LastName == "Doe");
        var filter3 = new ExpressionQueryFilter<Person>(x => x.Email == "john.doe@example.com");

        // Act
        var combined = QueryFilter.Combine(filter1, filter2, filter3);
        var lambda = combined.AsLambda<Person>();

        // Assert
        Assert.NotNull(lambda);
        Assert.Equal(
            "x => (((x.FirstName == \"John\") AndAlso (x.LastName == \"Doe\")) AndAlso (x.Email == \"john.doe@example.com\"))",
            lambda.ToString());
    }

	#endregion

	[Fact]
	public void CombinedQueryFilter_Apply_WithMultipleFilters_FiltersCorrectly() {
		var people = new List<Person> {
			new Person { FirstName = "A", LastName = "B" },
			new Person { FirstName = "A", LastName = "C" },
			new Person { FirstName = "B", LastName = "B" }
		}.AsQueryable();
		var f1 = QueryFilter.Where<Person>(x => x.FirstName == "A");
		var f2 = QueryFilter.Where<Person>(x => x.LastName == "B");
		var combined = QueryFilter.Combine(f1, f2);

		var result = combined.Apply(people).ToList();

		Assert.Single(result);
		Assert.Equal("A", result[0].FirstName);
		Assert.Equal("B", result[0].LastName);
	}

	[Fact]
	public void CombinedQueryFilter_Initialize_CallsAllFilters() {
		var f1 = QueryFilter.Where<Person>(x => x.FirstName == "A");
		var f2 = QueryFilter.Where<Person>(x => x.LastName == "B");
		var combined = QueryFilter.Combine(f1, f2);
		var ctx = new DefaultFilterContext(new ServiceCollection().BuildServiceProvider());
		combined.Initialize(ctx);
	}
}
