#if !DEBUG
using System.Reflection;
#endif

namespace CustomizePlus.Core.Helpers;

internal static class VersionHelper
{
    public static string Version { get; private set; } = "Initializing";

    public static bool IsTesting { get; private set; } = false;

    public static bool IsDebug { get; private set; } = false;

    public static bool IsValidate { get; private set; } = false;

    static VersionHelper()
    {
        #if DEBUG
            Version = $"{ThisAssembly.Git.Commit}+{ThisAssembly.Git.Sha} [DEBUG]";
            IsDebug = true;
        #else
            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty;
        #endif

        if (ThisAssembly.Git.BaseTag.ToLowerInvariant().Contains("testing"))
            IsTesting = true;

        #if VALIDATE_BUILD
        IsValidate = true;
        #endif

        if (IsTesting)
            Version += " [TESTING BUILD]";

        if (IsValidate)
            Version += " [VALIDATE BUILD]";
    }
}
