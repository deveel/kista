using Microsoft.AspNetCore.Http;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
[Trait("Feature", "AspNetCoreExtensions")]
public class AspNetCoreExtensionsTests {
	[Fact]
	public void WithHttpRequestCancellation_Should_RegisterServices() {
		// Arrange
		var services = new ServiceCollection();
		var builder = new RepositoryContextBuilder(services);

		// Act
		builder.WithHttpRequestCancellation();

		// Assert
		var provider = services.BuildServiceProvider();
		var accessor = provider.GetService<IHttpContextAccessor>();
		Assert.NotNull(accessor);
		var source = provider.GetService<IOperationCancellationSource>();
		Assert.NotNull(source);
		Assert.IsType<HttpRequestCancellationSource>(source);
	}

	[Fact]
	public void WithHttpRequestCancellation_Should_ReturnSameBuilder() {
		// Arrange
		var services = new ServiceCollection();
		var builder = new RepositoryContextBuilder(services);

		// Act
		var result = builder.WithHttpRequestCancellation();

		// Assert
		Assert.Same(builder, result);
	}
}
