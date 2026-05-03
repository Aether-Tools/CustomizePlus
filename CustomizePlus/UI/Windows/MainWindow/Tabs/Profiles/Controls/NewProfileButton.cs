using CustomizePlus.Profiles;
using CustomizePlus.Templates;
using System;
using System.Collections.Generic;
using System.Text;

namespace CustomizePlus.UI.Windows.MainWindow.Tabs.Profiles.Controls;

public sealed class NewProfileButton(ProfileManager profileManager) : BaseIconButton<AwesomeIcon>
{
    public override AwesomeIcon Icon
        => LunaStyle.AddObjectIcon;

    public override bool HasTooltip
        => true;

    public override void DrawTooltip()
        => Im.Text("Create a new profile with default configuration."u8);

    public override void OnClick()
    {
        Im.Popup.Open("##NewProfile"u8);
    }

    protected override void PostDraw()
    {
        if (!InputPopup.OpenName("##NewProfile"u8, out var newName))
            return;

        profileManager.Create(newName, true);
    }
}
