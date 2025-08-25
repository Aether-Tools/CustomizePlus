using Dalamud.Plugin;
using Newtonsoft.Json.Linq;
using OtterGui.Log;
using Penumbra.Api.Helpers;
using Penumbra.Api.IpcSubscribers;
using System;

namespace CustomizePlus.Interop.Ipc;

public sealed class PenumbraIpcHandler : IIpcSubscriber
{
    private readonly Logger _log;
    private readonly ApiVersion _version;

    private readonly EventSubscriber<JObject, ushort, string> _pcpCreated;
    private readonly EventSubscriber<JObject, string, Guid> _pcpParsed;
    private readonly IDisposable _penumbraInit;
    private readonly IDisposable _penumbraDisp;

    private const int RequiredMajor = 5;
    private const int RequiredMinor = 8;
    private int CurrentMajor = 0;
    private int CurrentMinor = 0;

    private bool _available = false;
    public bool Available => _available;

    private bool _shownVersionWarning = false;

    public PenumbraIpcHandler(IDalamudPluginInterface pi, Logger log)
    {
        _log = log;
        _version = new ApiVersion(pi);

        _pcpCreated = CreatingPcp.Subscriber(pi);
        _pcpParsed = ParsingPcp.Subscriber(pi);

        _penumbraInit = Initialized.Subscriber(pi, () =>
        {
            if (CheckApiVersion())
            {
                _pcpCreated.Enable();
                _pcpParsed.Enable();
                _log.Information("Penumbra IPC Subscribed after Initialization.");
            }
        });

        _penumbraDisp = Disposed.Subscriber(pi, () =>
        {
            _pcpCreated.Disable();
            _pcpParsed.Disable();
            _log.Information("Penumbra IPC Disabled after Disposal.");
        });
    }

    public event Action<JObject, ushort, string> PcpCreated
    {
        add => _pcpCreated.Event += value;
        remove => _pcpCreated.Event -= value;
    }

    public event Action<JObject, string, Guid> PcpParsed
    {
        add => _pcpParsed.Event += value;
        remove => _pcpParsed.Event -= value;
    }

    public bool CheckApiVersion()
    {
        try
        {
            var (major, minor) = _version.Invoke();

            CurrentMajor = major;
            CurrentMinor = minor;

            _available = major == RequiredMajor && minor >= RequiredMinor;
        }
        catch
        {
            _available = false;
        }

        if (!_available && !_shownVersionWarning)
        {
            _shownVersionWarning = true;
            _log.Warning($"Penumbra IPC version is not supported. Required: {RequiredMajor}.{RequiredMinor}+");
        }

        return _available;
    }

    public void Initialize()
    {
        if (!CheckApiVersion())
            return;

        _pcpCreated.Enable();
        _pcpParsed.Enable();

        _log.Information($"Subscribed to Penumbra IPC. Penumbra IPC {CurrentMajor}.{CurrentMinor}.");
    }

    public void Dispose()
    {
        _pcpCreated.Disable();
        _pcpParsed.Disable();
        _pcpCreated.Dispose();
        _pcpParsed.Dispose();

        _penumbraInit.Dispose();
        _penumbraDisp.Dispose();

        _log.Information("Unsubscribed from Penumbra IPC.");
    }
}
