using System;
using CustomizePlus.Core.Helpers;
using CustomizePlus.Game.Services;
using CustomizePlus.UI.Windows;
using Dalamud.Plugin.Services;

namespace CustomizePlus.Core.Services;

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
