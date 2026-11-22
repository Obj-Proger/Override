namespace Override.Learning.Tests;

public sealed class ReviewQueueTests
{
    [Fact]
    public void InterleavesTopicsStartingFromTheWeakest()
    {
        var due = new[]
        {
            new DueQuestion("strong", "s1", 0.8),
            new DueQuestion("strong", "s2", 0.8),
            new DueQuestion("weak", "w1", 0.2),
            new DueQuestion("weak", "w2", 0.2),
            new DueQuestion("weak", "w3", 0.2),
        };

        var queue = ReviewQueue.Build(due);

        Assert.Equal(new[] { "w1", "s1", "w2", "s2", "w3" }, queue.Select(q => q.QuestionId));
    }

    [Fact]
    public void StopsAtTheLimit()
    {
        var due = Enumerable.Range(0, 30).Select(i => new DueQuestion($"t{i % 3}", $"q{i}", 0.5));

        Assert.Equal(10, ReviewQueue.Build(due, limit: 10).Count);
    }

    [Fact]
    public void EmptyWhenNothingIsDue()
    {
        Assert.Empty(ReviewQueue.Build([]));
    }
}
