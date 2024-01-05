using Dalamud.Game;
using Dalamud.Game.ClientState.Objects;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CustomizePlus.Core.Services;

public class DalamudServices
{
    [PluginService]
    [RequiredVersion("1.0")]
    public DalamudPluginInterface PluginInterface { get; private set; } = null!;

    [PluginService]
    [RequiredVersion("1.0")]
    public ISigScanner SigScanner { get; private set; } = null!;

    [PluginService]
    public IFramework Framework { get; private set; } = null!;

    [PluginService]
    [RequiredVersion("1.0")]
    public IObjectTable ObjectTable { get; private set; } = null!;

    [PluginService]
    [RequiredVersion("1.0")]
    public ICommandManager CommandManager { get; private set; } = null!;

    [PluginService]
    [RequiredVersion("1.0")]
    public IChatGui ChatGui { get; private set; } = null!;

    [PluginService]
    [RequiredVersion("1.0")]
    public IClientState ClientState { get; private set; } = null!;

    [PluginService]
    [RequiredVersion("1.0")]
    public IGameGui GameGui { get; private set; } = null!;

    [PluginService]
    [RequiredVersion("1.0")]
    internal IGameInteropProvider Hooker { get; private set; } = null!;

    [PluginService]
    [RequiredVersion("1.0")]
    public IKeyState KeyState { get; private set; } = null!;

    //GameData
    [PluginService]
    [RequiredVersion("1.0")]
    public IDataManager DataManager { get; private set; } = null!;

    [PluginService]
    [RequiredVersion("1.0")]
    public IPluginLog PluginLog { get; private set; } = null!;

    /*[PluginService]
    [RequiredVersion("1.0")]
    public ICondition Condition { get; private set; } = null!;*/

    [PluginService]
    [RequiredVersion("1.0")]
    public ITargetManager TargetManager { get; private set; } = null!;

    public DalamudServices(DalamudPluginInterface pluginInterface)
    {
        pluginInterface.Inject(this);
    }

    public void AddServices(IServiceCollection services)
    {
        services
            .AddSingleton(PluginInterface)
            .AddSingleton(SigScanner)
            .AddSingleton(Framework)
            .AddSingleton(ObjectTable)
            .AddSingleton(CommandManager)
            .AddSingleton(ChatGui)
            .AddSingleton(ClientState)
            .AddSingleton(GameGui)
            .AddSingleton(Hooker)
            .AddSingleton(KeyState)
            .AddSingleton(this)
            .AddSingleton(PluginInterface.UiBuilder)
            .AddSingleton(DataManager)
            .AddSingleton(PluginLog)
            //.AddSingleton(Condition)
            .AddSingleton(TargetManager);
    }
}