namespace Deveel.Data;

/// <summary>
/// Tests for <see cref="RepositoryException"/> construction, verifying
/// parameterless, message-only, and message + inner exception constructors.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Core")]
[Trait("Feature", "Exceptions")]
public class RepositoryExceptionTests
{
	[Fact]
	public void ParameterlessConstructor_ShouldCreateException()
	{
		// Act
		var exception = new RepositoryException();

		// Assert
		Assert.NotNull(exception);
	}

	[Fact]
	public void ConstructorWithMessage_ShouldSetMessage()
	{
		// Arrange
		var message = "Test error message";

		// Act
		var exception = new RepositoryException(message);

		// Assert
		Assert.Equal(message, exception.Message);
	}

	[Fact]
	public void ConstructorWithMessageAndInnerException_ShouldSetBoth()
	{
		// Arrange
		var message = "Test error message";
		var inner = new InvalidOperationException("Inner");

		// Act
		var exception = new RepositoryException(message, inner);

		// Assert
		Assert.Equal(message, exception.Message);
		Assert.Same(inner, exception.InnerException);
	}
}
