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

        //We no longer support migrations of any versions < 4
        if (configVersion < 3)
        {
            _messageService.NotificationMessage($"Unable to migrate your Customize+ configuration because it is too old. Manually install latest version of Customize+ 1.x to migrate your configuration to supported version first.", NotificationType.Error);
            return;
        }

        if (configVersion < 4)
        {
            _messageService.NotificationMessage($"Unable to migrate your Customize+ configuration because it is too old. Manually install Customize+ 2.0.6.5 to migrate your configuration to supported version first.", NotificationType.Error);
            return;
        }

        throw new NotImplementedException();

        config.Version = Constants.ConfigurationVersion;
        _saveService.ImmediateSave(config);
    }
}
