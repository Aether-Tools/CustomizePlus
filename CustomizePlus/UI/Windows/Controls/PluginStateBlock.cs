using Dalamud.Interface.Utility;
using Dalamud.Interface;
using ImGuiNET;
using System.Numerics;
using CustomizePlus.Core.Services;
using CustomizePlus.Game.Services;
using CustomizePlus.Configuration.Data;
using CustomizePlus.UI.Windows.MainWindow.Tabs.Templates;
using CustomizePlus.Core.Helpers;

namespace CustomizePlus.UI.Windows.Controls;

public class PluginStateBlock
{
    private readonly BoneEditorPanel _boneEditorPanel;
    private readonly PluginConfiguration _configuration;
    private readonly GameStateService _gameStateService;
    private readonly FantasiaPlusDetectService _fantasiaPlusDetectService;

    private static Vector4 normalColor = new Vector4(1, 1, 1, 1);
    private static Vector4 warnColor = new Vector4(1, 0.5f, 0, 1);
    private static Vector4 errorColor = new Vector4(1, 0, 0, 1);

    public PluginStateBlock(
        BoneEditorPanel boneEditorPanel,
        PluginConfiguration configuration,
        GameStateService gameStateService,
        FantasiaPlusDetectService fantasiaPlusDetectService)
    {
        _boneEditorPanel = boneEditorPanel;
        _configuration = configuration;
        _gameStateService = gameStateService;
        _fantasiaPlusDetectService = fantasiaPlusDetectService;
    }

    public void Draw(float yPos)
    {
        var severity = PluginStateSeverity.Normal;
        string? message = null;

        if (_fantasiaPlusDetectService.IsFantasiaPlusInstalled)
        {
            severity = PluginStateSeverity.Error;
            message = $"Fantasia+ detected. The plugin is disabled until Fantasia+ is disabled and the game is restarted.";
        }
        else if (_gameStateService.GameInPosingMode())
        {
            severity = PluginStateSeverity.Warning;
            message = $"GPose active. Most editor features are unavailable while you're in this mode.";
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

        if (message != null)
        {
            ImGui.SetCursorPos(new Vector2(ImGui.GetWindowContentRegionMax().X - ImGui.CalcTextSize(message).X - 30, yPos - ImGuiHelpers.GlobalScale));

            var icon = FontAwesomeIcon.InfoCircle;
            var color = normalColor;
            switch (severity)
            {
                case PluginStateSeverity.Warning:
                    icon = FontAwesomeIcon.ExclamationTriangle;
                    color = warnColor;
                    break;
                case PluginStateSeverity.Error:
                    icon = FontAwesomeIcon.ExclamationTriangle;
                    color = errorColor;
                    break;
            }

            ImGui.PushStyleColor(ImGuiCol.Text, color);
            CtrlHelper.LabelWithIcon(icon, message, false);
            ImGui.PopStyleColor();
        }
    }

    private enum PluginStateSeverity
    {
        Normal,
        Warning,
        Error
    }
}
