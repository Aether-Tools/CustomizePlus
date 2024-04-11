using Dalamud.Game.ClientState.Objects.Enums;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Interop;
using PenumbraExtensions = Penumbra.GameData.Actors.ActorIdentifierExtensions;

namespace CustomizePlus.GameData.Extensions;

public static class ActorIdentifierExtensions
{
    /// <summary>
    /// Get actor name. Without owner's name if this is owned object.
    /// </summary>
    /// <param name="identifier"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static string ToNameWithoutOwnerName(this ActorIdentifier identifier)
    {
        if (!identifier.IsValid)
            return "Invalid";

        if (identifier.Type != IdentifierType.Owned)
            return identifier.ToName();

        if (PenumbraExtensions.Manager == null)
            throw new Exception("ActorIdentifier.Manager is not initialized");

        return PenumbraExtensions.Manager.Data.ToName(identifier.Kind, identifier.DataId);
    }

    /// <summary>
    /// Wrapper around Incognito which returns non-incognito name in debug builds
    /// </summary>
    /// <param name="identifier"></param>
    /// <returns></returns>
    public static string IncognitoDebug(this ActorIdentifier identifier)
    {
        if (!identifier.IsValid)
            return "Invalid";

        try
        {
#if !INCOGNIFY_STRINGS
            return identifier.ToString();
#else
            return identifier.Incognito(null);
#endif
        }
        catch (Exception e)
        {
#if DEBUG
            throw;
#else
            return "Unknown";
#endif
        }
    }

    /// <summary>
    /// For now used to determine if root scaling should be allowed or not
    /// </summary>
    /// <param name="identifier"></param>
    /// <returns></returns>
    public static bool IsAllowedForProfiles(this ActorIdentifier identifier)
    {
        if (!identifier.IsValid)
            return false;

        switch (identifier.Type)
        {
            case IdentifierType.Player:
            case IdentifierType.Retainer:
            case IdentifierType.Npc:
                return true;
            case IdentifierType.Owned:
                return
                    identifier.Kind == ObjectKind.BattleNpc ||
                    //identifier.Kind == ObjectKind.MountType ||
                    identifier.Kind == ObjectKind.Companion;
            default:
                return false;
        }
    }
}
