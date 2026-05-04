using CustomizePlus.Configuration.Services;
using CustomizePlus.Core.Data;
using CustomizePlus.Core.Helpers;
using CustomizePlus.Core.Services;
using CustomizePlus.UI.Windows;
using Dalamud.Configuration;
using Dalamud.Interface.ImGuiNotification;
using Newtonsoft.Json;
using Penumbra.GameData.Actors;
using ErrorEventArgs = Newtonsoft.Json.Serialization.ErrorEventArgs;

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
        public DoubleModifier DeleteModifier { get; set; } = new(ModifierHotkey.Control, ModifierHotkey.Shift);

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

        [JsonConverter(typeof(ActorIdentifierJsonConverter))]

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
    public class IntegrationSettingsEntries
    {
        public bool PenumbraPCPIntegrationEnabled { get; set; } = true;
    }

    public IntegrationSettingsEntries IntegrationSettings { get; set; } = new();

    [JsonConverter(typeof(SortModeConverter))]
    [JsonProperty(Order = int.MaxValue)]
    public ISortMode SortMode { get; set; } = ISortMode.FoldersFirst;

    [JsonIgnore]
    public LunaUiConfiguration LunaUiConfiguration { get; internal set; }

    [JsonIgnore]
    private readonly SaveService _saveService;

    [JsonIgnore]
    private readonly MessageService _messageService;

    public PluginConfiguration(
        SaveService saveService,
        MessageService messageService,
        ConfigurationMigrator migrator,
        LunaUiConfiguration lunaUiConfiguration)
    {
        _saveService = saveService;
        _messageService = messageService;
        LunaUiConfiguration = lunaUiConfiguration;

        Load(migrator);
    }

    public void Load(ConfigurationMigrator migrator)
    {
        static void HandleDeserializationError(object? sender, ErrorEventArgs errorArgs)
        {
            CustomizePlus.Logger.Error(
                $"Error parsing configuration at {errorArgs.ErrorContext.Path}, using default or migrating:\n{errorArgs.ErrorContext.Error}");
            errorArgs.ErrorContext.Handled = true;
        }

        if (!File.Exists(_saveService.FileNames.ConfigurationFile))
            return;

        try
        {
            var text = File.ReadAllText(_saveService.FileNames.ConfigurationFile);
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

    public string ToFilePath(FilenameService fileNames)
        => fileNames.ConfigurationFile;

    public void Save(Stream stream)
    {
        using var writer = new StreamWriter(stream);
        using var jWriter = new JsonTextWriter(writer);
        jWriter.Formatting = Formatting.Indented;
        var serializer = new JsonSerializer { Formatting = Formatting.Indented };
        serializer.Serialize(jWriter, this);
    }

    public void Save()
        => _saveService.DelaySave(this);

    /// <summary> Convert SortMode Types to their name. </summary>
    private class SortModeConverter : JsonConverter<ISortMode>
    {
        public override void WriteJson(JsonWriter writer, ISortMode? value, JsonSerializer serializer)
        {
            value ??= ISortMode.FoldersFirst;
            serializer.Serialize(writer, value.GetType().Name);
        }

        public override ISortMode ReadJson(JsonReader reader, Type objectType, ISortMode? existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            if (serializer.Deserialize<string>(reader) is { } name)
                return ISortMode.Valid.GetValueOrDefault(name, existingValue ?? ISortMode.FoldersFirst);

            return existingValue ?? ISortMode.FoldersFirst;
        }
    }
}
