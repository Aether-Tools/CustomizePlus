using Dalamud.Plugin.Services;
using Dalamud.Plugin;
using OtterGui.Log;
using Penumbra.GameData.Data;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using CustomizePlus.GameData.ReverseSearchDictionaries.Bases;
using Dalamud.Utility;
using Lumina.Excel.Sheets;

namespace CustomizePlus.GameData.ReverseSearchDictionaries;

/// <summary> A dictionary that matches names to mount ids. </summary>
public sealed class ReverseSearchDictMount(IDalamudPluginInterface pluginInterface, Logger log, IDataManager gameData)
    : ReverseNameDictionary(pluginInterface, log, gameData, "ReverseSearchMounts", 7, () => CreateMountData(gameData))
{
    /// <summary> Create the data. </summary>
    private static IReadOnlyDictionary<string, uint> CreateMountData(IDataManager gameData)
    {
        var sheet = gameData.GetExcelSheet<Mount>(gameData.Language)!;
        var dict = new Dictionary<string, uint>((int)sheet.Count);
        // Add some custom data.
        dict.TryAdd("Falcon (Porter)", 119);
        dict.TryAdd("Hippo Cart (Quest)", 295);
        dict.TryAdd("Hippo Cart (Quest)", 296);
        dict.TryAdd("Miw Miisv (Quest)", 298);
        dict.TryAdd("Moon-hopper (Quest)", 309);
        foreach (var m in sheet)
        {
            if (m.Singular.ByteLength > 0 && m.Order >= 0)
            {
                dict.TryAdd(DataUtility.ToTitleCaseExtended(m.Singular, m.Article), m.RowId);
            }
            else if (m.Unknown1.ByteLength > 0)
            {
                // Try to transform some file names into category names.
                var whistle = m.Unknown18.ToString();
                whistle = whistle.Replace("SE_Bt_Etc_", string.Empty)
                    .Replace("Mount_", string.Empty)
                    .Replace("_call", string.Empty)
                    .Replace("Whistle", string.Empty);
                dict.TryAdd($"? {whistle} #{m.RowId}", m.RowId);
            }
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