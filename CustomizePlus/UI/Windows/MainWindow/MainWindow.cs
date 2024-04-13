using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using ImGuiNET;
using OtterGui.Raii;
using System;
using System.Numerics;
using SettingsTab = CustomizePlus.UI.Windows.MainWindow.Tabs.SettingsTab;
using CustomizePlus.Core.Services;
using CustomizePlus.UI.Windows.MainWindow.Tabs.Debug;
using CustomizePlus.Configuration.Data;
using CustomizePlus.UI.Windows.MainWindow.Tabs.Templates;
using CustomizePlus.UI.Windows.Controls;
using CustomizePlus.UI.Windows.MainWindow.Tabs.Profiles;
using CustomizePlus.UI.Windows.MainWindow.Tabs;
using CustomizePlus.Templates;
using ECommons.ImGuiMethods;
using static System.Windows.Forms.AxHost;
using Dalamud.Interface.Colors;

namespace CustomizePlus.UI.Windows.MainWindow;

public class MainWindow : Window, IDisposable
{
    private readonly SettingsTab _settingsTab;
    private readonly TemplatesTab _templatesTab;
    private readonly ProfilesTab _profilesTab;
    private readonly MessagesTab _messagesTab;
    private readonly IPCTestTab _ipcTestTab;
    private readonly StateMonitoringTab _stateMonitoringTab;

    private readonly PluginStateBlock _pluginStateBlock;

    private readonly TemplateEditorManager _templateEditorManager;
    private readonly FantasiaPlusDetectService _fantasiaPlusDetectService;
    private readonly PluginConfiguration _configuration;
    private readonly HookingService _hookingService;

    public MainWindow(
        DalamudPluginInterface pluginInterface,
        SettingsTab settingsTab,
        TemplatesTab templatesTab,
        ProfilesTab profilesTab,
        MessagesTab messagesTab,
        IPCTestTab ipcTestTab,
        StateMonitoringTab stateMonitoringTab,
        PluginStateBlock pluginStateBlock,
        TemplateEditorManager templateEditorManager,
        PluginConfiguration configuration,
        FantasiaPlusDetectService fantasiaPlusDetectService,
        HookingService hookingService
        ) : base($"Customize+ v{Plugin.Version}###CPlusMainWindow")
    {
        _settingsTab = settingsTab;
        _templatesTab = templatesTab;
        _profilesTab = profilesTab;
        _messagesTab = messagesTab;
        _ipcTestTab = ipcTestTab;
        _stateMonitoringTab = stateMonitoringTab;

        _pluginStateBlock = pluginStateBlock;

        _templateEditorManager = templateEditorManager;
        _configuration = configuration;
        _fantasiaPlusDetectService = fantasiaPlusDetectService;
        _hookingService = hookingService;

        pluginInterface.UiBuilder.DisableGposeUiHide = true;
        SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new Vector2(700, 675),
            MaximumSize = ImGui.GetIO().DisplaySize,
        };

        IsOpen = pluginInterface.IsDevMenuOpen && configuration.DebuggingModeEnabled;
    }

    public void Dispose()
    {
        //throw new NotImplementedException();
    }

    public override void Draw()
    {
        var yPos = ImGui.GetCursorPosY();

        using (var disabled = ImRaii.Disabled(_fantasiaPlusDetectService.IsFantasiaPlusInstalled || _hookingService.RenderHookFailed || _hookingService.MovementHookFailed))
        {
            LockWindowClosureIfNeeded();
            ImGuiEx.EzTabBar("##tabs", [
                ("Settings", _settingsTab.Draw, null, true),
                ("Templates", _templatesTab.Draw, null, true),
                ("Profiles", _profilesTab.Draw, null, true),
                (_configuration.DebuggingModeEnabled ? "IPC Test" : null, _ipcTestTab.Draw, ImGuiColors.DalamudGrey, true),
                (_configuration.DebuggingModeEnabled ? "State monitoring" : null, _stateMonitoringTab.Draw, ImGuiColors.DalamudGrey, true),
            ]);
        }

        _pluginStateBlock.Draw(yPos);
    }

    private void LockWindowClosureIfNeeded()
    {
        if (_templateEditorManager.IsEditorActive)
        {
            ShowCloseButton = false;
            RespectCloseHotkey = false;
        }
        else
        {
            ShowCloseButton = true;
            RespectCloseHotkey = true;
        }
    }
}
