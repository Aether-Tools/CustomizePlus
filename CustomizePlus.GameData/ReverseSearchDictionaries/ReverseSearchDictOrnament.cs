using Dalamud.Plugin.Services;
using Dalamud.Plugin;
using OtterGui.Log;
using Penumbra.GameData.Data;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using CustomizePlus.GameData.ReverseSearchDictionaries.Bases;
using Lumina.Excel.Sheets;

namespace CustomizePlus.GameData.ReverseSearchDictionaries;

/// <summary> A dictionary that matches names to ornament ids. </summary>
public sealed class ReverseSearchDictOrnament(IDalamudPluginInterface pluginInterface, Logger log, IDataManager gameData)
    : ReverseNameDictionary(pluginInterface, log, gameData, "ReverseSearchOrnaments", Penumbra.GameData.DataContainers.Version.DictOrnament, () => CreateOrnamentData(gameData))
{
    /// <summary> Create the data. </summary>
    private static IReadOnlyDictionary<string, uint> CreateOrnamentData(IDataManager gameData)
    {
        var sheet = gameData.GetExcelSheet<Ornament>(gameData.Language)!;
        var dict = new Dictionary<string, uint>((int)sheet.Count);

        const uint removedContentIconId = 786;
        foreach (var m in sheet.Where(m => m.Singular.ByteLength > 0 && m.Order >= 0 && m.Icon != removedContentIconId)) {
            dict.TryAdd(DataUtility.ToTitleCaseExtended(m.Singular, gameData.Language), m.RowId);
        }

        return dict.ToFrozenDictionary();
    }

    /// <inheritdoc cref="ReverseNameDictionary.TryGetValue"/>
    public bool TryGetValue(string key, [NotNullWhen(true)] out uint value)
        => Value.TryGetValue(key, out value);

    /// <inheritdoc cref="ReverseNameDictionary.this"/>
    public uint this[string key]
        => Value[key];
}
