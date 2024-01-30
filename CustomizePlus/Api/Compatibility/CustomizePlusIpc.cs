using Dalamud.Plugin.Services;
using Dalamud.Plugin;
using System;
using System.Text;
using OtterGui.Log;
using Newtonsoft.Json;
using System.IO.Compression;
using System.IO;
using Dalamud.Plugin.Ipc;
using Dalamud.Game.ClientState.Objects.Types;
using CustomizePlus.Configuration.Helpers;
using CustomizePlus.Profiles;
using CustomizePlus.Configuration.Data.Version3;
using CustomizePlus.Profiles.Data;
using CustomizePlus.Game.Services;
using CustomizePlus.Templates.Events;
using CustomizePlus.Profiles.Events;
using CustomizePlus.Templates.Data;
using CustomizePlus.GameData.Data;
using CustomizePlus.Core.Extensions;
using CustomizePlus.Armatures.Events;
using CustomizePlus.Armatures.Data;
using CustomizePlus.GameData.Extensions;
using CustomizePlus.Templates;
using System.Linq;
using System.Runtime.CompilerServices;
using CustomizePlus.Profiles.Enums;
using IPCProfileDataTuple = (string Name, string characterName, bool IsEnabled, System.Guid ID);

namespace CustomizePlus.Api.Compatibility;

public class CustomizePlusIpc : IDisposable
{
    private readonly IObjectTable _objectTable;
    private readonly DalamudPluginInterface _pluginInterface;
    private readonly Logger _logger;
    private readonly ProfileManager _profileManager;
    private readonly GameObjectService _gameObjectService;

    private readonly ProfileChanged _profileChangedEvent;
    private readonly ArmatureChanged _armatureChangedEvent;

    private const int _configurationVersion = 3;

    public const string ProviderApiVersionLabel = $"CustomizePlus.{nameof(GetApiVersion)}";
    public const string GetProfileFromCharacterLabel = $"CustomizePlus.{nameof(GetProfileFromCharacter)}";
    public const string SetProfileToCharacterLabel = $"CustomizePlus.{nameof(SetProfileToCharacter)}";
    public const string RevertCharacterLabel = $"CustomizePlus.{nameof(RevertCharacter)}";
    public const string OnProfileUpdateLabel = $"CustomizePlus.{nameof(OnProfileUpdate)}";
    public const string GetProfileListLabel = $"CustomizePlus.{nameof(GetProfileList)}";
    public const string EnableProfileByUniqueIdLabel = $"CustomizePlus.{nameof(EnableProfileByUniqueId)}";
    public const string DisableProfileByUniqueIdLabel = $"CustomizePlus.{nameof(DisableProfileByUniqueId)}";
    public static readonly (int, int) ApiVersion = (3, 0);

    //Sends local player's profile every time their active profile is changed
    //If no profile is applied sends null
    internal ICallGateProvider<string?, string?, object?>? ProviderOnProfileUpdate;
    internal ICallGateProvider<Character?, object>? ProviderRevertCharacter;
    internal ICallGateProvider<string, Character?, object>? ProviderSetProfileToCharacter;
    internal ICallGateProvider<Character?, string?>? ProviderGetProfileFromCharacter;
    internal ICallGateProvider<(int, int)>? ProviderGetApiVersion;
    internal ICallGateProvider<IPCProfileDataTuple[]>? ProviderGetProfileList;
    internal ICallGateProvider<Guid, object>? ProviderEnableProfileByUniqueId;
    internal ICallGateProvider<Guid, object>? ProviderDisableProfileByUniqueId;

    public CustomizePlusIpc(
        IObjectTable objectTable,
        DalamudPluginInterface pluginInterface,
        Logger logger,
        ProfileManager profileManager,
        GameObjectService gameObjectService,
        ArmatureChanged armatureChangedEvent,
        ProfileChanged profileChangedEvent)
    {
        _objectTable = objectTable;
        _pluginInterface = pluginInterface;
        _logger = logger;
        _profileManager = profileManager;
        _gameObjectService = gameObjectService;
         _profileChangedEvent = profileChangedEvent;
        _armatureChangedEvent = armatureChangedEvent;

        InitializeProviders();

        _profileChangedEvent.Subscribe(OnProfileChange, ProfileChanged.Priority.CustomizePlusIpc);
        _armatureChangedEvent.Subscribe(OnArmatureChanged, ArmatureChanged.Priority.CustomizePlusIpc);
    }

    public void Dispose()
    {
        _profileChangedEvent.Unsubscribe(OnProfileChange);
        _armatureChangedEvent.Unsubscribe(OnArmatureChanged);
        DisposeProviders();
    }

    //warn: limitation - ignores default profiles but why you would use default profile on your own character
    private void OnProfileChange(ProfileChanged.Type type, Profile? profile, object? arg3)
    {
        if (type != ProfileChanged.Type.AddedTemplate &&
            type != ProfileChanged.Type.RemovedTemplate &&
            type != ProfileChanged.Type.MovedTemplate &&
            type != ProfileChanged.Type.ChangedTemplate)
            return;

        if (profile == null ||
            !profile.Enabled ||
            profile.CharacterName.Text != _gameObjectService.GetCurrentPlayerName())
            return;

        OnProfileUpdate(profile);
    }

    private void OnArmatureChanged(ArmatureChanged.Type type, Armature armature, object? arg3)
    {
        string currentPlayerName = _gameObjectService.GetCurrentPlayerName();

        if (armature.ActorIdentifier.ToNameWithoutOwnerName() != currentPlayerName)
            return;

        if (type == ArmatureChanged.Type.Created ||
            type == ArmatureChanged.Type.Rebound)
        {
            if(armature.Profile == null)
                _logger.Warning("Armature created/rebound and profile is null");

            OnProfileUpdate(armature.Profile);
            return;
        }

        if(type == ArmatureChanged.Type.Deleted)
        {
            OnProfileUpdate(null);
            return;
        }
    }

    private void InitializeProviders()
    {
        _logger.Debug("Initializing legacy Customize+ IPC providers.");
        try
        {
            ProviderGetApiVersion = _pluginInterface.GetIpcProvider<(int, int)>(ProviderApiVersionLabel);
            ProviderGetApiVersion.RegisterFunc(GetApiVersion);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error registering legacy Customize+ IPC provider for {ProviderApiVersionLabel}: {ex}");
        }

        try
        {
            ProviderGetProfileFromCharacter =
                _pluginInterface.GetIpcProvider<Character?, string?>(GetProfileFromCharacterLabel);
            ProviderGetProfileFromCharacter.RegisterFunc(GetProfileFromCharacter);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error registering legacy Customize+ IPC provider for {GetProfileFromCharacterLabel}: {ex}");
        }

        try
        {
            ProviderSetProfileToCharacter =
                _pluginInterface.GetIpcProvider<string, Character?, object>(SetProfileToCharacterLabel);
            ProviderSetProfileToCharacter.RegisterAction(SetProfileToCharacter);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error registering legacy Customize+ IPC provider for {SetProfileToCharacterLabel}: {ex}");
        }

        try
        {
            ProviderRevertCharacter =
                _pluginInterface.GetIpcProvider<Character?, object>(RevertCharacterLabel);
            ProviderRevertCharacter.RegisterAction(RevertCharacter);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error registering legacy Customize+ IPC provider for {RevertCharacterLabel}: {ex}");
        }

        try
        {
            ProviderOnProfileUpdate = _pluginInterface.GetIpcProvider<string?, string?, object?>(OnProfileUpdateLabel);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error registering legacy Customize+ IPC provider for {OnProfileUpdateLabel}: {ex}");
        }

        try
        {
            ProviderGetProfileList = _pluginInterface.GetIpcProvider<IPCProfileDataTuple[]>(GetProfileListLabel);
            ProviderGetProfileList.RegisterFunc(GetProfileList);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error registering Customize+ IPC provider for {GetProfileListLabel}: {ex}");
        }

        try
        {
            ProviderEnableProfileByUniqueId =
                _pluginInterface.GetIpcProvider<Guid, object>(EnableProfileByUniqueIdLabel);
            ProviderEnableProfileByUniqueId.RegisterAction(EnableProfileByUniqueId);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error registering Customize+ IPC provider for {EnableProfileByUniqueIdLabel}: {ex}");
        }

        try
        {
            ProviderDisableProfileByUniqueId =
                _pluginInterface.GetIpcProvider<Guid, object>(DisableProfileByUniqueIdLabel);
            ProviderDisableProfileByUniqueId.RegisterAction(DisableProfileByUniqueId);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error registering Customize+ IPC provider for {DisableProfileByUniqueIdLabel}: {ex}");
        }
    }

    private void DisposeProviders()
    {
        ProviderGetProfileFromCharacter?.UnregisterFunc();
        ProviderSetProfileToCharacter?.UnregisterAction();
        ProviderRevertCharacter?.UnregisterAction();
        ProviderGetApiVersion?.UnregisterFunc();
        ProviderOnProfileUpdate?.UnregisterFunc();
        ProviderGetProfileList?.UnregisterFunc();
        ProviderEnableProfileByUniqueId?.UnregisterAction();
        ProviderDisableProfileByUniqueId?.UnregisterAction();
    }

    private void OnProfileUpdate(Profile? profile)
    {
        _logger.Debug($"Sending local player update message: {(profile != null ? profile.ToString() : "no profile")}");

        var convertedProfile = profile != null ? GetVersion3Profile(profile) : null;

        ProviderOnProfileUpdate?.SendMessage(convertedProfile?.CharacterName ?? null, convertedProfile == null ? null : JsonConvert.SerializeObject(convertedProfile));
    }

    private static (int, int) GetApiVersion()
    {
        return ApiVersion;
    }

    private string? GetCharacterProfile(string characterName)
    {
        var profile = _profileManager.GetProfileByCharacterName(characterName, true);

        var convertedProfile = profile != null ? GetVersion3Profile(profile) : null;

        return convertedProfile != null ? JsonConvert.SerializeObject(convertedProfile) : null;
    }

    private string? GetProfileFromCharacter(Character? character)
    {
        return character == null ? null : GetCharacterProfile(character.Name.ToString());
    }

    private void SetProfileToCharacter(string profileJson, Character? character)
    {
        if (character == null)
            return;

        var actor = (Actor)character.Address;
        if (!actor.Valid)
            return;

        /*if (character == _objectTable[0])
        {
            _logger.Error($"Received request to set profile on local character, this is not allowed");
            return;
        }*/

        try
        {
            var profile = JsonConvert.DeserializeObject<Version3Profile>(profileJson);
            if (profile != null)
            {
                if (profile.ConfigVersion != _configurationVersion)
                    throw new Exception("Incompatible version");

                _profileManager.AddTemporaryProfile(GetProfileFromVersion3(profile).Item1, actor);
            }
        }
        catch (Exception ex)
        {
            _logger.Warning($"Unable to set body profile. Character: {character?.Name}, exception: {ex}, debug data: {GetBase64String(profileJson)}");
        }
    }

    private void RevertCharacter(Character? character)
    {
        if (character == null)
            return;

        var actor = (Actor)character.Address;
        if (!actor.Valid)
            return;

        /*if (character == _objectTable[0])
        {
            _logger.Error($"Received request to revert profile on local character, this is not allowed");
            return;
        }*/

        _profileManager.RemoveTemporaryProfile(actor);
    }

    private string GetBase64String(string data)
    {
        var json = JsonConvert.SerializeObject(data, Formatting.None);
        var bytes = Encoding.UTF8.GetBytes(json);
        using var compressedStream = new MemoryStream();
        using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            zipStream.Write(bytes, 0, bytes.Length);

        return Convert.ToBase64String(compressedStream.ToArray());
    }

    private Version3Profile GetVersion3Profile(Profile profile)
    {
        return V4ProfileToV3Converter.Convert(profile);
    }

    private (Profile, Template) GetProfileFromVersion3(Version3Profile profile)
    {
        return V3ProfileToV4Converter.Convert(profile);
    }

    private IPCProfileDataTuple[] GetProfileList()
    {
        return _profileManager.Profiles.Where(x => x.ProfileType == ProfileType.Normal).Select(x => (x.Name.Text, x.CharacterName.Text, x.Enabled, x.UniqueId)).ToArray();
    }

    private void EnableProfileByUniqueId(Guid UniqueID)
    {
        _profileManager.SetEnabled(UniqueID, true);
    }

    private void DisableProfileByUniqueId(Guid UniqueID)
    {
        _profileManager.SetEnabled(UniqueID, false);
    }
}
