using Override.Content.Highlighting;
using Override.Content.Rendering;

namespace Override.Content.Tests;

public sealed class CodeHighlighterTests
{
    [Fact]
    public void ColorsCSharpBySemantics()
    {
        var html = CodeHighlighter.Highlight(
            """
            int count = 1;
            var items = new List<int>();
            if (count > 0)
                Console.WriteLine(count); // done
            """,
            "csharp");

        Assert.Contains("<span class=\"c-kw\">int</span> <span class=\"c-local\">count</span>", html, StringComparison.Ordinal);
        Assert.Contains("<span class=\"c-num\">1</span>", html, StringComparison.Ordinal);
        Assert.Contains("<span class=\"c-type\">List</span>", html, StringComparison.Ordinal);
        Assert.Contains("<span class=\"c-kw-ctrl\">if</span>", html, StringComparison.Ordinal);
        Assert.Contains("<span class=\"c-type\">Console</span>.<span class=\"c-method\">WriteLine</span>", html, StringComparison.Ordinal);
        Assert.Contains("<span class=\"c-cmt\">// done</span>", html, StringComparison.Ordinal);
    }

    [Fact]
    public void ColorsStructsDifferentlyFromClasses()
    {
        var html = CodeHighlighter.Highlight("Point p = default;\nobject o = p;\n\nstruct Point { }", "cs");

        Assert.Contains("<span class=\"c-type-struct\">Point</span> <span class=\"c-local\">p</span>", html, StringComparison.Ordinal);
    }

    [Fact]
    public void EscapesHtmlInsideCode()
    {
        var html = CodeHighlighter.Highlight("var s = \"<b>&\";", "csharp");

        Assert.Contains("<span class=\"c-str\">\"&lt;b&gt;&amp;\"</span>", html, StringComparison.Ordinal);
    }

    [Fact]
    public void ColorsSql()
    {
        var html = CodeHighlighter.Highlight("SELECT name FROM users WHERE id = 1 AND note = 'it''s' -- first", "sql");

        Assert.Equal(
            "<span class=\"c-kw\">SELECT</span> name <span class=\"c-kw\">FROM</span> users "
            + "<span class=\"c-kw\">WHERE</span> id = <span class=\"c-num\">1</span> <span class=\"c-kw\">AND</span> "
            + "note = <span class=\"c-str\">'it''s'</span> <span class=\"c-cmt\">-- first</span>",
            html);
    }

    [Fact]
    public void SpansDoNotCrossLines()
    {
        var html = CodeHighlighter.Highlight("/* one\ntwo */", "csharp");

        Assert.Equal("<span class=\"c-cmt\">/* one</span>\n<span class=\"c-cmt\">two */</span>", html);
    }

    [Fact]
    public void OtherLanguagesArePlainText()
    {
        Assert.Equal("dotnet build &amp;&amp; echo &lt;ok&gt;", CodeHighlighter.Highlight("dotnet build && echo <ok>", "shell"));
    }

    [Fact]
    public void MarkdownCodeBlocksAreHighlighted()
    {
        var html = MarkdownRenderer.Block("Text.\n\n```csharp\nint x;\n```");

        Assert.Equal(
            "<p>Text.</p>\n<pre class=\"code\"><code class=\"language-csharp\"><span class=\"c-kw\">int</span> <span class=\"c-local\">x</span>;</code></pre>",
            html);
    }
}
