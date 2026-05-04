using CustomizePlus.Configuration.Data;

namespace CustomizePlus.UI.Windows.MainWindow.Tabs.Profiles;

public sealed class ProfileFilter : TokenizedFilter<ProfileFilterTokenType, ProfileFileSystemCache.ProfileData, ProfileFilterToken>,
    IFileSystemFilter<ProfileFileSystemCache.ProfileData>, IUiService
{
    public ProfileFilter(PluginConfiguration configuration)
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
        Im.Text("Filter profiles for those where their full paths or names contain the given strings, split by spaces."u8);
        ImEx.TextMultiColored("Enter "u8).Then("n:[string]"u8, highlightColor).Then(" to filter only for profiles names, ignoring the paths."u8)
            .End();
        ImEx.TextMultiColored("Enter "u8).Then("f:[string]"u8, highlightColor).Then(
                " to filter for profiles containing the text in name or path."u8)
            .End();
        Im.Line.New();
        ImEx.TextMultiColored("Use "u8).Then("None"u8, highlightColor).Then(" as a placeholder value that only matches empty lists or names."u8)
            .End();
        Im.Text("Regularly, a profiles has to match all supplied criteria separately."u8);
        ImEx.TextMultiColored("Put a "u8).Then("'-'"u8, highlightColor)
            .Then(" in front of a search token to search only for profiles not matching the criterion."u8).End();
        ImEx.TextMultiColored("Put a "u8).Then("'?'"u8, highlightColor)
            .Then(" in front of a search token to search for profiles matching at least one of the '?'-criteria."u8).End();
        ImEx.TextMultiColored("Wrap spaces in "u8).Then("\"[string with space]\""u8, highlightColor)
            .Then(" to match this exact combination of words."u8).End();
    }

    protected override bool Matches(in ProfileFilterToken token, in ProfileFileSystemCache.ProfileData cacheItem)
        => token.Type switch
        {
            ProfileFilterTokenType.Default => cacheItem.Node.FullPath.Contains(token.Needle, StringComparison.OrdinalIgnoreCase)
             || cacheItem.Node.Value.Name.Contains(token.Needle, StringComparison.OrdinalIgnoreCase),
            ProfileFilterTokenType.Name => cacheItem.Node.Value.Name.Contains(token.Needle, StringComparison.OrdinalIgnoreCase),
            ProfileFilterTokenType.FullContext => CheckFullContext(token.Needle, cacheItem),
            _ => true,
        };

    protected override bool MatchesNone(ProfileFilterTokenType type, bool negated, in ProfileFileSystemCache.ProfileData cacheItem)
        => true;

    private static bool CheckFullContext(string needle, in ProfileFileSystemCache.ProfileData cacheItem)
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
                ProfileFilterTokenType.Name => !folder.Name.Contains(token.Needle, StringComparison.OrdinalIgnoreCase),
                ProfileFilterTokenType.Default => !folder.FullPath.Contains(token.Needle, StringComparison.OrdinalIgnoreCase),
                ProfileFilterTokenType.FullContext => !folder.FullPath.Contains(token.Needle, StringComparison.OrdinalIgnoreCase),
                _ => true,
            })
                return false;
        }

        foreach (var token in Negated)
        {
            if (token.Type switch
            {
                ProfileFilterTokenType.Name => folder.Name.Contains(token.Needle, StringComparison.OrdinalIgnoreCase),
                ProfileFilterTokenType.Default => folder.FullPath.Contains(token.Needle, StringComparison.OrdinalIgnoreCase),
                ProfileFilterTokenType.FullContext => folder.FullPath.Contains(token.Needle, StringComparison.OrdinalIgnoreCase),
                _ => false,
            })
                return false;
        }

        foreach (var token in General)
        {
            if (token.Type switch
            {
                ProfileFilterTokenType.Name => folder.Name.Contains(token.Needle, StringComparison.OrdinalIgnoreCase),
                ProfileFilterTokenType.Default => folder.FullPath.Contains(token.Needle, StringComparison.OrdinalIgnoreCase),
                ProfileFilterTokenType.FullContext => !folder.FullPath.Contains(token.Needle, StringComparison.OrdinalIgnoreCase),
                _ => false,
            })
                return true;
        }

        return General.Count is 0;
    }
}