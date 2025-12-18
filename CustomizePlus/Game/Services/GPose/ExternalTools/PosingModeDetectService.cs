using Dalamud.Game;
using Dalamud.Plugin.Services;

namespace CustomizePlus.Game.Services.GPose.ExternalTools;

/// <summary>
///     Service which detects if Anamnesis/Ktisis posing mode is enabled.
/// </summary>
public class PosingModeDetectService
{
    // Borrowed from Ktisis:
    // If this is NOP'd, Anam posing is enabled.
    internal static unsafe byte* AnamnesisFreezePosition;
    internal static unsafe byte* AnamnesisFreezeRotation;
    internal static unsafe byte* AnamnesisFreezeScale;

    internal static unsafe bool IsAnamnesisPositionFrozen =>
        AnamnesisFreezePosition != null && *AnamnesisFreezePosition == 0x90 || *AnamnesisFreezePosition == 0x00;

    internal static unsafe bool IsAnamnesisRotationFrozen =>
        AnamnesisFreezeRotation != null && *AnamnesisFreezeRotation == 0x90 || *AnamnesisFreezeRotation == 0x00;

    internal static unsafe bool IsAnamnesisScalingFrozen =>
        AnamnesisFreezeScale != null && *AnamnesisFreezeScale == 0x90 || *AnamnesisFreezeScale == 0x00;

    internal static bool IsAnamnesis =>
        IsAnamnesisPositionFrozen || IsAnamnesisRotationFrozen || IsAnamnesisScalingFrozen;

    public bool IsInPosingMode => IsAnamnesis; //Can't detect Ktisis for now

    public unsafe PosingModeDetectService(ISigScanner sigScanner)
    {
        AnamnesisFreezePosition = (byte*)sigScanner.ScanText("41 0F 29 24 12");
        AnamnesisFreezeRotation = (byte*)sigScanner.ScanText("41 0F 29 5C 12 10");
        AnamnesisFreezeScale = (byte*)sigScanner.ScanText("41 0F 29 44 12 20");
    }
}