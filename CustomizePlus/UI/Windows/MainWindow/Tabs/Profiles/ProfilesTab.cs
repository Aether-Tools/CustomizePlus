using Dalamud.Interface.Utility;
using Dalamud.Bindings.ImGui;

namespace CustomizePlus.UI.Windows.MainWindow.Tabs.Profiles;

public class ProfilesTab
{
    private readonly ProfileFileSystemSelector _selector;
    private readonly ProfilePanel _panel;

    public ProfilesTab(ProfileFileSystemSelector selector, ProfilePanel panel)
    {
        _selector = selector;
        _panel = panel;
    }

    public void Draw()
    {
        _selector.Draw(200f * ImGuiHelpers.GlobalScale); // not sure
        ImGui.SameLine();
        _panel.Draw();
    }
}
