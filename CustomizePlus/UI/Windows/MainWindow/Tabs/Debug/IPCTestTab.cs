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
using System;
using IPCProfileDataTuple = (string Name, string characterName, bool IsEnabled, System.Guid ID);

namespace CustomizePlus.UI.Windows.MainWindow.Tabs.Debug;

public class IPCTestTab //: IDisposable
{
    private readonly IObjectTable _objectTable;
    private readonly ProfileManager _profileManager;
    private readonly PopupSystem _popupSystem;
    private readonly GameObjectService _gameObjectService;
    private readonly ObjectManager _objectManager;
    private readonly ActorService _actorService;

    private readonly ICallGateSubscriber<(int, int)>? _getApiVersion;
    private readonly ICallGateSubscriber<string, Character?, object>? _setCharacterProfile;
    private readonly ICallGateSubscriber<Character?, string>? _getProfileFromCharacter;
    private readonly ICallGateSubscriber<Character?, object>? _revertCharacter;
    //private readonly ICallGateSubscriber<string?, string?, object?>? _onProfileUpdate;
    private readonly ICallGateSubscriber<IPCProfileDataTuple[]>? _GetProfileList;
    private readonly ICallGateSubscriber<Guid, object>? _EnableProfileByUniqueId;
    private readonly ICallGateSubscriber<Guid, object>? _DisableProfileByUniqueId;

    private string? _rememberedProfileJson;

    private (int, int) _apiVersion;

    private string? _targetCharacterName;

    private string _targetProfileID = "";

    public IPCTestTab(
        DalamudPluginInterface pluginInterface,
        IObjectTable objectTable,
        ProfileManager profileManager,
        PopupSystem popupSystem,
        ObjectManager objectManager,
        GameObjectService gameObjectService,
        ActorService actorService)
    {
        _objectTable = objectTable;
        _profileManager = profileManager;
        _popupSystem = popupSystem;
        _objectManager = objectManager;
        _gameObjectService = gameObjectService;
        _actorService = actorService;

        _popupSystem.RegisterPopup("ipc_v4_profile_remembered", "Current profile has been copied into memory");
        _popupSystem.RegisterPopup("ipc_get_profile_from_character_remembered", "GetProfileFromCharacter result has been copied into memory");
        _popupSystem.RegisterPopup("ipc_set_profile_to_character_done", "SetProfileToCharacter has been called with data from memory");
        _popupSystem.RegisterPopup("ipc_revert_done", "Revert has been called");

        _getApiVersion = pluginInterface.GetIpcSubscriber<(int, int)>("CustomizePlus.GetApiVersion");
        _apiVersion = _getApiVersion.InvokeFunc();

        _setCharacterProfile = pluginInterface.GetIpcSubscriber<string, Character?, object>("CustomizePlus.SetProfileToCharacter");
        _getProfileFromCharacter = pluginInterface.GetIpcSubscriber<Character?, string>("CustomizePlus.GetProfileFromCharacter");
        _revertCharacter = pluginInterface.GetIpcSubscriber<Character?, object>("CustomizePlus.RevertCharacter");
        /*_onProfileUpdate = pluginInterface.GetIpcSubscriber<string?, string?, object?>("CustomizePlus.OnProfileUpdate");
        _onProfileUpdate.Subscribe(OnProfileUpdate);*/
        _GetProfileList = pluginInterface.GetIpcSubscriber<IPCProfileDataTuple[]>("CustomizePlus.GetProfileList");
        _EnableProfileByUniqueId = pluginInterface.GetIpcSubscriber<Guid, object>("CustomizePlus.EnableProfileByUniqueId");
        _DisableProfileByUniqueId = pluginInterface.GetIpcSubscriber<Guid, object>("CustomizePlus.DisableProfileByUniqueId");
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
        //ImGui.Text($"Last profile update: {_lastProfileUpdate}, Character: {_lastProfileUpdateName}");
        ImGui.Text($"Memory: {(string.IsNullOrWhiteSpace(_rememberedProfileJson) ? "empty" : "has data")}");

        ImGui.Text("Character to operate on:");
        ImGui.SameLine();
        ImGui.InputText("##operateon", ref _targetCharacterName, 128);

        if (ImGui.Button("Copy current profile into memory as V3"))
        {
            var actors = _gameObjectService.FindActorsByName(_targetCharacterName).ToList();
            if (actors.Count == 0)
                return;

            if (!actors[0].Item2.Identifier(_actorService.AwaitedService, out var identifier))
                return;

            var profile = _profileManager.GetEnabledProfilesByActor(identifier).FirstOrDefault();
            if (profile == null)
                return;

            _rememberedProfileJson = JsonConvert.SerializeObject(V4ProfileToV3Converter.Convert(profile));
            _popupSystem.ShowPopup("ipc_v4_profile_remembered");
        }

        if (ImGui.Button("GetProfileFromCharacter into memory"))
        {
            var actors = _gameObjectService.FindActorsByName(_targetCharacterName).ToList();
            if (actors.Count == 0)
                return;

            _rememberedProfileJson = _getProfileFromCharacter!.InvokeFunc(FindCharacterByAddress(actors[0].Item2.Address));
            _popupSystem.ShowPopup("ipc_get_profile_from_character_remembered");
        }

        using (var disabled = ImRaii.Disabled(_rememberedProfileJson == null))
        {
            if (ImGui.Button("SetProfileToCharacter from memory") && _rememberedProfileJson != null)
            {
                var actors = _gameObjectService.FindActorsByName(_targetCharacterName).ToList();
                if (actors.Count == 0)
                    return;

                _setCharacterProfile!.InvokeAction(_rememberedProfileJson, FindCharacterByAddress(actors[0].Item2.Address));
                _popupSystem.ShowPopup("ipc_set_profile_to_character_done");
            }
        }

        if (ImGui.Button("RevertCharacter") && _rememberedProfileJson != null)
        {
            var actors = _gameObjectService.FindActorsByName(_targetCharacterName).ToList();
            if (actors.Count == 0)
                return;

            _revertCharacter!.InvokeAction(FindCharacterByAddress(actors[0].Item2.Address));
            _popupSystem.ShowPopup("ipc_revert_done");
        }

        if (ImGui.Button("Copy profile list to clipboard"))
        {
            ImGui.SetClipboardText(string.Join("\n", _GetProfileList!.InvokeFunc().Select(x => $"{x.Name}, {x.characterName}, {x.IsEnabled}, {x.ID}")));
        }

        ImGui.Text("Profile GUID to set:");
        ImGui.SameLine();
        ImGui.InputText("##profileguid", ref _targetProfileID, 128);

        if (ImGui.Button("Enable profile by GUID"))
        {
            _EnableProfileByUniqueId!.InvokeAction(Guid.Parse(_targetProfileID));
        }

        if (ImGui.Button("Disable profile by GUID"))
        {
            _DisableProfileByUniqueId!.InvokeAction(Guid.Parse(_targetProfileID));
        }
    }

    private Character? FindCharacterByAddress(nint address)
    {
        foreach (var obj in _objectTable)
            if (obj.Address == address)
                return (Character)obj;

        return null;
    }
}
