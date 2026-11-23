namespace Override.Learning.Tests;

public sealed class TestDrawTests
{
    private static readonly string[] _pool = ["a1", "a2", "a3", "b1", "b2", "c1", "x1", "x2"];

    private static string? PartOf(string question) => question[0] == 'x' ? null : question[..1];

    [Fact]
    public void TakesTheRequestedNumberWithoutRepeats()
    {
        var drawn = TestDraw.Draw(_pool, PartOf, 5, new Random(1));

        Assert.Equal(5, drawn.Count);
        Assert.Equal(drawn.Count, drawn.Distinct().Count());
        Assert.All(drawn, question => Assert.Contains(question, _pool));
    }

    [Fact]
    public void CoversEveryPartWhenThereIsRoom()
    {
        for (var seed = 0; seed < 50; seed++)
        {
            var drawn = TestDraw.Draw(_pool, PartOf, 3, new Random(seed));

            Assert.Equal(new[] { "a", "b", "c" }, drawn.Select(q => q[..1]).Order());
        }
    }

    [Fact]
    public void TakesTheWholePoolWhenItIsSmall()
    {
        var drawn = TestDraw.Draw(_pool, PartOf, 20, new Random(2));

        Assert.Equal(_pool.Order(), drawn.Order());
    }

    [Fact]
    public void DifferentAttemptsGetDifferentSets()
    {
        var sets = Enumerable.Range(0, 20)
            .Select(seed => string.Join(",", TestDraw.Draw(_pool, PartOf, 4, new Random(seed)).Order()))
            .Distinct()
            .Count();

        Assert.True(sets > 1);
    }
}
