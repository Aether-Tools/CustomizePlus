using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.System.Resource.Handle;
using Penumbra.GameData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomizePlus.GameData.Services;

public unsafe class GameEventManager : IDisposable
{
    private const string Prefix = $"[{nameof(GameEventManager)}]";

    public event CharacterDestructorEvent? CharacterDestructor;
    public event CopyCharacterEvent? CopyCharacter;
    public event CreatingCharacterBaseEvent? CreatingCharacterBase;
    public event CharacterBaseCreatedEvent? CharacterBaseCreated;
    public event CharacterBaseDestructorEvent? CharacterBaseDestructor;

    public GameEventManager(IGameInteropProvider interop)
    {
        interop.InitializeFromAttributes(this);

        _copyCharacterHook =
            interop.HookFromAddress<CopyCharacterDelegate>((nint)CharacterSetupContainer.MemberFunctionPointers.CopyFromCharacter, CopyCharacterDetour);
        _characterBaseCreateHook =
            interop.HookFromAddress<CharacterBaseCreateDelegate>((nint)CharacterBase.MemberFunctionPointers.Create, CharacterBaseCreateDetour);
        _characterBaseDestructorHook =
            interop.HookFromAddress<CharacterBaseDestructorEvent>((nint)CharacterBase.MemberFunctionPointers.Destroy,
                CharacterBaseDestructorDetour);
        _characterDtorHook.Enable();
        _copyCharacterHook.Enable();
        _characterBaseCreateHook.Enable();
        _characterBaseDestructorHook.Enable();
    }

    public void Dispose()
    {
        _characterDtorHook.Dispose();
        _copyCharacterHook.Dispose();
        _characterBaseCreateHook.Dispose();
        _characterBaseDestructorHook.Dispose();
    }

    #region Character Destructor

    private delegate void CharacterDestructorDelegate(Character* character);

    [Signature(Sigs.CharacterDestructor, DetourName = nameof(CharacterDestructorDetour))]
    private readonly Hook<CharacterDestructorDelegate> _characterDtorHook = null!;

    private void CharacterDestructorDetour(Character* character)
    {
        if (CharacterDestructor != null)
            foreach (var subscriber in CharacterDestructor.GetInvocationList())
            {
                try
                {
                    ((CharacterDestructorEvent)subscriber).Invoke(character);
                }
                catch (Exception ex)
                {
                    //Penumbra.Log.Error($"{Prefix} Error in {nameof(CharacterDestructor)} event when executing {subscriber.Method.Name}:\n{ex}");
                    //todo: log
                }
            }

        //Penumbra.Log.Verbose($"{Prefix} {nameof(CharacterDestructor)} triggered with 0x{(nint)character:X}.");
        //todo: log
        _characterDtorHook.Original(character);
    }

    public delegate void CharacterDestructorEvent(Character* character);

    #endregion

    #region Copy Character

    private delegate ulong CopyCharacterDelegate(CharacterSetupContainer* target, GameObject* source, uint unk);

    private readonly Hook<CopyCharacterDelegate> _copyCharacterHook;

    private ulong CopyCharacterDetour(CharacterSetupContainer* target, GameObject* source, uint unk)
    {
        // TODO: update when CS updated.
        var character = ((Character**)target)[1];
        if (CopyCharacter != null)
            foreach (var subscriber in CopyCharacter.GetInvocationList())
            {
                try
                {
                    ((CopyCharacterEvent)subscriber).Invoke(character, (Character*)source);
                }
                catch (Exception ex)
                {
                    /*Penumbra.Log.Error(
                        $"{Prefix} Error in {nameof(CopyCharacter)} event when executing {subscriber.Method.Name}:\n{ex}");*/
                    //todo: log
                }
            }

        /*Penumbra.Log.Verbose(
            $"{Prefix} {nameof(CopyCharacter)} triggered with target 0x{(nint)target:X} and source 0x{(nint)source:X}.");*/
        //todo: log
        return _copyCharacterHook.Original(target, source, unk);
    }

    public delegate void CopyCharacterEvent(Character* target, Character* source);

    #endregion

    #region CharacterBaseCreate

    private delegate nint CharacterBaseCreateDelegate(uint a, nint b, nint c, byte d);

    private readonly Hook<CharacterBaseCreateDelegate> _characterBaseCreateHook;

    private nint CharacterBaseCreateDetour(uint a, nint b, nint c, byte d)
    {
        if (CreatingCharacterBase != null)
            foreach (var subscriber in CreatingCharacterBase.GetInvocationList())
            {
                try
                {
                    ((CreatingCharacterBaseEvent)subscriber).Invoke((nint)(&a), b, c);
                }
                catch (Exception ex)
                {
                    /*Penumbra.Log.Error(
                        $"{Prefix} Error in {nameof(CharacterBaseCreateDetour)} event when executing {subscriber.Method.Name}:\n{ex}");*/
                    //todo: log
                }
            }

        var ret = _characterBaseCreateHook.Original(a, b, c, d);
        if (CharacterBaseCreated != null)
            foreach (var subscriber in CharacterBaseCreated.GetInvocationList())
            {
                try
                {
                    ((CharacterBaseCreatedEvent)subscriber).Invoke(a, b, c, ret);
                }
                catch (Exception ex)
                {
                    /*Penumbra.Log.Error(
                        $"{Prefix} Error in {nameof(CharacterBaseCreateDetour)} event when executing {subscriber.Method.Name}:\n{ex}");*/
                    //todo: log
                }
            }

        return ret;
    }

    public delegate void CreatingCharacterBaseEvent(nint modelCharaId, nint customize, nint equipment);
    public delegate void CharacterBaseCreatedEvent(uint modelCharaId, nint customize, nint equipment, nint drawObject);

    #endregion

    #region CharacterBase Destructor

    public delegate void CharacterBaseDestructorEvent(nint drawBase);

    private readonly Hook<CharacterBaseDestructorEvent> _characterBaseDestructorHook;

    private void CharacterBaseDestructorDetour(nint drawBase)
    {
        if (CharacterBaseDestructor != null)
            foreach (var subscriber in CharacterBaseDestructor.GetInvocationList())
            {
                try
                {
                    ((CharacterBaseDestructorEvent)subscriber).Invoke(drawBase);
                }
                catch (Exception ex)
                {
                    /*Penumbra.Log.Error(
                        $"{Prefix} Error in {nameof(CharacterBaseDestructorDetour)} event when executing {subscriber.Method.Name}:\n{ex}");*/
                    //todo: log
                }
            }

        _characterBaseDestructorHook.Original.Invoke(drawBase);
    }

    #endregion
}
