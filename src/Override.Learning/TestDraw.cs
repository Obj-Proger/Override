namespace Override.Learning;

/// <summary>
/// Draws the questions of one test attempt from the pool of a topic. Each lesson part gets at least one question
/// while there is room, the rest are random, and the order is shuffled. A new attempt is a new set, so a test
/// cannot be passed by remembering it.
/// </summary>
public static class TestDraw
{
    /// <param name="pool">All questions the reader may get.</param>
    /// <param name="partOf">The lesson part a question checks, or null if it is not tied to a part.</param>
    /// <param name="size">How many questions to take; fewer if the pool is smaller.</param>
    /// <param name="random">The source of randomness; tests pass a seeded one.</param>
    public static IReadOnlyList<T> Draw<T>(IReadOnlyList<T> pool, Func<T, string?> partOf, int size, Random random)
    {
        var shuffled = pool.OrderBy(_ => random.Next()).ToList();
        var picked = new List<T>();
        var taken = new HashSet<int>();

        // The first question of each part in the shuffled order: one per part, the parts themselves in random order.
        var firstOfPart = shuffled
            .Select((question, index) => (Part: partOf(question), Index: index))
            .Where(item => item.Part is not null)
            .GroupBy(item => item.Part)
            .Select(group => group.First().Index);

        foreach (var index in firstOfPart)
        {
            if (picked.Count == size)
                break;

            picked.Add(shuffled[index]);
            taken.Add(index);
        }

        for (var index = 0; index < shuffled.Count && picked.Count < size; index++)
        {
            if (taken.Add(index))
                picked.Add(shuffled[index]);
        }

        return picked.OrderBy(_ => random.Next()).ToList();
    }
}
