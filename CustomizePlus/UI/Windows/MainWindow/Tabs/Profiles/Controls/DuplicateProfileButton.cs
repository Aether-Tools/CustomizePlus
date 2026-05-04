using CustomizePlus.Profiles;
using CustomizePlus.Profiles.Data;

namespace CustomizePlus.UI.Windows.MainWindow.Tabs.Profiles.Controls;

public sealed class DuplicateProfileButton(
    ProfileFileSystem fileSystem,
    ProfileManager profileManager) : BaseIconButton<AwesomeIcon>
{
    private readonly WeakReference<Profile> _profile = new(null!);

    public override AwesomeIcon Icon
        => LunaStyle.DuplicateIcon;

    public override bool HasTooltip
        => true;

    public override bool Enabled
        => fileSystem.Selection.Selection is not null;

    public override void DrawTooltip()
        => Im.Text(fileSystem.Selection.Selection is null ? "No profile selected."u8 : "Clone the currently selected profile to a duplicate."u8);

    public override void OnClick()
    {
        _profile.SetTarget(fileSystem.Selection.Selection?.GetValue<Profile>()!);
        Im.Popup.Open("##CloneProfile"u8);
    }

    protected override void PostDraw()
    {
        if (!InputPopup.OpenName("##CloneProfile"u8, out var newName))
            return;

        if (_profile.TryGetTarget(out var profile))
            profileManager.Clone(profile, newName, true);

        _profile.SetTarget(null!);
    }
}
