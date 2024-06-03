using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using ImGuiNET;
using Newtonsoft.Json;
using OtterGui.Raii;
using System.Linq;
using CustomizePlus.Profiles;
using CustomizePlus.Configuration.Helpers;
using CustomizePlus.Game.Services;
using CustomizePlus.GameData.Services;
using Penumbra.GameData.Actors;
using ECommons.EzIpcManager;
using System;
using System.Collections;
using System.Collections.Generic;

using IPCProfileDataTuple = (System.Guid UniqueId, string Name, string VirtualPath, string CharacterName, bool IsEnabled);
using OtterGui.Log;
using CustomizePlus.Core.Extensions;
using CustomizePlus.Configuration.Data;
using CustomizePlus.Api.Data;

namespace CustomizePlus.UI.Windows.MainWindow.Tabs.Debug;

public class IPCTestTab //: IDisposable
{
    private readonly IObjectTable _objectTable;
    private readonly ProfileManager _profileManager;
    private readonly PopupSystem _popupSystem;
    private readonly GameObjectService _gameObjectService;
    private readonly ObjectManager _objectManager;
    private readonly ActorManager _actorManager;
    private readonly Logger _logger;

    [EzIPC("General.GetApiVersion")] 
    private readonly Func<(int, int)> _getApiVersionIpcFunc;

    [EzIPC("General.IsValid")]
    private readonly Func<bool> _isValidIpcFunc;

    [EzIPC("Profile.GetList")]
    private readonly Func<IList<IPCProfileDataTuple>> _getProfileListIpcFunc;

    [EzIPC("Profile.EnableByUniqueId")]
    private readonly Func<Guid, int> _enableProfileByUniqueIdIpcFunc;

    [EzIPC("Profile.DisableByUniqueId")]
    private readonly Func<Guid, int> _disableProfileByUniqueIdIpcFunc;

    [EzIPC("Profile.GetActiveProfileIdOnCharacter")]
    private readonly Func<Character, (int, Guid?)> _getActiveProfileIdOnCharacterIpcFunc;

    [EzIPC("Profile.SetTemporaryProfileOnCharacter")]
    private readonly Func<Character, string, (int, Guid?)> _setTemporaryProfileOnCharacterIpcFunc;

    [EzIPC("Profile.DeleteTemporaryProfileOnCharacter")]
    private readonly Func<Character, int> _deleteTemporaryProfileOnCharacterIpcFunc;

    [EzIPC("Profile.DeleteTemporaryProfileByUniqueId")]
    private readonly Func<Guid, int> _deleteTemporaryProfileByUniqueIdIpcFunc;

    [EzIPC("Profile.GetByUniqueId")]
    private readonly Func<Guid, (int, string?)> _getProfileByIdIpcFunc;

    //private readonly ICallGateSubscriber<string, Character?, object>? _setCharacterProfile;
    //private readonly ICallGateSubscriber<Character?, string>? _getProfileFromCharacter;
    //private readonly ICallGateSubscriber<Character?, object>? _revertCharacter;
    //private readonly ICallGateSubscriber<string?, string?, object?>? _onProfileUpdate;

    private string? _rememberedProfileJson;

    private (int, int) _apiVersion;
    private DateTime _lastValidCheckAt;
    private bool _validResult;

    private string? _targetCharacterName;

    private string _targetProfileId = "";

    public IPCTestTab(
        DalamudPluginInterface pluginInterface,
        IObjectTable objectTable,
        ProfileManager profileManager,
        PopupSystem popupSystem,
        ObjectManager objectManager,
        GameObjectService gameObjectService,
        ActorManager actorManager,
        Logger logger,
        PluginConfiguration configuration)
    {
        _objectTable = objectTable;
        _profileManager = profileManager;
        _popupSystem = popupSystem;
        _objectManager = objectManager;
        _gameObjectService = gameObjectService;
        _actorManager = actorManager;
        _logger = logger;

        if(configuration.DebuggingModeEnabled)
            EzIPC.Init(this, "CustomizePlus"); //do not init EzIPC if debugging disabled so no debug event hook is created

        if (_getApiVersionIpcFunc != null)
            _apiVersion = _getApiVersionIpcFunc();

        //_setCharacterProfile = pluginInterface.GetIpcSubscriber<string, Character?, object>("CustomizePlus.SetProfileToCharacter");
        //_getProfileFromCharacter = pluginInterface.GetIpcSubscriber<Character?, string>("CustomizePlus.GetProfileFromCharacter");
        //_revertCharacter = pluginInterface.GetIpcSubscriber<Character?, object>("CustomizePlus.RevertCharacter");
        /*_onProfileUpdate = pluginInterface.GetIpcSubscriber<string?, string?, object?>("CustomizePlus.OnProfileUpdate");
        _onProfileUpdate.Subscribe(OnProfileUpdate);*/
    }
    /* public void Dispose()
     {
         _onProfileUpdate?.Unsubscribe(OnProfileUpdate);
     }

     private void OnProfileUpdate(string? characterName, string? profileJson)
     {
         _lastProfileUpdate = DateTime.Now;
         _lastProfileUpdateName = characterName;
     }
    */
    public unsafe void Draw()
    {
        _objectManager.Update();

        if (_targetCharacterName == null)
            _targetCharacterName = _gameObjectService.GetCurrentPlayerName();

        ImGui.Text($"Version: {_apiVersion.Item1}.{_apiVersion.Item2}");

        ImGui.Text($"IsValid: {_validResult} ({_lastValidCheckAt} UTC)");

        ImGui.SameLine();
        if(ImGui.Button("Check IPC validity") || _lastValidCheckAt == DateTime.MinValue)
        {
            _validResult = _isValidIpcFunc();
            _lastValidCheckAt = DateTime.UtcNow;
        }

        ImGui.Separator();

        //ImGui.Text($"Last profile update: {_lastProfileUpdate}, Character: {_lastProfileUpdateName}");
        ImGui.Text($"Memory: {(string.IsNullOrWhiteSpace(_rememberedProfileJson) ? "empty" : "has data")}");

        ImGui.Text("Character to operate on:");
        ImGui.SameLine();
        ImGui.InputText("##operateon", ref _targetCharacterName, 128);

        if (ImGui.Button("Copy current profile into memory"))
        {
            var actors = _gameObjectService.FindActorsByName(_targetCharacterName).ToList();
            if (actors.Count == 0)
                return;

            if (!actors[0].Item2.Identifier(_actorManager, out var identifier))
                return;

            var profile = _profileManager.GetEnabledProfilesByActor(identifier).FirstOrDefault();
            if (profile == null)
                return;

            _rememberedProfileJson = JsonConvert.SerializeObject(IPCCharacterProfile.FromFullProfile(profile));
            _popupSystem.ShowPopup(PopupSystem.Messages.IPCProfileRemembered);
        }

        if (ImGui.Button("GetActiveProfileIdOnCharacter into clipboard"))
        {
            var actors = _gameObjectService.FindActorsByName(_targetCharacterName).ToList();
            if (actors.Count == 0)
                return;

            (int result, Guid? uniqueId) = _getActiveProfileIdOnCharacterIpcFunc(FindCharacterByAddress(actors[0].Item2.Address));

            if(result == 0)
            {
                ImGui.SetClipboardText(uniqueId.ToString());
                _popupSystem.ShowPopup(PopupSystem.Messages.IPCCopiedToClipboard);
            }
            else
            {
                _logger.Error($"Error code {result} while calling GetCurrentlyActiveProfileOnCharacter");
                _popupSystem.ShowPopup(PopupSystem.Messages.ActionError);
            }
        }

        using (var disabled = ImRaii.Disabled(_rememberedProfileJson == null))
        {
            if (ImGui.Button("SetTemporaryProfileOnCharacter from memory") && _rememberedProfileJson != null)
            {
                var actors = _gameObjectService.FindActorsByName(_targetCharacterName).ToList();
                if (actors.Count == 0)
                    return;

                (int result, Guid? profileGuid) = _setTemporaryProfileOnCharacterIpcFunc(FindCharacterByAddress(actors[0].Item2.Address), _rememberedProfileJson);
                if (result == 0)
                {
                    _popupSystem.ShowPopup(PopupSystem.Messages.IPCSetProfileToChrDone);
                    _logger.Information($"Temporary profile id: {profileGuid}");
                }
                else
                {
                    _logger.Error($"Error code {result} while calling SetTemporaryProfileOnCharacter");
                    _popupSystem.ShowPopup(PopupSystem.Messages.ActionError);
                }
            }
        }

        if (ImGui.Button("DeleteTemporaryProfileOnCharacter"))
        {
            var actors = _gameObjectService.FindActorsByName(_targetCharacterName).ToList();
            if (actors.Count == 0)
                return;

            int result = _deleteTemporaryProfileOnCharacterIpcFunc(FindCharacterByAddress(actors[0].Item2.Address));
            if (result == 0)
                _popupSystem.ShowPopup(PopupSystem.Messages.IPCRevertDone);
            else
            {
                _logger.Error($"Error code {result} while calling DeleteTemporaryProfileOnCharacter");
                _popupSystem.ShowPopup(PopupSystem.Messages.ActionError);
            }
        }

        ImGui.Separator();

        if (ImGui.Button("Copy user profile list to clipboard"))
        {
            ImGui.SetClipboardText(string.Join("\n", _getProfileListIpcFunc().Select(x => $"{x.UniqueId}, {x.Name}, {x.VirtualPath}, {x.CharacterName}, {x.IsEnabled}")));
            _popupSystem.ShowPopup(PopupSystem.Messages.IPCCopiedToClipboard);
        }

        ImGui.Text("Profile Unique ID:");
        ImGui.SameLine();
        ImGui.InputText("##profileguid", ref _targetProfileId, 128);

        if (ImGui.Button("Get profile by Unique ID into clipboard"))
        {
            (int result, string? profileJson) = _getProfileByIdIpcFunc(Guid.Parse(_targetProfileId));
            if (result == 0)
            {
                ImGui.SetClipboardText(profileJson);
                _popupSystem.ShowPopup(PopupSystem.Messages.IPCCopiedToClipboard);
            }
            else
            {
                _logger.Error($"Error code {result} while calling GetProfileById");
                _popupSystem.ShowPopup(PopupSystem.Messages.ActionError);
            }
        }

        if (ImGui.Button("Get profile by Unique ID into memory"))
        {
            (int result, string? profileJson) = _getProfileByIdIpcFunc(Guid.Parse(_targetProfileId));
            if (result == 0)
            {
                _rememberedProfileJson = profileJson;
                _popupSystem.ShowPopup(PopupSystem.Messages.IPCProfileRemembered);
            }
            else
            {
                _logger.Error($"Error code {result} while calling GetProfileById");
                _popupSystem.ShowPopup(PopupSystem.Messages.ActionError);
            }
        }

        if (ImGui.Button("Enable profile by Unique ID"))
        {
            int result = _enableProfileByUniqueIdIpcFunc(Guid.Parse(_targetProfileId));
            if (result == 0)
            {
                _popupSystem.ShowPopup(PopupSystem.Messages.IPCEnableProfileByIdDone);
            }
            else
            {
                _logger.Error($"Error code {result} while calling EnableByUniqueId");
                _popupSystem.ShowPopup(PopupSystem.Messages.ActionError);
            }
        }

        if (ImGui.Button("Disable profile by Unique ID"))
        {
            int result = _disableProfileByUniqueIdIpcFunc(Guid.Parse(_targetProfileId));
            if (result == 0)
            {
                _popupSystem.ShowPopup(PopupSystem.Messages.IPCDisableProfileByIdDone);
            }
            else
            {
                _logger.Error($"Error code {result} while calling DisableByUniqueId");
                _popupSystem.ShowPopup(PopupSystem.Messages.ActionError);
            }
        }

        if (ImGui.Button("DeleteTemporaryProfileByUniqueId"))
        {
            var actors = _gameObjectService.FindActorsByName(_targetCharacterName).ToList();
            if (actors.Count == 0)
                return;

            int result = _deleteTemporaryProfileByUniqueIdIpcFunc(Guid.Parse(_targetProfileId));
            if (result == 0)
                _popupSystem.ShowPopup(PopupSystem.Messages.IPCRevertDone);
            else
            {
                _logger.Error($"Error code {result} while calling DeleteTemporaryProfileByUniqueId");
                _popupSystem.ShowPopup(PopupSystem.Messages.ActionError);
            }
        }
    }

    [EzIPCEvent("Profile.OnUpdate")]
    private void OnProfileUpdate(Character Character, Guid ProfileUniqueId)
    {
        _logger.Debug($"IPC Test Tab - OnProfileUpdate: Character: {Character.Name.ToString().Incognify()}, Profile ID: {(ProfileUniqueId != Guid.Empty ? ProfileUniqueId.ToString() : "no id")}");
    }

    private Character? FindCharacterByAddress(nint address)
    {
        foreach (var obj in _objectTable)
            if (obj.Address == address)
                return (Character)obj;

        return null;
    }
}
