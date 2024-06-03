using System.Numerics;

namespace CustomizePlus.UI.Windows;

public partial class PopupSystem
{
    public static class Messages
    {
        public const string ActionError = "action_error";

        public const string FantasiaPlusDetected = "fantasia_detected_warn";

        public const string IPCProfileRemembered = "ipc_profile_remembered";
        public const string IPCGetProfileByIdRemembered = "ipc_get_profile_by_id_remembered";
        public const string IPCSetProfileToChrDone = "ipc_set_profile_to_character_done";
        public const string IPCRevertDone = "ipc_revert_done";
        public const string IPCCopiedToClipboard = "ipc_copied_to clipboard";
        public const string IPCEnableProfileByIdDone = "ipc_enable_profile_by_id_done";
        public const string IPCDisableProfileByIdDone = "ipc_disable_profile_by_id_done";

        public const string TemplateEditorActiveWarning = "template_editor_active_warn";
        public const string ClipboardDataUnsupported = "clipboard_data_unsupported_version";

        public const string ClipboardDataNotLongTerm = "clipboard_data_not_longterm";
    }

    private void RegisterMessages()
    {
        RegisterPopup(Messages.ActionError, "Error while performing selected action.\nDetails have been printed to Dalamud log (/xllog in chat).");

        RegisterPopup(Messages.FantasiaPlusDetected, "Customize+ detected that you have Fantasia+ installed.\nPlease delete or turn it off and restart your game to use Customize+.");

        RegisterPopup(Messages.IPCProfileRemembered, "Current profile has been copied into memory");
        RegisterPopup(Messages.IPCGetProfileByIdRemembered, "GetProfileByUniqueId result has been copied into memory");
        RegisterPopup(Messages.IPCSetProfileToChrDone, "SetProfileToCharacter has been called with data from memory, profile id printed to log");
        RegisterPopup(Messages.IPCRevertDone, "DeleteTemporaryProfileByUniqueId has been called");
        RegisterPopup(Messages.IPCCopiedToClipboard, "Copied into clipboard");
        RegisterPopup(Messages.IPCEnableProfileByIdDone, "Enable profile by id has been called");
        RegisterPopup(Messages.IPCDisableProfileByIdDone, "Disable profile by id has been called");

        RegisterPopup(Messages.TemplateEditorActiveWarning, "You need to stop bone editing before doing this action");
        RegisterPopup(Messages.ClipboardDataUnsupported, "Clipboard data you are trying to use cannot be used in this version of Customize+.");

        RegisterPopup(Messages.ClipboardDataNotLongTerm, "Warning: clipboard data is not designed to be used as long-term way of storing your templates.\nCompatibility of copied data between different Customize+ versions is not guaranteed.", true, new Vector2(5, 10));
    }
}
