using System;
using CustomizePlusPlus.Api;
using CustomizePlusPlus.Core;
using CustomizePlusPlus.Core.Helpers;
using CustomizePlusPlus.Core.Services;
using CustomizePlusPlus.UI;
using Dalamud.Plugin;
using ECommonsLite;
using OtterGui.Log;
using OtterGui.Services;
using Penumbra.GameData.Actors;

namespace CustomizePlusPlus;

public sealed class Plugin : IDalamudPlugin
{
    private readonly ServiceManager _services;

    public static readonly Logger Logger = new(); //for loggin in static classes/methods

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        try
        {
            ECommonsLiteMain.Init(pluginInterface, this);

            _services = ServiceManagerBuilder.CreateProvider(pluginInterface, Logger);

            _services.GetService<ActorManager>(); //needs to be initialized early for config to be read properly

            _services.GetService<UserNotifierService>();
            _services.GetService<CustomizePlusIpc>();
            _services.GetService<CPlusWindowSystem>();
            _services.GetService<CommandService>();

            Logger.Information($"Customize++ {VersionHelper.Version} ({ThisAssembly.Git.Commit}+{ThisAssembly.Git.Sha}) [FantasiaPlus] started");
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

        ECommonsLiteMain.Dispose();
    }
}