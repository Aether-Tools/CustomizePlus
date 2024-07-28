﻿using System.Collections.Generic;
using CustomizePlus.Core.Data;
using CustomizePlus.GameData.Extensions;
using Dalamud.Plugin.Services;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Interop;
using ObjectManager = CustomizePlus.GameData.Services.ObjectManager;
using DalamudGameObject = Dalamud.Game.ClientState.Objects.Types.IGameObject;
using ECommons.Configuration;
using System;
using CustomizePlus.Configuration.Data;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Penumbra.GameData;
using Penumbra.String;
using Dalamud.Logging;

namespace CustomizePlus.Game.Services;

public class GameObjectService
{
    private readonly ActorManager _actorManager;
    private readonly IObjectTable _objectTable;
    private readonly ObjectManager _objectManager;
    private readonly PluginConfiguration _configuration;

    public GameObjectService(
        ActorManager actorManager,
        IObjectTable objectTable,
        ObjectManager objectManager,
        PluginConfiguration configuration)
    {
        _actorManager = actorManager;
        _objectTable = objectTable;
        _objectManager = objectManager;
        _configuration = configuration;
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

    public DalamudGameObject? GetGameObjectById(ulong id)
    {
        return _objectTable.SearchById(id);
    }

    /// <summary>
    /// Case sensitive
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public IEnumerable<(ActorIdentifier, Actor)> FindActorsByName(string name)
    {
        _objectManager.Update();

        foreach (var kvPair in _objectManager.Identifiers)
        {
            var identifier = kvPair.Key;

            (identifier, _) = GetTrueActorForSpecialTypeActor(identifier);

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
        _objectManager.Update();
        return _objectManager.Player;
    }

    public DalamudGameObject? GetDalamudGameObjectFromActor(Actor actor)
    {
        return _objectTable.CreateObjectReference(actor);
    }

    /// <summary>
    /// Get "true" actor for special actors.
    /// This should be used everywhere where resolving proper actor is crucial for proper profile application
    /// as identifiers returned by object manager with type "Special" need special handling.
    /// </summary>
    public (ActorIdentifier, SpecialResult) GetTrueActorForSpecialTypeActor(ActorIdentifier identifier)
    {
        if (identifier.Type != IdentifierType.Special)
            return (identifier, SpecialResult.Invalid);

        if (_actorManager.ResolvePartyBannerPlayer(identifier.Special, out var id))
            return _configuration.ProfileApplicationSettings.ApplyInCards ? (id, SpecialResult.PartyBanner) : (identifier, SpecialResult.Invalid);

        if (_actorManager.ResolvePvPBannerPlayer(identifier.Special, out id))
            return _configuration.ProfileApplicationSettings.ApplyInCards ? (id, SpecialResult.PvPBanner) : (identifier, SpecialResult.Invalid);

        if (_actorManager.ResolveMahjongPlayer(identifier.Special, out id))
            return _configuration.ProfileApplicationSettings.ApplyInCards ? (id, SpecialResult.Mahjong) : (identifier, SpecialResult.Invalid);

        switch (identifier.Special)
        {
            case ScreenActor.GPosePlayer:
                return (_actorManager.GetCurrentPlayer(), SpecialResult.GPosePlayer);
            case ScreenActor.CharacterScreen when _configuration.ProfileApplicationSettings.ApplyInCharacterWindow:
                return (_actorManager.GetCurrentPlayer(), SpecialResult.CharacterScreen);
            case ScreenActor.FittingRoom when _configuration.ProfileApplicationSettings.ApplyInTryOn:
                return (_actorManager.GetCurrentPlayer(), SpecialResult.FittingRoom);
            case ScreenActor.DyePreview when _configuration.ProfileApplicationSettings.ApplyInTryOn:
                return (_actorManager.GetCurrentPlayer(), SpecialResult.DyePreview);
            case ScreenActor.Portrait when _configuration.ProfileApplicationSettings.ApplyInCards:
                return (_actorManager.GetCurrentPlayer(), SpecialResult.Portrait);
            case ScreenActor.ExamineScreen:
                {
                    identifier = _actorManager.GetInspectPlayer();
                    if (identifier.IsValid)
                        return (_configuration.ProfileApplicationSettings.ApplyInInspect ? identifier : ActorIdentifier.Invalid, SpecialResult.Inspect);

                    identifier = _actorManager.GetCardPlayer();
                    if (identifier.IsValid)
                        return (_configuration.ProfileApplicationSettings.ApplyInInspect ? identifier : ActorIdentifier.Invalid, SpecialResult.Card);

                    return _configuration.ProfileApplicationSettings.ApplyInTryOn
                        ? (_actorManager.GetGlamourPlayer(), SpecialResult.Glamour) //returns ActorIdentifier.Invalid if player is invalid
                        : (identifier, SpecialResult.Invalid);
                }
            default: return (identifier, SpecialResult.Invalid);
        }
    }

    public enum SpecialResult
    {
        PartyBanner,
        PvPBanner,
        Mahjong,
        CharacterScreen,
        FittingRoom,
        DyePreview,
        Portrait,
        Inspect,
        Card,
        Glamour,
        GPosePlayer,
        Invalid,
    }
}
