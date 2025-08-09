using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Bindings.ImGui;
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
using ECommonsLite.ImGuiMethods;
using static System.Windows.Forms.AxHost;
using Dalamud.Interface.Colors;
using CustomizePlus.Templates.Events;
using CustomizePlus.Templates.Data;
using ECommonsLite.Schedulers;
using CustomizePlus.Core.Helpers;
using CustomizePlus.Core.Services.Dalamud;

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
    private readonly PluginConfiguration _configuration;
    private readonly HookingService _hookingService;

    private readonly TemplateEditorEvent _templateEditorEvent;

    /// <summary>
    /// Used to force the main window to switch to specific tab
    /// </summary>
    private string? _switchToTab = null;

    private Action? _actionAfterTabSwitch = null;

    public MainWindow(
        IDalamudPluginInterface pluginInterface,
        SettingsTab settingsTab,
        TemplatesTab templatesTab,
        ProfilesTab profilesTab,
        MessagesTab messagesTab,
        IPCTestTab ipcTestTab,
        StateMonitoringTab stateMonitoringTab,
        PluginStateBlock pluginStateBlock,
        TemplateEditorManager templateEditorManager,
        PluginConfiguration configuration,
        HookingService hookingService,
        TemplateEditorEvent templateEditorEvent
        ) : base($"Customize+ {VersionHelper.Version}###CPlusMainWindow")
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
        _hookingService = hookingService;

        _templateEditorEvent = templateEditorEvent;

        _templateEditorEvent.Subscribe(OnTemplateEditorEvent, TemplateEditorEvent.Priority.MainWindow);

        SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new Vector2(700, 675),
            MaximumSize = ImGui.GetIO().DisplaySize,
        };

        IsOpen = configuration.UISettings.OpenWindowAtStart;
    }

    public void Dispose()
    {
        _templateEditorEvent.Unsubscribe(OnTemplateEditorEvent);
    }

    public override void Draw()
    {
        var yPos = ImGui.GetCursorPosY();

        using (var disabled = ImRaii.Disabled(_hookingService.RenderHookFailed || _hookingService.MovementHookFailed))
        {
            LockWindowClosureIfNeeded();
            ImGuiEx.EzTabBar("##tabs", null, _switchToTab, [
                ("Settings", _settingsTab.Draw, null, true),
                ("Templates", _templatesTab.Draw, null, true),
                ("Profiles", _profilesTab.Draw, null, true),
                (_configuration.DebuggingModeEnabled ? "IPC Test" : null, _ipcTestTab.Draw, ImGuiColors.DalamudGrey, true),
                (_configuration.DebuggingModeEnabled ? "State monitoring" : null, _stateMonitoringTab.Draw, ImGuiColors.DalamudGrey, true),
            ]);

            _switchToTab = null;

            if (_actionAfterTabSwitch != null)
            {
                _actionAfterTabSwitch();
                _actionAfterTabSwitch = null;
            }
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

    private void OnTemplateEditorEvent(TemplateEditorEvent.Type type, Template? template)
    {
        if (type != TemplateEditorEvent.Type.EditorEnableRequested)
            return;

        if (template == null)
            return;

        if (!template.IsWriteProtected && !_templateEditorManager.IsEditorActive)
        {
            new TickScheduler(() =>
            {
                _switchToTab = "Templates";

                //To make sure the tab has switched, ugly but imgui is shit and I don't trust it.
                _actionAfterTabSwitch = () => { _templateEditorEvent.Invoke(TemplateEditorEvent.Type.EditorEnableRequestedStage2, template); };
            });
        }
    }
}
