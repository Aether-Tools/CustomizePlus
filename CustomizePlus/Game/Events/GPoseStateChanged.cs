using OtterGui.Classes;

namespace CustomizePlus.Game.Events;

/// <summary>
/// Triggered when GPose is entered/exited
/// </summary>
public sealed class GPoseStateChanged() : EventWrapper<GPoseStateChanged.Type, GPoseStateChanged.Priority>(nameof(GPoseStateChanged))
{
    public enum Type
    {
        Entered,
        AttemptingExit,
        Exiting,
        Exited
    }

    public enum Priority
    {
        TemplateEditorManager = -1,
        GPoseAmnesisKtisisWarningService
    }
}
