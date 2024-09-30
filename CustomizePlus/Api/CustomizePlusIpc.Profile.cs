using CustomizePlus.Profiles.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using ECommons.EzIpcManager;
using Newtonsoft.Json;
using CustomizePlus.Api.Data;
using CustomizePlus.Api.Enums;
using CustomizePlus.Profiles.Exceptions;
using CustomizePlus.Profiles.Data;
using CustomizePlus.Core.Extensions;
using CustomizePlus.Armatures.Data;
using CustomizePlus.Armatures.Events;
using CustomizePlus.GameData.Extensions;
using Dalamud.Game.ClientState.Objects.Types;
using Penumbra.GameData.Interop;

//Virtual path is full path to the profile in the virtual folders created by user in the profile list UI
using IPCProfileDataTuple = (System.Guid UniqueId, string Name, string VirtualPath, string CharacterName, bool IsEnabled);
using Penumbra.GameData.Structs;

namespace CustomizePlus.Api;

public partial class CustomizePlusIpc
{
    /// <summary>
    /// Triggered when changes in currently active profiles are detected. (like changing active profile or making any changes to it)
    /// Not triggered if any changes happen due to character no longer existing.
    /// Right now ignores every character but local player. It is not recommended to assume that this will always be the case and not perform any checks on your side.
    /// Ignores temporary profiles.
    /// Returns game object table index and profile id
    /// /!\ If no profile is set on specified character profile id will be equal to Guid.Empty
    /// </summary>
    [EzIPCEvent("Profile.OnUpdate")]
    private Action<ushort, Guid> OnProfileUpdate;

    /// <summary>
    /// Retrieve list of all user profiles
    /// /!\ This might be somewhat heavy method to call, so please use with caution.
    /// </summary>
    [EzIPC("Profile.GetList")]
    private IList<IPCProfileDataTuple> GetProfileList()
    {
        return _profileManager.Profiles
            .Where(x => x.ProfileType == ProfileType.Normal)
            .Select(x =>
            {
                string path = _profileFileSystem.FindLeaf(x, out var leaf) ? leaf.FullName() : x.Name.Text;
                return (x.UniqueId, x.Name.Text, path, x.CharacterName.Text, x.Enabled);
            })
            .ToList();
    }

    /// <summary>
    /// Get JSON copy of profile with specified unique id
    /// </summary>
    [EzIPC("Profile.GetByUniqueId")]
    private (int, string?) GetProfileByUniqueId(Guid uniqueId)
    {
        if (uniqueId == Guid.Empty)
            return ((int)ErrorCode.ProfileNotFound, null);

        var profile = _profileManager.Profiles.Where(x => x.UniqueId == uniqueId && !x.IsTemporary).FirstOrDefault(); //todo: move into profile manager

        if (profile == null)
            return ((int)ErrorCode.ProfileNotFound, null);

        var convertedProfile = IPCCharacterProfile.FromFullProfile(profile);

        if (convertedProfile == null)
        {
            _logger.Error($"IPCCharacterProfile.FromFullProfile returned empty converted profile for id: {uniqueId}");
            return ((int)ErrorCode.UnknownError, null);
        }

        try
        {
            return ((int)ErrorCode.Success, JsonConvert.SerializeObject(convertedProfile));
        }
        catch (Exception ex)
        {
            _logger.Error($"Exception in IPCCharacterProfile.FromFullProfile for id {uniqueId}: {ex}");
            return ((int)ErrorCode.UnknownError, null);
        }
    }

    /// <summary>
    /// Enable profile using its Unique ID. Does not work on temporary profiles.
    /// </summary>
    [EzIPC("Profile.EnableByUniqueId")]
    private int EnableProfileByUniqueId(Guid uniqueId)
    {
        return (int)SetProfileStateInternal(uniqueId, true);
    }

    /// <summary>
    /// Disable profile using its Unique ID. Does not work on temporary profiles.
    /// </summary>
    [EzIPC("Profile.DisableByUniqueId")]
    private int DisableProfileByUniqueId(Guid uniqueId)
    {
        return (int)SetProfileStateInternal(uniqueId, false);
    }

    private ErrorCode SetProfileStateInternal(Guid uniqueId, bool state)
    {
        if (uniqueId == Guid.Empty)
            return ErrorCode.ProfileNotFound;

        try
        {
            _profileManager.SetEnabled(uniqueId, state);
            return ErrorCode.Success;
        }
        catch (ProfileNotFoundException ex)
        {
            return ErrorCode.ProfileNotFound;
        }
        catch (Exception ex)
        {
            _logger.Error($"Exception in SetProfileStateInternal. Unique id: {uniqueId}, state: {state}, exception: {ex}.");
            return ErrorCode.UnknownError;
        }
    }

    /// <summary>
    /// Get unique id of currently active profile for character using its game object table index.
    /// </summary>
    [EzIPC("Profile.GetActiveProfileIdOnCharacter")]
    private (int, Guid?) GetActiveProfileIdOnCharacter(ushort gameObjectIndex)
    {
        var actor = _gameObjectService.GetActorByObjectIndex(gameObjectIndex);

        if (actor == null || !actor.Value.Valid || !actor.Value.IsCharacter)
            return ((int)ErrorCode.InvalidCharacter, null);

        var profile = _profileManager.GetProfileByCharacterName(actor.Value.Utf8Name.ToString(), true);

        if (profile == null)
            return ((int)ErrorCode.ProfileNotFound, null);

        return ((int)ErrorCode.Success, profile.UniqueId);
    }

    /// <summary>
    /// Apply provided profile as temporary profile on specified character using its game object table index.
    /// Returns profile's unique id which can be used to manipulate it at a later date.
    /// </summary>
    [EzIPC("Profile.SetTemporaryProfileOnCharacter")]
    private (int, Guid?) SetTemporaryProfileOnCharacter(ushort gameObjectIndex, string profileJson)
    {
        var actor = _gameObjectService.GetActorByObjectIndex(gameObjectIndex);

        //todo: do not allow to set temporary profile on reserved actors (examine, etc)
        if (actor == null || !actor.Value.Valid || !actor.Value.IsCharacter)
            return ((int)ErrorCode.InvalidCharacter, null);

        try
        {
            IPCCharacterProfile? profile;
            try
            {
                profile = JsonConvert.DeserializeObject<IPCCharacterProfile>(profileJson);
            }
            catch (Exception ex)
            {
                _logger.Error($"IPCCharacterProfile deserialization issue. Character: {actor.Value.Utf8Name.ToString().Incognify()}, exception: {ex}.");
                return ((int)ErrorCode.CorruptedProfile, null);
            }

            if (profile == null)
            {
                _logger.Error($"IPCCharacterProfile is null after deserialization. Character: {actor.Value.Utf8Name.ToString().Incognify()}.");
                return ((int)ErrorCode.CorruptedProfile, null);
            }

            //todo: ideally we'd probably want to make sure ID returned by that function does not have collision with other profiles
            var fullProfile = IPCCharacterProfile.ToFullProfile(profile).Item1;

            _profileManager.AddTemporaryProfile(fullProfile, actor.Value);
            return ((int)ErrorCode.Success, fullProfile.UniqueId);

        }
        catch (Exception ex)
        {
            _logger.Error($"Unable to set temporary profile. Character: {actor.Value.Utf8Name.ToString().Incognify()}, exception: {ex}.");
            return ((int)ErrorCode.UnknownError, null);
        }
    }

    /// <summary>
    /// Delete temporary profile currently active on character using its game object table index.
    /// </summary>
    [EzIPC("Profile.DeleteTemporaryProfileOnCharacter")]
    private int DeleteTemporaryProfileOnCharacter(ushort gameObjectIndex)
    {
        var actor = _gameObjectService.GetActorByObjectIndex(gameObjectIndex);

        if (actor == null || !actor.Value.Valid || !actor.Value.IsCharacter)
            return (int)ErrorCode.InvalidCharacter;

        try
        {
            _profileManager.RemoveTemporaryProfile(actor.Value);
            return (int)ErrorCode.Success;
        }
        catch(ProfileException ex)
        {
            switch(ex)
            { 
                case ActorNotFoundException _:
                    return (int)ErrorCode.InvalidCharacter;
                case ProfileNotFoundException:
                    return (int)ErrorCode.ProfileNotFound;
                default:
                    _logger.Error($"Exception in DeleteTemporaryProfileOnCharacter. Character: {actor.Value.Utf8Name.ToString().Incognify()}. Exception: {ex}");
                    return (int)ErrorCode.UnknownError;
            }
        }
        catch(Exception ex)
        {
            _logger.Error($"Exception in DeleteTemporaryProfileOnCharacter. Character: {actor.Value.Utf8Name.ToString().Incognify()}. Exception: {ex}");
            return (int)ErrorCode.UnknownError;
        }
    }

    /// <summary>
    /// Delete temporary profile using its unique id.
    /// </summary>
    [EzIPC("Profile.DeleteTemporaryProfileByUniqueId")]
    private int DeleteTemporaryProfileByUniqueId(Guid uniqueId)
    {
        if (uniqueId == Guid.Empty)
            return (int)ErrorCode.ProfileNotFound;

        try
        {
            _profileManager.RemoveTemporaryProfile(uniqueId);
            return (int)ErrorCode.Success;
        }
        catch (ProfileException ex)
        {
            switch (ex)
            {
                case ActorNotFoundException _:
                    return (int)ErrorCode.InvalidCharacter; //note: this is not considered an error for this case, returned just so external caller knows what is going on
                case ProfileNotFoundException:
                    return (int)ErrorCode.ProfileNotFound;
                default:
                    _logger.Error($"Exception in DeleteTemporaryProfileOnCharacter. Unique id: {uniqueId}. Exception: {ex}");
                    return (int)ErrorCode.UnknownError;
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Exception in DeleteTemporaryProfileOnCharacter. Unique id: {uniqueId}. Exception: {ex}");
            return (int)ErrorCode.UnknownError;
        }
    }

    //warn: intended limitation - ignores default profiles because why you would use default profile on your own character
    private void OnArmatureChanged(ArmatureChanged.Type type, Armature armature, object? arg3)
    {
        string currentPlayerName = _gameObjectService.GetCurrentPlayerName();

        if (armature.ActorIdentifier.ToNameWithoutOwnerName() != currentPlayerName)
            return;

        if (armature.ActorIdentifier.HomeWorld == WorldId.AnyWorld) //Cutscene/GPose actors
            return;

        ICharacter? localPlayerCharacter = (ICharacter?)_gameObjectService.GetDalamudGameObjectFromActor(_gameObjectService.GetLocalPlayerActor());
        if (localPlayerCharacter == null)
            return;

        if (type == ArmatureChanged.Type.Created ||
            type == ArmatureChanged.Type.Updated)
        {
            if (armature.Profile == null)
                _logger.Fatal("INTEGRITY ERROR: Armature created/updated and profile is null");

            (Profile? activeProfile, Profile? oldProfile) = (null, null);
            if (type == ArmatureChanged.Type.Created)
                (activeProfile, oldProfile) = ((Profile?)arg3, null);
            else
                (activeProfile, oldProfile) = ((Profile?, Profile?))arg3;

            if (activeProfile != null)
            {
                if (activeProfile == _profileManager.DefaultProfile || activeProfile.ProfileType == ProfileType.Editor)
                {
                    //ignore any changes while player is in editor or if player changes between default profiles
                    //also do not send event if there were no active profile before
                    if (activeProfile == oldProfile || oldProfile == null)
                        return;

                    OnProfileUpdateInternal(localPlayerCharacter, null); //send empty profile when player enters editor or turns on default profile
                    return;
                }
            }

            //do not send event if we are exiting editor or disabling default profile and don't have any active profile
            if (oldProfile != null &&
                (oldProfile == _profileManager.DefaultProfile || oldProfile.ProfileType == ProfileType.Editor) &&
                activeProfile == null)
                return;

            OnProfileUpdateInternal(localPlayerCharacter, activeProfile);
            return;
        }

        if (type == ArmatureChanged.Type.Deleted)
        {
            //Do not send event if default or editor profile was used
            if (armature.Profile == _profileManager.DefaultProfile || armature.Profile.ProfileType == ProfileType.Editor) //todo: never send if ProfileType != normal?
                return;

            OnProfileUpdateInternal(localPlayerCharacter, null);
            return;
        }
    }

    private void OnProfileUpdateInternal(ICharacter character, Profile? profile)
    {
        if (character == null)
            return;

        if (profile != null && profile.IsTemporary)
            return;

        _logger.Debug($"Sending player update message: Character: {character.Name.ToString().Incognify()}, Profile: {(profile != null ? profile.ToString() : "no profile")}");

        OnProfileUpdate(character.ObjectIndex, profile != null ? profile.UniqueId : Guid.Empty);
    }
}
