using CustomizePlus.Configuration.Data;

namespace CustomizePlus.UI.Windows.MainWindow.Tabs.Templates;

public sealed class TemplateFilter : TokenizedFilter<TemplateFilterTokenType, TemplateFileSystemCache.TemplateData, TemplateFilterToken>,
    IFileSystemFilter<TemplateFileSystemCache.TemplateData>, IUiService
{
    public TemplateFilter(PluginConfiguration configuration)
    {
        //todo
        /*if (config.RememberDesignFilter)
            Set(config.Filters.DesignFilter);
        FilterChanged += () => config.Filters.DesignFilter = Text;*/
    }

    protected override void DrawTooltip()
    {
        if (!Im.Item.Hovered())
            return;

        using var tt = Im.Tooltip.Begin();
        var highlightColor = ColorId.EnabledProfile.Value().ToVector();
        Im.Text("Filter templates for those where their full paths or names contain the given strings, split by spaces."u8);
        ImEx.TextMultiColored("Enter "u8).Then("n:[string]"u8, highlightColor).Then(" to filter only for templates names, ignoring the paths."u8)
            .End();
        ImEx.TextMultiColored("Enter "u8).Then("f:[string]"u8, highlightColor).Then(
                " to filter for templates containing the text in name or path."u8)
            .End();
        Im.Line.New();
        ImEx.TextMultiColored("Use "u8).Then("None"u8, highlightColor).Then(" as a placeholder value that only matches empty lists or names."u8)
            .End();
        Im.Text("Regularly, a template has to match all supplied criteria separately."u8);
        ImEx.TextMultiColored("Put a "u8).Then("'-'"u8, highlightColor)
            .Then(" in front of a search token to search only for template not matching the criterion."u8).End();
        ImEx.TextMultiColored("Put a "u8).Then("'?'"u8, highlightColor)
            .Then(" in front of a search token to search for template matching at least one of the '?'-criteria."u8).End();
        ImEx.TextMultiColored("Wrap spaces in "u8).Then("\"[string with space]\""u8, highlightColor)
            .Then(" to match this exact combination of words."u8).End();
    }

    protected override bool Matches(in TemplateFilterToken token, in TemplateFileSystemCache.TemplateData cacheItem)
        => token.Type switch
        {
            TemplateFilterTokenType.Default => cacheItem.Node.FullPath.Contains(token.Needle, StringComparison.OrdinalIgnoreCase)
             || cacheItem.Node.Value.Name.Contains(token.Needle, StringComparison.OrdinalIgnoreCase),
            TemplateFilterTokenType.Name => cacheItem.Node.Value.Name.Contains(token.Needle, StringComparison.OrdinalIgnoreCase),
            TemplateFilterTokenType.FullContext => CheckFullContext(token.Needle, cacheItem),
            _ => true,
        };

    protected override bool MatchesNone(TemplateFilterTokenType type, bool negated, in TemplateFileSystemCache.TemplateData cacheItem)
        => true;

    private static bool CheckFullContext(string needle, in TemplateFileSystemCache.TemplateData cacheItem)
    {
        if (needle.Length is 0)
            return true;

        if (cacheItem.Node.FullPath.Contains(needle, StringComparison.OrdinalIgnoreCase))
            return true;

        var template = cacheItem.Node.Value;
        if (template.Name.Contains(needle, StringComparison.OrdinalIgnoreCase))
            return true;

        if (template.UniqueId.ToString().Contains(needle, StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    public bool WouldBeVisible(in FileSystemFolderCache folder)
    {
        switch (State)
        {
            case FilterState.NoFilters: return true;
            case FilterState.NoMatches: return false;
        }

        foreach (var token in Forced)
        {
            if (token.Type switch
            {
                TemplateFilterTokenType.Name => !folder.Name.Contains(token.Needle, StringComparison.OrdinalIgnoreCase),
                TemplateFilterTokenType.Default => !folder.FullPath.Contains(token.Needle, StringComparison.OrdinalIgnoreCase),
                TemplateFilterTokenType.FullContext => !folder.FullPath.Contains(token.Needle, StringComparison.OrdinalIgnoreCase),
                _ => true,
            })
                return false;
        }

        foreach (var token in Negated)
        {
            if (token.Type switch
            {
                TemplateFilterTokenType.Name => folder.Name.Contains(token.Needle, StringComparison.OrdinalIgnoreCase),
                TemplateFilterTokenType.Default => folder.FullPath.Contains(token.Needle, StringComparison.OrdinalIgnoreCase),
                TemplateFilterTokenType.FullContext => folder.FullPath.Contains(token.Needle, StringComparison.OrdinalIgnoreCase),
                _ => false,
            })
                return false;
        }

        foreach (var token in General)
        {
            if (token.Type switch
            {
                TemplateFilterTokenType.Name => folder.Name.Contains(token.Needle, StringComparison.OrdinalIgnoreCase),
                TemplateFilterTokenType.Default => folder.FullPath.Contains(token.Needle, StringComparison.OrdinalIgnoreCase),
                TemplateFilterTokenType.FullContext => !folder.FullPath.Contains(token.Needle, StringComparison.OrdinalIgnoreCase),
                _ => false,
            })
                return true;
        }

        return General.Count is 0;
    }
}