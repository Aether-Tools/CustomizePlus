using System;

namespace CustomizePlusPlus.Interop.Ipc;

public interface IIpcSubscriber : IDisposable
{
    void Initialize();
    bool CheckApiVersion();
}