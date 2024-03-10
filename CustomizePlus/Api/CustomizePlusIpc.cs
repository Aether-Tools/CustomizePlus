using CustomizePlus.Core.Services;
using CustomizePlus.Profiles;
using Dalamud.Plugin;
using ECommons.EzIpcManager;
using OtterGui.Log;
using System;

namespace CustomizePlus.Api;

public partial class CustomizePlusIpc
{
    private readonly DalamudPluginInterface _pluginInterface;
    private readonly Logger _logger;
    private readonly HookingService _hookingService;
    private readonly ProfileManager _profileManager;

    /// <summary>
    /// Shows if IPC failed to initialize or any other unrecoverable fatal error occured.
    /// </summary>
    public bool IPCFailed { get; private set; }

    public CustomizePlusIpc(
        DalamudPluginInterface pluginInterface,
        Logger logger,
        HookingService hookingService,
        ProfileManager profileManager)
    {
        _pluginInterface = pluginInterface;
        _logger = logger;
        _hookingService = hookingService;
        _profileManager = profileManager;

        EzIPC.Init(this, "CustomizePlus");
    }
}
