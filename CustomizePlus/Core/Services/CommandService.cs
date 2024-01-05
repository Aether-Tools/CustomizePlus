using Dalamud.Game.Command;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Plugin.Services;
using OtterGui.Classes;
using System;
using System.Linq;
using OtterGui.Log;
using Dalamud.Game.Text.SeStringHandling;
using CustomizePlus.Profiles;
using CustomizePlus.Game.Services;
using CustomizePlus.UI.Windows.MainWindow.Tabs.Templates;
using CustomizePlus.UI.Windows.MainWindow;

namespace CustomizePlus.Core.Services;

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

    private static readonly string[] Commands = new[] { "/customize", "/c+" };

    public CommandService(
        ProfileManager profileManager,
        GameObjectService gameObjectService,
        ICommandManager commandManager,
        MainWindow mainWindow,
        ChatService chatService,
        BoneEditorPanel boneEditorPanel,
        Logger logger,
        MessageService messageService)
    {
        _profileManager = profileManager;
        _gameObjectService = gameObjectService;
        _commandManager = commandManager;
        _logger = logger;
        _chatService = chatService;
        _mainWindow = mainWindow;
        _boneEditorPanel = boneEditorPanel;
        _messageService = messageService;

        foreach (var command in Commands)
        {
            _commandManager.AddHandler(command, new CommandInfo(OnMainCommand) { HelpMessage = "Toggles main plugin window if no commands passed. Use \"/customize help\" for list of available commands." });
        }

        chatService.PrintInChat($"Started!"); //safe to assume at this point we have successfully initialized
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
                case "apply":
                    Apply(argument);
                    return;
                case "toggle":
                    Apply(argument, true);
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

        _chatService.PrintInChat(new SeStringBuilder().AddCommand("apply", "Applies a given profile for a given character. Use without arguments for help.")
            .BuiltString);
        _chatService.PrintInChat(new SeStringBuilder().AddCommand("toggle", "Toggles a given profile for a given character. Use without arguments for help.")
    .BuiltString);
        return true;
    }

    private void Apply(string argument, bool toggle = false)
    {
        var argumentList = argument.Split(',', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (argumentList.Length != 2)
        {
            _chatService.PrintInChat(new SeStringBuilder().AddText($"Usage: /customize {(toggle ? "toggle" : "apply")} ").AddBlue("Character Name", true)
                .AddText(",")
                .AddRed("Profile Name", true)
                .BuiltString);
            _chatService.PrintInChat(new SeStringBuilder().AddText("    》 ")
                .AddBlue("Character Name", true).AddText("can be either full character name or one of the following:").BuiltString);
            _chatService.PrintInChat(new SeStringBuilder().AddText("    》 To apply to yourself: ").AddBlue("<me>").AddText(", ").AddBlue("self").BuiltString);
            _chatService.PrintInChat(new SeStringBuilder().AddText("    》 To apply to target: ").AddBlue("<t>").AddText(", ").AddBlue("target").BuiltString);
            return;
        }

        string charaName = "", profName = "";

        try
        {
            (charaName, profName) = argumentList switch { var a => (a[0].Trim(), a[1].Trim()) };

            charaName = charaName switch
            {
                "<me>" => _gameObjectService.GetCurrentPlayerName() ?? string.Empty,
                "self" => _gameObjectService.GetCurrentPlayerName() ?? string.Empty,
                "<t>" => _gameObjectService.GetCurrentPlayerTargetName() ?? string.Empty,
                "target" => _gameObjectService.GetCurrentPlayerTargetName() ?? string.Empty,
                _ => charaName,
            };

            if (!_profileManager.Profiles.Any())
            {
                _chatService.PrintInChat(
                    $"Can't {(toggle ? "toggle" : "apply")} profile \"{profName}\" for character \"{charaName}\" because no profiles exist", ChatService.ChatMessageColor.Error);
                return;
            }

            if (_profileManager.Profiles.Count(x => x.Name == profName && x.CharacterName == charaName) > 1)
            {
                _logger.Warning(
                    $"Found more than one profile matching profile \"{profName}\" and character \"{charaName}\". Using first match.");
            }

            var outProf = _profileManager.Profiles.FirstOrDefault(x => x.Name == profName && x.CharacterName == charaName);

            if (outProf == null)
            {
                _chatService.PrintInChat(
                    $"Can't {(toggle ? "toggle" : "apply")} profile \"{(string.IsNullOrWhiteSpace(profName) ? "empty (none provided)" : profName)}\" " +
                    $"for Character \"{(string.IsNullOrWhiteSpace(charaName) ? "empty (none provided)" : charaName)}\"\n" +
                    "Check if the profile and character names were provided correctly and said profile exists for chosen character", ChatService.ChatMessageColor.Error);
                return;
            }

            if (!toggle)
                _profileManager.SetEnabled(outProf, true);
            else
                _profileManager.SetEnabled(outProf, !outProf.Enabled);

            _chatService.PrintInChat(
                $"{outProf.Name} was successfully {(toggle ? "toggled" : "applied")} for {outProf.CharacterName}", ChatService.ChatMessageColor.Info);
        }
        catch (Exception e)
        {
            _chatService.PrintInChat($"Error while {(toggle ? "toggling" : "applying")} profile, details are available in dalamud log", ChatService.ChatMessageColor.Error);
            _logger.Error($"Error {(toggle ? "toggling" : "applying")} profile by command: \n" +
                            $"Profile name \"{(string.IsNullOrWhiteSpace(profName) ? "empty (none provided)" : profName)}\"\n" +
                            $"Character name \"{(string.IsNullOrWhiteSpace(charaName) ? "empty (none provided)" : charaName)}\"\n" +
                            $"Error: {e}");
        }
    }
}
