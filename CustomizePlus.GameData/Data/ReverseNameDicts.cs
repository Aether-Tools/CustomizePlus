using CustomizePlus.GameData.ReverseSearchDictionaries;
using Dalamud.Game.ClientState.Objects.Enums;
using OtterGui.Services;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Structs;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CustomizePlus.GameData.Data;

/// <summary> A collection service for all the name dictionaries required for reverse name search. </summary>
/// note: this is mvp for profile upgrading purposes, not intended to be used for anything else.
public sealed class ReverseNameDicts(
    ReverseSearchDictMount _mounts,
    ReverseSearchDictCompanion _companions,
    ReverseSearchDictBNpc _bNpcs,
    ReverseSearchDictENpc _eNpcs)
    : IAsyncService
{
    /// <summary> Valid Mount ids by name in title case. </summary>
    public readonly ReverseSearchDictMount Mounts = _mounts;

    /// <summary> Valid Companion ids by name in title case. </summary>
    public readonly ReverseSearchDictCompanion Companions = _companions;

    /// <summary> Valid BNPC ids by name in title case. </summary>
    public readonly ReverseSearchDictBNpc BNpcs = _bNpcs;

    /// <summary> Valid ENPC ids by name in title case. </summary>
    public readonly ReverseSearchDictENpc ENpcs = _eNpcs;
   
    /// <summary> Finished when all name dictionaries are finished. </summary>
    public Task Awaiter { get; } =
        Task.WhenAll(_mounts.Awaiter, _companions.Awaiter, _bNpcs.Awaiter, _eNpcs.Awaiter);

    /// <inheritdoc/>
    public bool Finished
        => Awaiter.IsCompletedSuccessfully;

    /// <summary> Convert a given name for a certain ObjectKind to an ID. </summary>
    /// <returns> default or a valid id. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public uint ToID(ObjectKind kind, string name)
        => TryGetID(kind, name, out var ret) ? ret : default;

    /// <summary> Convert a given ID for a certain ObjectKind to a name. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public bool TryGetID(ObjectKind kind, string name, [NotNullWhen(true)] out uint npcId)
    {
        npcId = default;
        return kind switch
        {
            ObjectKind.MountType => Mounts.TryGetValue(name, out npcId),
            ObjectKind.Companion => Companions.TryGetValue(name, out npcId),
            ObjectKind.BattleNpc => BNpcs.TryGetValue(name, out npcId),
             ObjectKind.EventNpc => ENpcs.TryGetValue(name, out npcId),
            _ => false,
        };
    }
}