using Override.Content.Model;

namespace Override.Web.Content;

/// <summary>Levels of pieces of a lesson.</summary>
public static class Levels
{
    /// <summary>
    /// The level a block is meant for: a question's own level or its block's, whichever is higher.
    /// Null means the block uses the level of its topic.
    /// </summary>
    public static Level? Of(PageBlock block) => block switch
    {
        QuestionBlock { Quiz.Level: { } own } question when question.Level is null || own > question.Level => own,
        _ => block.Level,
    };
}
