using System.Text;
using Override.Content.Model;

namespace Override.Content.Parsing;

/// <summary>
/// Parses a topic file: YAML front matter between --- lines, then lesson steps.
/// A step is a :::: container, a level block is a ::: container inside a step,
/// and a question is a fenced code block with the language quiz.
/// The format is described in content/FORMAT.md.
/// </summary>
public static class TopicParser
{
    private const string FrontMatterFence = "---";
    private const string StepFence = "::::";
    private const string LevelFence = ":::";
    private const string LevelKeyword = "level";
    private const string QuizLanguage = "quiz";
    private const int MaxQuickCheck = 4;
    private const int MinQuickCheck = 3;
    private const int MinTestQuestions = 2;

    private static readonly Dictionary<string, StepKind> _stepKinds = new()
    {
        ["warmup"] = StepKind.Warmup,
        ["part"] = StepKind.Part,
        ["example"] = StepKind.Example,
        ["practice"] = StepKind.Practice,
        ["traps"] = StepKind.Traps,
        ["summary"] = StepKind.Summary,
        ["test"] = StepKind.Test,
    };

    /// <summary>Questions in the quick check must be graded automatically and take seconds.</summary>
    private static readonly Type[] _quickCheckTypes =
    [
        typeof(ChoiceQuiz), typeof(MultiQuiz), typeof(InputQuiz), typeof(OutputQuiz),
        typeof(FillQuiz), typeof(FindBugQuiz), typeof(ParsonsQuiz),
    ];

    public static ParseResult Parse(string file, string text)
    {
        var diagnostics = new List<ContentDiagnostic>();
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

        if (lines.Length == 0 || lines[0] != FrontMatterFence)
        {
            diagnostics.Add(new(file, 1, "The file must start with front matter between --- lines."));
            return new ParseResult(null, diagnostics);
        }

        var frontMatterEnd = Array.IndexOf(lines, FrontMatterFence, 1);
        if (frontMatterEnd < 0)
        {
            diagnostics.Add(new(file, 1, "The front matter is not closed with a --- line."));
            return new ParseResult(null, diagnostics);
        }

        var metadata = FrontMatterReader.Read(
            string.Join('\n', lines[1..frontMatterEnd]), file, firstLine: 2, diagnostics);

        var steps = new BodyReader(file, diagnostics).Read(lines, frontMatterEnd + 1);

        if (metadata is not null)
            Validate(file, metadata, steps, diagnostics);

        var topic = diagnostics.Count == 0 && metadata is not null ? new Topic(metadata, steps) : null;
        return new ParseResult(topic, diagnostics);
    }

    private static void Validate(
        string file, TopicMetadata metadata, IReadOnlyList<Step> steps, List<ContentDiagnostic> diagnostics)
    {
        void Error(int line, string message) => diagnostics.Add(new(file, line, message));

        if (!steps.Any(s => s.Kind == StepKind.Part))
            Error(1, "A topic needs at least one ::::part step.");

        var lesson = steps.Where(s => s.Kind != StepKind.Test).ToList();
        var summaries = lesson.Where(s => s.Kind == StepKind.Summary).ToList();
        if (summaries.Count != 1 || lesson.Count == 0 || lesson[^1].Kind != StepKind.Summary)
            Error(1, "A topic needs exactly one ::::summary step, and it must be the last lesson step.");

        var tests = steps.Where(s => s.Kind == StepKind.Test).ToList();
        if (tests.Count > 1 || (tests.Count == 1 && steps[^1].Kind != StepKind.Test))
            Error(tests.Count > 0 ? tests[^1].Line : 1, "A topic can have one ::::test step, after the summary.");

        foreach (var test in tests)
        {
            if (test.Blocks.Count < MinTestQuestions)
                Error(test.Line, $"The test step needs at least {MinTestQuestions} questions.");

            foreach (var block in test.Blocks)
            {
                if (block is not QuizBlock { Level: null } testQuiz)
                    Error(block.Line, "The test step contains only questions, without text or level blocks.");
                else if (!_quickCheckTypes.Contains(testQuiz.Quiz.GetType()))
                    Error(block.Line, $"Question '{testQuiz.Quiz.Id}' cannot be in the test: it is not graded automatically.");
            }
        }

        if (metadata.TestMinutes is not null && tests.Count == 0)
            Error(2, "'testMinutes' is set, but the topic has no ::::test step.");

        var testIds = tests.SelectMany(t => t.Blocks).OfType<QuizBlock>().Select(b => b.Quiz.Id).ToHashSet();

        foreach (var summary in summaries)
        {
            if (summary.Blocks is not [QuizBlock { Quiz: RecallQuiz, Level: null }])
                Error(summary.Line, "The summary step must contain exactly one recall question and nothing else.");
        }

        for (var i = 1; i < steps.Count; i++)
        {
            if (steps[i].Kind == StepKind.Warmup)
                Error(steps[i].Line, "The warmup step must be the first one.");
        }

        var quizzes = new Dictionary<string, QuizBlock>();
        foreach (var block in steps.SelectMany(s => s.Blocks))
        {
            if (block.Level is { } blockLevel && blockLevel <= metadata.Level)
                Error(block.Line, $"A ':::level {Name(blockLevel)}' block must be above the topic level ({Name(metadata.Level)}).");

            if (block is not QuizBlock quizBlock)
                continue;

            var quiz = quizBlock.Quiz;
            if (!quizzes.TryAdd(quiz.Id, quizBlock))
                Error(block.Line, $"Question id '{quiz.Id}' is used twice in this topic.");

            if (quiz.Level is { } quizLevel)
            {
                if (quizLevel < metadata.Level)
                    Error(block.Line, $"Question '{quiz.Id}' is below the topic level.");
                else if (block.Level is { } container && quizLevel < container)
                    Error(block.Line, $"Question '{quiz.Id}' is below the level of its block.");
            }
        }

        var quickCheck = metadata.QuickCheck;
        if (quickCheck.Count is > 0 and (< MinQuickCheck or > MaxQuickCheck))
            Error(2, $"'quickCheck' must list from {MinQuickCheck} to {MaxQuickCheck} questions.");

        foreach (var id in quickCheck)
        {
            if (!quizzes.TryGetValue(id, out var block))
            {
                Error(2, $"'quickCheck' refers to '{id}', which is not a question in this topic.");
                continue;
            }

            if (testIds.Contains(id))
            {
                Error(block.Line, $"Question '{id}' is in the test, so it cannot be in the quick check.");
                continue;
            }

            if (!_quickCheckTypes.Contains(block.Quiz.GetType()))
                Error(block.Line, $"Question '{id}' cannot be in the quick check: it is not graded automatically in seconds.");
            else if (block.Level is not null || block.Quiz.Level > metadata.Level)
                Error(block.Line, $"Question '{id}' cannot be in the quick check: it is above the topic level.");
        }
    }

    private static string Name(Level level) => level.ToString().ToLowerInvariant();

    /// <summary>Reads the body line by line, keeping track of fences, steps and level blocks.</summary>
    private sealed class BodyReader(string file, List<ContentDiagnostic> diagnostics)
    {
        private readonly List<Step> _steps = [];
        private readonly StringBuilder _markdown = new();

        private StepKind? _stepKind;
        private string? _stepTitle;
        private int _stepLine;
        private List<Block> _blocks = [];

        private Level? _level;
        private int _levelLine;
        private int _markdownLine;

        public IReadOnlyList<Step> Read(string[] lines, int start)
        {
            string? fence = null;
            var fenceLine = 0;
            var isQuiz = false;
            var quizLines = new List<string>();

            for (var i = start; i < lines.Length; i++)
            {
                var line = lines[i];
                var lineNumber = i + 1;

                if (fence is not null)
                {
                    if (IsClosingFence(line, fence))
                    {
                        if (isQuiz)
                        {
                            var quiz = QuizReader.Read(string.Join('\n', quizLines), file, fenceLine + 1, diagnostics);
                            if (quiz is not null)
                                _blocks.Add(new QuizBlock(quiz, _level, fenceLine));
                        }
                        else
                        {
                            AppendMarkdown(line, lineNumber);
                        }

                        fence = null;
                    }
                    else if (isQuiz)
                    {
                        quizLines.Add(line);
                    }
                    else
                    {
                        AppendMarkdown(line, lineNumber);
                    }

                    continue;
                }

                if (TryOpenFence(line, out var marker, out var info))
                {
                    fence = marker;
                    fenceLine = lineNumber;
                    isQuiz = info == QuizLanguage;

                    if (_stepKind is null)
                        Error(lineNumber, "Content must be inside a :::: step.");

                    if (isQuiz)
                    {
                        FlushMarkdown();
                        quizLines.Clear();
                    }
                    else
                    {
                        AppendMarkdown(line, lineNumber);
                    }

                    continue;
                }

                if (line.StartsWith(StepFence, StringComparison.Ordinal))
                {
                    HandleStepFence(line[StepFence.Length..].Trim(), lineNumber);
                    continue;
                }

                if (line.StartsWith(LevelFence, StringComparison.Ordinal))
                {
                    HandleLevelFence(line[LevelFence.Length..].Trim(), lineNumber);
                    continue;
                }

                if (_stepKind is null)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                        Error(lineNumber, "Content must be inside a :::: step.");

                    continue;
                }

                AppendMarkdown(line, lineNumber);
            }

            if (fence is not null)
                Error(fenceLine, "The code block is not closed.");

            if (_level is not null)
                Error(_levelLine, "The level block is not closed with :::.");

            if (_stepKind is not null)
            {
                Error(_stepLine, "The step is not closed with ::::.");
                CloseStep();
            }

            return _steps;
        }

        private void HandleStepFence(string rest, int lineNumber)
        {
            if (rest.Length == 0)
            {
                if (_stepKind is null)
                {
                    Error(lineNumber, "A :::: line closes a step, but no step is open.");
                    return;
                }

                if (_level is not null)
                {
                    Error(_levelLine, "The level block is not closed with ::: before the end of the step.");
                    _level = null;
                }

                CloseStep();
                return;
            }

            if (_stepKind is not null)
            {
                Error(lineNumber, "Steps cannot be nested: close the previous step with :::: first.");
                return;
            }

            var space = rest.IndexOf(' ', StringComparison.Ordinal);
            var name = space < 0 ? rest : rest[..space];
            var title = space < 0 ? null : rest[(space + 1)..].Trim();

            if (!_stepKinds.TryGetValue(name, out var kind))
            {
                Error(lineNumber, $"Unknown step '{name}'. Use one of: {string.Join(", ", _stepKinds.Keys)}.");
                return;
            }

            var needsTitle = kind is StepKind.Part or StepKind.Example;
            if (needsTitle && string.IsNullOrEmpty(title))
                Error(lineNumber, $"The ::::{name} step needs a title.");
            else if (!needsTitle && title is not null)
                Error(lineNumber, $"The ::::{name} step does not take a title.");

            _stepKind = kind;
            _stepTitle = needsTitle ? title : null;
            _stepLine = lineNumber;
            _blocks = [];
        }

        private void HandleLevelFence(string rest, int lineNumber)
        {
            if (rest.Length == 0)
            {
                if (_level is null)
                {
                    Error(lineNumber, "A ::: line closes a level block, but no level block is open.");
                    return;
                }

                FlushMarkdown();
                _level = null;
                return;
            }

            var parts = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts is not [LevelKeyword, var value])
            {
                Error(lineNumber, "Unknown block. A level block starts with ':::level middle' or ':::level senior'.");
                return;
            }

            if (_stepKind is null)
            {
                Error(lineNumber, "A level block must be inside a :::: step.");
                return;
            }

            if (_level is not null)
            {
                Error(lineNumber, "Level blocks cannot be nested.");
                return;
            }

            if (!Values.TryParseLevel(value, out var level))
            {
                Error(lineNumber, $"Unknown level '{value}'. Use middle or senior.");
                return;
            }

            FlushMarkdown();
            _level = level;
            _levelLine = lineNumber;
        }

        private void AppendMarkdown(string line, int lineNumber)
        {
            if (_stepKind is null)
                return;

            if (_markdown.Length == 0)
                _markdownLine = lineNumber;

            _markdown.Append(line).Append('\n');
        }

        private void FlushMarkdown()
        {
            // Line points at the first line of the text, not at the empty lines before it.
            var raw = _markdown.ToString();
            var leadingLines = raw.Length - raw.TrimStart('\n').Length;
            var text = raw.Trim('\n');
            if (!string.IsNullOrWhiteSpace(text))
                _blocks.Add(new MarkdownBlock(text, _level, _markdownLine + leadingLines));

            _markdown.Clear();
        }

        private void CloseStep()
        {
            FlushMarkdown();
            _steps.Add(new Step(_stepKind!.Value, _stepTitle, _stepLine, _blocks));
            _stepKind = null;
            _stepTitle = null;
        }

        private void Error(int line, string message) => diagnostics.Add(new(file, line, message));

        /// <summary>A fence opens with three or more backticks or tildes; the rest of the line is the info string.</summary>
        private static bool TryOpenFence(string line, out string marker, out string info)
        {
            var trimmed = line.TrimStart(' ');
            var indent = line.Length - trimmed.Length;
            marker = "";
            info = "";

            if (indent > 3 || trimmed.Length < 3 || (trimmed[0] != '`' && trimmed[0] != '~'))
                return false;

            var length = 0;
            while (length < trimmed.Length && trimmed[length] == trimmed[0])
                length++;

            if (length < 3)
                return false;

            marker = trimmed[..length];
            info = trimmed[length..].Trim();
            return true;
        }

        /// <summary>A fence closes with at least as many of the same characters and nothing else.</summary>
        private static bool IsClosingFence(string line, string marker)
        {
            var trimmed = line.Trim();
            return trimmed.Length >= marker.Length && trimmed.All(c => c == marker[0]);
        }
    }
}
