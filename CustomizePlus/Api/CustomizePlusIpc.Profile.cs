using CustomizePlus.Profiles.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using ECommons.EzIpcManager;
using Newtonsoft.Json;
using CustomizePlus.Api.Data;
using CustomizePlus.GameData.Data;
using CustomizePlus.Api.Enums;
using CustomizePlus.Profiles.Exceptions;
using CustomizePlus.Profiles.Data;
using CustomizePlus.Core.Extensions;
using CustomizePlus.Profiles.Events;
using CustomizePlus.Armatures.Data;
using CustomizePlus.Armatures.Events;
using CustomizePlus.GameData.Extensions;
using Dalamud.Game.ClientState.Objects.Types;
using Penumbra.GameData.Actors;

using IPCProfileDataTuple = (System.Guid UniqueId, string Name, string CharacterName, bool IsEnabled);
//using OnUpdateTuple = (Dalamud.Game.ClientState.Objects.Types.Character Character, System.Guid? ProfileUniqueId, string? ProfileJson);

namespace CustomizePlus.Api;

public partial class CustomizePlusIpc
{
    /// <summary>
    /// Triggered when changes in currently active profiles are detected. (like changing active profile or making any changes to it)
    /// Not triggered if any changes happen due to character no longer existing.
    /// Right now ignores every character but local player.
    /// Ignores temporary profiles.
    /// </summary>
    [EzIPCEvent("Profile.OnUpdate")]
    private Action<Character, Guid?> OnProfileUpdate;

    /// <summary>
    /// Retrieve list of all user profiles
    /// </summary>
    /// <returns></returns>
    [EzIPC("Profile.GetList")]
    private IList<IPCProfileDataTuple> GetProfileList()
    {
        return _profileManager.Profiles
            .Where(x => x.ProfileType == ProfileType.Normal)
            .Select(x => (x.UniqueId, x.Name.Text, x.CharacterName.Text, x.Enabled))
            .ToList();
    }

    /// <summary>
    /// Get JSON copy of profile with specified unique id
    /// </summary>
    [EzIPC("Profile.GetProfileById")]
    private (int, string?) GetProfileById(Guid uniqueId)
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
    /// <param name="uniqueId"></param>
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
    /// Get JSON copy of active profile for character.
    /// </summary>
    [EzIPC("Profile.GetCurrentlyActiveProfileOnCharacter")]
    private (int, string?) GetCurrentlyActiveProfileOnCharacter(Character character)
    {
        if (character == null)
            return ((int)ErrorCode.InvalidCharacter, null);

        var profile = _profileManager.GetProfileByCharacterName(character.Name.ToString(), true);

        if (profile == null)
            return ((int)ErrorCode.ProfileNotFound, null);

        var convertedProfile = IPCCharacterProfile.FromFullProfile(profile);

        if (convertedProfile == null)
        {
            _logger.Error($"IPCCharacterProfile.FromFullProfile returned empty converted profile for character {character?.Name.ToString().Incognify()}, profile: {profile.UniqueId}");
            return ((int)ErrorCode.UnknownError, null);
        }

        try
        {
            return ((int)ErrorCode.Success, JsonConvert.SerializeObject(convertedProfile));
        }
        catch(Exception ex)
        {
            _logger.Error($"Exception in IPCCharacterProfile.FromFullProfile for character {character?.Name.ToString().Incognify()}, profile: {profile.UniqueId}: {ex}");
            return ((int)ErrorCode.UnknownError, null);
        }
    }

    /// <summary>
    /// Apply provided profile as temporary profile on specified character.
    /// Returns profile's unique id which can be used to manipulate it at a later date.
    /// </summary>
    [EzIPC("Profile.SetTemporaryProfileOnCharacter")]
    private (int, Guid?) SetTemporaryProfileOnCharacter(Character character, string profileJson)
    {
        if (character == null)
            return ((int)ErrorCode.InvalidCharacter, null);

        var actor = (Actor)character.Address;
        if (!actor.Valid)
            return ((int)ErrorCode.InvalidCharacter, null);

        /*if (character == _objectTable[0])
        {
            _logger.Error($"Received request to set profile on local character, this is not allowed");
            return;
        }*/

        try
        {
            IPCCharacterProfile? profile;
            try
            {
                profile = JsonConvert.DeserializeObject<IPCCharacterProfile>(profileJson);
            }
            catch (Exception ex)
            {
                _logger.Error($"IPCCharacterProfile deserialization issue. Character: {character?.Name.ToString().Incognify()}, exception: {ex}.");
                return ((int)ErrorCode.CorruptedProfile, null);
            }

            if (profile == null)
            {
                _logger.Error($"IPCCharacterProfile is null after deserialization. Character: {character?.Name.ToString().Incognify()}.");
                return ((int)ErrorCode.CorruptedProfile, null);
            }

            //todo: ideally we'd probably want to make sure ID returned by that function does not have collision with other profiles
            var fullProfile = IPCCharacterProfile.ToFullProfile(profile).Item1;

            _profileManager.AddTemporaryProfile(fullProfile, actor);
            return ((int)ErrorCode.Success, fullProfile.UniqueId);

        }
        catch (Exception ex)
        {
            _logger.Error($"Unable to set temporary profile. Character: {character?.Name.ToString().Incognify()}, exception: {ex}.");
            return ((int)ErrorCode.UnknownError, null);
        }
    }

    /// <summary>
    /// Delete temporary profile currently active on character
    /// </summary>
    [EzIPC("Profile.DeleteTemporaryProfileOnCharacter")]
    private int DeleteTemporaryProfileOnCharacter(Character character)
    {
        if (character == null)
            return (int)ErrorCode.InvalidCharacter;

        var actor = (Actor)character.Address;
        if (!actor.Valid)
            return (int)ErrorCode.InvalidCharacter;

        /*if (character == _objectTable[0])
        {
            _logger.Error($"Received request to revert profile on local character, this is not allowed");
            return;
        }*/

        try
        {
            _profileManager.RemoveTemporaryProfile(actor);
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
                    _logger.Error($"Exception in DeleteTemporaryProfileOnCharacter. Character: {character?.Name.ToString().Incognify()}. Exception: {ex}");
                    return (int)ErrorCode.UnknownError;
            }
        }
        catch(Exception ex)
        {
            _logger.Error($"Exception in DeleteTemporaryProfileOnCharacter. Character: {character?.Name.ToString().Incognify()}. Exception: {ex}");
            return (int)ErrorCode.UnknownError;
        }
    }

    /// <summary>
    /// Delete temporary profile using its unique id
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

    //warn: limitation - ignores default profiles but why you would use default profile on your own character
    private void OnProfileChange(ProfileChanged.Type type, Profile? profile, object? arg3)
    {
        if (type != ProfileChanged.Type.AddedTemplate &&
            type != ProfileChanged.Type.RemovedTemplate &&
            type != ProfileChanged.Type.MovedTemplate &&
            type != ProfileChanged.Type.ChangedTemplate &&
            type != ProfileChanged.Type.Toggled)
            return;

        if (profile == null ||
            !profile.Enabled ||  //profile = null event will be sent from OnArmatureChanged
            profile.CharacterName.Text != _gameObjectService.GetCurrentPlayerName())
            return;

        Character? localPlayerCharacter = (Character?)_gameObjectService.GetDalamudGameObjectFromActor(_gameObjectService.GetLocalPlayerActor());
        if (localPlayerCharacter == null)
            return;

        OnProfileUpdateInternal(localPlayerCharacter, profile);
    }

    private void OnArmatureChanged(ArmatureChanged.Type type, Armature armature, object? arg3)
    {
        string currentPlayerName = _gameObjectService.GetCurrentPlayerName();

        if (armature.ActorIdentifier.ToNameWithoutOwnerName() != currentPlayerName)
            return;

        Character? localPlayerCharacter = (Character?)_gameObjectService.GetDalamudGameObjectFromActor(_gameObjectService.GetLocalPlayerActor());
        if (localPlayerCharacter == null)
            return;

        if (type == ArmatureChanged.Type.Created || //todo: might create second call after OnProfileChange?
            type == ArmatureChanged.Type.Rebound)
        {
            if (armature.Profile == null)
                _logger.Warning("Armature created/rebound and profile is null");

            OnProfileUpdateInternal(localPlayerCharacter, armature.Profile);
            return;
        }

        if (type == ArmatureChanged.Type.Deleted)
        {
            OnProfileUpdateInternal(localPlayerCharacter, null);
            return;
        }
    }

    private void OnProfileUpdateInternal(Character character, Profile? profile)
    {
        if (character == null)
            return;

        if (profile != null && profile.IsTemporary)
            return;

        _logger.Debug($"Sending player update message: Character: {character.Name.ToString().Incognify()}, Profile: {(profile != null ? profile.ToString() : "no profile")}");

        OnProfileUpdate(character, profile != null ? profile.UniqueId : null);
    }
}
