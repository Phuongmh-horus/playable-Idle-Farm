using System.Text.RegularExpressions;

internal enum HtmlPatchKind
{
    ReplaceRegex,
    InjectBefore,
    InjectAfter
}

internal sealed class HtmlPatchOperation
{
    public string name;
    public bool required;
    public HtmlPatchKind kind;
    public Regex regex;
    public string marker;
    public string replacement;
    public string patchDescription;
}
