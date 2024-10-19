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
    /// Matches() method but ignoring ownership for owned objects.
    /// </summary>
    public static bool MatchesIgnoringOwnership(this ActorIdentifier identifier, ActorIdentifier other)
    {
        if (identifier.Type != other.Type)
            return false;

        return identifier.Type switch
        {
            IdentifierType.Owned => PenumbraExtensions.Manager.DataIdEquals(identifier, other),
            _ => identifier.Matches(other)
        };
    }

    /// <summary>
    /// Check if owned actor is owned by local player. Will return false if Type is not Owned.
    /// </summary>
    public static bool IsOwnedByLocalPlayer(this ActorIdentifier identifier)
    {
        if (identifier.Type != IdentifierType.Owned)
            return false;

        if (PenumbraExtensions.Manager == null)
            return false;

        return identifier.PlayerName == PenumbraExtensions.Manager.GetCurrentPlayer().PlayerName;
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

    public static string TypeToString(this ActorIdentifier identifier)
    {
        return identifier.Type switch
        {
            IdentifierType.Player => $" ({PenumbraExtensions.Manager?.Data.ToWorldName(identifier.HomeWorld) ?? "Unknown"})",
            IdentifierType.Retainer => $"{identifier.Retainer switch
            {
                ActorIdentifier.RetainerType.Bell => " (Bell)",
                ActorIdentifier.RetainerType.Mannequin => " (Mannequin)",
                _ => " (Retainer)",
            }}",
            IdentifierType.Owned => " (Companion/Mount)",
            IdentifierType.Npc => " (NPC)",
            _ => "",
        };
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
