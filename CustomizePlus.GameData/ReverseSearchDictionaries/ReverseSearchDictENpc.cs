using Dalamud.Plugin.Services;
using Dalamud.Plugin;
using OtterGui.Log;
using Penumbra.GameData.Data;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using CustomizePlusPlus.GameData.ReverseSearchDictionaries.Bases;
using Lumina.Excel.Sheets;
using Dalamud.Game.ClientState.Objects.Enums;

namespace CustomizePlusPlus.GameData.ReverseSearchDictionaries;

#pragma warning disable SeStringEvaluator

/// <summary> A dictionary that matches names to event npc ids. </summary>
public sealed class ReverseSearchDictENpc(IDalamudPluginInterface pluginInterface, Logger log, IDataManager gameData, ISeStringEvaluator evaluator)
    : ReverseNameDictionary(pluginInterface, log, gameData, "ReverseSearchENpcs", Penumbra.GameData.DataContainers.Version.DictENpc, () => CreateENpcData(gameData, evaluator))
{
    /// <summary> Create the data. </summary>
    private static IReadOnlyDictionary<string, uint> CreateENpcData(IDataManager gameData, ISeStringEvaluator evaluator)
    {
        var sheet = gameData.GetExcelSheet<ENpcResident>(gameData.Language)!;
        var dict = new Dictionary<string, uint>((int)sheet.Count);
        foreach (var n in sheet.Where(e => e.Singular.ByteLength > 0))
            dict.TryAdd(evaluator.EvaluateObjStr(ObjectKind.EventNpc, n.RowId, gameData.Language), n.RowId);
        return dict.ToFrozenDictionary();
    }

    /// <inheritdoc cref="ReverseNameDictionary.TryGetValue"/>
    public bool TryGetValue(string key, [NotNullWhen(true)] out uint value)
        => Value.TryGetValue(key, out value);

    /// <inheritdoc cref="ReverseNameDictionary.this"/>
    public uint this[string key]
        => Value[key];
}