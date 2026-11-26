namespace Override.Learning.Tests;

public sealed class ReviewScheduleTests
{
    private static readonly DateTimeOffset _start = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

    private static Attempt At(double days, Outcome outcome, bool first = true) => new(_start.AddDays(days), outcome, first);

    [Fact]
    public void NeverAnsweredIsBoxZeroWithoutReview()
    {
        var state = ReviewSchedule.StateOf([]);

        Assert.Equal(0, state.Box);
        Assert.Null(state.Due);
    }

    [Fact]
    public void RightFirstAttemptsMoveUpWithLongerIntervals()
    {
        var state = ReviewSchedule.StateOf([At(0, Outcome.Correct), At(1, Outcome.Correct), At(4, Outcome.Correct)]);

        Assert.Equal(3, state.Box);
        Assert.Equal(_start.AddDays(4 + 7), state.Due);
    }

    [Fact]
    public void BoxStopsAtTheTop()
    {
        var attempts = Enumerable.Range(0, 8).Select(i => At(i * 40, Outcome.Correct));

        Assert.Equal(ReviewSchedule.MaxBox, ReviewSchedule.StateOf(attempts).Box);
    }

    [Fact]
    public void AnswerToADueQuestionCountsEvenRightAfterAnother()
    {
        // Wrong at day 0, due at day 1; a retry at day 1 minus a few hours is not first,
        // but an answer once the question is due moves it up.
        var state = ReviewSchedule.StateOf([At(0, Outcome.Wrong), At(1, Outcome.Correct, first: false)]);

        Assert.Equal(2, state.Box);
        Assert.False(state.LastWasProblem);
        Assert.Equal(_start.AddDays(1 + 3), state.Due);
    }

    [Fact]
    public void ProblemSendsBackToFirstBox()
    {
        var state = ReviewSchedule.StateOf([At(0, Outcome.Correct), At(1, Outcome.Correct), At(4, Outcome.Wrong)]);

        Assert.Equal(1, state.Box);
        Assert.True(state.LastWasProblem);
        Assert.Equal(_start.AddDays(5), state.Due);
    }

    [Fact]
    public void RetriesDoNotCount()
    {
        var state = ReviewSchedule.StateOf([At(0, Outcome.Wrong), At(0.01, Outcome.Correct, first: false)]);

        Assert.Equal(1, state.Box);
        Assert.True(state.LastWasProblem);
    }

    [Fact]
    public void RecalledNeverRaisesTheBox()
    {
        var state = ReviewSchedule.StateOf([At(0, Outcome.Recalled), At(5, Outcome.Recalled)]);

        Assert.Equal(0, state.Box);
        Assert.Null(state.Due);
    }

    [Fact]
    public void ForgottenRecallComesBackUntilRecalled()
    {
        var forgotten = ReviewSchedule.StateOf([At(0, Outcome.Forgot)]);
        var recalledLater = ReviewSchedule.StateOf([At(0, Outcome.Forgot), At(1, Outcome.Recalled)]);

        Assert.True(forgotten.IsDue(_start.AddDays(1)));
        Assert.Null(recalledLater.Due);
        Assert.False(recalledLater.LastWasProblem);
    }
}
