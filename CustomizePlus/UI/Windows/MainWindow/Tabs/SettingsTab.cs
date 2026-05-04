using CustomizePlus.Armatures.Services;
using CustomizePlus.Configuration.Data;
using CustomizePlus.Core.Helpers;
using CustomizePlus.Core.Services;
using CustomizePlus.Templates;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Utility;
using Dalamud.Plugin;
using Dalamud.Utility;

namespace CustomizePlus.UI.Windows.MainWindow.Tabs;

public class SettingsTab : ITab<MainTabType>
{
    private const uint DiscordColor = 0xFFDA8972;
    private const uint DonateColor = 0xFF5B5EFF;

    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly PluginConfiguration _configuration;
    private readonly ArmatureManager _armatureManager;
    private readonly HookingService _hookingService;
    private readonly TemplateEditorManager _templateEditorManager;
    private readonly CPlusChangeLog _changeLog;
    private readonly MessageService _messageService;
    private readonly SupportLogBuilderService _supportLogBuilderService;
    private readonly PcpService _pcpService;

    public SettingsTab(
        IDalamudPluginInterface pluginInterface,
        PluginConfiguration configuration,
        ArmatureManager armatureManager,
        HookingService hookingService,
        TemplateEditorManager templateEditorManager,
        CPlusChangeLog changeLog,
        MessageService messageService,
        SupportLogBuilderService supportLogBuilderService,
        PcpService pcpService)
    {
        _pluginInterface = pluginInterface;
        _configuration = configuration;
        _armatureManager = armatureManager;
        _hookingService = hookingService;
        _templateEditorManager = templateEditorManager;
        _changeLog = changeLog;
        _messageService = messageService;
        _supportLogBuilderService = supportLogBuilderService;
        _pcpService = pcpService;
    }

    public ReadOnlySpan<byte> Label
        => "Settings"u8;

    public MainTabType Identifier
        => MainTabType.Settings;

    public void DrawContent()
    {
        UiHelpers.SetupCommonSizes();
        using var child = Im.Child.Begin("MainWindowChild"u8);
        if (!child)
            return;

        DrawGeneralSettings();

        Im.Line.New();
        Im.Line.New();
        Im.Line.New();
        Im.Line.New();

        using (var child2 = Im.Child.Begin("SettingsChild"u8))
        {
            DrawProfileApplicationSettings();
            DrawInterface();
            DrawCommands();
            DrawExternal();
            DrawAdvancedSettings();
        }

        DrawSupportButtons();
    }

    #region General Settings
    // General Settings
    private void DrawGeneralSettings()
    {
        DrawPluginEnabledCheckbox();
    }

    private void DrawPluginEnabledCheckbox()
    {
        using (var disabled = Im.Disabled(_templateEditorManager.IsEditorActive))
        {
            var isChecked = _configuration.PluginEnabled;

            //users doesn't really need to know what exactly this checkbox does so we just tell them it toggles all profiles
            if (CtrlHelper.CheckboxWithTextAndHelp("##pluginenabled", "Enable Customize+",
                    "Globally enables or disables all plugin functionality.", ref isChecked))
            {
                _configuration.PluginEnabled = isChecked;
                _configuration.Save();
                _hookingService.ReloadHooks();
            }
        }
    }
    #endregion

    #region Profile application settings
    private void DrawProfileApplicationSettings()
    {
        var isShouldDraw = Im.Tree.Header("Profile Application"u8);

        if (!isShouldDraw)
            return;

        DrawApplyInCharacterWindowCheckbox();
        DrawApplyInTryOnCheckbox();
        DrawApplyInCardsCheckbox();
        DrawApplyInInspectCheckbox();
        DrawApplyInLobbyCheckbox();
    }

    private void DrawApplyInCharacterWindowCheckbox()
    {
        var isChecked = _configuration.ProfileApplicationSettings.ApplyInCharacterWindow;

        if (CtrlHelper.CheckboxWithTextAndHelp("##applyincharwindow", "Apply Profiles in Character Window",
                "Apply profile for your character in your main character window, if it is set.", ref isChecked))
        {
            _configuration.ProfileApplicationSettings.ApplyInCharacterWindow = isChecked;
            _configuration.Save();
            _armatureManager.RebindAllArmatures();
        }
    }

    private void DrawApplyInTryOnCheckbox()
    {
        var isChecked = _configuration.ProfileApplicationSettings.ApplyInTryOn;

        if (CtrlHelper.CheckboxWithTextAndHelp("##applyintryon", "Apply Profiles in Try-On Window",
                "Apply profile for your character in your try-on, dye preview or glamour plate window, if it is set.", ref isChecked))
        {
            _configuration.ProfileApplicationSettings.ApplyInTryOn = isChecked;
            _configuration.Save();
            _armatureManager.RebindAllArmatures();
        }
    }

    private void DrawApplyInCardsCheckbox()
    {
        var isChecked = _configuration.ProfileApplicationSettings.ApplyInCards;

        if (CtrlHelper.CheckboxWithTextAndHelp("##applyincards", "Apply Profiles in Adventurer Cards",
                "Apply appropriate profile for the adventurer card you are currently looking at.", ref isChecked))
        {
            _configuration.ProfileApplicationSettings.ApplyInCards = isChecked;
            _configuration.Save();
            _armatureManager.RebindAllArmatures();
        }
    }

    private void DrawApplyInInspectCheckbox()
    {
        var isChecked = _configuration.ProfileApplicationSettings.ApplyInInspect;

        if (CtrlHelper.CheckboxWithTextAndHelp("##applyininspect", "Apply Profiles in Inspect Window",
                "Apply appropriate profile for the character you are currently inspecting.", ref isChecked))
        {
            _configuration.ProfileApplicationSettings.ApplyInInspect = isChecked;
            _configuration.Save();
            _armatureManager.RebindAllArmatures();
        }
    }

    private void DrawApplyInLobbyCheckbox()
    {
        var isChecked = _configuration.ProfileApplicationSettings.ApplyInLobby;

        if (CtrlHelper.CheckboxWithTextAndHelp("##applyinlobby", "Apply Profiles on Character Select Screen",
                "Apply appropriate profile for the character you have currently selected on character select screen during login.", ref isChecked))
        {
            _configuration.ProfileApplicationSettings.ApplyInLobby = isChecked;
            _configuration.Save();
            _armatureManager.RebindAllArmatures();
        }
    }
    #endregion

    #region Chat Commands Settings
    private void DrawCommands()
    {
        var isShouldDraw = Im.Tree.Header("Chat Commands"u8);

        if (!isShouldDraw)
            return;

        DrawPrintSuccessMessages();
    }

    private void DrawPrintSuccessMessages()
    {
        var isChecked = _configuration.CommandSettings.PrintSuccessMessages;

        if (CtrlHelper.CheckboxWithTextAndHelp("##displaychatcommandconfirms", "Print Successful Command Execution Messages to Chat",
                "Controls whether successful execution of chat commands will be acknowledged by separate chat message or not.", ref isChecked))
        {
            _configuration.CommandSettings.PrintSuccessMessages = isChecked;
            _configuration.Save();
        }
    }
    #endregion

    #region Interface Settings

    private void DrawInterface()
    {
        var isShouldDraw = Im.Tree.Header("Interface"u8);

        if (!isShouldDraw)
            return;

        DrawOpenWindowAtStart();
        DrawHideWindowInCutscene();
        DrawHideWindowWhenUiHidden();
        DrawHideWindowInGPose();

        UiHelpers.DefaultLineSpace();

        DrawFoldersDefaultOpen();

        UiHelpers.DefaultLineSpace();

        DrawSetPreviewToCurrentCharacterOnLogin();

        UiHelpers.DefaultLineSpace();

        if (KeySelector.DoubleModifier("Template Deletion Modifier"u8,
            "A modifier you need to hold while clicking the Delete Template button for it to take effect."u8, 100 * ImGuiHelpers.GlobalScale,
            _configuration.UISettings.DeleteModifier, v => _configuration.UISettings.DeleteModifier = v))
            _configuration.Save();
    }

    private void DrawOpenWindowAtStart()
    {
        var isChecked = _configuration.UISettings.OpenWindowAtStart;

        if (CtrlHelper.CheckboxWithTextAndHelp("##openwindowatstart", "Open Customize+ Window at Game Start",
                "Controls whether main Customize+ window will be opened when you launch the game or not.", ref isChecked))
        {
            _configuration.UISettings.OpenWindowAtStart = isChecked;

            _configuration.Save();
        }
    }

    private void DrawHideWindowInCutscene()
    {
        var isChecked = _configuration.UISettings.HideWindowInCutscene;

        if (CtrlHelper.CheckboxWithTextAndHelp("##hidewindowincutscene", "Hide Plugin Windows in Cutscenes",
                "Controls whether any Customize+ windows are hidden during cutscenes or not.", ref isChecked))
        {
            _pluginInterface.UiBuilder.DisableCutsceneUiHide = !isChecked;
            _configuration.UISettings.HideWindowInCutscene = isChecked;

            _configuration.Save();
        }
    }

    private void DrawHideWindowWhenUiHidden()
    {
        var isChecked = _configuration.UISettings.HideWindowWhenUiHidden;

        if (CtrlHelper.CheckboxWithTextAndHelp("##hidewindowwhenuihidden", "Hide Plugin Windows when UI is Hidden",
                "Controls whether any Customize+ windows are hidden when you manually hide the in-game user interface.", ref isChecked))
        {
            _pluginInterface.UiBuilder.DisableUserUiHide = !isChecked;
            _configuration.UISettings.HideWindowWhenUiHidden = isChecked;
            _configuration.Save();
        }
    }

    private void DrawHideWindowInGPose()
    {
        var isChecked = _configuration.UISettings.HideWindowInGPose;

        if (CtrlHelper.CheckboxWithTextAndHelp("##hidewindowingpose", "Hide Plugin Windows in GPose",
                "Controls whether any Customize+ windows are hidden when you enter GPose.", ref isChecked))
        {
            _pluginInterface.UiBuilder.DisableGposeUiHide = !isChecked;
            _configuration.UISettings.HideWindowInGPose = isChecked;
            _configuration.Save();
        }
    }

    private void DrawFoldersDefaultOpen()
    {
        var isChecked = _configuration.UISettings.FoldersDefaultOpen;

        if (CtrlHelper.CheckboxWithTextAndHelp("##foldersdefaultopen", "Open All Folders by Default",
                "Controls whether folders in template and profile lists are open by default or not.", ref isChecked))
        {
            _configuration.UISettings.FoldersDefaultOpen = isChecked;
            _configuration.Save();
        }
    }

    private void DrawSetPreviewToCurrentCharacterOnLogin()
    {
        var isChecked = _configuration.EditorConfiguration.SetPreviewToCurrentCharacterOnLogin;

        if (CtrlHelper.CheckboxWithTextAndHelp("##setpreviewcharaonlogin", "Automatically Set Current Character as Editor Preview Character",
                "Controls whether editor character will be automatically set to the current character during login.", ref isChecked))
        {
            _configuration.EditorConfiguration.SetPreviewToCurrentCharacterOnLogin = isChecked;
            _configuration.Save();
        }
    }

    #endregion

    #region Integrations

    private void DrawExternal()
    {
        var isShouldDraw = Im.Tree.Header("Integrations"u8);

        if (!isShouldDraw)
            return;

        DrawHandlePCP();
    }

    private void DrawHandlePCP()
    {
        var isChecked = _configuration.IntegrationSettings.PenumbraPCPIntegrationEnabled;

        if (CtrlHelper.CheckboxWithTextAndHelp("##pcpintegrationenabled", "Enable Penumbra PCP integration",
            "Controls whether C+ will add the currently active profile data from an actor to .pcp files upon creation, and construct new profile for said actor upon import.", ref isChecked))
        {
            _configuration.IntegrationSettings.PenumbraPCPIntegrationEnabled = isChecked;
            _pcpService.SetEnabled(isChecked);
            _configuration.Save();
        }
    }

    #endregion

    #region Advanced Settings
    // Advanced Settings
    private void DrawAdvancedSettings()
    {
        var isShouldDraw = Im.Tree.Header("Advanced"u8);

        if (!isShouldDraw)
            return;

        Im.Line.New();
        CtrlHelper.LabelWithIcon(FontAwesomeIcon.ExclamationTriangle,
            "These are advanced settings. Enable them at your own risk.");
        Im.Line.New();

        DrawEnableRootPositionCheckbox();
        DrawDebugModeCheckbox();
    }

    private void DrawEnableRootPositionCheckbox()
    {
        var isChecked = _configuration.EditorConfiguration.RootPositionEditingEnabled;
        if (CtrlHelper.CheckboxWithTextAndHelp("##rootpos", "Root editing",
                "Enables ability to edit the root bones.", ref isChecked))
        {
            _configuration.EditorConfiguration.RootPositionEditingEnabled = isChecked;
            _configuration.Save();
        }
    }

    private void DrawDebugModeCheckbox()
    {
        var isChecked = _configuration.DebuggingModeEnabled;
        if (CtrlHelper.CheckboxWithTextAndHelp("##debugmode", "Debug mode",
                "Enables debug mode. Requires plugin restart for all features to become properly initialized.", ref isChecked))
        {
            _configuration.DebuggingModeEnabled = isChecked;
            _configuration.Save();
        }
    }

    #endregion

    #region Support Area
    private void DrawSupportButtons()
    {
        var width = Im.Font.CalculateSize("Copy Support Info to Clipboard"u8).X + Im.Style.FramePadding.X * 2;
        var xPos = Im.Window.Width - width;
        // Respect the scroll bar width.
        if (Im.Scroll.MaximumY > 0)
            xPos -= Im.Style.ScrollbarSize + Im.Style.FramePadding.X;

        Im.Cursor.Position = new Vector2(xPos, 0);
        DrawUrlButton("Join Discord for Support", "https://discord.gg/KvGJCCnG8t", DiscordColor, width,
            "Join Discord server run by community volunteers who can help you with your questions. Opens https://discord.gg/KvGJCCnG8t in your web browser.");

        Im.Cursor.Position = new Vector2(xPos, Im.Style.FrameHeightWithSpacing);
        DrawUrlButton("Support developer using Ko-fi", "https://ko-fi.com/risadev", DonateColor, width,
            "Any donations made are voluntary and treated as a token of gratitude for work done on Customize+. Opens https://ko-fi.com/risadev in your web browser.");

        Im.Cursor.Position = new Vector2(xPos, 2 * Im.Style.FrameHeightWithSpacing);
        if (Im.Button("Copy Support Info to Clipboard"u8))
        {
            var text = _supportLogBuilderService.BuildSupportLog();
            Im.Clipboard.Set(text);
            _messageService.NotificationMessage($"Copied Support Info to Clipboard.", NotificationType.Success, false);
        }

        Im.Cursor.Position = new Vector2(xPos, 3 * Im.Style.FrameHeightWithSpacing);
        if (Im.Button("Show update history"u8, new Vector2(width, 0)))
            _changeLog.Changelog.ForceOpen = true;
    }

    /// <summary> Draw a button to open some url. </summary>
    private void DrawUrlButton(string text, string url, uint buttonColor, float width, string? description = null)
    {
        using var color = ImGuiColor.Button.Push(buttonColor);
        if (Im.Button(text, new Vector2(width, 0)))
            try
            {
                Util.OpenLink(url);
            }
            catch
            {
                _messageService.NotificationMessage($"Unable to open url {url}.", NotificationType.Error, false);
            }

        UiHelpers.DrawHoverTooltip(description ?? $"Open {url}");
    }
    #endregion
}
