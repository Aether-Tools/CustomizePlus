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

namespace CustomizePlus.Api.Compatibility;

public class CustomizePlusIpc : IDisposable
{
    private readonly IObjectTable _objectTable;
    private readonly DalamudPluginInterface _pluginInterface;
    private readonly Logger _logger;
    private readonly ProfileManager _profileManager;
    private readonly GameObjectService _gameObjectService;

    private readonly TemplateChanged _templateChangedEvent;
    private readonly ProfileChanged _profileChangedEvent;

    private const int _configurationVersion = 3;

    public const string ProviderApiVersionLabel = $"CustomizePlus.{nameof(GetApiVersion)}";
    public const string GetProfileFromCharacterLabel = $"CustomizePlus.{nameof(GetProfileFromCharacter)}";
    public const string SetProfileToCharacterLabel = $"CustomizePlus.{nameof(SetProfileToCharacter)}";
    public const string RevertCharacterLabel = $"CustomizePlus.{nameof(RevertCharacter)}";
    //public const string OnProfileUpdateLabel = $"CustomizePlus.{nameof(OnProfileUpdate)}"; //I'm honestly not sure this is even used by mare
    public static readonly (int, int) ApiVersion = (3, 0);

    //Sends local player's profile on hooks reload (plugin startup) as well as any updates to their profile.
    //If no profile is applied sends null
    internal ICallGateProvider<string?, string?, object?>? ProviderOnProfileUpdate;
    internal ICallGateProvider<Character?, object>? ProviderRevertCharacter;
    internal ICallGateProvider<string, Character?, object>? ProviderSetProfileToCharacter;
    internal ICallGateProvider<Character?, string?>? ProviderGetProfileFromCharacter;
    internal ICallGateProvider<(int, int)>? ProviderGetApiVersion;

    public CustomizePlusIpc(
        IObjectTable objectTable,
        DalamudPluginInterface pluginInterface,
        Logger logger,
        ProfileManager profileManager,
        GameObjectService gameObjectService//,
        /*TemplateChanged templateChangedEvent,
        ProfileChanged profileChangedEvent*/)
    {
        _objectTable = objectTable;
        _pluginInterface = pluginInterface;
        _logger = logger;
        _profileManager = profileManager;
        _gameObjectService = gameObjectService;
        /*            _templateChangedEvent = templateChangedEvent;
                _profileChangedEvent = profileChangedEvent;*/

        InitializeProviders();

        /*_templateChangedEvent.Subscribe(OnTemplateChange, TemplateChanged.Priority.CustomizePlusIpc);
        _profileChangedEvent.Subscribe(OnProfileChange, ProfileChanged.Priority.CustomizePlusIpc);*/
    }

    public void Dispose()
    {
        DisposeProviders();
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
        /*
        try
        {
            ProviderOnProfileUpdate = _pluginInterface.GetIpcProvider<string?, string?, object?>(OnProfileUpdateLabel);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error registering legacy Customize+ IPC provider for {OnProfileUpdateLabel}: {ex}");
        }*/
    }

    private void DisposeProviders()
    {
        ProviderGetProfileFromCharacter?.UnregisterFunc();
        ProviderSetProfileToCharacter?.UnregisterAction();
        ProviderRevertCharacter?.UnregisterAction();
        ProviderGetApiVersion?.UnregisterFunc();
        ProviderOnProfileUpdate?.UnregisterFunc();
    }

    private void OnProfileUpdate(Profile? profile)
    {
        //Get player's body profile string and send IPC message
        _logger.Debug($"Sending local player update message: {profile?.Name ?? "no profile"} - {profile?.CharacterName ?? "no profile"}");

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
}
