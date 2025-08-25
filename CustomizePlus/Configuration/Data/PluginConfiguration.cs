using System;
using System.Collections.Generic;
using System.IO;
using Dalamud.Configuration;
using Newtonsoft.Json;
using OtterGui.Classes;
using OtterGui.Widgets;
using ErrorEventArgs = Newtonsoft.Json.Serialization.ErrorEventArgs;
using CustomizePlusPlus.Core.Services;
using CustomizePlusPlus.Core.Data;
using CustomizePlusPlus.Configuration.Services;
using CustomizePlusPlus.UI.Windows;
using Dalamud.Interface.ImGuiNotification;
using Penumbra.GameData.Actors;
using CustomizePlusPlus.Core.Helpers;

namespace CustomizePlusPlus.Configuration.Data;

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

    /// <summary>
    /// Id of the profile applied to any character user logins with. Can be set to Empty to disable this feature.
    /// </summary>
    public Guid DefaultLocalPlayerProfile { get; set; } = Guid.Empty;

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

        public bool OpenWindowAtStart { get; set; } = false;

        public bool HideWindowInCutscene { get; set; } = true;

        public bool HideWindowWhenUiHidden { get; set; } = true;

        public bool HideWindowInGPose { get; set; } = false;

        public bool IncognitoMode { get; set; } = false;

        public float CurrentTemplateSelectorWidth { get; set; } = 200f;

        public float TemplateSelectorMinimumScale { get; set; } = 0.1f;

        public float TemplateSelectorMaximumScale { get; set; } = 0.5f;

        public float CurrentProfileSelectorWidth { get; set; } = 200f;

        public float ProfileSelectorMinimumScale { get; set; } = 0.1f;

        public float ProfileSelectorMaximumScale { get; set; } = 0.5f;


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

        public ActorIdentifier PreviewCharacter { get; set; } = ActorIdentifier.Invalid;

        public int EditorValuesPrecision { get; set; } = 3;

        public BoneAttribute EditorMode { get; set; } = BoneAttribute.Position;

        public bool SetPreviewToCurrentCharacterOnLogin { get; set; } = false;

        public HashSet<string> FavoriteBones { get; set; } = new();
    }

    public EditorConfigurationEntries EditorConfiguration { get; set; } = new();

    [Serializable]
    public class CommandSettingsEntries
    {
        public bool PrintSuccessMessages { get; set; } = true;
    }

    public CommandSettingsEntries CommandSettings { get; set; } = new();

    [Serializable]
    public class ProfileApplicationSettingsEntries
    {
        public bool ApplyInCharacterWindow { get; set; } = true;
        public bool ApplyInTryOn { get; set; } = true;
        public bool ApplyInCards { get; set; } = true;
        public bool ApplyInInspect { get; set; } = true;
        public bool ApplyInLobby { get; set; } = true;
    }

    public ProfileApplicationSettingsEntries ProfileApplicationSettings { get; set; } = new();

    [Serializable]
    public class ExternalSettingsEntries
    {
        public bool HandlePCPFiles { get; set; } = true;
    }

    public ExternalSettingsEntries ExternalSettings { get; set; } = new();

    [JsonIgnore]
    private readonly SaveService _saveService;

    [JsonIgnore]
    private readonly MessageService _messageService;

    public PluginConfiguration(
        SaveService saveService,
        MessageService messageService,
        ConfigurationMigrator migrator)
    {
        _saveService = saveService;
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
                Converters = new List<JsonConverter> { new ActorIdentifierJsonConverter() }
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
        serializer.Converters.Add(new ActorIdentifierJsonConverter());
        serializer.Serialize(jWriter, this);
    }

    public void Save()
        => _saveService.DelaySave(this);
}