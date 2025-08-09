using Penumbra.GameData.Interop;

namespace CustomizePlus.GameData.Extensions;

public static unsafe class ActorExtensions
{
    /// <summary>
    /// Returns if this actor is currently being rendered by the game or only exists as invisible object table entry. 
    /// </summary>
    public static bool IsRenderedByGame(this Actor actor)
    {
        return actor.AsCharacter->DrawObject != null && actor.Model.AsCharacterBase != null;
    }
}
