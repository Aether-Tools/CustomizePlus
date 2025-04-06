using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CustomizePlus.Armatures.Data;
using CustomizePlus.Armatures.Events;
using CustomizePlus.Core.Data;
using CustomizePlus.Core.Extensions;
using CustomizePlus.Game.Services;
using CustomizePlus.Game.Services.GPose;
using CustomizePlus.GameData.Extensions;
using CustomizePlus.Profiles;
using CustomizePlus.Profiles.Data;
using CustomizePlus.Profiles.Events;
using CustomizePlus.Templates.Events;
using Dalamud.Plugin.Services;
using OtterGui.Classes;
using OtterGui.Log;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Interop;

namespace CustomizePlus.Armatures.Services;

public unsafe sealed class ArmatureManager : IDisposable
{
    private readonly ProfileManager _profileManager;
    private readonly IObjectTable _objectTable;
    private readonly GameObjectService _gameObjectService;
    private readonly TemplateChanged _templateChangedEvent;
    private readonly ProfileChanged _profileChangedEvent;
    private readonly Logger _logger;
    private readonly FrameworkManager _framework;
    private readonly ActorObjectManager _objectManager;
    private readonly ActorManager _actorManager;
    private readonly GPoseService _gposeService;
    private readonly ArmatureChanged _event;

    /// <summary>
    /// This is a movement flag for every object. Used to prevent calls to ApplyRootTranslation from both movement and render hooks.
    /// I know there are less than 1000 objects in object table but I want to be semi-protected from object table getting bigger in the future.
    /// </summary>
    private readonly bool[] _objectMovementFlagsArr = new bool[1000];

    public Dictionary<ActorIdentifier, Armature> Armatures { get; private set; } = new();

    public ArmatureManager(
        ProfileManager profileManager,
        IObjectTable objectTable,
        GameObjectService gameObjectService,
        TemplateChanged templateChangedEvent,
        ProfileChanged profileChangedEvent,
        Logger logger,
        FrameworkManager framework,
        ActorObjectManager objectManager,
        ActorManager actorManager,
        GPoseService gposeService,
        ArmatureChanged @event)
    {
        _profileManager = profileManager;
        _objectTable = objectTable;
        _gameObjectService = gameObjectService;
        _templateChangedEvent = templateChangedEvent;
        _profileChangedEvent = profileChangedEvent;
        _logger = logger;
        _framework = framework;
        _objectManager = objectManager;
        _actorManager = actorManager;
        _gposeService = gposeService;
        _event = @event;

        _templateChangedEvent.Subscribe(OnTemplateChange, TemplateChanged.Priority.ArmatureManager);
        _profileChangedEvent.Subscribe(OnProfileChange, ProfileChanged.Priority.ArmatureManager);
    }

    public void Dispose()
    {
        _templateChangedEvent.Unsubscribe(OnTemplateChange);
        _profileChangedEvent.Unsubscribe(OnProfileChange);
    }

    /// <summary>
    /// Main rendering function, called from rendering hook
    /// </summary>
    public void OnRender()
    {
        try
        {
            RefreshArmatures();
            ApplyArmatureTransforms();
        }
        catch (Exception ex)
        {
            _logger.Error($"Exception while rendering armatures:\n\t{ex}");
        }
    }

    /// <summary>
    /// Function called when game object movement is detected
    /// </summary>
    public void OnGameObjectMove(Actor actor)
    {
        if (!actor.Identifier(_actorManager, out var identifier))
            return;

        if (Armatures.TryGetValue(identifier, out var armature) && armature.IsBuilt && armature.IsVisible)
        {
            _objectMovementFlagsArr[actor.AsObject->ObjectIndex] = true;
            ApplyRootTranslation(armature, actor);
        }
    }

    /// <summary>
    /// Force profile rebind for all armatures
    /// </summary>
    public void RebindAllArmatures()
    {
        foreach (var kvPair in Armatures)
            kvPair.Value.IsPendingProfileRebind = true;
    }

    /// <summary>
    /// Deletes armatures which no longer have actor associated with them and creates armatures for new actors
    /// </summary>
    private void RefreshArmatures()
    {
        var currentTime = DateTime.UtcNow;
        var armatureExpirationDateTime = currentTime.AddSeconds(-30);
        foreach (var kvPair in Armatures.ToList())
        {
            var armature = kvPair.Value;
            //Only remove armatures which haven't been seen for a while
            //But remove armatures of special actors (like examine screen) right away
            if (!_objectManager.ContainsKey(kvPair.Value.ActorIdentifier) &&
                (armature.LastSeen <= armatureExpirationDateTime || armature.ActorIdentifier.Type == IdentifierType.Special))
            {
                _logger.Debug($"Removing armature {armature} because {kvPair.Key.IncognitoDebug()} is gone");
                RemoveArmature(armature, ArmatureChanged.DeletionReason.Gone);

                continue;
            }

            //armature is considered visible if 1 or less seconds passed since last time we've seen the actor
            armature.IsVisible = armature.LastSeen.AddSeconds(1) >= currentTime;
        }

        foreach (var obj in _objectManager)
        {
            var actorIdentifier = obj.Key.CreatePermanent();
            if (!Armatures.ContainsKey(actorIdentifier))
            {
                var activeProfile = _profileManager.GetEnabledProfilesByActor(actorIdentifier).FirstOrDefault();
                if (activeProfile == null)
                    continue;

                var newArm = new Armature(actorIdentifier, activeProfile);
                TryLinkSkeleton(newArm);
                Armatures.Add(actorIdentifier, newArm);
                _logger.Debug($"Added '{newArm}' for {actorIdentifier.IncognitoDebug()} to cache");
                _event.Invoke(ArmatureChanged.Type.Created, newArm, activeProfile);

                continue;
            }

            var armature = Armatures[actorIdentifier];

            armature.UpdateLastSeen(currentTime);

            if (armature.IsPendingProfileRebind)
            {
                _logger.Debug($"Armature {armature} is pending profile/bone rebind, rebinding...");
                armature.IsPendingProfileRebind = false;

                var activeProfile = _profileManager.GetEnabledProfilesByActor(actorIdentifier).FirstOrDefault();
                Profile? oldProfile = armature.Profile;
                if (activeProfile != armature.Profile)
                {
                    if (activeProfile == null)
                    {
                        _logger.Debug($"Removing armature {armature} because it doesn't have any active profiles");
                        RemoveArmature(armature, ArmatureChanged.DeletionReason.NoActiveProfiles);

                        if (obj.Value.Objects != null)
                        {
                            //Reset root translation
                            foreach (var actor in obj.Value.Objects)
                                ApplyRootTranslation(armature, actor, true);
                        }

                        continue;
                    }

                    armature.Profile.Armatures.Remove(armature);
                    armature.Profile = activeProfile;
                    activeProfile.Armatures.Add(armature);
                }

                armature.RebuildBoneTemplateBinding();

                _event.Invoke(ArmatureChanged.Type.Updated, armature, (activeProfile, oldProfile));
            }

            //Needed because skeleton sometimes appears to be not ready when armature is created
            //and also because we want to keep armature up to date with any character skeleton changes
            TryLinkSkeleton(armature);
        }
    }

    private unsafe void ApplyArmatureTransforms()
    {
        foreach (var kvPair in Armatures)
        {
            var armature = kvPair.Value;

            if (armature.IsBuilt && armature.IsVisible && _objectManager.TryGetValue(armature.ActorIdentifier, out var actorData))
            {
                foreach (var actor in actorData.Objects)
                {
                    ApplyPiecewiseTransformation(armature, actor, armature.ActorIdentifier);

                    if (!_objectMovementFlagsArr[actor.AsObject->ObjectIndex])
                    {
                        //todo: ApplyRootTranslation causes character flashing in gpose
                        //research if this can be fixed without breaking this functionality
                        if (_gposeService.IsInGPose)
                            continue;

                        ApplyRootTranslation(armature, actor);
                    }
                    else
                        _objectMovementFlagsArr[actor.AsObject->ObjectIndex] = false;
                }
            }
        }
    }

    /// <summary>
    /// Returns whether or not a link can be established between the armature and an in-game object.
    /// If unbuilt, the armature will be rebuilded.
    /// </summary>
    private bool TryLinkSkeleton(Armature armature)
    {

        if (!_objectManager.ContainsKey(armature.ActorIdentifier))
            return false;

        var actor = _objectManager[armature.ActorIdentifier].Objects[0];

        if (!armature.IsBuilt || armature.IsSkeletonUpdated(actor.Model.AsCharacterBase))
        {
            _logger.Debug($"Skeleton for actor #{actor.AsObject->ObjectIndex} tied to \"{armature}\" has changed");
            armature.RebuildSkeleton(actor.Model.AsCharacterBase);
        }

        return true;
    }

    /// <summary>
    /// Iterate through the skeleton of the given character base, and apply any transformations
    /// for which this armature contains corresponding model bones. This method of application
    /// is safer but more computationally costly
    /// </summary>
    private void ApplyPiecewiseTransformation(Armature armature, Actor actor, ActorIdentifier actorIdentifier)
    {
        var cBase = actor.Model.AsCharacterBase;

        var isMount = actorIdentifier.Type == IdentifierType.Owned &&
            actorIdentifier.Kind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.MountType;

        Actor? mountOwner = null;
        Armature? mountOwnerArmature = null;
        if (isMount)
        {
            (var ident, mountOwner) = _gameObjectService.FindActorsByName(actorIdentifier.PlayerName.ToString()).FirstOrDefault();
            Armatures.TryGetValue(ident, out mountOwnerArmature);
        }

        if (cBase != null)
        {
            for (var pSkeleIndex = 0; pSkeleIndex < cBase->Skeleton->PartialSkeletonCount; ++pSkeleIndex)
            {
                var currentPose = cBase->Skeleton->PartialSkeletons[pSkeleIndex].GetHavokPose(Constants.TruePoseIndex);

                if (currentPose != null)
                {
                    for (var boneIndex = 0; boneIndex < currentPose->Skeleton->Bones.Length; ++boneIndex)
                    {
                        if (armature.GetBoneAt(pSkeleIndex, boneIndex) is ModelBone mb
                            && mb != null
                            && mb.BoneName == currentPose->Skeleton->Bones[boneIndex].Name.String)
                        {
                            if (mb == armature.MainRootBone)
                            {
                                if (_gameObjectService.IsActorHasScalableRoot(actor) && mb.IsModifiedScale())
                                {
                                    cBase->DrawObject.Object.Scale = mb.CustomizedTransform!.Scaling;

                                    //Fix mount owner's scale if needed
                                    //todo: always keep owner's scale proper instead of scaling with mount if no armature found
                                    if (isMount && mountOwner != null && mountOwnerArmature != null)
                                    {
                                        var ownerDrawObject = cBase->DrawObject.Object.ChildObject;

                                        //limit to only modified scales because that is just easier to handle
                                        //because we don't need to hook into dismount code to reset character scale
                                        //todo: hook into dismount
                                        //https://github.com/Cytraen/SeatedSidekickSpectator/blob/main/SetModeHook.cs?
                                        if (cBase->DrawObject.Object.ChildObject == mountOwner.Value.Model &&
                                            mountOwnerArmature.MainRootBone.IsModifiedScale())
                                        {
                                            var baseScale = mountOwnerArmature.MainRootBone.CustomizedTransform!.Scaling;

                                            ownerDrawObject->Scale = new Vector3(Math.Abs(baseScale.X / cBase->DrawObject.Object.Scale.X),
                                                    Math.Abs(baseScale.Y / cBase->DrawObject.Object.Scale.Y),
                                                    Math.Abs(baseScale.Z / cBase->DrawObject.Object.Scale.Z));
                                        }
                                    }
                                }
                            }
                            else
                            {
                                mb.ApplyModelTransform(cBase);
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Apply root bone translation. If reset = true then this will only reset translation if it was edited in supplied armature.
    /// </summary>
    private void ApplyRootTranslation(Armature arm, Actor actor, bool reset = false)
    {
        //I'm honestly not sure if we should or even can check if cBase->DrawObject or cBase->DrawObject.Object is a valid object
        //So for now let's assume we don't need to check for that

        //2024/11/21: we no longer check cBase->DrawObject.IsVisible here so we can set object position in render hook.

        var cBase = actor.Model.AsCharacterBase;
        if (cBase != null)
        {
            //warn: hotpath for characters with n_root edits. IsApproximately might have some performance hit.
            var rootBoneTransform = arm.GetAppliedBoneTransform("n_root");
            if (rootBoneTransform == null || 
                rootBoneTransform.Translation.IsApproximately(Vector3.Zero, 0.00001f))
                return;

            if (reset)
            {
                cBase->DrawObject.Object.Position = actor.AsObject->Position;
                return;
            }

            if (rootBoneTransform.Translation.X == 0 &&
                rootBoneTransform.Translation.Y == 0 &&
                rootBoneTransform.Translation.Z == 0)
                return;

            //Reset position so we don't fly away
            cBase->DrawObject.Object.Position = actor.AsObject->Position;

            var newPosition = new FFXIVClientStructs.FFXIV.Common.Math.Vector3
            {
                X = cBase->DrawObject.Object.Position.X + rootBoneTransform.Translation.X,
                Y = cBase->DrawObject.Object.Position.Y + rootBoneTransform.Translation.Y,
                Z = cBase->DrawObject.Object.Position.Z + rootBoneTransform.Translation.Z
            };

            cBase->DrawObject.Object.Position = newPosition;
        }
    }

    private void RemoveArmature(Armature armature, ArmatureChanged.DeletionReason reason)
    {
        armature.Profile.Armatures.Remove(armature);
        Armatures.Remove(armature.ActorIdentifier);
        _logger.Debug($"Armature {armature} removed from cache");

        _event.Invoke(ArmatureChanged.Type.Deleted, armature, reason);
    }

    private void OnTemplateChange(TemplateChanged.Type type, Templates.Data.Template? template, object? arg3)
    {
        if (type is not TemplateChanged.Type.NewBone &&
            type is not TemplateChanged.Type.DeletedBone &&
            type is not TemplateChanged.Type.EditorCharacterChanged &&
            type is not TemplateChanged.Type.EditorEnabled &&
            type is not TemplateChanged.Type.EditorDisabled)
            return;

        if (type == TemplateChanged.Type.NewBone ||
            type == TemplateChanged.Type.DeletedBone) //type == TemplateChanged.Type.EditorCharacterChanged?
        {
            //In case a lot of events are triggered at the same time for the same template this should limit the amount of times bindings are unneccessary rebuilt
            _framework.RegisterImportant($"TemplateRebuild @ {template.UniqueId}", () =>
            {
                foreach (var profile in _profileManager.GetProfilesUsingTemplate(template))
                {
                    _logger.Debug($"ArmatureManager.OnTemplateChange New/Deleted bone or character changed: {type}, template: {template.Name.Text.Incognify()}, profile: {profile.Name.Text.Incognify()}->{profile.Enabled}->{profile.Armatures.Count} armatures");
                    if (!profile.Enabled || profile.Armatures.Count == 0)
                        continue;

                    profile.Armatures.ForEach(x => x.IsPendingProfileRebind = true);
                }
            });

            return;
        }

        if (type == TemplateChanged.Type.EditorCharacterChanged)
        {
            (var character, var profile) = ((ActorIdentifier, Profile))arg3;

            foreach (var armature in GetArmaturesForCharacter(character))
            {
                armature.IsPendingProfileRebind = true;
                _logger.Debug($"ArmatureManager.OnTemplateChange Editor profile character name changed, armature rebind scheduled: {type}, {armature}");
            }

            if (profile.Armatures.Count == 0)
                return;

            //Rebuild armatures for previous character
            foreach (var armature in profile.Armatures)
                armature.IsPendingProfileRebind = true;

            _logger.Debug($"ArmatureManager.OnTemplateChange Editor profile character name changed, armature rebind scheduled: {type}, profile: {profile.Name.Text.Incognify()}->{profile.Enabled}, new name: {character.Incognito(null)}");

            return;
        }

        if (type == TemplateChanged.Type.EditorEnabled ||
            type == TemplateChanged.Type.EditorDisabled)
        {
            ActorIdentifier actor;
            bool hasChanges;

            if(type == TemplateChanged.Type.EditorEnabled)
                actor = (ActorIdentifier)arg3;
            else
                (actor, hasChanges) = ((ActorIdentifier, bool))arg3;

            foreach (var armature in GetArmaturesForCharacter(actor))
            {
                armature.IsPendingProfileRebind = true;
                _logger.Debug($"ArmatureManager.OnTemplateChange template editor enabled/disabled: {type}, pending profile set for {armature}");
            }

            return;
        }
    }

    private void OnProfileChange(ProfileChanged.Type type, Profile? profile, object? arg3)
    {
        if (type is not ProfileChanged.Type.AddedTemplate &&
            type is not ProfileChanged.Type.RemovedTemplate &&
            type is not ProfileChanged.Type.MovedTemplate &&
            type is not ProfileChanged.Type.ChangedTemplate &&
            type is not ProfileChanged.Type.Toggled &&
            type is not ProfileChanged.Type.Deleted &&
            type is not ProfileChanged.Type.TemporaryProfileAdded &&
            type is not ProfileChanged.Type.TemporaryProfileDeleted &&
            type is not ProfileChanged.Type.AddedCharacter &&
            type is not ProfileChanged.Type.RemovedCharacter &&
            type is not ProfileChanged.Type.PriorityChanged &&
            type is not ProfileChanged.Type.ChangedDefaultProfile &&
            type is not ProfileChanged.Type.ChangedDefaultLocalPlayerProfile)
            return;

        if (type == ProfileChanged.Type.ChangedDefaultProfile || type == ProfileChanged.Type.ChangedDefaultLocalPlayerProfile)
        {
            var oldProfile = (Profile?)arg3;

            if (oldProfile == null || oldProfile.Armatures.Count == 0)
                return;

            foreach (var armature in oldProfile.Armatures)
                armature.IsPendingProfileRebind = true;

            _logger.Debug($"ArmatureManager.OnProfileChange Profile no longer default/default for local player, armatures rebind scheduled: {type}, old profile: {oldProfile.Name.Text.Incognify()}->{oldProfile.Enabled}");

            return;
        }

        if (profile == null)
        {
            _logger.Error($"ArmatureManager.OnProfileChange Invalid input for event: {type}, profile is null.");
            return;
        }

        if(type == ProfileChanged.Type.PriorityChanged)
        {
            if (!profile.Enabled)
                return;

            foreach (var character in profile.Characters)
            {
                if (!character.IsValid)
                    continue;

                foreach (var armature in GetArmaturesForCharacter(character))
                {
                    armature.IsPendingProfileRebind = true;
                    _logger.Debug($"ArmatureManager.OnProfileChange profile {profile} priority changed, planning rebind for armature {armature}");
                }
            }

            return;
        }

        if (type == ProfileChanged.Type.Toggled)
        {
            if (!profile.Enabled && profile.Armatures.Count == 0)
                return;

            if (profile == _profileManager.DefaultProfile ||
                profile == _profileManager.DefaultLocalPlayerProfile)
            {
                foreach (var kvPair in Armatures)
                {
                    var armature = kvPair.Value;
                    if (armature.Profile == _profileManager.DefaultProfile || //not the best solution but w/e
                        armature.Profile == _profileManager.DefaultLocalPlayerProfile)
                        armature.IsPendingProfileRebind = true;

                    _logger.Debug($"ArmatureManager.OnProfileChange default/default local player profile toggled, planning rebind for armature {armature}");
                }

                return;
            }

            foreach(var character in profile.Characters)
            {
                if (!character.IsValid)
                    continue;

                foreach (var armature in GetArmaturesForCharacter(character))
                {
                    armature.IsPendingProfileRebind = true;
                    _logger.Debug($"ArmatureManager.OnProfileChange profile {profile} toggled, planning rebind for armature {armature}");
                }
            }

            return;
        }

        if (type == ProfileChanged.Type.TemporaryProfileAdded)
        {
            foreach(var character in profile.Characters)
            {
                if (!character.IsValid || !Armatures.ContainsKey(character))
                    continue;

                var armature = Armatures[character];

                if (armature.Profile == profile)
                    return;

                armature.UpdateLastSeen();

                armature.IsPendingProfileRebind = true;
            }

            _logger.Debug($"ArmatureManager.OnProfileChange TemporaryProfileAdded, calling rebind for existing armature: {type}, data payload: {arg3?.ToString()}, profile: {profile.Name.Text.Incognify()}->{profile.Enabled}");

            return;
        }

        if (type == ProfileChanged.Type.AddedCharacter ||
            type == ProfileChanged.Type.RemovedCharacter)
        {
            if (arg3 == null)
                throw new InvalidOperationException("AddedCharacter/RemovedCharacter must supply actor identifier as an argument");

            ActorIdentifier actorIdentifier = (ActorIdentifier)arg3;
            if (!actorIdentifier.IsValid)
                return;

            foreach (var armature in GetArmaturesForCharacter(actorIdentifier))
                armature.IsPendingProfileRebind = true;

            _logger.Debug($"ArmatureManager.OnProfileChange AC/RC, armature rebind scheduled: {type}, data payload: {arg3?.ToString()?.Incognify()}, profile: {profile.Name.Text.Incognify()}->{profile.Enabled}");
            
            return;
        }

        if (type == ProfileChanged.Type.Deleted ||
            type == ProfileChanged.Type.TemporaryProfileDeleted)
        {
            if (profile.Armatures.Count == 0)
                return;

            foreach (var armature in profile.Armatures)
            {
                if (type == ProfileChanged.Type.TemporaryProfileDeleted)
                    armature.UpdateLastSeen(); //just to be safe

                armature.IsPendingProfileRebind = true;
            }

            _logger.Debug($"ArmatureManager.OnProfileChange DEL/TPD, armature rebind scheduled: {type}, data payload: {arg3?.ToString()?.Incognify()}, profile: {profile.Name.Text.Incognify()}->{profile.Enabled}");

            return;
        }

        //todo: shouldn't happen, but happens sometimes? I think?
        if (profile.Armatures.Count == 0)
            return;

        _logger.Debug($"ArmatureManager.OnProfileChange Added/Deleted/Moved/Changed template: {type}, data payload: {arg3?.ToString()}, profile: {profile.Name}->{profile.Enabled}->{profile.Armatures.Count} armatures");

        profile!.Armatures.ForEach(x => x.IsPendingProfileRebind = true);
    }

    /// <summary>
    /// Warn: should not be used for temporary profiles as this limits search for Type = Owned to things owned by local player.
    /// </summary>
    private IEnumerable<Armature> GetArmaturesForCharacter(ActorIdentifier actorIdentifier)
    {
        foreach (var kvPair in Armatures)
        {
            (var armatureActorIdentifier, _) = _gameObjectService.GetTrueActorForSpecialTypeActor(kvPair.Key);

            if (actorIdentifier.IsValid && armatureActorIdentifier.MatchesIgnoringOwnership(actorIdentifier) &&
                (armatureActorIdentifier.Type != IdentifierType.Owned || armatureActorIdentifier.IsOwnedByLocalPlayer()))
                yield return kvPair.Value;
        }
    }
}