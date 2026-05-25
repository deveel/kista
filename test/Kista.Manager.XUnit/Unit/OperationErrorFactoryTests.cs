namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
[Trait("Feature", "OperationErrorFactory")]
public class OperationErrorFactoryTests {
    private readonly OperationErrorFactory _factory = new();

    #region CreateError

    [Fact]
    public void Should_CreateErrorWithCodeAndDomain_When_MinimalArgsProvided() {
        // Act
        var error = _factory.CreateError(EntityErrorCodes.NotFound, "test");

        // Assert
        Assert.NotNull(error);
        Assert.Equal(EntityErrorCodes.NotFound, error.Code);
        Assert.Equal("test", error.Domain);
        Assert.Null(error.Message);
    }

    [Fact]
    public void Should_CreateErrorWithMessage_When_MessageProvided() {
        // Act
        var error = _factory.CreateError(EntityErrorCodes.NotFound, "test", "The entity was not found");

        // Assert
        Assert.NotNull(error);
        Assert.Equal("test", error.Domain);
        Assert.Equal(EntityErrorCodes.NotFound, error.Code);
        Assert.Equal("The entity was not found", error.Message);
    }

    [Fact]
    public void Should_CreateUnknownError_When_GenericExceptionProvided() {
        // Arrange
        var exception = new Exception("Something went wrong");

        // Act
        var error = _factory.CreateError(exception);

        // Assert
        Assert.NotNull(error);
        Assert.Equal(EntityErrorCodes.UnknownError, error.Code);
        Assert.Equal(EntityErrorCodes.UnknownDomain, error.Domain);
        Assert.Equal("Something went wrong", error.Message);
    }

    [Fact]
    public void Should_CreateErrorFromOperationException_When_CodeAndDomainSet() {
        // Arrange
        var exception = new OperationException(EntityErrorCodes.NotFound, "test");

        // Act
        var error = _factory.CreateError(exception);

        // Assert
        Assert.NotNull(error);
        Assert.Equal(EntityErrorCodes.NotFound, error.Code);
        Assert.NotNull(error.Message);
    }

    [Fact]
    public void Should_CreateErrorWithMessage_When_OperationExceptionHasMessage() {
        // Arrange
        var exception = new OperationException(EntityErrorCodes.NotFound, "test", "The entity was not found");

        // Act
        var error = _factory.CreateError(exception);

        // Assert
        Assert.NotNull(error);
        Assert.Equal(EntityErrorCodes.NotFound, error.Code);
        Assert.Equal("The entity was not found", error.Message);
    }

    #endregion
}
