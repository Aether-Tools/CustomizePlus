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

namespace CustomizePlus.UI.Windows.MainWindow.Tabs.Debug;

public class IPCTestTab //: IDisposable
{
    private readonly IObjectTable _objectTable;
    private readonly ProfileManager _profileManager;
    private readonly PopupSystem _popupSystem;
    private readonly GameObjectService _gameObjectService;
    private readonly ObjectManager _objectManager;
    private readonly ActorManager _actorManager;

    [EzIPC("General.GetApiVersion")] 
    private readonly Func<(int, int)> _getApiVersionIpcFunc;

    [EzIPC("General.IsValid")]
    private readonly Func<bool> _isValidIpcFunc;

    private readonly ICallGateSubscriber<string, Character?, object>? _setCharacterProfile;
    private readonly ICallGateSubscriber<Character?, string>? _getProfileFromCharacter;
    private readonly ICallGateSubscriber<Character?, object>? _revertCharacter;
    //private readonly ICallGateSubscriber<string?, string?, object?>? _onProfileUpdate;

    private string? _rememberedProfileJson;

    private (int, int) _apiVersion;
    private DateTime _lastValidCheckAt;
    private bool _validResult;

    private string? _targetCharacterName;

    public IPCTestTab(
        DalamudPluginInterface pluginInterface,
        IObjectTable objectTable,
        ProfileManager profileManager,
        PopupSystem popupSystem,
        ObjectManager objectManager,
        GameObjectService gameObjectService,
        ActorManager actorManager)
    {
        _objectTable = objectTable;
        _profileManager = profileManager;
        _popupSystem = popupSystem;
        _objectManager = objectManager;
        _gameObjectService = gameObjectService;
        _actorManager = actorManager;

        EzIPC.Init(this, "CustomizePlus");

        if (_getApiVersionIpcFunc != null)
            _apiVersion = _getApiVersionIpcFunc();

        _setCharacterProfile = pluginInterface.GetIpcSubscriber<string, Character?, object>("CustomizePlus.SetProfileToCharacter");
        _getProfileFromCharacter = pluginInterface.GetIpcSubscriber<Character?, string>("CustomizePlus.GetProfileFromCharacter");
        _revertCharacter = pluginInterface.GetIpcSubscriber<Character?, object>("CustomizePlus.RevertCharacter");
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

            if (!actors[0].Item2.Identifier(_actorManager, out var identifier))
                return;

            var profile = _profileManager.GetEnabledProfilesByActor(identifier).FirstOrDefault();
            if (profile == null)
                return;

            _rememberedProfileJson = JsonConvert.SerializeObject(V4ProfileToV3Converter.Convert(profile));
            _popupSystem.ShowPopup(PopupSystem.Messages.IPCV4ProfileRemembered);
        }

        if (ImGui.Button("GetProfileFromCharacter into memory"))
        {
            var actors = _gameObjectService.FindActorsByName(_targetCharacterName).ToList();
            if (actors.Count == 0)
                return;

            _rememberedProfileJson = _getProfileFromCharacter!.InvokeFunc(FindCharacterByAddress(actors[0].Item2.Address));
            _popupSystem.ShowPopup(PopupSystem.Messages.IPCGetProfileFromChrRemembered);
        }

        using (var disabled = ImRaii.Disabled(_rememberedProfileJson == null))
        {
            if (ImGui.Button("SetProfileToCharacter from memory") && _rememberedProfileJson != null)
            {
                var actors = _gameObjectService.FindActorsByName(_targetCharacterName).ToList();
                if (actors.Count == 0)
                    return;

                _setCharacterProfile!.InvokeAction(_rememberedProfileJson, FindCharacterByAddress(actors[0].Item2.Address));
                _popupSystem.ShowPopup(PopupSystem.Messages.IPCSetProfileToChrDone);
            }
        }

        if (ImGui.Button("RevertCharacter") && _rememberedProfileJson != null)
        {
            var actors = _gameObjectService.FindActorsByName(_targetCharacterName).ToList();
            if (actors.Count == 0)
                return;

            _revertCharacter!.InvokeAction(FindCharacterByAddress(actors[0].Item2.Address));
            _popupSystem.ShowPopup(PopupSystem.Messages.IPCRevertDone);
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
