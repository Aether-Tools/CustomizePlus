using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Penumbra.GameData;
using Penumbra.GameData.Actors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomizePlus.GameData.Services;

public abstract class AsyncServiceWrapper<T> : IDisposable
{
    public string Name { get; }
    public T? Service { get; private set; }

    public T AwaitedService
    {
        get
        {
            _task?.Wait();
            return Service!;
        }
    }

    public bool Valid
        => Service != null && !_isDisposed;

    public event Action? FinishedCreation;
    private Task? _task;

    private bool _isDisposed;

    protected AsyncServiceWrapper(string name, Func<T> factory)
    {
        Name = name;
        _task = Task.Run(() =>
        {
            var service = factory();
            if (_isDisposed)
            {
                if (service is IDisposable d)
                    d.Dispose();
            }
            else
            {
                Service = service;
                _task = null;
            }
        });
        _task.ContinueWith((t, x) =>
        {
            if (!_isDisposed)
                FinishedCreation?.Invoke();
        }, null);
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _task = null;
        if (Service is IDisposable d)
            d.Dispose();
    }
}

public sealed class ActorService : AsyncServiceWrapper<ActorManager>
{
    public ActorService(DalamudPluginInterface pi, IObjectTable objects, IClientState clientState, IFramework framework, IGameInteropProvider interop, IDataManager gameData,
        IGameGui gui, CutsceneService cutsceneService, IPluginLog log)
        : base(nameof(ActorService),
            () => new ActorManager(pi, objects, clientState, framework, interop, gameData, gui, idx => (short)cutsceneService.GetParentIndex(idx), log))
    { }
}
