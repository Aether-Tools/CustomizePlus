using Penumbra.GameData.Interop;
using OtterGui.Log;
using System.Linq;

namespace CustomizePlus.Game.Services;

public class EmoteService
{

    private readonly Logger _logger;

    public EmoteService(Logger logger)
    {
        _logger = logger;
    }

    private static readonly ushort[] ChairSitEmotes = { 50, 95, 96, 254, 255 }; // not groundsit

    public unsafe bool IsSitting(Actor actor)
    {
        if (!actor.IsCharacter || actor.AsCharacter == null)
            return false;

        var emoteId = actor.AsCharacter->EmoteController.EmoteId;
        var isSitting = ChairSitEmotes.Contains(emoteId);

        _logger.Debug($"Actor {actor.Utf8Name} EmoteId: {emoteId} | Sitting: {isSitting}");
        return isSitting;
    }
}
