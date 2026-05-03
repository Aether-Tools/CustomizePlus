using CustomizePlus.Configuration.Data;
using CustomizePlus.Core.Data;
using CustomizePlus.Core.Services;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.ImGuiNotification;
using Newtonsoft.Json.Linq;

namespace CustomizePlus.Configuration.Services;

public class ConfigurationMigrator
{
    private readonly MessageService _messageService; //we can't use popups here since they rely on PluginConfiguration and using them here hangs plugin loading
    private readonly SaveService _saveService;
    private readonly BackupService _backupService;
    private readonly Logger _logger;

    public ConfigurationMigrator(
        MessageService messageService,
        SaveService saveService,
        BackupService backupService,
        Logger logger)
    {
        _messageService = messageService;
        _saveService = saveService;
        _backupService = backupService;
        _logger = logger;
    }

    public void Migrate(PluginConfiguration config)
    {
        var configVersion = config.Version;

        if (configVersion >= Constants.ConfigurationVersion)
            return;

        //We no longer support migrations of any versions < 4
        if (configVersion < 4)
        {
            _messageService.NotificationMessage("Unsupported version of Customize+ configuration data detected. Check FAQ over at https://github.com/Aether-Tools/CustomizePlus for information.", NotificationType.Error);
            return;
        }

        // V4 to V5: Added ChildScaling field to BoneTransform
        if (configVersion == 4)
        {
            _logger.Information("Migrating configuration from V4 to V5 (ChildScaling feature)");
        }

        var data = JObject.Parse(File.ReadAllText(_saveService.FileNames.ConfigurationFile));

        if (configVersion == 5)
        {
            _logger.Information("Migrating configuration from V4 to V5");

            _backupService.CreateMigrationBackup("pre_filesystem_update",
                _saveService.FileNames.MigrationProfileFileSystem, _saveService.FileNames.MigrationTemplateFileSystem);

            if (data["UISettings"] is JObject uiSettings && uiSettings["DeleteTemplateModifier"] is JObject deleteModifier)
            {
                _logger.Debug("Migrating DeleteTemplateModifier");

                var modifier1 = deleteModifier["Modifier1"]?["Modifier"]?.Value<ushort>() ?? (ushort)VirtualKey.CONTROL;
                var modifier2 = deleteModifier["Modifier2"]?["Modifier"]?.Value<ushort>() ?? (ushort)VirtualKey.SHIFT;
                config.UISettings.DeleteModifier = new DoubleModifier((VirtualKey)modifier1, (VirtualKey)modifier2);

                config.Save();
            }
        }

        config.Version = Constants.ConfigurationVersion;
        return;
    }
}
