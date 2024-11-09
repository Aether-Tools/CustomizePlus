using System;
using CustomizePlus.Core.Helpers;
using CustomizePlus.Game.Services;
using Dalamud.Plugin.Services;

namespace CustomizePlus.Core.Services;

public class TestingVersionNotifierService : IDisposable
{
    private readonly IClientState _clientState;
    private readonly ChatService _chatService;

    public TestingVersionNotifierService(IClientState clientState, ChatService chatService)
    {
        _clientState = clientState;
        _chatService = chatService;

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
    }
}
