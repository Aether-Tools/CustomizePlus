using Dalamud.Plugin.Services;
using Dalamud.Plugin;
using OtterGui.Log;
using Penumbra.GameData.Data;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using CustomizePlus.GameData.ReverseSearchDictionaries.Bases;
using Lumina.Excel.Sheets;
using Dalamud.Game.ClientState.Objects.Enums;

namespace CustomizePlus.GameData.ReverseSearchDictionaries;

#pragma warning disable SeStringEvaluator

/// <summary> A dictionary that matches companion names to their ids. </summary>
public sealed class ReverseSearchDictCompanion(IDalamudPluginInterface pluginInterface, Logger log, IDataManager gameData, ISeStringEvaluator evaluator)
    : ReverseNameDictionary(pluginInterface, log, gameData, "ReverseSearchCompanions", Penumbra.GameData.DataContainers.Version.DictCompanion, () => CreateCompanionData(gameData, evaluator))
{
    /// <summary> Create the data. </summary>
    private static IReadOnlyDictionary<string, uint> CreateCompanionData(IDataManager gameData, ISeStringEvaluator evaluator)
    {
        var sheet = gameData.GetExcelSheet<Companion>(gameData.Language)!;
        var dict = new Dictionary<string, uint>((int)sheet.Count);
        foreach (var c in sheet.Where(c => c.Singular.ByteLength > 0 && c.Order < ushort.MaxValue))
            dict.TryAdd(evaluator.EvaluateObjStr(ObjectKind.Companion, c.RowId, gameData.Language), c.RowId);
        return dict.ToFrozenDictionary();
    }

    /// <inheritdoc cref="ReverseNameDictionary.TryGetValue"/>
    public bool TryGetValue(string key, [NotNullWhen(true)] out uint value)
        => Value.TryGetValue(key, out value);

    /// <inheritdoc cref="ReverseNameDictionary.this"/>
    public uint this[string key]
        => Value[key];
}