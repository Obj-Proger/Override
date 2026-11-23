using Override.Content.Model;
using Override.Content.Parsing;

namespace Override.Content.Tests;

public sealed class TopicParserTests
{
    [Fact]
    public void ParsesValidTopic()
    {
        var result = Parse(Samples.ValidTopic);

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
        var text = Samples.ValidTopic[..Samples.ValidTopic.LastIndexOf("::::", StringComparison.Ordinal)];

        AssertError(text, "The step is not closed");
    }

    [Fact]
    public void ReportsLevelBlockAtTopicLevel() =>
        AssertError(Samples.ValidTopic.Replace(":::level middle", ":::level junior", StringComparison.Ordinal), "above the topic level");

    [Fact]
    public void ReportsChoiceAnswerOutOfRange() =>
        AssertError(Samples.ValidTopic.Replace("answer: 1", "answer: 2", StringComparison.Ordinal), "index of the correct option");

    [Fact]
    public void ReportsMisspelledQuizField() =>
        AssertError(Samples.ValidTopic.Replace("explain: b is right.", "explian: b is right.", StringComparison.Ordinal), "Invalid YAML");

    [Fact]
    public void ReportsFieldNotUsedByType() =>
        AssertError(
            Samples.ValidTopic.Replace("prompt: Type it.", "prompt: Type it.\noptions: [x, y]", StringComparison.Ordinal),
            "'options' is not used by type 'input'");

    [Fact]
    public void ReportsDuplicateQuizId() =>
        AssertError(Samples.ValidTopic.Replace("id: q-input", "id: q-choice", StringComparison.Ordinal), "is used twice");

    [Fact]
    public void ReportsQuickCheckToUnknownQuestion() =>
        AssertError(Samples.ValidTopic.Replace("q-output, q-input]", "q-output, q-missing]", StringComparison.Ordinal), "'q-missing'");

    [Fact]
    public void RejectsRecallInQuickCheck() =>
        AssertError(Samples.ValidTopic.Replace("q-output, q-input]", "q-output, q-recall]", StringComparison.Ordinal), "cannot be in the quick check");

    [Fact]
    public void RequiresSummaryToHoldOnlyRecall() =>
        AssertError(Samples.ValidTopic.Replace("::::summary", "::::summary\nExtra text.", StringComparison.Ordinal), "exactly one recall question");

    [Fact]
    public void ReportsUnknownStep() =>
        AssertError(Samples.ValidTopic.Replace("::::practice", "::::exercise", StringComparison.Ordinal), "Unknown step 'exercise'");

    [Fact]
    public void ParsesTopicTestAfterSummary()
    {
        var result = Parse(Samples.ValidTopic + Samples.TestStep);

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics));
        var test = result.Topic!.Steps[^1];
        Assert.Equal(StepKind.Test, test.Kind);
        Assert.Equal(2, test.Blocks.Count);
    }

    [Fact]
    public void ReportsTestBeforeSummary() =>
        AssertError(
            Samples.ValidTopic.Replace("::::summary", Samples.TestStep.Trim() + "\n\n::::summary", StringComparison.Ordinal),
            "after the summary");

    [Fact]
    public void ReportsTextInTest() =>
        AssertError(
            Samples.ValidTopic + Samples.TestStep.Replace("::::test", "::::test\n\nSome words.", StringComparison.Ordinal),
            "contains only questions");

    [Fact]
    public void ReportsTestMinutesWithoutTest() =>
        AssertError(Samples.ValidTopic.Replace("order: 1", "order: 1\ntestMinutes: 5", StringComparison.Ordinal), "no ::::test step");

    [Fact]
    public void ReadsTestQuestionParts()
    {
        var result = Parse(Samples.ValidTopic + Samples.TestStep.Replace("id: t-one", "id: t-one\npart: First part", StringComparison.Ordinal));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics));
        Assert.Equal("First part", Assert.IsType<QuizBlock>(result.Topic!.Steps[^1].Blocks[0]).Part);
        Assert.Equal(2, result.Topic!.TestSize);
    }

    [Fact]
    public void ReportsUnknownTestPart() =>
        AssertError(
            Samples.ValidTopic + Samples.TestStep.Replace("id: t-one", "id: t-one\npart: Missing", StringComparison.Ordinal),
            "no '::::part Missing' step");

    [Fact]
    public void ReportsPartOutsideTest() =>
        AssertError(Samples.ValidTopic.Replace("id: q-input", "id: q-input\npart: First part", StringComparison.Ordinal), "only in the test step");

    [Fact]
    public void ReportsTestSizeAboveThePool() =>
        AssertError(
            Samples.ValidTopic.Replace("order: 1", "order: 1\ntestSize: 5", StringComparison.Ordinal) + Samples.TestStep,
            "'testSize' is 5");

    private static ParseResult Parse(string text) => TopicParser.Parse("sample.md", text);

    private static void AssertError(string text, string expected)
    {
        var result = Parse(text);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Message.Contains(expected, StringComparison.Ordinal));
    }
}
