using CustomizePlus.Armatures.Events;
using CustomizePlus.Core.Services;
using CustomizePlus.Game.Services;
using CustomizePlus.GameData.Services;
using CustomizePlus.Profiles;
using CustomizePlus.Profiles.Events;
using CustomizePlus.Templates.Data;
using CustomizePlus.Templates.Events;
using Dalamud.Plugin;
using ECommonsLite.EzIpcManager;
using OtterGui.Log;
using System;

namespace CustomizePlus.Api;

/// <summary>
/// Customize+ IPC.
/// All of the function/event names start with "CustomizePlus." prefix.
/// For example: CustomizePlus.Profile.GetList.
/// While Customize+ is using EzIPC to make it easier to work with IPC,
/// you are not required to use it to interact with the plugin.
/// </summary>
public partial class CustomizePlusIpc : IDisposable
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly Logger _logger;
    private readonly HookingService _hookingService;
    private readonly ProfileManager _profileManager;
    private readonly GameObjectService _gameObjectService;
    private readonly ProfileFileSystem _profileFileSystem;
    private readonly CutsceneService _cutsceneService;

    private readonly ArmatureChanged _armatureChangedEvent;
    private readonly TemplateChanged _templateChangedEvent;

    /// <summary>
    /// Shows if IPC failed to initialize or any other unrecoverable fatal error occured.
    /// </summary>
    public bool IPCFailed { get; private set; }

    public CustomizePlusIpc(
        IDalamudPluginInterface pluginInterface,
        Logger logger,
        HookingService hookingService,
        ProfileManager profileManager,
        GameObjectService gameObjectService,
        ProfileFileSystem profileFileSystem,
        CutsceneService cutsceneService,
        ArmatureChanged armatureChangedEvent,
        TemplateChanged templateChangedEvent)
    {
        _pluginInterface = pluginInterface;
        _logger = logger;
        _hookingService = hookingService;
        _profileManager = profileManager;
        _gameObjectService = gameObjectService;
        _profileFileSystem = profileFileSystem;
        _cutsceneService = cutsceneService;

        _armatureChangedEvent = armatureChangedEvent;
        _templateChangedEvent = templateChangedEvent;

        EzIPC.Init(this, "CustomizePlus");

        _armatureChangedEvent.Subscribe(OnArmatureChanged, ArmatureChanged.Priority.CustomizePlusIpc);
        _templateChangedEvent.Subscribe(OnTemplateChanged, TemplateChanged.Priority.CustomizePlusIpc);
    }

    public void Dispose()
    {
        _armatureChangedEvent.Unsubscribe(OnArmatureChanged);
        _templateChangedEvent.Unsubscribe(OnTemplateChanged);
    }
}
