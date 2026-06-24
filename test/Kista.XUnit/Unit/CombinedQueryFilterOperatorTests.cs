namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Core")]
[Trait("Feature", "QueryFilter")]
public class CombinedQueryFilterOperatorTests {
    [Fact]
    public void Should_DefaultToAnd_When_NoOperatorSpecified() {
        var filter = new CombinedQueryFilter(new IQueryFilter[] {
            QueryFilter.Where<Person>(p => p.FirstName == "John"),
            QueryFilter.Where<Person>(p => p.LastName == "Doe")
        });

        Assert.Equal(FilterLogicalOperator.And, filter.LogicalOperator);
    }

    [Fact]
    public void Should_UseAnd_When_ExplicitlySpecified() {
        var filter = new CombinedQueryFilter(new IQueryFilter[] {
            QueryFilter.Where<Person>(p => p.FirstName == "John"),
            QueryFilter.Where<Person>(p => p.LastName == "Doe")
        }, FilterLogicalOperator.And);

        Assert.Equal(FilterLogicalOperator.And, filter.LogicalOperator);
    }

    [Fact]
    public void Should_UseOr_When_Specified() {
        var filter = new CombinedQueryFilter(new IQueryFilter[] {
            QueryFilter.Where<Person>(p => p.FirstName == "John"),
            QueryFilter.Where<Person>(p => p.LastName == "Doe")
        }, FilterLogicalOperator.Or);

        Assert.Equal(FilterLogicalOperator.Or, filter.LogicalOperator);
    }

    [Fact]
    public void Should_ProduceAndExpression_When_OperatorIsAnd() {
        var filter = new CombinedQueryFilter(new IQueryFilter[] {
            QueryFilter.Where<Person>(p => p.FirstName == "John"),
            QueryFilter.Where<Person>(p => p.LastName == "Doe")
        }, FilterLogicalOperator.And);

        var lambda = filter.AsLambda<Person>();
        var compiled = lambda.Compile();

        var match = new Person { FirstName = "John", LastName = "Doe" };
        var noMatch = new Person { FirstName = "John", LastName = "Smith" };

        Assert.True(compiled(match));
        Assert.False(compiled(noMatch));
    }

    [Fact]
    public void Should_ProduceOrExpression_When_OperatorIsOr() {
        var filter = new CombinedQueryFilter(new IQueryFilter[] {
            QueryFilter.Where<Person>(p => p.FirstName == "John"),
            QueryFilter.Where<Person>(p => p.LastName == "Doe")
        }, FilterLogicalOperator.Or);

        var lambda = filter.AsLambda<Person>();
        var compiled = lambda.Compile();

        var matchFirst = new Person { FirstName = "John", LastName = "Smith" };
        var matchLast = new Person { FirstName = "Jane", LastName = "Doe" };
        var noMatch = new Person { FirstName = "Jane", LastName = "Smith" };

        Assert.True(compiled(matchFirst));
        Assert.True(compiled(matchLast));
        Assert.False(compiled(noMatch));
    }

    [Fact]
    public void Should_PreserveOperator_When_Combining() {
        var f1 = new CombinedQueryFilter(new IQueryFilter[] {
            QueryFilter.Where<Person>(p => p.FirstName == "John")
        }, FilterLogicalOperator.Or);

        var f2 = QueryFilter.Where<Person>(p => p.LastName == "Doe");
        var combined = f1.Combine(f2);

        Assert.Equal(FilterLogicalOperator.Or, combined.LogicalOperator);
    }

    [Fact]
    public void Should_ApplyOrFilter_ToQueryable() {
        var filter = new CombinedQueryFilter(new IQueryFilter[] {
            QueryFilter.Where<Person>(p => p.FirstName == "John"),
            QueryFilter.Where<Person>(p => p.LastName == "Doe")
        }, FilterLogicalOperator.Or);

        var people = new List<Person> {
            new Person { FirstName = "John", LastName = "Smith" },
            new Person { FirstName = "Jane", LastName = "Doe" },
            new Person { FirstName = "Jane", LastName = "Smith" }
        };

        var result = filter.Apply(people.AsQueryable()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.FirstName == "John");
        Assert.Contains(result, p => p.LastName == "Doe");
    }

    [Fact]
    public void Should_ApplyAndFilter_ToQueryable() {
        var filter = new CombinedQueryFilter(new IQueryFilter[] {
            QueryFilter.Where<Person>(p => p.FirstName == "John"),
            QueryFilter.Where<Person>(p => p.LastName == "Doe")
        }, FilterLogicalOperator.And);

        var people = new List<Person> {
            new Person { FirstName = "John", LastName = "Doe" },
            new Person { FirstName = "John", LastName = "Smith" },
            new Person { FirstName = "Jane", LastName = "Doe" }
        };

        var result = filter.Apply(people.AsQueryable()).ToList();

        Assert.Single(result);
        Assert.Equal("John", result[0].FirstName);
        Assert.Equal("Doe", result[0].LastName);
    }
}
