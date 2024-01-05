using Dalamud.Interface.Internal.Notifications;
using Newtonsoft.Json;
using OtterGui.Classes;
using OtterGui.Log;
using System;
using System.Collections.Generic;
using System.IO;
using CustomizePlus.Core.Data;
using CustomizePlus.Core.Services;
using CustomizePlus.Configuration.Helpers;
using CustomizePlus.Configuration.Data;
using CustomizePlus.Core.Events;
using CustomizePlus.Configuration.Data.Version3;

namespace CustomizePlus.Configuration.Services;

public class ConfigurationMigrator
{
    private readonly SaveService _saveService;
    private readonly BackupService _backupService;
    private readonly MessageService _messageService;
    private readonly Logger _logger;
    private readonly ReloadEvent _reloadEvent;

    public ConfigurationMigrator(
        SaveService saveService,
        BackupService backupService,
        MessageService messageService,
        Logger logger,
        ReloadEvent reloadEvent
        )
    {
        _saveService = saveService;
        _backupService = backupService;
        _messageService = messageService;
        _logger = logger;
        _reloadEvent = reloadEvent;
    }

    public void Migrate(PluginConfiguration config)
    {
        var configVersion = config.Version;

        if (configVersion >= Constants.ConfigurationVersion)
            return;

        //V3 migration code
        if (configVersion < 3)
        {
            _messageService.NotificationMessage($"Unable to migrate your Customize+ configuration because it is too old. Manually install latest version of Customize+ 1.x to migrate your configuration to supported version first.", NotificationType.Error);
            return;
        }

        MigrateV3ToV4();
        // /V3 migration code

        //I'm sorry, I'm too lazy so v3's enable root position setting is not getting migrated for now
        //MigrateV3ToV4(configVersion);

        config.Version = Constants.ConfigurationVersion;
        _saveService.ImmediateSave(config);
    }

    private void MigrateV3ToV4()
    {
        _backupService.CreateV3Backup();

        //I'm sorry, I'm too lazy so v3's enable root position setting is not getting migrated

        var usedGuids = new HashSet<Guid>();
        foreach (var file in Directory.EnumerateFiles(_saveService.FileNames.ConfigDirectory, "*.profile", SearchOption.TopDirectoryOnly))
        {
            _logger.Debug($"Migrating v3 profile {file}");

            var legacyProfile = JsonConvert.DeserializeObject<Version3Profile>(File.ReadAllText(file));
            if (legacyProfile == null)
                continue;

            _logger.Debug($"v3 profile {file} loaded as {legacyProfile.ProfileName}");

            (var profile, var template) = V3ProfileToV4Converter.Convert(legacyProfile);

            //regenerate guids just to be safe
            do
            {
                profile.UniqueId = Guid.NewGuid();
            }
            while (profile.UniqueId == Guid.Empty || usedGuids.Contains(profile.UniqueId));
            usedGuids.Add(profile.UniqueId);

            do
            {
                template.UniqueId = Guid.NewGuid();
            }
            while (template.UniqueId == Guid.Empty || usedGuids.Contains(template.UniqueId));
            usedGuids.Add(template.UniqueId);

            _saveService.ImmediateSave(template);
            _saveService.ImmediateSave(profile);

            _logger.Debug($"Migrated v3 profile {legacyProfile.ProfileName} to profile {profile.UniqueId} and template {template.UniqueId}");
            File.Delete(file);
        }

        _reloadEvent.Invoke(ReloadEvent.Type.ReloadAll);
    }
}
