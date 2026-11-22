using Override.Content.Highlighting;
using Override.Content.Model;
using Override.Content.Parsing;
using Override.Content.Site;

namespace Override.Content.Rendering;

/// <summary>Turns a parsed topic into the page the site shows: Markdown becomes HTML, figures are inlined, code is highlighted.</summary>
internal static class TopicRenderer
{
    public static TopicPage Render(SourceTopic source, ICollection<ContentDiagnostic> diagnostics)
    {
        var metadata = source.Topic.Metadata;
        var steps = source.Topic.Steps
            .Where(step => step.Kind != StepKind.Test)
            .Select(step => new PageStep(step.Kind, step.Title, step.Blocks.Select(RenderBlock).ToList()))
            .ToList();

        var test = source.Topic.Steps
            .Where(step => step.Kind == StepKind.Test)
            .SelectMany(step => step.Blocks)
            .Select(RenderBlock)
            .OfType<QuestionBlock>()
            .ToList();

        return new TopicPage(
            metadata.Id,
            source.Path,
            metadata.Title,
            metadata.Level,
            metadata.Tracks,
            metadata.Minutes,
            metadata.Requires,
            metadata.QuickCheck,
            steps,
            test,
            metadata.TestMinutes);

        PageBlock RenderBlock(Block block) => block switch
        {
            MarkdownBlock markdown => new HtmlBlock(
                markdown.Level,
                MarkdownRenderer.Block(FigureInliner.Inline(markdown.Markdown, source.FilePath, markdown.Line, diagnostics))),
            QuizBlock quiz => new QuestionBlock(
                quiz.Level, RenderQuiz(quiz.Quiz), RenderCode(quiz.Quiz), RenderLines(quiz.Quiz), RenderSolution(quiz.Quiz)),
            _ => throw new InvalidOperationException($"Unknown block type {block.GetType().Name}."),
        };
    }

    /// <summary>Code shown with a question: SQL for SQL tasks, the task language for code tasks, otherwise C#.</summary>
    private static string? RenderCode(Quiz quiz)
    {
        if (quiz.Code is null)
            return null;

        var language = quiz switch
        {
            SqlQuiz => CodeHighlighter.Sql,
            CodeQuiz code => code.Language,
            _ => CodeHighlighter.CSharp,
        };

        return CodeHighlighter.Block(quiz.Code, language);
    }

    private static string? RenderSolution(Quiz quiz) => quiz switch
    {
        CodeQuiz code => CodeHighlighter.Block(code.Solution, code.Language),
        SqlQuiz query => CodeHighlighter.Block(query.Solution, CodeHighlighter.Sql),
        _ => null,
    };

    /// <summary>Highlighted single lines for questions that work with lines; null for the others.</summary>
    private static List<string>? RenderLines(Quiz quiz) => quiz switch
    {
        FindBugQuiz { Code: { } code } => CodeHighlighter
            .Highlight(code.TrimEnd('\n'), CodeHighlighter.CSharp)
            .Split('\n')
            .ToList(),
        ParsonsQuiz parsons => parsons.Lines
            .Concat(parsons.Distractors)
            .Select(line => CodeHighlighter.Highlight(line, CodeHighlighter.CSharp))
            .ToList(),
        _ => null,
    };

    /// <summary>Texts meant for reading become HTML; answers the user types and code stay as written.</summary>
    private static Quiz RenderQuiz(Quiz quiz)
    {
        var rendered = quiz with
        {
            Prompt = MarkdownRenderer.Inline(quiz.Prompt),
            Explain = quiz.Explain is null ? null : MarkdownRenderer.Block(quiz.Explain),
        };

        return rendered switch
        {
            ChoiceQuiz choice => choice with { Options = choice.Options.Select(MarkdownRenderer.Inline).ToList() },
            MultiQuiz multi => multi with { Options = multi.Options.Select(MarkdownRenderer.Inline).ToList() },
            RecallQuiz recall => recall with { Answer = MarkdownRenderer.Block(recall.Answer) },
            _ => rendered,
        };
    }
}
