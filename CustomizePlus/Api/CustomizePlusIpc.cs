using CustomizePlus.Core.Services;
using Dalamud.Plugin;
using OtterGui.Log;
using System;

namespace CustomizePlus.Api;

public partial class CustomizePlusIpc : IDisposable
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

        InitializeProviders();
    }

    private void InitializeProviders()
    {
        try
        {
            InitializeGeneralProviders();
        }
        catch(Exception ex)
        {
            _logger.Fatal($"Fatal error while initializing Customize+ IPC: {ex}");

            IPCFailed = true;

            DisposeProviders();
        }
    }

    private void DisposeProviders()
    {
        DisposeGeneralProviders();
    }

    public void Dispose()
    {
        DisposeProviders();
    }
}
