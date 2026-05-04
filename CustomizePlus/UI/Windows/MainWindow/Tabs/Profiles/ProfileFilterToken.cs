namespace CustomizePlus.UI.Windows.MainWindow.Tabs.Profiles;

public enum ProfileFilterTokenType
{
    Default,
    Name,
    FullContext,
}

public readonly struct ProfileFilterToken() : IFilterToken<ProfileFilterTokenType, ProfileFilterToken>
{
    public string Needle { get; init; } = string.Empty;
    public ProfileFilterTokenType Type { get; init; }

    public bool Contains(ProfileFilterToken other)
    {
        if (Type != other.Type)
            return false;

        return Needle.Contains(other.Needle);
    }

    public static bool ConvertToken(char tokenCharacter, out ProfileFilterTokenType type)
    {
        type = tokenCharacter switch
        {
            'n' or 'N' => ProfileFilterTokenType.Name,
            'f' or 'F' => ProfileFilterTokenType.FullContext,
            _ => ProfileFilterTokenType.Default,
        };
        return type is not ProfileFilterTokenType.Default;
    }

    public static bool AllowsNone(ProfileFilterTokenType type)
        => false;

    public static bool ProcessList(List<ProfileFilterToken> list, TokenModifier modifier)
        => false;
}
