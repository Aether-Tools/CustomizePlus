using CustomizePlus.UI.Windows.MainWindow.Tabs;
using CustomizePlus.UI.Windows.MainWindow.Tabs.Debug;
using CustomizePlus.UI.Windows.MainWindow.Tabs.Profiles;
using CustomizePlus.UI.Windows.MainWindow.Tabs.Templates;

namespace CustomizePlus.UI.Windows.MainWindow;

public sealed class MainTabBar : TabBar<MainTabType>
{
    public readonly SettingsTab Settings;

    public MainTabBar(LunaLogger log, SettingsTab settings, TemplatesTab templates, ProfilesTab profiles,
        IPCTestTab ipcTest, StateMonitoringTab stateMonitoring)
        : base("MainTabBar", log, settings, templates, profiles, ipcTest, stateMonitoring)
    {
        Settings = settings;
    }
}