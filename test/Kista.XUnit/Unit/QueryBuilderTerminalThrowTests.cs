#pragma warning disable CS8618

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Core")]
[Trait("Feature", "QueryBuilder")]
public class QueryBuilderTerminalThrowTests {
	[Fact]
	public void Should_ThrowNotBound_When_FirstOrDefaultAsyncOnStandaloneBuilder() {
		var builder = new QueryBuilder<Person>();
		Assert.Throws<InvalidOperationException>(() => builder.FirstOrDefaultAsync());
	}

	[Fact]
	public void Should_ThrowNotBound_When_ToListAsyncOnStandaloneBuilder() {
		var builder = new QueryBuilder<Person>();
		Assert.Throws<InvalidOperationException>(() => builder.ToListAsync());
	}

	[Fact]
	public void Should_ThrowNotBound_When_CountAsyncOnStandaloneBuilder() {
		var builder = new QueryBuilder<Person>();
		Assert.Throws<InvalidOperationException>(() => builder.CountAsync());
	}

	[Fact]
	public void Should_ThrowNotBound_When_AnyAsyncOnStandaloneBuilder() {
		var builder = new QueryBuilder<Person>();
		Assert.Throws<InvalidOperationException>(() => builder.AnyAsync());
	}

	[Fact]
	public void Should_ThrowNotBound_When_GetPageAsyncOnStandaloneBuilder() {
		var builder = new QueryBuilder<Person>();
		Assert.Throws<InvalidOperationException>(() => builder.GetPageAsync(1, 10));
	}
}