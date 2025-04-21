using ECommonsLite.EzIpcManager;

namespace CustomizePlus.Api;

public partial class CustomizePlusIpc
{
    private readonly (int Breaking, int Feature) _apiVersion = (6, 1);

    /// <summary>
    /// When there are breaking changes the first number is bumped up and second one is reset.
    /// When there are non-breaking changes only second number is bumped up.
    /// In general clients should not try to use IPC if they encounter unexpected Breaking version.
    /// </summary>
    [EzIPC("General.GetApiVersion")]
    private (int, int) GetApiVersion()
    {
        return _apiVersion;
    }

    /// <summary>
    /// This indicates if Customize+ is in valid state and can accept IPC requests.
    /// This only indicates that no fatal errors occured in Customize+.
    /// This will still be true if, for example, user turns off Customize+ in its settings.
    /// </summary>
    [EzIPC("General.IsValid")]
    private bool IsValid()
    {
        return !IPCFailed &&
            !_hookingService.RenderHookFailed &&
            !_hookingService.MovementHookFailed;
    }
}
