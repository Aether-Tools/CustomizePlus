using Dalamud.Game.Command;
using Dalamud.Plugin.Services;
using OtterGui.Classes;
using System;
using System.Linq;
using OtterGui.Log;
using Dalamud.Game.Text.SeStringHandling;
using CustomizePlusPlus.Profiles;
using CustomizePlusPlus.Game.Services;
using CustomizePlusPlus.UI.Windows.MainWindow.Tabs.Templates;
using CustomizePlusPlus.UI.Windows.MainWindow;
using static System.Windows.Forms.AxHost;
using CustomizePlusPlus.Profiles.Data;
using CustomizePlusPlus.Configuration.Data;
using Dalamud.Interface.ImGuiNotification;
using CustomizePlusPlus.GameData.Extensions;
using System.Collections.Generic;
using ECommonsLite;
using CustomizePlusPlus.Core.Helpers;

namespace CustomizePlusPlus.Core.Services;

public class CommandService : IDisposable
{
    private readonly ProfileManager _profileManager;
    private readonly GameObjectService _gameObjectService;
    private readonly ICommandManager _commandManager;
    private readonly Logger _logger;
    private readonly ChatService _chatService;
    private readonly MainWindow _mainWindow;
    private readonly BoneEditorPanel _boneEditorPanel;
    private readonly MessageService _messageService;
    private readonly PluginConfiguration _pluginConfiguration;

    private static readonly string[] Commands = new[] { "/customize", "/c+" };

    public CommandService(
        ProfileManager profileManager,
        GameObjectService gameObjectService,
        ICommandManager commandManager,
        MainWindow mainWindow,
        ChatService chatService,
        BoneEditorPanel boneEditorPanel,
        Logger logger,
        MessageService messageService,
        PluginConfiguration pluginConfiguration)
    {
        _profileManager = profileManager;
        _gameObjectService = gameObjectService;
        _commandManager = commandManager;
        _logger = logger;
        _chatService = chatService;
        _mainWindow = mainWindow;
        _boneEditorPanel = boneEditorPanel;
        _messageService = messageService;
        _pluginConfiguration = pluginConfiguration;

        foreach (var command in Commands)
        {
            _commandManager.AddHandler(command, new CommandInfo(OnMainCommand) { HelpMessage = "Toggles main plugin window if no commands passed. Use \"/customize help\" for list of available commands." });
        }
    }

    public void Dispose()
    {
        foreach (var command in Commands)
        {
            _commandManager.RemoveHandler(command);
        }
    }

    private void OnMainCommand(string command, string arguments)
    {
        if (_boneEditorPanel.IsEditorActive)
        {
            _messageService.NotificationMessage("Customize+ commands cannot be used when editor is active", NotificationType.Error);
            return;
        }

        var argumentList = arguments.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var argument = argumentList.Length == 2 ? argumentList[1] : string.Empty;

        if (arguments.Length > 0)
        {
            switch (argumentList[0].ToLowerInvariant())
            {
                case "profile":
                    ProfileCommand(argument);
                    return;
                default:
                case "help":
                    PrintHelp(argumentList[0]);
                    return;
            }
        }

        _mainWindow.Toggle();
    }

    private bool PrintHelp(string argument)
    {
        if (!string.Equals(argument, "help", StringComparison.OrdinalIgnoreCase) && argument != "?")
            _chatService.PrintInChat(new SeStringBuilder().AddText("The given argument ").AddRed(argument, true)
                .AddText(" is not valid. Valid arguments are:").BuiltString);
        else
            _chatService.PrintInChat(new SeStringBuilder().AddText("Valid arguments for /customize are:").BuiltString);

        _chatService.PrintInChat(new SeStringBuilder().AddCommand("profile", "Change the state of profiles. Use without arguments for help.")
            .BuiltString);
        return true;
    }

    private void ProfileCommand(string argument)
    {
        var argumentList = argument.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        string[]? subArgumentList = null;

        if(argumentList.Length == 2)
            subArgumentList = argumentList[1].Split(',', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        bool isTurningOffAllProfiles = subArgumentList != null && subArgumentList.Length != 2 && argumentList[0] == "disable";

        if (argumentList.Length != 2 || subArgumentList == null || (subArgumentList.Length != 2 && !isTurningOffAllProfiles))
        {
            _chatService.PrintInChat(new SeStringBuilder().AddText($"Usage: /customize profile ")
                .AddBlue("enable, disable or toggle", true)
                .AddText(" ")
                .AddRed("Character Name", true)
                .AddText(",")
                .AddYellow("Profile Name", true)
                .BuiltString);
            _chatService.PrintInChat(new SeStringBuilder().AddText("    》 ").AddBlue("disable", true)
                .AddText(" option can also be used without supplying profile name to turn off currently active profile for the character").BuiltString);
            _chatService.PrintInChat(new SeStringBuilder().AddText("    》 ")
                .AddRed("Character Name", true).AddText(" can be either full character name or one of the following:").BuiltString);
            _chatService.PrintInChat(new SeStringBuilder().AddText("    》 To apply to yourself: ").AddBlue("<me>").AddText(", ").AddBlue("self").BuiltString);
            _chatService.PrintInChat(new SeStringBuilder().AddText("    》 To apply to target: ").AddBlue("<t>").AddText(", ").AddBlue("target").BuiltString);
            return;
        }

        string characterName = "", profileName = "";

        try
        {
            if (!_profileManager.Profiles.Any())
            {
                _chatService.PrintInChat(new SeStringBuilder().AddText("This command cannot be executed because no profiles exist").BuiltString);
                return;
            }

            bool? state = null;
            switch (argumentList[0].ToLowerInvariant())
            {
                case "enabled":
                case "enable":
                case "on":
                case "true":
                    state = true;
                    break;
                case "disabled":
                case "disable":
                case "off":
                case "false":
                    state = false;
                    break;
                case "toggle":
                case "switch":
                    break;
            }

            Profile? targetProfile = null;
            List<Profile> profilesToDisable = new List<Profile>(_profileManager.Profiles.Count);

            characterName = subArgumentList[0].Trim();
            characterName = characterName switch
            {
                "<me>" => _gameObjectService.GetCurrentPlayerName() ?? string.Empty,
                "self" => _gameObjectService.GetCurrentPlayerName() ?? string.Empty,
                "<t>" => _gameObjectService.GetCurrentPlayerTargetName() ?? string.Empty,
                "target" => _gameObjectService.GetCurrentPlayerTargetName() ?? string.Empty,
                _ => characterName,
            };

            if (!isTurningOffAllProfiles)
            {
                profileName = subArgumentList[1].Trim();
                foreach (var profile in _profileManager.Profiles)
                {
                    if (!profile.Characters.Any(x => x.ToNameWithoutOwnerName() == characterName))
                        continue;

                    if (profile.Name != profileName)
                    {
                        if(profile.Enabled)
                            profilesToDisable.Add(profile);
                        continue;
                    }

                    targetProfile = profile;
                }
            }
            else
                profilesToDisable = _profileManager.Profiles.Where(x => x.Characters.Any(x => x.ToNameWithoutOwnerName() == characterName) && x.Enabled).ToList();

            if((!isTurningOffAllProfiles && targetProfile == null) || (isTurningOffAllProfiles && profilesToDisable.Count == 0))
            {
                _chatService.PrintInChat(new SeStringBuilder()
                    .AddText("Cannot execute command because profile ")
                    .AddYellow(string.IsNullOrWhiteSpace(profileName) ? "[Any enabled profile]" : profileName)
                    .AddText(" for ")
                    .AddRed(characterName)
                    .AddText(" was not found").BuiltString);
                return;
            }

            if(!isTurningOffAllProfiles)
            {
                if (state != null)
                {
                    //todo: still check and disable other profiles in this case?
                    if (targetProfile!.Enabled == state)
                    {
                        _chatService.PrintInChat(new SeStringBuilder()
                            .AddText("Profile ")
                            .AddYellow(targetProfile.Name)
                            .AddText(" for ")
                            .AddBlue(characterName)
                            .AddText(" is already ")
                            .AddGreen((bool)state ? "enabled" : "disabled").BuiltString);
                        return;
                    }

                    _profileManager.SetEnabled(targetProfile, (bool)state);
                }
                else
                    _profileManager.SetEnabled(targetProfile!, !targetProfile!.Enabled);
            }
            
            if (isTurningOffAllProfiles || targetProfile!.Enabled)
            {
                foreach (var profile in profilesToDisable)
                    _profileManager.SetEnabled(profile, false);
            }

            if (_pluginConfiguration.CommandSettings.PrintSuccessMessages)
            {
                if (isTurningOffAllProfiles)
                    _chatService.PrintInChat(new SeStringBuilder()
                        .AddYellow($"{profilesToDisable.Count} profile(s)")
                        .AddText(" successfully ")
                        .AddBlue("disabled")
                        .AddText(" for ")
                        .AddRed(characterName).BuiltString);
                else
                    _chatService.PrintInChat(new SeStringBuilder()
                        .AddText("Profile ")
                        .AddYellow(targetProfile!.Name)
                        .AddText(" was successfully ")
                        .AddBlue(state != null ? ((bool)state ? "enabled" : "disabled") : "toggled")
                        .AddText(" for ")
                        .AddRed(string.Join(',', targetProfile.Characters.Select(x => x.ToNameWithoutOwnerName())))
                        .AddText(". ")
                        .AddItalics($"({profilesToDisable.Count} profile(s) disabled)")
                        .BuiltString);
            }

        }
        catch (Exception e)
        {
            _chatService.PrintInChat(new SeStringBuilder()
                .AddRed($"Error while changing state of profile, details are available in dalamud log").BuiltString);

            _logger.Error($"Error while changing state of profile by command:\n" +
                            $"Profile name \"{(string.IsNullOrWhiteSpace(profileName) ? "empty (none provided)" : profileName)}\"\n" +
                            $"Character name \"{(string.IsNullOrWhiteSpace(characterName) ? "empty (none provided)" : characterName)}\"\n" +
                            $"Arguments: {argument}\nError: {e}");
        }
    }
}
