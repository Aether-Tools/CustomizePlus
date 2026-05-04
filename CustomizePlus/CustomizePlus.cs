using CustomizePlus.Api;
using CustomizePlus.Armatures.Data;
using CustomizePlus.Configuration.Data;
using CustomizePlus.Core;
using CustomizePlus.Core.Helpers;
using CustomizePlus.Core.Services;
using Dalamud.Plugin;
using ECommonsLite;
using Penumbra.GameData.Actors;

namespace CustomizePlus;

public sealed class CustomizePlus : IDalamudPlugin
{
    private readonly ServiceManager _services;

    public static readonly MainLogger Logger = new("CustomizePlus"); //for loggin in static classes/methods
    public static MessageService Messager { get; private set; } = null!;

    public CustomizePlus(IDalamudPluginInterface pluginInterface)
    {
        try
        {
            ECommonsLiteMain.Init(pluginInterface, this);
            InteropAlloc.Init();

            _services = ServiceManagerBuilder.CreateProvider(pluginInterface, Logger);
            Messager = _services.GetService<MessageService>();
            foreach (var _ in _services.GetServicesImplementing<IHookService>())
                ;
            _ = _services.GetService<ImSharpDalamudContext>();
            _services.EnsureRequiredServices();

            _services.GetService<PluginConfiguration>(); //initialize early

            _services.GetService<IpcHandler>().Initialize();
            _services.GetService<PcpService>();
            _services.GetService<ActorManager>(); //needs to be initialized early for config to be read properly

            _services.GetService<UserNotifierService>();
            _services.GetService<CustomizePlusIpc>();
            _services.GetService<CPlusWindowSystem>();
            _services.GetService<CommandService>();

            Logger.Information($"Customize+ {VersionHelper.Version} ({ThisAssembly.Git.Commit}+{ThisAssembly.Git.Sha}) [FantasiaPlus] started");
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