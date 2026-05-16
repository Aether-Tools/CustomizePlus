using CustomizePlus.Core.Helpers;
using CustomizePlus.Profiles;
using CustomizePlus.Profiles.Data;
using System;

namespace CustomizePlus.UI.Windows.MainWindow.Tabs.Profiles.Controls;

public sealed class ExportProfileButton(ProfileFileSystem fileSystem, PopupSystem popupSystem) : BaseIconButton<AwesomeIcon>
{
    public override bool IsVisible
        => fileSystem.Selection.Selection is not null;

    public override AwesomeIcon Icon
        => LunaStyle.ToClipboardIcon;

    public override bool HasTooltip
        => true;

    public override void DrawTooltip()
        => Im.Text("Copy the current profile combined into one template to your clipboard."u8);

    public override void OnClick()
    {
        var profile = (Profile)fileSystem.Selection.Selection!.Value;

        try
        {
            var text = Base64Helper.ExportProfileToBase64(profile);
            Im.Clipboard.Set(text);
            popupSystem.ShowPopup(PopupSystem.Messages.ClipboardDataNotLongTerm);
        }
        catch (Exception ex)
        {
            CustomizePlus.Logger.Error($"Could not copy data from profile {profile.UniqueId} to clipboard: {ex}");
            popupSystem.ShowPopup(PopupSystem.Messages.ActionError);
        }
    }
}
