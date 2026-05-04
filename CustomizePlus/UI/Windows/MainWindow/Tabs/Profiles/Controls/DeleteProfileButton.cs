using CustomizePlus.Configuration.Data;
using CustomizePlus.Profiles;
using CustomizePlus.Profiles.Data;

namespace CustomizePlus.UI.Windows.MainWindow.Tabs.Profiles.Controls;

public sealed class DeleteProfileButton(
    ProfileFileSystem fileSystem,
    ProfileManager profileManager,
    PluginConfiguration config)
    : BaseIconButton<AwesomeIcon>
{
    /// <inheritdoc/>
    public override AwesomeIcon Icon
        => LunaStyle.DeleteIcon;

    /// <inheritdoc/>
    public override bool HasTooltip
        => true;

    /// <inheritdoc/>
    public override void DrawTooltip()
    {
        var anySelected = fileSystem.Selection.DataNodes.Count > 0;
        var modifier = Enabled;

        Im.Text(anySelected
            ? "Delete the currently selected profiles entirely from your drive\nThis can not be undone."u8
            : "No profiles selected."u8);
        if (!modifier)
            Im.Text($"\nHold {config.UISettings.DeleteModifier} while clicking to delete the profiles.");
    }

    /// <inheritdoc/>
    public override bool Enabled
        => config.UISettings.DeleteModifier.IsActive() && fileSystem.Selection.DataNodes.Count > 0;

    /// <inheritdoc/>
    public override void OnClick()
    {
        var profiles = fileSystem.Selection.DataNodes.Select(n => n.Value).OfType<Profile>().ToList();
        fileSystem.Selection.UnselectAll();
        foreach (var profile in profiles)
            profileManager.Delete(profile);
    }
}
