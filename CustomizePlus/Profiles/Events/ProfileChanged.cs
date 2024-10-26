using CustomizePlus.Profiles.Data;
using OtterGui.Classes;
using System;

namespace CustomizePlus.Profiles.Events;

/// <summary>
/// Triggered when profile is changed
/// </summary>
public sealed class ProfileChanged() : EventWrapper<ProfileChanged.Type, Profile?, object?, ProfileChanged.Priority>(nameof(ProfileChanged))
{
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
        ProfileFileSystem,
        ArmatureManager,
        TemplateManager,
        CustomizePlusLegacyIpc
    }
}
