using Dalamud.Utility;

namespace CustomizePlus.Core.Extensions
{
    internal static class StringExtensions
    {
        /// <summary>
        /// Incognify string. Usually used for logging character names and stuff. Does nothing in debug build.
        /// </summary>
        public static string Incognify(this string str)
        {
            if (str.IsNullOrWhitespace())
                return str;

#if DEBUG
            return str;
#endif

            if (str.Contains(" "))
            {
                var split = str.Split(' ');

                if (split.Length > 2)
                    return $"{str[..2]}...";

                return $"{split[0][0]}.{split[1][0]}";
            }

            return $"{str[..2]}...";
        }
    }
}
