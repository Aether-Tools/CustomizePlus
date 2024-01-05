using Dalamud.Game.ClientState.Objects.Enums;
using Penumbra.GameData.Actors;

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
        if (identifier == ActorIdentifier.Invalid)
            return "Invalid";

        if (!identifier.IsValid || identifier.Type != IdentifierType.Owned)
            return identifier.ToName();

        if (ActorIdentifier.Manager == null)
            throw new Exception("ActorIdentifier.Manager is not initialized");

        return ActorIdentifier.Manager.Data.ToName(identifier.Kind, identifier.DataId);
    }

    /// <summary>
    /// Wrapper around Incognito which returns non-incognito name in debug builds
    /// </summary>
    /// <param name="identifier"></param>
    /// <returns></returns>
    public static string IncognitoDebug(this ActorIdentifier identifier)
    {
        if (identifier == ActorIdentifier.Invalid)
            return "Invalid";

        try
        {
#if DEBUG
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
        if (identifier == ActorIdentifier.Invalid)
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

    /// <summary>
    /// Get "true" actor for special actors. Returns ActorIdentifier.Invalid for non-special actors or if actor cannot be found.
    /// </summary>
    /// <param name="identifier"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static ActorIdentifier GetTrueActorForSpecialType(this ActorIdentifier identifier)
    {
        if (!identifier.IsValid)
            return ActorIdentifier.Invalid;

        if (identifier.Type != IdentifierType.Special)
            return ActorIdentifier.Invalid;

        if (ActorIdentifier.Manager == null)
            throw new Exception("ActorIdentifier.Manager is not initialized");

        switch (identifier.Special)
        {
            case ScreenActor.GPosePlayer:
            case ScreenActor.CharacterScreen:
            case ScreenActor.FittingRoom:
            case ScreenActor.DyePreview:
            case ScreenActor.Portrait:
                return ActorIdentifier.Manager.GetCurrentPlayer();
            case ScreenActor.ExamineScreen:
                var examineIdentifier = ActorIdentifier.Manager.GetInspectPlayer();

                if (!examineIdentifier.IsValid)
                    examineIdentifier = ActorIdentifier.Manager.GetGlamourPlayer(); //returns ActorIdentifier.Invalid if player is invalid

                if (!examineIdentifier.IsValid)
                    return ActorIdentifier.Invalid;

                return examineIdentifier;
            case ScreenActor.Card6:
            case ScreenActor.Card7:
            case ScreenActor.Card8:
                return ActorIdentifier.Manager.GetCardPlayer();
        }

        return ActorIdentifier.Invalid;
    }
}
