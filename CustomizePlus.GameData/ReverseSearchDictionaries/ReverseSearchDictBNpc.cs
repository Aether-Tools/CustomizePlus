using Dalamud.Plugin.Services;
using Dalamud.Plugin;
using OtterGui.Log;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using CustomizePlus.GameData.ReverseSearchDictionaries.Bases;
using Lumina.Excel.Sheets;
using Dalamud.Game.ClientState.Objects.Enums;

namespace CustomizePlus.GameData.ReverseSearchDictionaries;

#pragma warning disable SeStringEvaluator

/// <summary> A dictionary that matches names to battle npc ids. </summary>
public sealed class ReverseSearchDictBNpc(IDalamudPluginInterface pluginInterface, Logger log, IDataManager gameData, ISeStringEvaluator evaluator)
    : ReverseNameDictionary(pluginInterface, log, gameData, "ReverseSearchBNpcs", Penumbra.GameData.DataContainers.Version.DictBNpc, () => CreateBNpcData(gameData, evaluator))
{
    /// <summary> Create the data. </summary>
    private static IReadOnlyDictionary<string, uint> CreateBNpcData(IDataManager gameData, ISeStringEvaluator evaluator)
    {
        var sheet = gameData.GetExcelSheet<BNpcName>(gameData.Language)!;
        var dict = new Dictionary<string, uint>((int)sheet.Count);
        foreach (var n in sheet.Where(n => n.Singular.ByteLength > 0))
            dict.TryAdd(evaluator.EvaluateObjStr(ObjectKind.BattleNpc, n.RowId, gameData.Language), n.RowId);
        return dict.ToFrozenDictionary();
    }

    /// <inheritdoc cref="ReverseNameDictionary.TryGetValue"/>
    public bool TryGetValue(string key, [NotNullWhen(true)] out uint value)
        => Value.TryGetValue(key, out value);

    /// <inheritdoc cref="ReverseNameDictionary.this"/>
    public uint this[string key]
        => Value[key];
}
