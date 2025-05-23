﻿using Dalamud.Plugin.Services;
using Dalamud.Plugin;
using OtterGui.Log;
using Penumbra.GameData.Data;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using CustomizePlus.GameData.ReverseSearchDictionaries.Bases;
using Lumina.Excel.Sheets;

namespace CustomizePlus.GameData.ReverseSearchDictionaries;

/// <summary> A dictionary that matches names to event npc ids. </summary>
public sealed class ReverseSearchDictENpc(IDalamudPluginInterface pluginInterface, Logger log, IDataManager gameData)
    : ReverseNameDictionary(pluginInterface, log, gameData, "ReverseSearchENpcs", Penumbra.GameData.DataContainers.Version.DictENpc, () => CreateENpcData(gameData))
{
    /// <summary> Create the data. </summary>
    private static IReadOnlyDictionary<string, uint> CreateENpcData(IDataManager gameData)
    {
        var sheet = gameData.GetExcelSheet<ENpcResident>(gameData.Language)!;
        var dict = new Dictionary<string, uint>((int)sheet.Count);
        foreach (var n in sheet.Where(e => e.Singular.ByteLength > 0))
            dict.TryAdd(DataUtility.ToTitleCaseExtended(n.Singular, n.Article), n.RowId);
        return dict.ToFrozenDictionary();
    }

    /// <inheritdoc cref="ReverseNameDictionary.TryGetValue"/>
    public bool TryGetValue(string key, [NotNullWhen(true)] out uint value)
        => Value.TryGetValue(key, out value);

    /// <inheritdoc cref="ReverseNameDictionary.this"/>
    public uint this[string key]
        => Value[key];
}