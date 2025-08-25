using CustomizePlusPlus.Interop.Ipc;
using System;
using System.Linq;
using System.Collections.Generic;

public sealed class IpcHandler
{
    private readonly List<IIpcSubscriber> _subscribers;

    public IpcHandler(IEnumerable<IIpcSubscriber> subscribers)
    {
        _subscribers = subscribers.ToList();
    }

    public void Initialize()
    {
        foreach (var subscriber in _subscribers)
            subscriber.Initialize();
    }
}
