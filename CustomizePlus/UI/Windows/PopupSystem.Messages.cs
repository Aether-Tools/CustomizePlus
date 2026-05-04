namespace CustomizePlus.UI.Windows;

public partial class PopupSystem
{
    public static class Messages
    {
        public const string ActionError = "action_error";
        public const string ActionDone = "action_done";

        public const string FantasiaPlusDetected = "fantasia_detected_warn";

        public const string IPCProfileRemembered = "ipc_profile_remembered";
        public const string IPCGetProfileByIdRemembered = "ipc_get_profile_by_id_remembered";
        public const string IPCSetProfileToChrDone = "ipc_set_profile_to_character_done";
        public const string IPCRevertDone = "ipc_revert_done";
        public const string IPCCopiedToClipboard = "ipc_copied_to clipboard";
        public const string IPCSuccessfullyExecuted = "ipc_successfully_executed";
        public const string IPCEnableProfileByIdDone = "ipc_enable_profile_by_id_done";
        public const string IPCDisableProfileByIdDone = "ipc_disable_profile_by_id_done";

        public const string TemplateEditorActiveWarning = "template_editor_active_warn";
        public const string ClipboardDataUnsupported = "clipboard_data_unsupported_version";

        public const string ClipboardDataNotLongTerm = "clipboard_data_not_longterm";

        public const string PluginDisabledNonReleaseDalamud = "non_release_dalamud";
    }

    private void RegisterMessages()
    {
        RegisterPopup(Messages.ActionError, "Action Failed", "Error while performing selected action.\nDetails have been printed to Dalamud log (/xllog in chat).");
        RegisterPopup(Messages.ActionDone, "Action Complete", "Action performed successfully.");

        RegisterPopup(Messages.FantasiaPlusDetected, "Fantasia+ Detected", "Customize+ detected that you have Fantasia+ installed.\nPlease delete or turn it off and restart your game to use Customize+.");

        RegisterPopup(Messages.IPCProfileRemembered, "Profile Copied", "Current profile has been copied into memory.");
        RegisterPopup(Messages.IPCGetProfileByIdRemembered, "Profile Copied", "GetProfileByUniqueId result has been copied into memory.");
        RegisterPopup(Messages.IPCSetProfileToChrDone, "IPC Executed", "SetProfileToCharacter has been called with data from memory. The profile ID was printed to the log.");
        RegisterPopup(Messages.IPCRevertDone, "IPC Executed", "DeleteTemporaryProfileByUniqueId has been called.");
        RegisterPopup(Messages.IPCCopiedToClipboard, "Copied", "Copied into clipboard.");
        RegisterPopup(Messages.IPCSuccessfullyExecuted, "IPC Executed", "Successfully executed.");
        RegisterPopup(Messages.IPCEnableProfileByIdDone, "IPC Executed", "Enable profile by ID has been called.");
        RegisterPopup(Messages.IPCDisableProfileByIdDone, "IPC Executed", "Disable profile by ID has been called.");

        RegisterPopup(Messages.TemplateEditorActiveWarning, "Bone Editing Active", "You need to stop bone editing before doing this action.");
        RegisterPopup(Messages.ClipboardDataUnsupported, "Unsupported Clipboard Data", "Clipboard data you are trying to use cannot be used in this version of Customize+.");

        RegisterPopup(Messages.ClipboardDataNotLongTerm, "Clipboard Warning", "Clipboard data is not designed to be used as long-term way of storing templates.\nCompatibility of copied data between different Customize+ versions is not guaranteed.", true, new Vector2(5, 10));
    }
}
