using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomizePlus.GameData.ReverseSearchDictionaries;

public static class Versions
{
    public const int GlobalOffset = 0;
    public const int UsesExtractTextOffset = 0;
    public const int UsesTitleCaseOffset = 0 + UsesExtractTextOffset;

    public const int DictCompanionOffset = 9;
    public const int DictCompanion = UsesTitleCaseOffset + DictCompanionOffset;

    public const int DictBNpcOffset = 9;
    public const int DictBNpc = UsesTitleCaseOffset + DictBNpcOffset;

    public const int DictMountOffset = 9;
    public const int DictMount = UsesTitleCaseOffset + DictMountOffset;

    public const int DictENpcOffset = 9;
    public const int DictENpc = UsesTitleCaseOffset + DictENpcOffset;
}
