using System;
using CustomizePlus.Core.Helpers;
using CustomizePlus.Core.Services.Dalamud;
using CustomizePlus.Game.Services;
using Dalamud.Plugin.Services;

namespace CustomizePlus.Core.Services;

public class UserNotifierService : IDisposable
{
    private readonly IClientState _clientState;
    private readonly ChatService _chatService;
    private readonly DalamudBranchService _dalamudBranchService;

    public UserNotifierService(
        IClientState clientState,
        ChatService chatService,
        DalamudBranchService dalamudBranchService)
    {
        _clientState = clientState;
        _chatService = chatService;
        _dalamudBranchService = dalamudBranchService;

        OnLogin();

        _clientState.Login += OnLogin;
    }

    public void Dispose()
    {
        _clientState.Login -= OnLogin;
    }

    private void OnLogin()
    {
        if (VersionHelper.IsTesting)
            _chatService.PrintInChat($"You are running testing version of Customize+! Some features like integration with other plugins might not function correctly.",
                ChatService.ChatMessageColor.Warning);

        if (_dalamudBranchService.CurrentBranch != DalamudBranchService.DalamudBranch.Release)
            _chatService.PrintInChat($"You are running development or testing version of Dalamud. This is not supported and might be actively prevented in the future.",
                ChatService.ChatMessageColor.Error);
    }
}
