namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Core")]
[Trait("Feature", "Specification")]
public class SpecificationTests {
    private sealed class ActivePersonSpec : Specification<Person> {
        public override IQuery ToQuery() {
            var filter = QueryFilter.Where<Person>(p => p.Email != null);
            return new Query(filter);
        }
    }

    private sealed class FirstNameSpec : Specification<Person> {
        private readonly string firstName;
        public FirstNameSpec(string firstName) => this.firstName = firstName;

        public override IQuery ToQuery() {
            var filter = QueryFilter.Where<Person>(p => p.FirstName == firstName);
            return new Query(filter);
        }
    }

    private sealed class LastNameSpec : Specification<Person> {
        private readonly string lastName;
        public LastNameSpec(string lastName) => this.lastName = lastName;

        public override IQuery ToQuery() {
            var filter = QueryFilter.Where<Person>(p => p.LastName == lastName);
            return new Query(filter);
        }
    }

    [Fact]
    public void Should_ProduceQuery_When_ToQueryIsCalled() {
        var spec = new ActivePersonSpec();
        var query = spec.ToQuery();

        Assert.NotNull(query);
        Assert.NotNull(query.Filter);
        Assert.False(query.Filter.IsEmpty());
    }

    [Fact]
    public void Should_ProduceAndQuery_When_CombiningWithAnd() {
        var spec = new FirstNameSpec("John") & new LastNameSpec("Doe");
        var query = spec.ToQuery();

        Assert.NotNull(query.Filter);
        Assert.IsType<CombinedQueryFilter>(query.Filter);
        Assert.Equal(FilterLogicalOperator.And, ((CombinedQueryFilter)query.Filter).LogicalOperator);
    }

    [Fact]
    public void Should_ProduceOrQuery_When_CombiningWithOr() {
        var spec = new FirstNameSpec("John") | new LastNameSpec("Doe");
        var query = spec.ToQuery();

        Assert.NotNull(query.Filter);
        Assert.IsType<CombinedQueryFilter>(query.Filter);
        Assert.Equal(FilterLogicalOperator.Or, ((CombinedQueryFilter)query.Filter).LogicalOperator);
    }

    [Fact]
    public void Should_ProduceNotQuery_When_Negating() {
        var spec = !new FirstNameSpec("John");
        var query = spec.ToQuery();

        Assert.NotNull(query.Filter);
        Assert.IsType<NotQueryFilter>(query.Filter);
    }

    [Fact]
    public void Should_ReturnEmptyQuery_When_AndSpecHasNoFilters() {
        var spec = new EmptySpec() & new AnotherEmptySpec();
        var query = spec.ToQuery();

        Assert.NotNull(query.Filter);
        Assert.True(query.Filter.IsEmpty());
    }

    [Fact]
    public void Should_ReturnEmptyQuery_When_OrSpecHasNoFilters() {
        var spec = new EmptySpec() | new AnotherEmptySpec();
        var query = spec.ToQuery();

        Assert.NotNull(query.Filter);
        Assert.True(query.Filter.IsEmpty());
    }

    [Fact]
    public void Should_ReturnEmptyQuery_When_NotSpecHasEmptyFilter() {
        var spec = !new EmptySpec();
        var query = spec.ToQuery();

        Assert.NotNull(query.Filter);
        Assert.True(query.Filter.IsEmpty());
    }

    private sealed class EmptySpec : Specification<Person> {
        public override IQuery ToQuery() => Query.Empty;
    }

    private sealed class AnotherEmptySpec : Specification<Person> {
        public override IQuery ToQuery() => Query.Empty;
    }
}
