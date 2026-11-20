using Override.Content.Model;
using Override.Content.Parsing;

namespace Override.Content.Tests;

public sealed class TopicParserTests
{
    private const string ValidTopic = """
        ---
        id: csharp.types.sample
        title: Sample
        level: junior
        tracks: [backend]
        minutes: 10
        order: 1
        quickCheck: [q-choice, q-output, q-input]
        ---

        ::::warmup

        ```quiz
        id: q-output
        type: output
        prompt: What does it print?
        code: |
          Console.WriteLine(1);
        answer: "1"
        explain: It prints 1.
        ```

        ::::

        ::::part First part

        Some text.

        ```csharp
        :::: not a step, this is code
        ```

        ```quiz
        id: q-choice
        type: choice
        prompt: Pick one.
        options: [a, b]
        answer: 1
        explain: b is right.
        ```

        :::level middle
        Deeper text.
        :::

        ::::

        ::::practice

        ```quiz
        id: q-input
        type: input
        prompt: Type it.
        answer: [Value, Other]
        explain: Value.
        ```

        ::::

        ::::summary

        ```quiz
        id: q-recall
        type: recall
        prompt: Explain.
        answer: |
          Reference answer.
        ```

        ::::
        """;

    [Fact]
    public void ParsesValidTopic()
    {
        var result = Parse(ValidTopic);

        Assert.True(result.Succeeded, string.Join(Environment.NewLine, result.Diagnostics));
        var topic = result.Topic!;
        Assert.Equal("csharp.types.sample", topic.Metadata.Id);
        Assert.Equal(Level.Junior, topic.Metadata.Level);
        Assert.Equal(new[] { Track.Backend }, topic.Metadata.Tracks);
        Assert.Equal(
            new[] { StepKind.Warmup, StepKind.Part, StepKind.Practice, StepKind.Summary },
            topic.Steps.Select(s => s.Kind));

        var part = topic.Steps[1];
        Assert.Equal("First part", part.Title);
        Assert.Collection(
            part.Blocks,
            block =>
            {
                var markdown = Assert.IsType<MarkdownBlock>(block);
                Assert.Contains(":::: not a step, this is code", markdown.Markdown, StringComparison.Ordinal);
                Assert.Null(markdown.Level);
            },
            block =>
            {
                var quiz = Assert.IsType<ChoiceQuiz>(Assert.IsType<QuizBlock>(block).Quiz);
                Assert.Equal(1, quiz.Answer);
                Assert.Equal(new[] { "a", "b" }, quiz.Options);
            },
            block =>
            {
                var markdown = Assert.IsType<MarkdownBlock>(block);
                Assert.Equal("Deeper text.", markdown.Markdown);
                Assert.Equal(Level.Middle, markdown.Level);
            });

        var input = Assert.IsType<InputQuiz>(Assert.IsType<QuizBlock>(topic.Steps[2].Blocks[0]).Quiz);
        Assert.Equal(new[] { "Value", "Other" }, input.Answers);

        var recall = Assert.IsType<RecallQuiz>(Assert.IsType<QuizBlock>(topic.Steps[3].Blocks[0]).Quiz);
        Assert.Equal("Reference answer.", recall.Answer);
    }

    [Fact]
    public void ReportsMissingFrontMatter()
    {
        var result = Parse("::::part Title\nText\n::::\n");

        Assert.False(result.Succeeded);
        Assert.Equal(1, Assert.Single(result.Diagnostics).Line);
    }

    [Fact]
    public void ReportsUnclosedStep()
    {
        var text = ValidTopic[..ValidTopic.LastIndexOf("::::", StringComparison.Ordinal)];

        AssertError(text, "The step is not closed");
    }

    [Fact]
    public void ReportsLevelBlockAtTopicLevel() =>
        AssertError(ValidTopic.Replace(":::level middle", ":::level junior", StringComparison.Ordinal), "above the topic level");

    [Fact]
    public void ReportsChoiceAnswerOutOfRange() =>
        AssertError(ValidTopic.Replace("answer: 1", "answer: 2", StringComparison.Ordinal), "index of the correct option");

    [Fact]
    public void ReportsMisspelledQuizField() =>
        AssertError(ValidTopic.Replace("explain: b is right.", "explian: b is right.", StringComparison.Ordinal), "Invalid YAML");

    [Fact]
    public void ReportsFieldNotUsedByType() =>
        AssertError(
            ValidTopic.Replace("prompt: Type it.", "prompt: Type it.\noptions: [x, y]", StringComparison.Ordinal),
            "'options' is not used by type 'input'");

    [Fact]
    public void ReportsDuplicateQuizId() =>
        AssertError(ValidTopic.Replace("id: q-input", "id: q-choice", StringComparison.Ordinal), "is used twice");

    [Fact]
    public void ReportsQuickCheckToUnknownQuestion() =>
        AssertError(ValidTopic.Replace("q-output, q-input]", "q-output, q-missing]", StringComparison.Ordinal), "'q-missing'");

    [Fact]
    public void RejectsRecallInQuickCheck() =>
        AssertError(ValidTopic.Replace("q-output, q-input]", "q-output, q-recall]", StringComparison.Ordinal), "cannot be in the quick check");

    [Fact]
    public void RequiresSummaryToHoldOnlyRecall() =>
        AssertError(ValidTopic.Replace("::::summary", "::::summary\nExtra text.", StringComparison.Ordinal), "exactly one recall question");

    [Fact]
    public void ReportsUnknownStep() =>
        AssertError(ValidTopic.Replace("::::practice", "::::exercise", StringComparison.Ordinal), "Unknown step 'exercise'");

    private static ParseResult Parse(string text) => TopicParser.Parse("sample.md", text);

    private static void AssertError(string text, string expected)
    {
        var result = Parse(text);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Message.Contains(expected, StringComparison.Ordinal));
    }
}
