using System;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using OtterGui.Log;
using CustomizePlus.Game.Events;

namespace CustomizePlus.Game.Services.GPose;

public class GPoseService : IDisposable
{
    private readonly ChatService _chatService;
    private readonly GPoseStateChanged _event;
    private readonly Logger _logger;

    private Hook<EnterGPoseDelegate>? _enterGPoseHook;
    private Hook<ExitGPoseDelegate>? _exitGPoseHook;

    private bool _fakeGPose;
    public GPoseState GPoseState { get; private set; }
    public bool IsInGPose => GPoseState == GPoseState.Inside;

    public bool FakeGPose
    {
        get => _fakeGPose;

        set
        {
            if (value != _fakeGPose)
            {
                if (!value)
                {
                    _fakeGPose = false;
                    HandleGPoseChange(GPoseState.Exiting);
                    HandleGPoseChange(GPoseState.Outside);
                }
                else
                {
                    HandleGPoseChange(GPoseState.Inside);
                    _fakeGPose = true;
                }
            }
        }
    }

    public unsafe GPoseService(
        IClientState clientState,
        IGameInteropProvider hooker,
        ChatService chatService,
        GPoseStateChanged @event,
        Logger logger)
    {
        _chatService = chatService;
        _event = @event;
        _logger = logger;

        GPoseState = clientState.IsGPosing ? GPoseState.Inside : GPoseState.Outside;

        var uiModule = Framework.Instance()->GetUIModule();
        var enterGPoseAddress = (nint)uiModule->VirtualTable->EnterGPose;
        var exitGPoseAddress = (nint)uiModule->VirtualTable->ExitGPose;

        _enterGPoseHook = hooker.HookFromAddress<EnterGPoseDelegate>(enterGPoseAddress, EnteringGPoseDetour);
        _enterGPoseHook.Enable();

        _exitGPoseHook = hooker.HookFromAddress<ExitGPoseDelegate>(exitGPoseAddress, ExitingGPoseDetour);
        _exitGPoseHook.Enable();
    }

    private void ExitingGPoseDetour(nint addr)
    {
        if (HandleGPoseChange(GPoseState.AttemptExit))
        {
            HandleGPoseChange(GPoseState.Exiting);
            _exitGPoseHook!.Original.Invoke(addr);
            HandleGPoseChange(GPoseState.Outside);
        }
    }

    private bool EnteringGPoseDetour(nint addr)
    {
        var didEnter = _enterGPoseHook!.Original.Invoke(addr);
        if (didEnter)
        {
            _fakeGPose = false;
            HandleGPoseChange(GPoseState.Inside);
        }

        return didEnter;
    }

    private bool HandleGPoseChange(GPoseState state)
    {
        if (state == GPoseState || _fakeGPose)
        {
            return true;
        }

        GPoseState = state;


        switch (state)
        {
            case GPoseState.Inside:
                _event.Invoke(GPoseStateChanged.Type.Entered);
                break;
            case GPoseState.AttemptExit:
                _event.Invoke(GPoseStateChanged.Type.AttemptingExit);
                break;
            case GPoseState.Exiting:
                _event.Invoke(GPoseStateChanged.Type.Exiting);
                break;
            case GPoseState.Outside:
                _event.Invoke(GPoseStateChanged.Type.Exited);
                break;
        }

        return true;
    }

    public void Dispose()
    {
        _exitGPoseHook?.Dispose();
        _enterGPoseHook?.Dispose();
    }

    private delegate void ExitGPoseDelegate(nint addr);

    private delegate bool EnterGPoseDelegate(nint addr);
}

public enum GPoseState
{
    Inside,
    AttemptExit,
    Exiting,
    Outside
}