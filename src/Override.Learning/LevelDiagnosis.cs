namespace Override.Learning;

/// <summary>
/// Picks the level from diagnostic answers. Levels are checked from the lowest: a level counts as reached
/// when at least three quarters of its questions are right, and the next level is asked only after that.
/// </summary>
public static class LevelDiagnosis
{
    public const double PassShare = 0.75;

    /// <summary>Whether a level with these answers is reached and the next level is worth asking.</summary>
    public static bool Passed(int correct, int total) => total > 0 && (double)correct / total >= PassShare;

    /// <summary>
    /// The highest reached level, counted from 0, given the results of each asked level in order.
    /// The lowest level is the floor even if it was not reached: the diagnostic does not go below it.
    /// </summary>
    public static int Reached(IReadOnlyList<(int Correct, int Total)> levels)
    {
        var reached = 0;
        for (var i = 0; i < levels.Count; i++)
        {
            if (!Passed(levels[i].Correct, levels[i].Total))
                break;

            reached = i;
        }

        return reached;
    }

    /// <summary>
    /// Picks up to count questions spread over topics: one from each topic in turn, so a single long
    /// topic does not decide the level.
    /// </summary>
    public static IReadOnlyList<T> Spread<T>(IEnumerable<IReadOnlyList<T>> byTopic, int count)
    {
        var queues = byTopic.Where(q => q.Count > 0).Select(q => new Queue<T>(q)).ToList();
        var result = new List<T>();

        while (result.Count < count && queues.Count > 0)
        {
            foreach (var queue in queues.ToList())
            {
                if (result.Count == count)
                    break;

                result.Add(queue.Dequeue());
                if (queue.Count == 0)
                    queues.Remove(queue);
            }
        }

        return result;
    }
}
