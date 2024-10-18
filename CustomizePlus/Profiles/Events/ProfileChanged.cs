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
        AddedCharacter,
        RemovedCharacter,
        //ChangedCharacter,
        AddedTemplate,
        RemovedTemplate,
        MovedTemplate,
        ChangedTemplate,
        ReloadedAll,
        WriteProtection,
        ChangedDefaultProfile,
        ChangedDefaultLocalPlayerProfile,
        TemporaryProfileAdded,
        TemporaryProfileDeleted,
        /*
        ToggledProfile,
        AddedTemplate,
        RemovedTemplate,
        MovedTemplate,
        ChangedTemplate*/
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
