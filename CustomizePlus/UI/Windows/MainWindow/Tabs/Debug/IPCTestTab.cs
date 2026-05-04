using CustomizePlus.Api.Data;
using CustomizePlus.Configuration.Data;
using CustomizePlus.Core.Extensions;
using CustomizePlus.Game.Services;
using CustomizePlus.GameData.Extensions;
using CustomizePlus.Profiles;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ECommonsLite.EzIpcManager;
using Newtonsoft.Json;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;

namespace CustomizePlus.UI.Windows.MainWindow.Tabs.Debug;

//todo: buttons for Profile.EnableTemplateByUniqueId, Profile.DisableTemplateByUniqueId, Profile.SetPriorityByUniqueId
public class IPCTestTab : ITab<MainTabType> //: IDisposable
{
    private const string _ownedTesProfile = "{\"Bones\":{\"n_root\":{\"Translation\":{\"X\":0.0,\"Y\":0.0,\"Z\":0.0},\"Rotation\":{\"X\":0.0,\"Y\":0.0,\"Z\":0.0},\"Scaling\":{\"X\":2.0,\"Y\":2.0,\"Z\":2.0}}}}";

    private static JsonSerializerSettings _ipcProfileSerializerSettings = new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore };

    private readonly IObjectTable _objectTable;
    private readonly ProfileManager _profileManager;
    private readonly PopupSystem _popupSystem;
    private readonly GameObjectService _gameObjectService;
    private readonly ActorObjectManager _objectManager;
    private readonly ActorManager _actorManager;
    private readonly Logger _logger;
    private readonly PluginConfiguration _configuration;

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

    [EzIPC("Profile.SetPriorityByUniqueId")]
    private readonly Func<Guid, int, int> _setPriorityByUniqueIdIpcFunc;

    [EzIPC("Profile.GetActiveProfileIdOnCharacter")]
    private readonly Func<ushort, (int, Guid?)> _getActiveProfileIdOnCharacterIpcFunc;

    [EzIPC("Profile.SetTemporaryProfileOnCharacter")]
    private readonly Func<ushort, string, (int, Guid?)> _setTemporaryProfileOnCharacterIpcFunc;

    [EzIPC("Profile.DeleteTemporaryProfileOnCharacter")]
    private readonly Func<ushort, int> _deleteTemporaryProfileOnCharacterIpcFunc;

    [EzIPC("Profile.DeleteTemporaryProfileByUniqueId")]
    private readonly Func<Guid, int> _deleteTemporaryProfileByUniqueIdIpcFunc;

    [EzIPC("Profile.AddPlayerCharacter")]
    private readonly Func<Guid, string, ushort, int> _addPlayerCharacterIpcFunc;

    [EzIPC("Profile.RemovePlayerCharacter")]
    private readonly Func<Guid, string, ushort, int> _removePlayerCharacterIpcFunc;

    [EzIPC("Profile.GetByUniqueId")]
    private readonly Func<Guid, (int, string?)> _getProfileByIdIpcFunc;

    [EzIPC("Profile.GetTemplates")]
    private readonly Func<Guid, (int, List<IPCTemplateStatusTuple>?)> _getProfileTemplatesIpcFunc;

    [EzIPC("GameState.GetCutsceneParentIndex")]
    private readonly Func<int, int> _getCutsceneParentIdxIpcFunc;

    [EzIPC("GameState.SetCutsceneParentIndex")]
    private readonly Func<int, int, int> _setCutsceneParentIdxIpcFunc;

    private string? _rememberedProfileJson;

    private (int, int) _apiVersion;
    private DateTime _lastValidCheckAt;
    private bool _validResult;

    private string? _targetCharacterName;

    private string _targetProfileId = string.Empty;
    private int _targetProfilePriority = 0;

    private int _cutsceneActorIdx;
    private int _cutsceneActorParentIdx;


    public IPCTestTab(
        IDalamudPluginInterface pluginInterface,
        IObjectTable objectTable,
        ProfileManager profileManager,
        PopupSystem popupSystem,
        ActorObjectManager objectManager,
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
        _configuration = configuration;

        if (configuration.DebuggingModeEnabled)
            EzIPC.Init(this, "CustomizePlus"); //do not init EzIPC if debugging disabled so no debug event hook is created

        if (_getApiVersionIpcFunc != null)
            _apiVersion = _getApiVersionIpcFunc();
    }

    public ReadOnlySpan<byte> Label
        => "IPC Test"u8;

    public MainTabType Identifier
        => MainTabType.IPCTest;

    public bool IsVisible => _configuration.DebuggingModeEnabled;

    public void DrawContent()
    {
        if (_targetCharacterName == null)
            _targetCharacterName = _gameObjectService.GetCurrentPlayerName();

        Im.Text($"Version: {_apiVersion.Item1}.{_apiVersion.Item2}");

        Im.Text($"IsValid: {_validResult} ({_lastValidCheckAt} UTC)");

        Im.Line.Same();
        if (Im.Button("Check IPC validity"u8) || _lastValidCheckAt == DateTime.MinValue)
        {
            _validResult = _isValidIpcFunc();
            _lastValidCheckAt = DateTime.UtcNow;
        }

        Im.Separator();

        if (Im.Button("Owned Actors Temporary Profile Test"u8))
        {
            bool found = false;
            foreach (var obj in _objectManager.Objects)
            {
                if (!obj.Identifier(_actorManager, out var ownedIdent) ||
                    ownedIdent.Type != Penumbra.GameData.Enums.IdentifierType.Owned ||
                    ownedIdent.IsOwnedByLocalPlayer())
                    continue;

                found = true;

                (int result, Guid? profileGuid) = _setTemporaryProfileOnCharacterIpcFunc(obj.Index.Index, _ownedTesProfile);
                if (result == 0)
                {
                    _popupSystem.ShowPopup(PopupSystem.Messages.IPCSetProfileToChrDone);
                    _logger.Information($"Temporary profile id: {profileGuid} on {ownedIdent}");
                }
                else
                {
                    _logger.Error($"Error code {result} while calling SetTemporaryProfileOnCharacter");
                    _popupSystem.ShowPopup(PopupSystem.Messages.ActionError);
                }

                break;
            }

            if (!found)
            {
                _logger.Error($"No characters found for Owned Test");
                _popupSystem.ShowPopup(PopupSystem.Messages.ActionError);
            }
        }

        Im.Separator();

        Im.Text($"Memory: {(string.IsNullOrWhiteSpace(_rememberedProfileJson) ? "empty" : "has data")}");

        Im.Text("Character to operate on:"u8);
        Im.Line.Same();
        Im.Input.Text("##operateon"u8, ref _targetCharacterName, maxLength: 128);

        if (Im.Button("Copy current profile into memory"u8))
        {
            var actors = _gameObjectService.FindActorsByName(_targetCharacterName).ToList();
            if (actors.Count == 0)
                return;

            if (!actors[0].Item2.Identifier(_actorManager, out var identifier))
                return;

            var profile = _profileManager.GetEnabledProfilesByActor(identifier).FirstOrDefault();
            if (profile == null)
                return;

            _rememberedProfileJson = JsonConvert.SerializeObject(IPCCharacterProfile.FromFullProfile(profile), _ipcProfileSerializerSettings);
            _popupSystem.ShowPopup(PopupSystem.Messages.IPCProfileRemembered);
        }

        if (Im.Button("GetActiveProfileIdOnCharacter into clipboard"u8))
        {
            var actors = _gameObjectService.FindActorsByName(_targetCharacterName).ToList();
            if (actors.Count == 0)
                return;

            (int result, Guid? uniqueId) = _getActiveProfileIdOnCharacterIpcFunc(actors[0].Item2.Index.Index);

            if (result == 0)
            {
                Im.Clipboard.Set($"{uniqueId}");
                _popupSystem.ShowPopup(PopupSystem.Messages.IPCCopiedToClipboard);
            }
            else
            {
                _logger.Error($"Error code {result} while calling GetCurrentlyActiveProfileOnCharacter");
                _popupSystem.ShowPopup(PopupSystem.Messages.ActionError);
            }
        }

        using (var disabled = Im.Disabled(_rememberedProfileJson == null))
        {
            if (Im.Button("SetTemporaryProfileOnCharacter from memory"u8) && _rememberedProfileJson != null)
            {
                var actors = _gameObjectService.FindActorsByName(_targetCharacterName).ToList();
                if (actors.Count == 0)
                    return;

                (int result, Guid? profileGuid) = _setTemporaryProfileOnCharacterIpcFunc(actors[0].Item2.Index.Index, _rememberedProfileJson);
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

        if (Im.Button("DeleteTemporaryProfileOnCharacter"u8))
        {
            var actors = _gameObjectService.FindActorsByName(_targetCharacterName).ToList();
            if (actors.Count == 0)
                return;

            int result = _deleteTemporaryProfileOnCharacterIpcFunc(actors[0].Item2.Index.Index);
            if (result == 0)
                _popupSystem.ShowPopup(PopupSystem.Messages.IPCRevertDone);
            else
            {
                _logger.Error($"Error code {result} while calling DeleteTemporaryProfileOnCharacter");
                _popupSystem.ShowPopup(PopupSystem.Messages.ActionError);
            }
        }

        Im.Separator();

        if (Im.Button("Copy user profile list to clipboard"u8))
        {
            Im.Clipboard.Set(string.Join("\n",
                _getProfileListIpcFunc().Select(x => $"{x.UniqueId}, {x.Name}, {x.VirtualPath}," +
                    $"|| {string.Join("|", x.Characters.Select(chr => $"{chr.Name}, {chr.WorldId}, {chr.CharacterType}, {chr.CharacterSubType}"))} ||, {x.Priority}, {x.IsEnabled}")));
            _popupSystem.ShowPopup(PopupSystem.Messages.IPCCopiedToClipboard);
        }

        Im.Text("Profile Unique ID:"u8);
        Im.Line.Same();
        Im.Input.Text("##profileguid"u8, ref _targetProfileId, maxLength: 128);

        if (Im.Button("Get profile by Unique ID into clipboard"u8))
        {
            (int result, string? profileJson) = _getProfileByIdIpcFunc(Guid.Parse(_targetProfileId));
            if (result == 0)
            {
                Im.Clipboard.Set(profileJson);
                _popupSystem.ShowPopup(PopupSystem.Messages.IPCCopiedToClipboard);
            }
            else
            {
                _logger.Error($"Error code {result} while calling GetProfileById");
                _popupSystem.ShowPopup(PopupSystem.Messages.ActionError);
            }
        }

        if (Im.Button("Get profile by Unique ID into memory"u8))
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

        if (Im.Button("Enable profile by Unique ID"u8))
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

        if (Im.Button("Disable profile by Unique ID"u8))
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

        Im.Separator();

        Im.Text("Profile priority:"u8);
        Im.Line.Same();
        Im.Input.Scalar("##profilepriority"u8, ref _targetProfilePriority, "%d"u8);

        if (Im.Button("Set profile priority by Unique ID"u8))
        {
            int result = _setPriorityByUniqueIdIpcFunc(Guid.Parse(_targetProfileId), _targetProfilePriority);
            if (result == 0)
            {
                _popupSystem.ShowPopup(PopupSystem.Messages.ActionDone);
            }
            else
            {
                _logger.Error($"Error code {result} while calling SetPriorityByUniqueId");
                _popupSystem.ShowPopup(PopupSystem.Messages.ActionError);
            }
        }

        Im.Separator();

        if (Im.Button("DeleteTemporaryProfileByUniqueId"u8))
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

        if (Im.Button("Add character to profile"u8))
        {
            int result = _addPlayerCharacterIpcFunc(Guid.Parse(_targetProfileId), _targetCharacterName, WorldId.AnyWorld.Id);

            if (result == 0)
                _popupSystem.ShowPopup(PopupSystem.Messages.ActionDone);
            else
            {
                _logger.Error($"Error code {result} while calling AddPlayerCharacter");
                _popupSystem.ShowPopup(PopupSystem.Messages.ActionError);
            }
        }

        if (Im.Button("Remove character from profile"u8))
        {
            int result = _removePlayerCharacterIpcFunc(Guid.Parse(_targetProfileId), _targetCharacterName, WorldId.AnyWorld.Id);

            if (result == 0)
                _popupSystem.ShowPopup(PopupSystem.Messages.ActionDone);
            else
            {
                _logger.Error($"Error code {result} while calling RemovePlayerCharacter");
                _popupSystem.ShowPopup(PopupSystem.Messages.ActionError);
            }
        }

        Im.Separator();

        if (Im.Button("Copy list of templates in profile to clipboard"u8))
        {
            var result = _getProfileTemplatesIpcFunc(Guid.Parse(_targetProfileId));

            if (result.Item1 == 0)
            {
                Im.Clipboard.Set(string.Join("\n",
                result.Item2!.Select(x => $"{x.UniqueId}, {x.Name}, {x.IsEnabled}," +
                    $"|| {string.Join("|", x.Bones
                    .Select(bone => $"{bone.Name}, {bone.Translation} ({bone.PropagateTranslation}), {bone.Rotation} ({bone.PropagateRotation}), {bone.Scale} ({bone.PropagateScale}), ChildScale: {bone.ChildScale}"))}")));

                _popupSystem.ShowPopup(PopupSystem.Messages.ActionDone);
            }
            else
            {
                _logger.Error($"Error code {result} while calling Profile.GetTemplates");
                _popupSystem.ShowPopup(PopupSystem.Messages.ActionError);
            }
        }

        Im.Separator();

        Im.Text("Cutscene actor index:"u8);
        Im.Line.Same();
        Im.Input.Scalar("##cutsceneactoridx"u8, ref _cutsceneActorIdx, "%d"u8);

        Im.Text("Cutscene actor parent index:"u8);
        Im.Line.Same();
        Im.Input.Scalar("##cutsceneactorparentidx"u8, ref _cutsceneActorParentIdx, "%d"u8);

        if (Im.Button("GameState.GetCutsceneParentIndex"u8))
        {
            int result = _getCutsceneParentIdxIpcFunc(_cutsceneActorIdx);
            if (result > -1)
            {
                _cutsceneActorParentIdx = result;
                _popupSystem.ShowPopup(PopupSystem.Messages.IPCSuccessfullyExecuted);
            }
            else
            {
                _logger.Error($"No parent for actor or actor not found while caling GetCutsceneParentIndex");
                _popupSystem.ShowPopup(PopupSystem.Messages.ActionError);
            }
        }

        if (Im.Button("GameState.SetCutsceneParentIndex"u8))
        {
            int result = _setCutsceneParentIdxIpcFunc(_cutsceneActorIdx, _cutsceneActorParentIdx);
            if (result == 0)
            {
                _cutsceneActorParentIdx = result;
                _popupSystem.ShowPopup(PopupSystem.Messages.IPCSuccessfullyExecuted);
            }
            else
            {
                _logger.Error($"Error code {result} while calling GameState.SetCutsceneParentIndex");
                _popupSystem.ShowPopup(PopupSystem.Messages.ActionError);
            }
        }
    }

    [EzIPCEvent("Profile.OnUpdate")]
    private void OnProfileUpdate(ushort gameObjectIndex, Guid profileUniqueId)
    {
        var actor = _gameObjectService.GetActorByObjectIndex(gameObjectIndex);

        _logger.Debug($"IPC Test Tab - OnProfileUpdate: Character: {actor?.Utf8Name.ToString().Incognify() ?? "None"}, Profile ID: {(profileUniqueId != Guid.Empty ? profileUniqueId.ToString() : "no id")}");
    }
}
