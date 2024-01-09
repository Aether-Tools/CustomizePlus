using Dalamud.Utility;
using System;

namespace CustomizePlus.Core.Extensions;

internal static class StringExtensions
{
    /// <summary>
    /// Incognify string. Usually used for logging character names and stuff. Does nothing in debug build.
    /// </summary>
    public static string Incognify(this string str)
    {
        if (str.IsNullOrWhitespace())
            return str;

#if !INCOGNIFY_STRINGS
        return str;
#endif
        if (str.Contains(" "))
        {
            var split = str.Split(' ');

            if (split.Length == 2)
                return $"{split[0][0]}.{split[1][0]}";
        }

        return str.GetCutString();
    }

    private static string GetCutString(this string str, int maxLength = 5)
    {
        if(str.Length > maxLength)
            return $"{str[..maxLength]}...";
        else
            return str[0..Math.Min(str.Length, maxLength)];
    }
}
