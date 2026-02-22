using OtterGui.Classes;

namespace CustomizePlus.Core.Events;

/// <summary>
/// Triggered when complete plugin reload is requested
/// </summary>
public sealed class ReloadEvent() : EventWrapper<ReloadEvent.Type, ReloadEvent.Priority>(nameof(ReloadEvent))
{
    public enum Type
    {
        ReloadAll,
        ReloadProfiles,
        ReloadTemplates
    }

    public enum Priority
    {
        TemplateManager = -2,
        ProfileManager = -1
    }
}
