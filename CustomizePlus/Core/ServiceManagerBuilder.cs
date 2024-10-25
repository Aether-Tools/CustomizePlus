using CustomizePlus.Anamnesis;
using CustomizePlus.Api;
using CustomizePlus.Armatures.Events;
using CustomizePlus.Armatures.Services;
using CustomizePlus.Configuration.Data;
using CustomizePlus.Configuration.Services;
using CustomizePlus.Core.Events;
using CustomizePlus.Core.Services;
using CustomizePlus.Game.Events;
using CustomizePlus.Game.Services;
using CustomizePlus.Game.Services.GPose;
using CustomizePlus.Game.Services.GPose.ExternalTools;
using CustomizePlus.GameData.Services;
using CustomizePlus.Profiles;
using CustomizePlus.Profiles.Events;
using CustomizePlus.Templates;
using CustomizePlus.Templates.Events;
using CustomizePlus.UI;
using CustomizePlus.UI.Windows;
using CustomizePlus.UI.Windows.Controls;
using CustomizePlus.UI.Windows.MainWindow;
using CustomizePlus.UI.Windows.MainWindow.Tabs;
using CustomizePlus.UI.Windows.MainWindow.Tabs.Debug;
using CustomizePlus.UI.Windows.MainWindow.Tabs.Profiles;
using CustomizePlus.UI.Windows.MainWindow.Tabs.Templates;
using Dalamud.Plugin;
using Microsoft.Extensions.DependencyInjection;
using OtterGui.Classes;
using OtterGui.Log;
using OtterGui.Raii;
using OtterGui.Services;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Structs;

namespace CustomizePlus.Core;

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
            .AddConfigServices()
            .AddDataLoaders()
            .AddApi();

        DalamudServices.AddServices(services, pi);

        services.AddIServices(typeof(EquipItem).Assembly);
        services.AddIServices(typeof(Plugin).Assembly);
        services.AddIServices(typeof(ObjectManager).Assembly);
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
            .AddSingleton<TestingVersionNotifierService>();

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
            .AddSingleton<ObjectManager>();

        return services;
    }
}