using System;
using CustomizePlusPlus.Core.Helpers;
using CustomizePlusPlus.Core.Services.Dalamud;
using CustomizePlusPlus.Game.Services;
using CustomizePlusPlus.UI.Windows;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace CustomizePlusPlus.Core.Services;

public class UserNotifierService : IDisposable
{
    private readonly IClientState _clientState;
    private readonly ChatService _chatService;
    private readonly PopupSystem _popupSystem;

    public UserNotifierService(
        IClientState clientState,
        ChatService chatService,
        PopupSystem popupSystem)
    {
        _clientState = clientState;
        _chatService = chatService;
        _popupSystem = popupSystem;

        NotifyUser(true);

        _clientState.Login += OnLogin;
    }

    public void Dispose()
    {
        _clientState.Login -= OnLogin;
    }

    private void OnLogin()
    {
        NotifyUser(true);
    }

    private void NotifyUser(bool displayOptionalMessages = false)
    {
        if (VersionHelper.IsTesting)
            _chatService.PrintInChat($"You are running testing version of Customize+! Some features like integration with other plugins might not function correctly.",
                ChatService.ChatMessageColor.Warning);

    }
}
