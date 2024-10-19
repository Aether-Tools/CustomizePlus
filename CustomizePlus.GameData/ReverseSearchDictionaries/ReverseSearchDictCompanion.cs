using Dalamud.Plugin.Services;
using Dalamud.Plugin;
using OtterGui.Log;
using Penumbra.GameData.Data;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Lumina.Excel.GeneratedSheets;
using CustomizePlus.GameData.ReverseSearchDictionaries.Bases;

namespace CustomizePlus.GameData.ReverseSearchDictionaries;

/// <summary> A dictionary that matches companion names to their ids. </summary>
public sealed class ReverseSearchDictCompanion(IDalamudPluginInterface pluginInterface, Logger log, IDataManager gameData)
    : ReverseNameDictionary(pluginInterface, log, gameData, "ReverseSearchCompanions", 7, () => CreateCompanionData(gameData))
{
    /// <summary> Create the data. </summary>
    private static IReadOnlyDictionary<string, uint> CreateCompanionData(IDataManager gameData)
    {
        var sheet = gameData.GetExcelSheet<Companion>(gameData.Language)!;
        var dict = new Dictionary<string, uint>((int)sheet.RowCount);
        foreach (var c in sheet.Where(c => c.Singular.RawData.Length > 0 && c.Order < ushort.MaxValue))
            dict.TryAdd(DataUtility.ToTitleCaseExtended(c.Singular, c.Article), c.RowId);
        return dict.ToFrozenDictionary();
    }

    /// <inheritdoc cref="ReverseNameDictionary.TryGetValue"/>
    public bool TryGetValue(string key, [NotNullWhen(true)] out uint value)
        => Value.TryGetValue(key, out value);

    /// <inheritdoc cref="ReverseNameDictionary.this"/>
    public uint this[string key]
        => Value[key];
}