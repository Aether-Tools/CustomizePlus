using Dalamud.Plugin.Ipc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Penumbra.Api.Ipc;

namespace CustomizePlus.Api;

//I'm not a big fan of having functions and variables/properties
//grouped up like that but it makes sense here

public partial class CustomizePlusIpc : IDisposable
{
    /// <summary>
    /// When there are breaking changes the first number is bumped up and second one is reset.
    /// When there are non-breaking changes only second number is bumped up.
    /// In general clients should not try to use IPC if they encounter unexpected Breaking version.
    /// </summary>
    private readonly (int Breaking, int Feature) _apiVersion = (4, 0);
    private const string _providerGetApiVersionLabel = $"CustomizePlus.General.{nameof(GetApiVersion)}";
    private ICallGateProvider<(int, int)>? _providerGetApiVersion;

    private (int, int) GetApiVersion()
    {
        return _apiVersion;
    }

    /// <summary>
    /// This indicates if Customize+ is in valid state and can accept IPC requests.
    /// This only indicates that no fatal errors occured in Customize+.
    /// This will still be true if, for example, user turns off Customize+ in its settings.
    /// </summary>
    private const string _providerIsValidLabel = $"CustomizePlus.General.{nameof(IsValid)}";
    private ICallGateProvider<bool>? _providerIsValid;

    private bool IsValid()
    {
        return !IPCFailed &&
            !_hookingService.RenderHookFailed &&
            !_hookingService.MovementHookFailed;
    }

    private void InitializeGeneralProviders()
    {
        _logger.Debug("Initializing General Customize+ IPC providers.");

        _providerGetApiVersion = _pluginInterface.GetIpcProvider<(int, int)>(_providerGetApiVersionLabel);
        _providerGetApiVersion.RegisterFunc(GetApiVersion);

        _providerIsValid = _pluginInterface.GetIpcProvider<bool>(_providerIsValidLabel);
        _providerIsValid.RegisterFunc(IsValid);
    }

    private void DisposeGeneralProviders()
    {
        _logger.Debug("Disposing General Customize+ IPC providers.");
        _providerGetApiVersion?.UnregisterFunc();
    }
}
