namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "DynamicLinq")]
public class DynamicLinqFilterTests {
    [Fact]
    public void Should_BuildTypedLambda_When_ValidExpressionProvided() {
        // Arrange
        var filter = new DynamicLinqFilter("p", "p.FirstName == \"John\"");

        // Act
        var expression = filter.AsLambda<Person>();

        // Assert
        Assert.Equal("p", filter.ParameterName);
        Assert.Equal("p.FirstName == \"John\"", filter.Expression);
        Assert.NotNull(expression);
        Assert.Equal(typeof(Person), expression.Parameters[0].Type);
        Assert.Equal(typeof(bool), expression.ReturnType);
    }

    [Fact]
    public void Should_ThrowInvalidOperationException_When_ExpressionDoesNotReturnBool() {
        // Arrange
        var filter = new DynamicLinqFilter("p", "p.FirstName");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => filter.AsLambda<Person>());
    }
}
