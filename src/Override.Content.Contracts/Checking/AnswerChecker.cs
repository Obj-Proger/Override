using Override.Content.Model;

namespace Override.Content.Checking;

/// <summary>
/// Checks answers to the questions the site grades by itself. Typed answers ignore spaces at the ends of lines
/// and empty lines around the text; letter case matters only when the question says so.
/// </summary>
public static class AnswerChecker
{
    /// <summary>Questions that can be checked without running code. The rest are graded by the user or by a runner.</summary>
    public static bool IsChecked(Quiz quiz) => quiz is not (RecallQuiz or CodeQuiz or SqlQuiz);

    /// <summary>Option index counts from 0, as in the content.</summary>
    public static bool Check(ChoiceQuiz quiz, int option) => option == quiz.Answer;

    public static bool Check(MultiQuiz quiz, IEnumerable<int> options) => options.ToHashSet().SetEquals(quiz.Answers);

    public static bool Check(InputQuiz quiz, string answer) => Matches(quiz.Answers, quiz.CaseSensitive, answer);

    public static bool Check(OutputQuiz quiz, string answer) => Matches(quiz.Answers, quiz.CaseSensitive, answer);

    public static bool Check(FillQuiz quiz, string answer) => Matches(quiz.Answers, quiz.CaseSensitive, answer);

    /// <summary>Line counts from 1, as in the content.</summary>
    public static bool Check(FindBugQuiz quiz, int line) => line == quiz.Line;

    /// <summary>The lines must be exactly the expected ones, in order; any distractor makes the answer wrong.</summary>
    public static bool Check(ParsonsQuiz quiz, IEnumerable<string> lines) => lines.SequenceEqual(quiz.Lines, StringComparer.Ordinal);

    public static string Normalize(string text, bool caseSensitive)
    {
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n')
            .Select(line => line.Trim());

        var normalized = string.Join('\n', lines).Trim('\n');
        return caseSensitive ? normalized : normalized.ToLowerInvariant();
    }

    private static bool Matches(IReadOnlyList<string> answers, bool caseSensitive, string answer)
    {
        var given = Normalize(answer, caseSensitive);
        return given.Length > 0 && answers.Any(expected => Normalize(expected, caseSensitive) == given);
    }
}
