using System.Collections.Generic;

namespace CustomizePlusPlus.UI;

public enum ColorId
{
    UsedTemplate,
    UnusedTemplate,
    EnabledProfile,
    DisabledProfile,
    LocalCharacterEnabledProfile,
    LocalCharacterDisabledProfile,
    FolderExpanded,
    FolderCollapsed,
    FolderLine,
    HeaderButtons,
}

public static class Colors
{
    public const uint SelectedRed = 0xFF2020D0;

    public static (uint DefaultColor, string Name, string Description) Data(this ColorId color)
        => color switch
        {
            // @formatter:off
            ColorId.UsedTemplate => (0xFFFFFFFF, "Used Template", "A template which is being used by at least one profile."),
            //ColorId.EnabledTemplate => (0xFFA0F0A0, "Enabled Automation Set", "An automation set that is currently enabled. Only one set can be enabled for each identifier at once."),
            ColorId.UnusedTemplate => (0xFF808080, "Unused template", "Template which is currently not being used by any profile."),
            ColorId.EnabledProfile => (0xFFFFFFFF, "Enabled profile", "A profile which is currently enabled."),
            ColorId.DisabledProfile => (0xFF808080, "Disabled profile", "A profile which is currently disabled"),
            ColorId.LocalCharacterEnabledProfile => (0xFF18C018, "Current character profile (enabled)", "A profile which is currently enabled and associated with your character."),
            ColorId.LocalCharacterDisabledProfile => (0xFF808080, "Current character profile (disabled)", "A profile which is currently disabled and associated with your character."),
            ColorId.FolderExpanded => (0xFFFFF0C0, "Expanded Folder", "A folder that is currently expanded."),
            ColorId.FolderCollapsed => (0xFFFFF0C0, "Collapsed Folder", "A folder that is currently collapsed."),
            ColorId.FolderLine => (0xFFFFF0C0, "Expanded Folder Line", "The line signifying which descendants belong to an expanded folder."),
            ColorId.HeaderButtons => (0xFFFFF0C0, "Header Buttons", "The text and border color of buttons in the header, like the write protection toggle."),
            _ => (0x00000000, string.Empty, string.Empty),
            // @formatter:on
        };

    private static IReadOnlyDictionary<ColorId, uint> _colors = new Dictionary<ColorId, uint>();

    /// <summary> Obtain the configured value for a color. </summary>
    public static uint Value(this ColorId color)
        => _colors.TryGetValue(color, out var value) ? value : color.Data().DefaultColor;

    /// <summary> Set the configurable colors dictionary to a value. </summary>
    /*public static void SetColors(Configuration config)
        => _colors = config.Colors;*/
}
