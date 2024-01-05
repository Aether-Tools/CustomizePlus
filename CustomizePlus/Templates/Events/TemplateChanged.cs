using CustomizePlus.Templates.Data;
using OtterGui.Classes;
using System;

namespace CustomizePlus.Templates.Events;

/// <summary>
/// Triggered when Template is changed
/// </summary>
public class TemplateChanged() : EventWrapper<TemplateChanged.Type, Template?, object?, TemplateChanged.Priority>(nameof(TemplateChanged))
{
    public enum Type
    {
        Created,
        Deleted,
        Renamed,
        NewBone,
        UpdatedBone,
        DeletedBone,
        EditorEnabled,
        EditorDisabled,
        EditorCharacterChanged,
        ReloadedAll,
        WriteProtection
    }

    public enum Priority
    {
        TemplateCombo = -2,
        TemplateFileSystemSelector = -1,
        TemplateFileSystem,
        ArmatureManager,
        ProfileManager,
        CustomizePlusIpc
    }
}
