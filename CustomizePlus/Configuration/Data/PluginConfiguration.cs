using System;
using System.Collections.Generic;
using System.IO;
using Dalamud.Configuration;
using Dalamud.Interface.Internal.Notifications;
using Newtonsoft.Json;
using OtterGui.Classes;
using OtterGui.Log;
using OtterGui.Widgets;
using ErrorEventArgs = Newtonsoft.Json.Serialization.ErrorEventArgs;
using CustomizePlus.Core.Services;
using CustomizePlus.Core.Data;
using CustomizePlus.Configuration.Services;
using CustomizePlus.Game.Services;
using CustomizePlus.UI.Windows;

namespace CustomizePlus.Configuration.Data;

[Serializable]
public class PluginConfiguration : IPluginConfiguration, ISavable
{
    public const int CurrentVersion = Constants.ConfigurationVersion;

    public int Version { get; set; } = CurrentVersion;

    public bool PluginEnabled { get; set; } = true;

    public bool DebuggingModeEnabled { get; set; }

    /// <summary>
    /// Id of the default profile applied to all characters without any profile. Can be set to Empty to disable this feature.
    /// </summary>
    public Guid DefaultProfile { get; set; } = Guid.Empty;

    [Serializable]
    public class ChangelogSettingsEntries
    {
        public int LastSeenVersion { get; set; } = CPlusChangeLog.LastChangelogVersion;
        public ChangeLogDisplayType ChangeLogDisplayType { get; set; } = ChangeLogDisplayType.New;
    }

    public ChangelogSettingsEntries ChangelogSettings { get; set; } = new();

    [Serializable]
    public class UISettingsEntries
    {
        public DoubleModifier DeleteTemplateModifier { get; set; } = new(ModifierHotkey.Control, ModifierHotkey.Shift);

        public bool FoldersDefaultOpen { get; set; } = true;

        public bool HideWindowInCutscene { get; set; } = true;

        public bool IncognitoMode { get; set; } = false;

        public List<string> ViewedMessageWindows { get; set; } = new();
    }

    public UISettingsEntries UISettings { get; set; } = new();

    [Serializable]
    public class EditorConfigurationEntries
    {
        /// <summary>
        /// Hides root position from the UI. DOES NOT DISABLE LOADING IT FROM THE CONFIG!
        /// </summary>
        public bool RootPositionEditingEnabled { get; set; } = false;

        public bool ShowLiveBones { get; set; } = true;

        public bool BoneMirroringEnabled { get; set; } = false;
        public bool LimitLookupToOwnedObjects { get; set; } = false;

        public string? PreviewCharacterName { get; set; } = null;

        public int EditorValuesPrecision { get; set; } = 3;
        public BoneAttribute EditorMode { get; set; } = BoneAttribute.Position;
    }

    public EditorConfigurationEntries EditorConfiguration { get; set; } = new();

    [Serializable]
    public class CommandSettingsEntries
    {
        public bool PrintSuccessMessages { get; set; } = true;
    }

    public CommandSettingsEntries CommandSettings { get; set; } = new();

    [JsonIgnore]
    private readonly SaveService _saveService;

    [JsonIgnore]
    private readonly Logger _logger;

    [JsonIgnore]
    private readonly ChatService _chatService;

    [JsonIgnore]
    private readonly MessageService _messageService;

    public PluginConfiguration(
        SaveService saveService,
        Logger logger,
        ChatService chatService,
        MessageService messageService,
        ConfigurationMigrator migrator)
    {
        _saveService = saveService;
        _logger = logger;
        _chatService = chatService;
        _messageService = messageService;

        Load(migrator);
    }

    public void Load(ConfigurationMigrator migrator)
    {
        static void HandleDeserializationError(object? sender, ErrorEventArgs errorArgs)
        {
            Plugin.Logger.Error(
                $"Error parsing configuration at {errorArgs.ErrorContext.Path}, using default or migrating:\n{errorArgs.ErrorContext.Error}");
            errorArgs.ErrorContext.Handled = true;
        }

        if (!File.Exists(_saveService.FileNames.ConfigFile))
            return;

        try
        {
            var text = File.ReadAllText(_saveService.FileNames.ConfigFile);
            JsonConvert.PopulateObject(text, this, new JsonSerializerSettings
            {
                Error = HandleDeserializationError,
            });
        }
        catch (Exception ex)
        {
            _messageService.NotificationMessage(ex,
                "Error reading configuration, reverting to default.\nYou may be able to restore your configuration using the rolling backups in the XIVLauncher/backups/CustomizePlus directory.",
                "Error reading configuration", NotificationType.Error);
        }

        migrator.Migrate(this);
    }

    public string ToFilename(FilenameService fileNames)
        => fileNames.ConfigFile;

    public void Save(StreamWriter writer)
    {
        using var jWriter = new JsonTextWriter(writer) { Formatting = Formatting.Indented };
        var serializer = new JsonSerializer { Formatting = Formatting.Indented };
        serializer.Serialize(jWriter, this);
    }

    public void Save()
        => _saveService.DelaySave(this);
}