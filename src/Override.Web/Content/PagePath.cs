using Microsoft.AspNetCore.Components;

namespace Override.Web.Content;

/// <summary>The address of the current page relative to the site root, without query and fragment.</summary>
public static class PagePath
{
    public static string Of(NavigationManager navigation)
    {
        var relative = navigation.ToBaseRelativePath(navigation.Uri);
        var end = relative.IndexOfAny(['?', '#']);
        return (end < 0 ? relative : relative[..end]).Trim('/');
    }
}
