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
using ECommons.Commands;
using ECommons.Configuration;
using OtterGui;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using CustomizePlus.Configuration.Data;
using CustomizePlus.Core.Extensions;
using CustomizePlus.Templates;
using CustomizePlus.Profiles;
using CustomizePlus.Armatures.Services;

namespace CustomizePlus;

public sealed class Plugin : IDalamudPlugin
{
#if DEBUG
    public static readonly string Version = $"{ThisAssembly.Git.Commit}+{ThisAssembly.Git.Sha} [DEBUG]";
#else
    public static readonly string Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty;
#endif

    private readonly ServiceManager _services;

    public static readonly Logger Logger = new(); //for loggin in static classes/methods

    public Plugin(IDalamudPluginInterface pluginInterface)
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

            Logger.Information($"Customize+ {Version} ({ThisAssembly.Git.Commit}+{ThisAssembly.Git.Sha}) [FantasiaPlus] started");
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