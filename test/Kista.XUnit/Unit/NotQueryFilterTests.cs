using System.Linq.Expressions;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Core")]
[Trait("Feature", "QueryFilter")]
public class NotQueryFilterTests {
    [Fact]
    public void Should_NegateExpression_When_AsLambdaIsCalled() {
        var inner = QueryFilter.Where<Person>(p => p.FirstName == "John");
        var notFilter = new NotQueryFilter(inner);

        var lambda = notFilter.AsLambda<Person>();
        var compiled = lambda.Compile();

        var person = new Person { FirstName = "John" };
        Assert.False(compiled(person));

        var other = new Person { FirstName = "Jane" };
        Assert.True(compiled(other));
    }

    [Fact]
    public void Should_ThrowArgumentNullException_When_InnerFilterIsNull() {
        Assert.Throws<ArgumentNullException>(() => new NotQueryFilter(null!));
    }

    [Fact]
    public void Should_ImplementIExpressionQueryFilter() {
        var notFilter = new NotQueryFilter(QueryFilter.Where<Person>(p => p.FirstName == "John"));

        Assert.IsAssignableFrom<IExpressionQueryFilter>(notFilter);
    }

    [Fact]
    public void Should_ExposeInnerFilter() {
        var inner = QueryFilter.Where<Person>(p => p.FirstName == "John");
        var notFilter = new NotQueryFilter(inner);

        Assert.Same(inner, notFilter.InnerFilter);
    }

    [Fact]
    public void Should_InitializeInnerFilter() {
        var inner = new TestFilter();
        var notFilter = new NotQueryFilter(inner);

        var context = new TestFilterContext();
        notFilter.Initialize(context);

        Assert.True(inner.WasInitialized);
    }

    private class TestFilter : IExpressionQueryFilter {
        public bool WasInitialized { get; private set; }

        public void Initialize(IFilterContext context) {
            WasInitialized = true;
        }

        public Expression<Func<TEntity, bool>> AsLambda<TEntity>() where TEntity : class {
            return _ => true;
        }
    }

    private class TestFilterContext : IFilterContext {
        public IServiceProvider Services => null!;
    }
}
