using System;
using System.Reflection;
using Dalamud.Plugin;
using OtterGui.Log;
using CustomizePlus.Core.Services;
using CustomizePlus.UI;
using CustomizePlus.Core;
using CustomizePlus.Configuration.Services.Temporary;
using OtterGui.Services;
using CustomizePlus.Api;
using ECommons;

namespace CustomizePlus;

public sealed class Plugin : IDalamudPlugin
{
#if DEBUG
    public static readonly string Version = $"{Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty} [DEBUG]";
#else
    public static readonly string Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty;
#endif

    private readonly ServiceManager _services;

    public static readonly Logger Logger = new(); //for loggin in static classes/methods

    public Plugin(DalamudPluginInterface pluginInterface)
    {
        try
        {
            ECommonsMain.Init(pluginInterface, this);

            _services = ServiceManagerBuilder.CreateProvider(pluginInterface, Logger);

            //temporary
            var v3ConfigFixer = _services.GetService<Version3ConfigFixer>();
            v3ConfigFixer.FixV3ConfigIfNeeded();

            _services.GetService<CustomizePlusIpc>();
            _services.GetService<CPlusWindowSystem>();
            _services.GetService<CommandService>();

            Logger.Information($"Customize+ v{Version} [FantasiaPlus] started");
        }
        catch (Exception ex)
        {
            Logger.Error($"Error instantiating plugin: {ex}");

            Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        _services?.Dispose();

        ECommonsMain.Dispose();
    }
}