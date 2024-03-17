using CustomizePlus.Armatures.Events;
using CustomizePlus.Core.Services;
using CustomizePlus.Game.Services;
using CustomizePlus.Profiles;
using CustomizePlus.Profiles.Events;
using Dalamud.Plugin;
using ECommons.EzIpcManager;
using OtterGui.Log;
using System;

namespace CustomizePlus.Api;

public partial class CustomizePlusIpc : IDisposable
{
    private readonly DalamudPluginInterface _pluginInterface;
    private readonly Logger _logger;
    private readonly HookingService _hookingService;
    private readonly ProfileManager _profileManager;
    private readonly GameObjectService _gameObjectService;

    private readonly ProfileChanged _profileChangedEvent;
    private readonly ArmatureChanged _armatureChangedEvent;

    /// <summary>
    /// Shows if IPC failed to initialize or any other unrecoverable fatal error occured.
    /// </summary>
    public bool IPCFailed { get; private set; }

    public CustomizePlusIpc(
        DalamudPluginInterface pluginInterface,
        Logger logger,
        HookingService hookingService,
        ProfileManager profileManager,
        GameObjectService gameObjectService,
        ArmatureChanged armatureChangedEvent,
        ProfileChanged profileChangedEvent)
    {
        _pluginInterface = pluginInterface;
        _logger = logger;
        _hookingService = hookingService;
        _profileManager = profileManager;
        _gameObjectService = gameObjectService;


        _profileChangedEvent = profileChangedEvent;
        _armatureChangedEvent = armatureChangedEvent;

        EzIPC.Init(this, "CustomizePlus");

        _profileChangedEvent.Subscribe(OnProfileChange, ProfileChanged.Priority.CustomizePlusIpc);
        _armatureChangedEvent.Subscribe(OnArmatureChanged, ArmatureChanged.Priority.CustomizePlusIpc);
    }

    public void Dispose()
    {
        _profileChangedEvent.Unsubscribe(OnProfileChange);
        _armatureChangedEvent.Unsubscribe(OnArmatureChanged);
    }
}
