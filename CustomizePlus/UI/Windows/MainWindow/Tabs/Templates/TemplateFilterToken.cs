namespace CustomizePlus.UI.Windows.MainWindow.Tabs.Templates;

public enum TemplateFilterTokenType
{
    Default,
    Name,
    FullContext,
}

public readonly struct TemplateFilterToken() : IFilterToken<TemplateFilterTokenType, TemplateFilterToken>
{
    public string Needle { get; init; } = string.Empty;
    public TemplateFilterTokenType Type { get; init; }

    public bool Contains(TemplateFilterToken other)
    {
        if (Type != other.Type)
            return false;

        return Needle.Contains(other.Needle);
    }

    public static bool ConvertToken(char tokenCharacter, out TemplateFilterTokenType type)
    {
        type = tokenCharacter switch
        {
            'n' or 'N' => TemplateFilterTokenType.Name,
            'f' or 'F' => TemplateFilterTokenType.FullContext,
            _ => TemplateFilterTokenType.Default,
        };
        return type is not TemplateFilterTokenType.Default;
    }

    public static bool AllowsNone(TemplateFilterTokenType type)
        => false;

    public static bool ProcessList(List<TemplateFilterToken> list, TokenModifier modifier)
        => false;
}
