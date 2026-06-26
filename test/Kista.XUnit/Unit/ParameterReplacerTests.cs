using System.Linq.Expressions;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Core")]
[Trait("Feature", "Query")]
public class ParameterReplacerTests
{
    [Fact]
    public void Should_CombineTwoExpressions_When_UsingCombine()
    {
        Expression<Func<string, bool>> left = x => x.Length > 3;
        Expression<Func<string, bool>> right = y => y.StartsWith('A');

        var combined = ParameterReplacer.Combine(left, right);
        var compiled = combined.Compile();

        Assert.True(compiled("ABCD"));
        Assert.False(compiled("AB"));
        Assert.False(compiled("BCD"));
    }

    [Fact]
    public void Should_CombineWithDifferentParameterNames()
    {
        Expression<Func<int, bool>> left = a => a > 0;
        Expression<Func<int, bool>> right = b => b < 10;

        var combined = ParameterReplacer.Combine(left, right);
        var compiled = combined.Compile();

        Assert.True(compiled(5));
        Assert.False(compiled(-1));
        Assert.False(compiled(15));
    }

    [Fact]
    public void Should_CombineWithSameParameterName()
    {
        Expression<Func<int, bool>> left = x => x > 0;
        Expression<Func<int, bool>> right = x => x < 10;

        var combined = ParameterReplacer.Combine(left, right);
        var compiled = combined.Compile();

        Assert.True(compiled(5));
        Assert.False(compiled(-1));
        Assert.False(compiled(15));
    }
}
