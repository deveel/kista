namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "Security")]
public class KistaParsingConfigTests {
    [Fact]
    public void Should_BlockNewOperator_When_HardenedConfigUsed() {
        // Arrange & Act & Assert — the `new` operator must be blocked by KistaParsingConfig.
        Assert.Throws<InvalidOperationException>(() =>
            FilterExpression.AsLambda<Person>("p", "new System.Text.StringBuilder().Length > 0"));
    }

    [Fact]
    public void Should_BlockNewStringExpression_When_HardenedConfigUsed() {
        // Arrange & Act & Assert — even `new("test")` must be blocked.
        Assert.Throws<InvalidOperationException>(() =>
            FilterExpression.AsLambda<Person>("p", "new(\"test\").Length > 0"));
    }

    [Fact]
    public void Should_BlockFullyQualifiedTypeCast_When_HardenedConfigUsed() {
        // Arrange & Act & Assert — fully-qualified type casts must be blocked.
        Assert.Throws<InvalidOperationException>(() =>
            FilterExpression.AsLambda<Person>("p", "(System.IO.FileInfo) null != null"));
    }

    [Fact]
    public void Should_BlockStaticMethodCall_When_HardenedConfigUsed() {
        // Arrange & Act & Assert — static method calls to arbitrary types must be blocked.
        Assert.Throws<InvalidOperationException>(() =>
            FilterExpression.AsLambda<Person>("p", "System.IO.File.Exists(\"x\")"));
    }

    [Fact]
    public void Should_AllowLegitimateFilter_When_HardenedConfigUsed() {
        // Arrange & Act — a legitimate filter expression must still parse.
        var expression = FilterExpression.AsLambda<Person>("p", "p.FirstName == \"John\" && p.LastName == \"Doe\"");

        // Assert
        Assert.NotNull(expression);
        Assert.Equal(typeof(bool), expression.ReturnType);
    }

    [Fact]
    public void Should_AllowMemberAccess_When_HardenedConfigUsed() {
        // Arrange & Act — member access on the parameter must still work.
        var expression = FilterExpression.AsLambda<Person>("p", "p.DateOfBirth.Year > 2000");

        // Assert
        Assert.NotNull(expression);
        Assert.Equal(typeof(bool), expression.ReturnType);
    }

    [Fact]
    public void Should_DisallowNewKeyword_OnConfigInstance() {
        // Assert — the hardened config must have DisallowNewKeyword = true.
        Assert.True(KistaParsingConfig.Instance.DisallowNewKeyword);
    }

    [Fact]
    public void Should_DisableFullyQualifiedCasting_OnConfigInstance() {
        // Assert — the hardened config must have SupportCastingToFullyQualifiedTypeAsString = false.
        Assert.False(KistaParsingConfig.Instance.SupportCastingToFullyQualifiedTypeAsString);
    }
}