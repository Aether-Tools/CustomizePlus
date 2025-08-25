using CustomizePlusPlus.Anamnesis;
using CustomizePlusPlus.Api;
using CustomizePlusPlus.Armatures.Events;
using CustomizePlusPlus.Armatures.Services;
using CustomizePlusPlus.Configuration.Data;
using CustomizePlusPlus.Configuration.Services;
using CustomizePlusPlus.Core.Events;
using CustomizePlusPlus.Core.Services;
using CustomizePlusPlus.Core.Services.Dalamud;
using CustomizePlusPlus.Game.Events;
using CustomizePlusPlus.Game.Services;
using CustomizePlusPlus.Game.Services.GPose;
using CustomizePlusPlus.Game.Services.GPose.ExternalTools;
using CustomizePlusPlus.GameData.Services;
using CustomizePlusPlus.Interop.Ipc;
using CustomizePlusPlus.Profiles;
using CustomizePlusPlus.Profiles.Events;
using CustomizePlusPlus.Templates;
using CustomizePlusPlus.Templates.Events;
using CustomizePlusPlus.UI;
using CustomizePlusPlus.UI.Windows;
using CustomizePlusPlus.UI.Windows.Controls;
using CustomizePlusPlus.UI.Windows.MainWindow;
using CustomizePlusPlus.UI.Windows.MainWindow.Tabs;
using CustomizePlusPlus.UI.Windows.MainWindow.Tabs.Debug;
using CustomizePlusPlus.UI.Windows.MainWindow.Tabs.Profiles;
using CustomizePlusPlus.UI.Windows.MainWindow.Tabs.Templates;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.DependencyInjection;
using OtterGui.Classes;
using OtterGui.Log;
using OtterGui.Raii;
using OtterGui.Services;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;
using System.Collections.Generic;

namespace CustomizePlusPlus.Core;

public static class ServiceManagerBuilder
{
    public static ServiceManager CreateProvider(IDalamudPluginInterface pi, Logger logger)
    {
        EventWrapperBase.ChangeLogger(logger);

        var services = new ServiceManager(logger)
            .AddExistingService(logger)
            .AddCore()
            .AddEvents()
            .AddGPoseServices()
            .AddArmatureServices()
            .AddUI()
            .AddGameDataServices()
            .AddTemplateServices()
            .AddProfileServices()
            .AddGameServices()
            .AddInterop()
            .AddConfigServices()
            .AddDataLoaders()
            .AddApi();

        DalamudServices.AddServices(services, pi);

        services.AddIServices(typeof(EquipItem).Assembly);
        services.AddIServices(typeof(Plugin).Assembly);
        services.AddIServices(typeof(CutsceneService).Assembly);
        services.AddIServices(typeof(ImRaii).Assembly);

        services.CreateProvider();

        return services;
    }

    private static ServiceManager AddGPoseServices(this ServiceManager services)
    {
        services
            .AddSingleton<PosingModeDetectService>()
            .AddSingleton<GPoseService>()
            .AddSingleton<GPoseStateChanged>();
        return services;
    }

    private static ServiceManager AddArmatureServices(this ServiceManager services)
    {
        services
            .AddSingleton<ArmatureManager>();
        return services;
    }

    private static ServiceManager AddUI(this ServiceManager services)
    {
        services
            .AddSingleton<TemplateCombo>()
            .AddSingleton<PluginStateBlock>()
            .AddSingleton<ActorAssignmentUi>()
            .AddSingleton<SettingsTab>()
            // template
            .AddSingleton<TemplatesTab>()
            .AddSingleton<TemplateFileSystemSelector>()
            .AddSingleton<TemplatePanel>()
            .AddSingleton<BoneEditorPanel>()
            // /template
            // profile
            .AddSingleton<ProfilesTab>()
            .AddSingleton<ProfileFileSystemSelector>()
            .AddSingleton<ProfilePanel>()
            // /profile
            // messages
            .AddSingleton<MessageService>()
            .AddSingleton<MessagesTab>()
            // /messages
            //
            .AddSingleton<IPCTestTab>()
            .AddSingleton<StateMonitoringTab>()
            //
            .AddSingleton<PopupSystem>()
            .AddSingleton<CPlusChangeLog>()
            .AddSingleton<CPlusWindowSystem>()
            .AddSingleton<MainWindow>();

        return services;
    }

    private static ServiceManager AddEvents(this ServiceManager services)
    {
        services
            .AddSingleton<ProfileChanged>()
            .AddSingleton<TemplateChanged>()
            .AddSingleton<ReloadEvent>()
            .AddSingleton<ArmatureChanged>();

        return services;
    }

    private static ServiceManager AddCore(this ServiceManager services)
    {
        services
            .AddSingleton<HookingService>()
            .AddSingleton<ChatService>()
            .AddSingleton<CommandService>()
            .AddSingleton<SaveService>()
            .AddSingleton<FilenameService>()
            .AddSingleton<BackupService>()
            .AddSingleton<FrameworkManager>()
            .AddSingleton<SupportLogBuilderService>()
            .AddSingleton<UserNotifierService>();

        return services;
    }

    // might be scuffed, not sure
    private static ServiceManager AddInterop(this ServiceManager services)
    {
        services
            .AddSingleton<PenumbraIpcHandler>()
            .AddSingleton<PcpService>()
            .AddSingleton<IpcHandler>(provider =>
            {
                var subscribers = new List<IIpcSubscriber>
                {
                provider.GetRequiredService<PenumbraIpcHandler>(),
                };

                return new IpcHandler(subscribers);
            });

        return services;
    }

    private static ServiceManager AddDataLoaders(this ServiceManager services)
    {
        services
            .AddSingleton<PoseFileBoneLoader>();

        return services;
    }

    private static ServiceManager AddApi(this ServiceManager services)
    {
        services
            .AddSingleton<CustomizePlusIpc>();

        return services;
    }

    private static ServiceManager AddConfigServices(this ServiceManager services)
    {
        services
            .AddSingleton<PluginConfiguration>()
            .AddSingleton<ConfigurationMigrator>();

        return services;
    }

    private static ServiceManager AddGameServices(this ServiceManager services)
    {
        services
            .AddSingleton<GameObjectService>()
            .AddSingleton<GameStateService>();

        return services;
    }

    private static ServiceManager AddProfileServices(this ServiceManager services)
    {
        services
            .AddSingleton<ProfileManager>()
            .AddSingleton<ProfileFileSystem>()
            .AddSingleton<TemplateEditorManager>();

        return services;
    }

    private static ServiceManager AddTemplateServices(this ServiceManager services)
    {
        services
            .AddSingleton<TemplateManager>()
            .AddSingleton<TemplateFileSystem>()
            .AddSingleton<TemplateEditorManager>();

        return services;
    }

    private static ServiceManager AddGameDataServices(this ServiceManager services)
    {
        services
            .AddSingleton<ActorManager>()
            .AddSingleton<CutsceneService>()
            .AddSingleton<GameEventManager>()
            .AddSingleton(p => new CutsceneResolver(idx => (short)p.GetRequiredService<CutsceneService>().GetParentIndex(idx)))
            .AddSingleton<ActorObjectManager>();

        return services;
    }
}