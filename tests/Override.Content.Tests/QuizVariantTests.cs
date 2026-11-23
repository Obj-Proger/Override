using Override.Content.Model;
using Override.Content.Parsing;

namespace Override.Content.Tests;

public sealed class QuizVariantTests
{
    [Fact]
    public void VariantTakesMissingFieldsFromTheQuestion()
    {
        const string yaml = """
            id: sum
            type: output
            prompt: What does it print?
            code: Console.WriteLine(1 + 1);
            answer: "2"
            explain: One plus one.
            variants:
              - code: Console.WriteLine(2 + 2);
                answer: "4"
            """;

        var diagnostics = new List<ContentDiagnostic>();
        var read = QuizReader.ReadWithVariants(yaml, "topic.md", 1, diagnostics);

        Assert.Empty(diagnostics);
        Assert.NotNull(read);
        var variant = Assert.IsType<OutputQuiz>(Assert.Single(read.Variants));
        Assert.Equal("sum", variant.Id);
        Assert.Equal("What does it print?", variant.Prompt);
        Assert.Equal("Console.WriteLine(2 + 2);", variant.Code);
        Assert.Equal(new[] { "4" }, variant.Answers);
        Assert.Equal("One plus one.", variant.Explain);
    }

    [Fact]
    public void VariantIsValidatedLikeAQuestion()
    {
        const string yaml = """
            id: pick
            type: choice
            prompt: Pick one.
            options: [a, b]
            answer: 0
            explain: Because.
            variants:
              - answer: 5
            """;

        var diagnostics = new List<ContentDiagnostic>();
        var read = QuizReader.ReadWithVariants(yaml, "topic.md", 1, diagnostics);

        Assert.Null(read);
        Assert.Contains(diagnostics, d => d.Message.Contains("variant 1", StringComparison.Ordinal));
    }

    [Fact]
    public void VariantCannotChangeTheType()
    {
        const string yaml = """
            id: pick
            type: choice
            prompt: Pick one.
            options: [a, b]
            answer: 0
            explain: Because.
            variants:
              - type: multi
                answer: [0]
            """;

        var diagnostics = new List<ContentDiagnostic>();

        Assert.Null(QuizReader.ReadWithVariants(yaml, "topic.md", 1, diagnostics));
        Assert.NotEmpty(diagnostics);
    }

    [Fact]
    public void RecallHasNoVariants()
    {
        const string yaml = """
            id: tell
            type: recall
            prompt: Tell it.
            answer: The answer.
            variants:
              - prompt: Tell it again.
            """;

        var diagnostics = new List<ContentDiagnostic>();

        Assert.Null(QuizReader.ReadWithVariants(yaml, "topic.md", 1, diagnostics));
        Assert.NotEmpty(diagnostics);
    }
}
