using CustomizePlus.Armatures.Data;
using OtterGui.Classes;
using System;

namespace CustomizePlus.Armatures.Events;

/// <summary>
/// Triggered when armature is changed
/// </summary>
public sealed class ArmatureChanged() : EventWrapper<ArmatureChanged.Type, Armature, object?, ArmatureChanged.Priority>(nameof(ArmatureChanged))
{
    public enum Type
    {
        Created,
        Deleted,
        Rebound
    }

    public enum Priority
    {
        ProfileManager,
        CustomizePlusIpc
    }

    public enum DeletionReason
    {
        Gone,
        NoActiveProfiles,
        ProfileManagerEvent,
        TemplateEditorEvent
    }
}
