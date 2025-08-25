﻿using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using OtterGui.Classes;
using OtterGui.Services;

namespace CustomizePlus.GameData.Hooks.Objects;
public sealed unsafe class CopyCharacter : EventWrapperPtr<Character, Character, CopyCharacter.Priority>, IHookService
{
    public enum Priority
    {
        /// <seealso cref="PathResolving.CutsceneService"/>
        CutsceneService = 0,
    }

    public CopyCharacter(HookManager hooks)
        : base("Copy Character")
        => _task = hooks.CreateHook<Delegate>(Name, Address, Detour, true);

    private readonly Task<Hook<Delegate>> _task;

    public nint Address
        => (nint)CharacterSetupContainer.MemberFunctionPointers.CopyFromCharacter;

    public void Enable()
        => _task.Result.Enable();

    public void Disable()
        => _task.Result.Disable();

    public Task Awaiter
        => _task;

    public bool Finished
        => _task.IsCompletedSuccessfully;

    private delegate ulong Delegate(CharacterSetupContainer* target, Character* source, uint unk);

    private ulong Detour(CharacterSetupContainer* target, Character* source, uint unk)
    {
        // TODO: update when CS updated.
        var character = ((Character**)target)[1];
        //Penumbra.Log.Verbose($"[{Name}] Triggered with target: 0x{(nint)target:X}, source : 0x{(nint)source:X} unk: {unk}.");
        Invoke(character, source);
        return _task.Result.Original(target, source, unk);
    }
}