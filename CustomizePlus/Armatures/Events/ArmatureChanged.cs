using CustomizePlus.Armatures.Data;
using OtterGui.Classes;

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
        /// <summary>
        /// Called when armature was rebound to other profile or bone template bindings were rebuilt
        /// </summary>
        Updated
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
