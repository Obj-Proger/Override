namespace Override.Learning.Tests;

public sealed class StrengthTests
{
    private static readonly DateTimeOffset _start = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

    private static ReviewState Box(int box) => new(box, _start, _start + ReviewSchedule.IntervalOf(box), false);

    [Fact]
    public void GrowsWithTheBox()
    {
        Assert.Equal(0.4, Strength.OfQuestion(Box(1), _start));
        Assert.Equal(1.0, Strength.OfQuestion(Box(5), _start));
    }

    [Fact]
    public void ProblemsAndNewQuestionsAreZero()
    {
        Assert.Equal(0.0, Strength.OfQuestion(ReviewState.New, _start));
        Assert.Equal(0.0, Strength.OfQuestion(Box(1) with { LastWasProblem = true }, _start));
    }

    [Fact]
    public void HalvesWithEachOverdueInterval()
    {
        // Box 3 has a 7 day interval: 7 days after the planned review the strength is half.
        var state = Box(3);

        Assert.Equal(0.75, Strength.OfQuestion(state, _start.AddDays(7)));
        Assert.Equal(0.375, Strength.OfQuestion(state, _start.AddDays(14)), precision: 6);
    }

    [Fact]
    public void TopicCountsUnansweredQuestionsAsZero()
    {
        var topic = Strength.OfTopic([Box(5), ReviewState.New], _start);

        Assert.Equal(0.5, topic);
    }
}
