using CustomizePlus.Core.Services;
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

    /// <summary>
    /// Shows if IPC failed to initialize or any other unrecoverable fatal error occured.
    /// </summary>
    public bool IPCFailed { get; private set; }

    public CustomizePlusIpc(
        DalamudPluginInterface pluginInterface,
        Logger logger,
        HookingService hookingService)
    {
        _pluginInterface = pluginInterface;
        _logger = logger;
        _hookingService = hookingService;

        EzIPC.Init(this, "CustomizePlus");
    }
}
