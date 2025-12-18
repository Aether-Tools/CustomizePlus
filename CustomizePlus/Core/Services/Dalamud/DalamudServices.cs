using Dalamud.Game;
using Dalamud.Game.ClientState.Objects;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.DependencyInjection;
using OtterGui.Services;

namespace CustomizePlus.Core.Services.Dalamud;

#pragma warning disable SeStringEvaluator

public class DalamudServices
{
    public static void AddServices(ServiceManager services, IDalamudPluginInterface pi)
    {
        services.AddExistingService(pi)
            .AddExistingService(pi.UiBuilder)
            .AddDalamudService<ISigScanner>(pi)
            .AddDalamudService<IFramework>(pi)
            .AddDalamudService<IObjectTable>(pi)
            .AddDalamudService<ICommandManager>(pi)
            .AddDalamudService<IChatGui>(pi)
            .AddDalamudService<IClientState>(pi)
            .AddDalamudService<IPlayerState>(pi)
            .AddDalamudService<IGameGui>(pi)
            .AddDalamudService<IGameInteropProvider>(pi)
            .AddDalamudService<IKeyState>(pi)
            .AddDalamudService<IDataManager>(pi)
            .AddDalamudService<IPluginLog>(pi)
            .AddDalamudService<ITargetManager>(pi)
            .AddDalamudService<INotificationManager>(pi)
            .AddDalamudService<IContextMenu>(pi)
            .AddDalamudService<ISeStringEvaluator>(pi);
    }
}