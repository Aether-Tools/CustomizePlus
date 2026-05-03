using CustomizePlus.Profiles.Data;

namespace CustomizePlus.Profiles.Events;

/// <summary>
/// Triggered when profile is changed
/// </summary>
public sealed class ProfileChanged(LunaLogger log)
    : EventBase<ProfileChanged.Arguments, ProfileChanged.Priority>(nameof(ProfileChanged), log)
{
    public readonly record struct Arguments(Type Type, Profile? Profile, object? Data);

    public enum Type
    {
        Created,
        Deleted,
        Renamed,
        Toggled,
        PriorityChanged,
        AddedCharacter,
        RemovedCharacter,
        AddedTemplate,
        RemovedTemplate,
        EnabledTemplate,
        DisabledTemplate,
        MovedTemplate,
        ChangedTemplate,
        ReloadedAll,
        WriteProtection,
        ChangedDefaultProfile,
        ChangedDefaultLocalPlayerProfile,
        TemporaryProfileAdded,
        TemporaryProfileDeleted
    }

    public enum Priority
    {
        ProfileFileSystemSelector = -2,
        TemplateFileSystemSelector = -1,
        ProfileFileSystem = 0,
        DesignHeader = 0,
        ArmatureManager = 1,
        TemplateManager = 2,
        CustomizePlusLegacyIpc = 3
    }
}
