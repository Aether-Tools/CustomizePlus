using Dalamud.Interface.Utility;
using Dalamud.Interface;
using Dalamud.Bindings.ImGui;
using System.Numerics;
using CustomizePlusPlus.Core.Services;
using CustomizePlusPlus.Game.Services;
using CustomizePlusPlus.Configuration.Data;
using CustomizePlusPlus.UI.Windows.MainWindow.Tabs.Templates;
using CustomizePlusPlus.Core.Helpers;
using CustomizePlusPlus.Api;
using CustomizePlusPlus.Core.Data;
using CustomizePlusPlus.Core.Services.Dalamud;

namespace CustomizePlusPlus.UI.Windows.Controls;

public class PluginStateBlock
{
    private readonly BoneEditorPanel _boneEditorPanel;
    private readonly PluginConfiguration _configuration;
    private readonly GameStateService _gameStateService;
    private readonly HookingService _hookingService;
    private readonly CustomizePlusIpc _ipcService;

    public PluginStateBlock(
        BoneEditorPanel boneEditorPanel,
        PluginConfiguration configuration,
        GameStateService gameStateService,
        HookingService hookingService,
        CustomizePlusIpc ipcService)
    {
        _boneEditorPanel = boneEditorPanel;
        _configuration = configuration;
        _gameStateService = gameStateService;
        _hookingService = hookingService;
        _ipcService = ipcService;
    }

    public void Draw(float yPos)
    {
        var severity = PluginStateSeverity.Normal;
        string? message = null;
        string? hoverInfo = null;

        if(_hookingService.RenderHookFailed || _hookingService.MovementHookFailed)
        {
            severity = PluginStateSeverity.Error;
            message = "Detected failure in game hooks. Customize+ disabled.";
        }
        else if (!_configuration.PluginEnabled)
        {
            severity = PluginStateSeverity.Warning;
            message = "Plugin is disabled, template bone editing is not available.";
        }
        else if (_boneEditorPanel.IsEditorActive)
        {
            if (!_boneEditorPanel.IsCharacterFound)
            {
                severity = PluginStateSeverity.Error;
                message = $"Selected preview character was not found.";
            }
            else
            {
                if (_boneEditorPanel.HasChanges)
                    severity = PluginStateSeverity.Warning;

                message = $"Editor is active.{(_boneEditorPanel.HasChanges ? " You have unsaved changes, finish template bone editing to open save/revert dialog." : "")}";
            }
        }
        else if (_gameStateService.GameInPosingMode())
        {
            severity = PluginStateSeverity.Warning;
            message = "GPose active. Compatibility with posing tools is limited.";
        }
        else if (_ipcService.IPCFailed) //this is a low priority error
        {
            severity = PluginStateSeverity.Error;
            message = "Detected failure in IPC. Integrations with other plugins will not function.";
        }
        else if(VersionHelper.IsTesting)
        {
            severity = PluginStateSeverity.Warning;
            message = "You are running testing version of Customize+, hover for more information.";
            hoverInfo = "This is a testing build of Customize+. Some features like integration with other plugins might not function correctly.";
        }

        if (message != null)
        {
            ImGui.SetCursorPos(new Vector2(ImGui.GetWindowContentRegionMax().X - ImGui.CalcTextSize(message).X - 30, yPos - ImGuiHelpers.GlobalScale));

            var icon = FontAwesomeIcon.InfoCircle;
            var color = Constants.Colors.Normal;
            switch (severity)
            {
                case PluginStateSeverity.Warning:
                    icon = FontAwesomeIcon.ExclamationTriangle;
                    color = Constants.Colors.Warning;
                    break;
                case PluginStateSeverity.Error:
                    icon = FontAwesomeIcon.ExclamationTriangle;
                    color = Constants.Colors.Error;
                    break;
            }

            ImGui.PushStyleColor(ImGuiCol.Text, color);
            CtrlHelper.LabelWithIcon(icon, message, false);
            ImGui.PopStyleColor();
            if (hoverInfo != null)
                CtrlHelper.AddHoverText(hoverInfo);
        }
    }

    private enum PluginStateSeverity
    {
        Normal,
        Warning,
        Error
    }
}
