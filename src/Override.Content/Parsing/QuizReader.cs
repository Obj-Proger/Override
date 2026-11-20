using Override.Content.Model;

namespace Override.Content.Parsing;

/// <summary>Reads and validates one ```quiz block.</summary>
internal static class QuizReader
{
    private const string FillGap = "___";

    /// <summary>Fields each question type accepts besides the common ones.</summary>
    private static readonly Dictionary<string, string[]> _typeFields = new()
    {
        ["choice"] = ["options", "answer"],
        ["multi"] = ["options", "answer"],
        ["input"] = ["answer", "caseSensitive"],
        ["output"] = ["answer", "caseSensitive"],
        ["recall"] = ["answer"],
        ["fill"] = ["answer", "caseSensitive"],
        ["find-bug"] = ["answer"],
        ["parsons"] = ["lines", "distractors"],
        ["code"] = ["lang", "starter", "tests", "solution"],
        ["sql"] = ["setup", "solution", "ordered"],
    };

    private static readonly string[] _languages = ["csharp"];

    public static Quiz? Read(string yaml, string file, int firstLine, ICollection<ContentDiagnostic> diagnostics)
    {
        var d = Yaml.Read<QuizDocument>(yaml, file, firstLine, diagnostics);
        if (d is null)
            return null;

        var errorsBefore = diagnostics.Count;
        var label = string.IsNullOrWhiteSpace(d.Id) ? "Question" : $"Question '{d.Id}'";
        void Error(string message) => diagnostics.Add(new(file, firstLine, $"{label}: {message}"));

        if (string.IsNullOrWhiteSpace(d.Id) || !Values.QuizId().IsMatch(d.Id))
            Error("'id' is required and must look like 'boxing-copy'.");

        if (d.Type is null || !_typeFields.TryGetValue(d.Type, out var allowed))
        {
            Error($"'type' must be one of: {string.Join(", ", _typeFields.Keys)}.");
            return null;
        }

        Level? level = null;
        if (d.Level is not null)
        {
            if (Values.TryParseLevel(d.Level, out var parsed))
                level = parsed;
            else
                Error("'level' must be junior, middle or senior.");
        }

        if (string.IsNullOrWhiteSpace(d.Prompt))
            Error("'prompt' is required.");

        if (d.Type != "recall" && string.IsNullOrWhiteSpace(d.Explain))
            Error("'explain' is required: the explanation shown after the answer.");

        foreach (var (field, isSet) in SetFields(d))
        {
            if (isSet && !allowed.Contains(field))
                Error($"'{field}' is not used by type '{d.Type}'.");
        }

        var id = d.Id ?? "";
        var prompt = Values.TrimBlock(d.Prompt) ?? "";
        var code = Values.TrimBlock(d.Code);
        var explain = Values.TrimBlock(d.Explain);
        var caseSensitive = d.CaseSensitive ?? false;

        Quiz? quiz = d.Type switch
        {
            "choice" => Choice(),
            "multi" => Multi(),
            "input" => Answers() is { } a ? new InputQuiz(id, level, prompt, code, explain, a, caseSensitive) : null,
            "output" => RequireCode() && Answers() is { } a
                ? new OutputQuiz(id, level, prompt, code, explain, a, caseSensitive)
                : null,
            "recall" => Recall(),
            "fill" => Fill(),
            "find-bug" => FindBug(),
            "parsons" => Parsons(),
            "code" => CodeTask(),
            "sql" => Sql(),
            _ => null,
        };

        return diagnostics.Count == errorsBefore ? quiz : null;

        Quiz? Choice()
        {
            if (!RequireOptions(out var options))
                return null;

            if (!Values.TryParseInt(d.Answer, out var answer) || answer >= options.Count)
            {
                Error($"'answer' must be the index of the correct option, from 0 to {options.Count - 1}.");
                return null;
            }

            return new ChoiceQuiz(id, level, prompt, code, explain, options, answer);
        }

        Quiz? Multi()
        {
            if (!RequireOptions(out var options))
                return null;

            var answers = Values.AsInts(d.Answer);
            if (answers is null || answers.Count == 0 || answers.Any(a => a >= options.Count))
            {
                Error($"'answer' must be a list of option indexes, each from 0 to {options.Count - 1}.");
                return null;
            }

            if (answers.Distinct().Count() != answers.Count)
            {
                Error("'answer' lists an option twice.");
                return null;
            }

            return new MultiQuiz(id, level, prompt, code, explain, options, answers);
        }

        Quiz? Recall()
        {
            if (d.Answer is not string answer || string.IsNullOrWhiteSpace(answer))
            {
                Error("'answer' is required: the reference answer.");
                return null;
            }

            return new RecallQuiz(id, level, prompt, code, explain, Values.TrimBlock(answer)!);
        }

        Quiz? Fill()
        {
            if (!RequireCode())
                return null;

            if (!code!.Contains(FillGap, StringComparison.Ordinal))
            {
                Error($"'code' must contain the gap {FillGap}.");
                return null;
            }

            return Answers() is { } answers
                ? new FillQuiz(id, level, prompt, code, explain, answers, caseSensitive)
                : null;
        }

        Quiz? FindBug()
        {
            if (!RequireCode())
                return null;

            var lineCount = code!.Split('\n').Length;
            if (!Values.TryParseInt(d.Answer, out var line) || line < 1 || line > lineCount)
            {
                Error($"'answer' must be the number of the line with the bug, from 1 to {lineCount}.");
                return null;
            }

            return new FindBugQuiz(id, level, prompt, code, explain, line);
        }

        Quiz? Parsons()
        {
            if (d.Lines is null || d.Lines.Count < 2 || d.Lines.Any(string.IsNullOrWhiteSpace))
            {
                Error("'lines' is required: at least two non-empty lines in the correct order.");
                return null;
            }

            var distractors = d.Distractors ?? [];
            if (distractors.Any(string.IsNullOrWhiteSpace))
            {
                Error("'distractors' must not contain empty lines.");
                return null;
            }

            return new ParsonsQuiz(id, level, prompt, code, explain, d.Lines, distractors);
        }

        Quiz? CodeTask()
        {
            if (d.Lang is null || !_languages.Contains(d.Lang))
                Error($"'lang' is required: {string.Join(", ", _languages)}.");

            if (string.IsNullOrWhiteSpace(d.Starter))
                Error("'starter' is required: the code the editor starts with.");

            if (string.IsNullOrWhiteSpace(d.Tests))
                Error("'tests' is required.");

            if (string.IsNullOrWhiteSpace(d.Solution))
                Error("'solution' is required.");

            if (diagnostics.Count != errorsBefore)
                return null;

            return new CodeQuiz(
                id, level, prompt, code, explain,
                d.Lang!,
                Values.TrimBlock(d.Starter)!,
                Values.TrimBlock(d.Tests)!,
                Values.TrimBlock(d.Solution)!);
        }

        Quiz? Sql()
        {
            if (string.IsNullOrWhiteSpace(d.Setup))
                Error("'setup' is required: SQL that creates and fills the tables.");

            if (string.IsNullOrWhiteSpace(d.Solution))
                Error("'solution' is required: the reference query.");

            if (diagnostics.Count != errorsBefore)
                return null;

            return new SqlQuiz(
                id, level, prompt, code, explain,
                Values.TrimBlock(d.Setup)!,
                Values.TrimBlock(d.Solution)!,
                d.Ordered ?? false);
        }

        bool RequireOptions(out IReadOnlyList<string> options)
        {
            options = d.Options ?? [];
            if (options.Count >= 2 && !options.Any(string.IsNullOrWhiteSpace))
                return true;

            Error("'options' is required: at least two non-empty options.");
            return false;
        }

        bool RequireCode()
        {
            if (!string.IsNullOrWhiteSpace(code))
                return true;

            Error($"'code' is required for type '{d.Type}'.");
            return false;
        }

        IReadOnlyList<string>? Answers()
        {
            var answers = Values.AsStrings(d.Answer);
            if (answers is { Count: > 0 } && !answers.Any(string.IsNullOrWhiteSpace))
                return answers.Select(a => Values.TrimBlock(a)!).ToList();

            Error("'answer' is required: the correct value or a list of accepted values.");
            return null;
        }
    }

    private static IEnumerable<(string Field, bool IsSet)> SetFields(QuizDocument d)
    {
        yield return ("options", d.Options is not null);
        yield return ("answer", d.Answer is not null);
        yield return ("caseSensitive", d.CaseSensitive is not null);
        yield return ("lines", d.Lines is not null);
        yield return ("distractors", d.Distractors is not null);
        yield return ("lang", d.Lang is not null);
        yield return ("starter", d.Starter is not null);
        yield return ("tests", d.Tests is not null);
        yield return ("solution", d.Solution is not null);
        yield return ("setup", d.Setup is not null);
        yield return ("ordered", d.Ordered is not null);
    }

    /// <summary>A question as written in YAML.</summary>
    internal sealed class QuizDocument
    {
        public string? Id { get; set; }

        public string? Type { get; set; }

        public string? Level { get; set; }

        public string? Prompt { get; set; }

        public string? Code { get; set; }

        public string? Explain { get; set; }

        public List<string>? Options { get; set; }

        public object? Answer { get; set; }

        public bool? CaseSensitive { get; set; }

        public List<string>? Lines { get; set; }

        public List<string>? Distractors { get; set; }

        public string? Lang { get; set; }

        public string? Starter { get; set; }

        public string? Tests { get; set; }

        public string? Solution { get; set; }

        public string? Setup { get; set; }

        public bool? Ordered { get; set; }
    }
}
