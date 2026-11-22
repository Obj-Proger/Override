namespace Override.Learning.Tests;

public sealed class LevelDiagnosisTests
{
    [Theory]
    [InlineData(3, 4, true)]
    [InlineData(2, 4, false)]
    [InlineData(0, 0, false)]
    public void PassesFromThreeQuarters(int correct, int total, bool expected) =>
        Assert.Equal(expected, LevelDiagnosis.Passed(correct, total));

    [Fact]
    public void ReachesTheHighestPassedLevelInARow()
    {
        Assert.Equal(1, LevelDiagnosis.Reached([(4, 4), (3, 4), (1, 4)]));
        Assert.Equal(2, LevelDiagnosis.Reached([(4, 4), (4, 4), (3, 4)]));
    }

    [Fact]
    public void LowestLevelIsTheFloor() => Assert.Equal(0, LevelDiagnosis.Reached([(1, 4)]));

    [Fact]
    public void SpreadsQuestionsOverTopics()
    {
        var picked = LevelDiagnosis.Spread<string>([["a1", "a2", "a3"], ["b1"], []], 4);

        Assert.Equal(new[] { "a1", "b1", "a2", "a3" }, picked);
    }
}
