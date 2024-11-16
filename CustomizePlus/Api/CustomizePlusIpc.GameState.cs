using CustomizePlus.Api.Enums;
using ECommonsLite.EzIpcManager;

namespace CustomizePlus.Api;

public partial class CustomizePlusIpc
{
    /// <summary>
    /// Retrieve parent for actor. If actor has no parent will return -1.
    /// This IPC method is identical to same method in Penumbra.
    /// /!\ Generally speaking use cases for this are quite limited and you should not use this unless you know what you are doing.
    /// Improper use of this method can lead to incorrect Customize+ behavior.
    /// </summary>
    [EzIPC("GameState.GetCutsceneParentIndex")]
    private int GetCutsceneParentIndex(int actorIndex)
    {
        return _cutsceneService.GetParentIndex(actorIndex);
    }

    /// <summary>
    /// Set parent for actor.
    /// This IPC method is identical to same method in Penumbra.
    /// /!\ Generally speaking use cases for this are quite limited and you should not use this unless you know what you are doing.
    /// Improper use of this method can lead to incorrect Customize+ behavior.
    /// </summary>
    [EzIPC("GameState.SetCutsceneParentIndex")]
    private int SetCutsceneParentIndex(int copyIndex, int newParentIndex)
    {
        return _cutsceneService.SetParentIndex(copyIndex, newParentIndex) ? (int)ErrorCode.Success : (int)ErrorCode.InvalidArgument;
    }
}
