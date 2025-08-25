using System;

namespace CustomizePlus.Interop.Ipc;

public interface IIpcSubscriber : IDisposable
{
    void Initialize();
    bool CheckApiVersion();
}