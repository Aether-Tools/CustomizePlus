using Dalamud.Plugin.Services;
using Penumbra.GameData.Actors;
using System.Collections.Generic;
using CustomizePlus.Core.Data;
using CustomizePlus.GameData.Data;
using CustomizePlus.GameData.Services;
using CustomizePlus.GameData.Extensions;
using Penumbra.GameData.Enums;
using DalamudGameObject = Dalamud.Game.ClientState.Objects.Types.GameObject;

namespace CustomizePlus.Game.Services;

public class GameObjectService
{
    private readonly ActorManager _actorManager;
    private readonly IObjectTable _objectTable;
    private readonly ObjectManager _objectManager;

    public GameObjectService(ActorManager actorManager, IObjectTable objectTable, ObjectManager objectManager)
    {
        _actorManager = actorManager;
        _objectTable = objectTable;
        _objectManager = objectManager;
    }

    public string GetCurrentPlayerName()
    {
        return _objectManager.PlayerData.Identifier.ToName();
    }

    public string GetCurrentPlayerTargetName()
    {
        return _objectManager.TargetData.Identifier.ToNameWithoutOwnerName();
    }

    public bool IsActorHasScalableRoot(Actor actor)
    {
        if (!actor.Identifier(_actorManager, out var identifier))
            return false;

        return !Constants.IsInObjectTableBusyNPCRange(actor.Index.Index)
            && (identifier.IsAllowedForProfiles()
                || actor == _objectTable.GetObjectAddress(0));
    }

    /// <summary>
    /// Case sensitive
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public IEnumerable<(ActorIdentifier, Actor)> FindActorsByName(string name)
    {
        foreach (var kvPair in _objectManager)
        {
            var identifier = kvPair.Key;

            if (kvPair.Key.Type == IdentifierType.Special)
                identifier = identifier.GetTrueActorForSpecialType();

            if (!identifier.IsValid)
                continue;

            if (identifier.ToNameWithoutOwnerName() == name)
            {
                if (kvPair.Value.Objects.Count > 1) //in gpose we can have more than a single object for one actor
                    foreach (var obj in kvPair.Value.Objects)
                        yield return (kvPair.Key.CreatePermanent(), obj);
                else
                    yield return (kvPair.Key.CreatePermanent(), kvPair.Value.Objects[0]);
            }
        }
    }

    public Actor GetLocalPlayerActor()
    {
        return _objectManager.Player;
    }

    public DalamudGameObject? GetDalamudGameObjectFromActor(Actor actor)
    {
        return _objectTable.CreateObjectReference(actor);
    }
}
