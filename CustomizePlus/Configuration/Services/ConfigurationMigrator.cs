﻿using Newtonsoft.Json;
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
using Dalamud.Interface.ImGuiNotification;

namespace CustomizePlus.Configuration.Services;

public class ConfigurationMigrator
{
    private readonly SaveService _saveService;
    private readonly BackupService _backupService;
    private readonly MessageService _messageService; //we can't use popups here since they rely on PluginConfiguration and using them here hangs plugin loading
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

        config.Version = Constants.ConfigurationVersion;
        _saveService.ImmediateSave(config);
    }

    private void MigrateV3ToV4()
    {
        _backupService.CreateV3Backup();

        //I'm sorry, I'm too lazy so v3's enable root position setting is not getting migrated

        bool anyMigrationFailures = false;

        var usedGuids = new HashSet<Guid>();
        foreach (var file in Directory.EnumerateFiles(_saveService.FileNames.ConfigDirectory, "*.profile", SearchOption.TopDirectoryOnly))
        {
            try
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

                _saveService.ImmediateSaveSync(template);
                _saveService.ImmediateSaveSync(profile);

                _logger.Debug($"Migrated v3 profile {legacyProfile.ProfileName} to profile {profile.UniqueId} and template {template.UniqueId}");
                File.Delete(file);
            }
            catch(Exception ex)
            {
                anyMigrationFailures = true;
                _logger.Error($"Error while migrating {file}: {ex}");
            }
        }

        if (anyMigrationFailures)
            _messageService.NotificationMessage($"Some of your Customize+ profiles failed to migrate correctly.\nDetails have been printed to Dalamud log (/xllog in chat).", NotificationType.Error);

        _reloadEvent.Invoke(ReloadEvent.Type.ReloadAll);
    }
}
