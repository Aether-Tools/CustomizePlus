using Dalamud.Plugin;
using Microsoft.Extensions.DependencyInjection;
using OtterGui.Classes;
using OtterGui.Log;
using CustomizePlus.Profiles;
using CustomizePlus.Core.Services;
using CustomizePlus.UI.Windows.MainWindow.Tabs.Debug;
using CustomizePlus.Game.Services;
using CustomizePlus.Configuration.Services;
using CustomizePlus.Templates;
using CustomizePlus.UI.Windows.MainWindow.Tabs.Templates;
using CustomizePlus.Armatures.Events;
using CustomizePlus.Configuration.Data;
using CustomizePlus.Core.Events;
using CustomizePlus.UI;
using CustomizePlus.UI.Windows.Controls;
using CustomizePlus.Anamnesis;
using CustomizePlus.Armatures.Services;
using CustomizePlus.UI.Windows.MainWindow.Tabs.Profiles;
using CustomizePlus.UI.Windows.MainWindow;
using CustomizePlus.Game.Events;
using CustomizePlus.UI.Windows;
using CustomizePlus.UI.Windows.MainWindow.Tabs;
using CustomizePlus.Templates.Events;
using CustomizePlus.Profiles.Events;
using CustomizePlus.Api.Compatibility;
using CustomizePlus.Game.Services.GPose;
using CustomizePlus.Game.Services.GPose.ExternalTools;
using CustomizePlus.GameData.Services;
using CustomizePlus.Configuration.Services.Temporary;

namespace CustomizePlus.Core;

public static class ServiceManager
{
    public static ServiceProvider CreateProvider(DalamudPluginInterface pi, Logger logger)
    {
        var services = new ServiceCollection()
            .AddSingleton(logger)
            .AddDalamud(pi)
            .AddCore()
            .AddEvents()
            .AddGPoseServices()
            .AddArmatureServices()
            .AddUI()
            .AddGameDataServices()
            .AddTemplateServices()
            .AddProfileServices()
            .AddGameServices()
            .AddConfigServices()
            .AddRestOfServices();
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
    }

    private static IServiceCollection AddDalamud(this IServiceCollection services, DalamudPluginInterface pluginInterface)
    {
        new DalamudServices(pluginInterface).AddServices(services);
        return services;
    }

    private static IServiceCollection AddGPoseServices(this IServiceCollection services)
    {
        services
            .AddSingleton<PosingModeDetectService>()
            .AddSingleton<GPoseService>()
            .AddSingleton<GPoseStateChanged>();
        return services;
    }

    private static IServiceCollection AddArmatureServices(this IServiceCollection services)
    {
        services
            .AddSingleton<ArmatureManager>();
        return services;
    }

    private static IServiceCollection AddUI(this IServiceCollection services)
    {
        services
            .AddSingleton<TemplateCombo>()
            .AddSingleton<PluginStateBlock>()
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

    private static IServiceCollection AddEvents(this IServiceCollection services)
    {
        services
            .AddSingleton<ProfileChanged>()
            .AddSingleton<TemplateChanged>()
            .AddSingleton<ReloadEvent>()
            .AddSingleton<ArmatureChanged>();

        return services;
    }

    private static IServiceCollection AddCore(this IServiceCollection services)
    {
        services
            .AddSingleton<HookingService>()
            .AddSingleton<ChatService>()
            .AddSingleton<CommandService>()
            .AddSingleton<SaveService>()
            .AddSingleton<FilenameService>()
            .AddSingleton<BackupService>()
            .AddSingleton<FantasiaPlusDetectService>()
            .AddSingleton<FrameworkManager>();

        return services;
    }

    private static IServiceCollection AddRestOfServices(this IServiceCollection services) //temp
    {
        services
            .AddSingleton<PoseFileBoneLoader>()
            .AddSingleton<CustomizePlusIpc>();

        return services;
    }

    private static IServiceCollection AddConfigServices(this IServiceCollection services)
    {
        services
            .AddSingleton<PluginConfiguration>()
            .AddSingleton<ConfigurationMigrator>()
            .AddSingleton<FantasiaPlusConfigMover>();

        return services;
    }

    private static IServiceCollection AddGameServices(this IServiceCollection services)
    {
        services
            .AddSingleton<GameObjectService>()
            .AddSingleton<GameStateService>();

        return services;
    }

    private static IServiceCollection AddProfileServices(this IServiceCollection services)
    {
        services
            .AddSingleton<ProfileManager>()
            .AddSingleton<ProfileFileSystem>()
            .AddSingleton<TemplateEditorManager>();

        return services;
    }

    private static IServiceCollection AddTemplateServices(this IServiceCollection services)
    {
        services
            .AddSingleton<TemplateManager>()
            .AddSingleton<TemplateFileSystem>()
            .AddSingleton<TemplateEditorManager>();

        return services;
    }

    private static IServiceCollection AddGameDataServices(this IServiceCollection services)
    {
        services
            .AddSingleton<CutsceneService>()
            .AddSingleton<GameEventManager>()
            .AddSingleton<ActorService>()
            .AddSingleton<ObjectManager>();

        return services;
    }
}