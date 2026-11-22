namespace Override.Content.Tests;

/// <summary>Content shared by the tests.</summary>
internal static class Samples
{
    /// <summary>An abstract topic that uses every part of the format once and passes validation.</summary>
    public const string ValidTopic = """
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

    /// <summary>A test step to append to the valid topic.</summary>
    public const string TestStep = """


        ::::test

        ```quiz
        id: t-one
        type: choice
        prompt: Which?
        options: [a, b]
        answer: 0
        explain: a.
        ```

        ```quiz
        id: t-two
        type: input
        prompt: Type.
        answer: x
        explain: x.
        ```

        ::::
        """;

    /// <summary>The valid topic with another id, order and required topics.</summary>
    public static string Topic(string id, int order = 1, params string[] requires)
    {
        var text = ValidTopic
            .Replace("id: csharp.types.sample", $"id: {id}", StringComparison.Ordinal)
            .Replace("order: 1", $"order: {order}", StringComparison.Ordinal);

        return requires.Length == 0
            ? text
            : text.Replace("quickCheck:", $"requires: [{string.Join(", ", requires)}]\nquickCheck:", StringComparison.Ordinal);
    }
}
