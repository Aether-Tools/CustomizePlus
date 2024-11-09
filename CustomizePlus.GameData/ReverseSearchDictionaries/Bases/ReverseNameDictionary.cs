using Dalamud.Plugin.Services;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Common.Lua;
using OtterGui.Log;
using Penumbra.GameData.Data;
using Penumbra.GameData.DataContainers.Bases;
using Penumbra.GameData.Structs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomizePlus.GameData.ReverseSearchDictionaries.Bases;

/// <summary> A base class for dictionaries from NPC names to their IDs. </summary>
/// <param name="pluginInterface"> The plugin interface. </param>
/// <param name="log"> A logger. </param>
/// <param name="gameData"> The data manger to fetch the data from. </param>
/// <param name="name"> The name of the data share. </param>
/// <param name="version"> The version of the data share. </param>
/// <param name="factory"> The factory function to create the data from. </param>
public abstract class ReverseNameDictionary(
    IDalamudPluginInterface pluginInterface,
    Logger log,
    IDataManager gameData,
    string name,
    int version,
    Func<IReadOnlyDictionary<string, uint>> factory)
    : DataSharer<IReadOnlyDictionary<string, uint>>(pluginInterface, log, name, gameData.Language, version, factory),
        IReadOnlyDictionary<string, NpcId>
{
    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<string, NpcId>> GetEnumerator()
        => Value.Select(kvp => new KeyValuePair<string, NpcId>(kvp.Key, new NpcId(kvp.Value))).GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <inheritdoc/>
    public int Count
        => Value.Count;

    /// <inheritdoc/>
    public bool ContainsKey(string key)
        => Value.ContainsKey(key);

    /// <inheritdoc/>
    public bool TryGetValue(string key, [NotNullWhen(true)] out NpcId value)
    {
        if (!Value.TryGetValue(key, out var uintVal))
        {
            value = default;
            return false;
        }

        value = new NpcId(uintVal);
        return true;
    }

    /// <inheritdoc/>
    public NpcId this[string key]
        => new NpcId(Value[key]);

    /// <inheritdoc/>
    public IEnumerable<string> Keys
        => Value.Keys;

    /// <inheritdoc/>
    public IEnumerable<NpcId> Values
        => Value.Values.Select(k => new NpcId(k));

    /// <inheritdoc/>
    protected override long ComputeMemory()
        => DataUtility.DictionaryMemory(16, Count) + Keys.Sum(v => v.Length * 2); //this seems to be only used by diagnostics stuff so I don't particularly care for this to be correct.

    /// <inheritdoc/>
    protected override int ComputeTotalCount()
        => Count;
}
