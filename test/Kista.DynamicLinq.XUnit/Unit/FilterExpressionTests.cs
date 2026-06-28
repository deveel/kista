
namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "FilterExpression")]
public class FilterExpressionTests {
    [Fact]
    public void Should_BuildBoolLambda_When_ValidExpressionProvided() {
        // Arrange & Act
        var expression = FilterExpression.AsLambda<Person>("p", "p.FirstName == \"John\"");

        // Assert
        Assert.NotNull(expression);
        Assert.Equal(typeof(Person), expression.Parameters[0].Type);
        Assert.Equal(typeof(bool), expression.ReturnType);
    }

    [Fact]
    public void Should_ThrowInvalidOperationException_When_ExpressionDoesNotReturnBool() {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => FilterExpression.AsLambda<Person>("p", "p.FirstName"));
    }

    [Fact]
    public void Should_ThrowInvalidOperationException_When_TypeMismatch() {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => FilterExpression.AsLambda<Person>("p", "p.FirstName == 1"));
    }

    [Fact]
    public void Should_CompileToTypedDelegate_When_SingleParameterExpression() {
        // Arrange & Act
        var result = FilterExpression.Compile(typeof(Person), "p", "p.FirstName == \"John\"");

        // Assert
        Assert.NotNull(result);
        var func = Assert.IsType<Func<Person, bool>>(result);
        Assert.NotNull(func.Method);
        var pars = func.Method.GetParameters();
        Assert.NotEmpty(pars);
        Assert.Equal(2, pars.Length);
        Assert.Equal(typeof(Person), pars[1].ParameterType);
        Assert.Equal(typeof(bool), func.Method.ReturnType);
    }

    [Fact]
    public void Should_CacheCompileResult_When_CacheProvided() {
        // Arrange
        var cache = new SimpleFilterCache();

        // Act
        var result = FilterExpression.Compile(cache, typeof(Person), "p", "p.FirstName == \"John\"");

        // Assert
        Assert.NotNull(result);
        var func = Assert.IsType<Func<Person, bool>>(result);
        Assert.True(cache.TryGet("p.FirstName == \"John\"", out var cachedFunc));
        Assert.NotNull(cachedFunc);
        Assert.Equal(func, cachedFunc);
    }

    [Fact]
    public void Should_CompileGenericDelegate_When_TypedCompileUsed() {
        // Arrange & Act
        var result = FilterExpression.Compile<Person>("p", "p.FirstName == \"John\"");

        // Assert
        Assert.NotNull(result);
        var func = Assert.IsType<Func<Person, bool>>(result);
        Assert.NotNull(func.Method);
        Assert.Equal(typeof(bool), func.Method.ReturnType);
    }

    private sealed class SimpleFilterCache : IFilterCache {
        private readonly Dictionary<string, Delegate> _cache = new();

        public void Set(string expression, Delegate lambda) => _cache[expression] = lambda;

        public bool TryGet(string expression, out Delegate? lambda) =>
            _cache.TryGetValue(expression, out lambda);

        public void Clear() => _cache.Clear();
    }
}
