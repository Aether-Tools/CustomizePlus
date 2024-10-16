using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using OtterGui.Custom;
using OtterGui.Log;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Data;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.DataContainers.Bases;
using Penumbra.GameData.Gui;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;
using Penumbra.String;

namespace CustomizePlus.UI.Windows.Controls;

public class ActorAssignmentUi
{
    private readonly ActorManager _actorManager;

    private WorldCombo _worldCombo = null!;
    private NpcCombo _mountCombo = null!;
    private NpcCombo _companionCombo = null!;
    private NpcCombo _bnpcCombo = null!;
    private NpcCombo _enpcCombo = null!;

    private bool _ready;

    private string _newCharacterName = string.Empty;
    private ObjectKind _newKind = ObjectKind.BattleNpc;

    public ActorIdentifier NpcIdentifier { get; private set; } = ActorIdentifier.Invalid;
    public ActorIdentifier PlayerIdentifier { get; private set; } = ActorIdentifier.Invalid;
    public ActorIdentifier RetainerIdentifier { get; private set; } = ActorIdentifier.Invalid;
    public ActorIdentifier MannequinIdentifier { get; private set; } = ActorIdentifier.Invalid;

    public bool CanSetPlayer
        => PlayerIdentifier.IsValid;

    public bool CanSetRetainer
        => RetainerIdentifier.IsValid;

    public bool CanSetMannequin
        => MannequinIdentifier.IsValid;

    public bool CanSetNpc
        => NpcIdentifier.IsValid;

    public ActorAssignmentUi(ActorManager actorManager, DictModelChara dictModelChara, DictBNpcNames bNpcNames, DictBNpc bNpc)
    {
        _actorManager = actorManager;

        _actorManager.Awaiter.ContinueWith(_ => SetupCombos(), TaskScheduler.Default);
    }

    public void DrawWorldCombo(float width)
    {
        if (_ready && _worldCombo.Draw(width))
            UpdateIdentifiersInternal();
    }


    public void DrawPlayerInput(float width)
    {
        if (!_ready)
            return;

        ImGui.SetNextItemWidth(width);
        if (ImGui.InputTextWithHint("##NewCharacter", "Character Name...", ref _newCharacterName, 32))
            UpdateIdentifiersInternal();
    }

    public void DrawObjectKindCombo(float width)
    {
        if (_ready && IndividualHelpers.DrawObjectKindCombo(width, _newKind, out _newKind, ObjectKinds))
            UpdateIdentifiersInternal();
    }

    public void DrawNpcInput(float width)
    {
        if (!_ready)
            return;

        var combo = GetNpcCombo(_newKind);
        if (combo.Draw(width))
            UpdateIdentifiersInternal();
    }

    private static readonly IReadOnlyList<ObjectKind> ObjectKinds = new[]
    {
        ObjectKind.BattleNpc,
        ObjectKind.EventNpc,
        ObjectKind.Companion,
        ObjectKind.MountType,
    };

    private Penumbra.GameData.Gui.NpcCombo GetNpcCombo(ObjectKind kind)
        => kind switch
        {
            ObjectKind.BattleNpc => _bnpcCombo,
            ObjectKind.EventNpc => _enpcCombo,
            ObjectKind.MountType => _mountCombo,
            ObjectKind.Companion => _companionCombo,
            _ => throw new NotImplementedException(),
        };


    private void SetupCombos()
    {
        _worldCombo = new WorldCombo(_actorManager.Data.Worlds, Plugin.Logger);
        _mountCombo = new Penumbra.GameData.Gui.NpcCombo("##mountCombo", _actorManager.Data.Mounts, Plugin.Logger);
        _companionCombo = new Penumbra.GameData.Gui.NpcCombo("##companionCombo", _actorManager.Data.Companions, Plugin.Logger);
        _bnpcCombo = new Penumbra.GameData.Gui.NpcCombo("##bnpcCombo", _actorManager.Data.BNpcs, Plugin.Logger);
        _enpcCombo = new Penumbra.GameData.Gui.NpcCombo("##enpcCombo", _actorManager.Data.ENpcs, Plugin.Logger);
        _ready = true;
    }

    private void UpdateIdentifiersInternal()
    {
        if (ByteString.FromString(_newCharacterName, out var byteName))
        {
            PlayerIdentifier = _actorManager.CreatePlayer(byteName, _worldCombo.CurrentSelection.Key);
            RetainerIdentifier = _actorManager.CreateRetainer(byteName, ActorIdentifier.RetainerType.Bell);
            MannequinIdentifier = _actorManager.CreateRetainer(byteName, ActorIdentifier.RetainerType.Mannequin);
        }

        var npcCombo = GetNpcCombo(_newKind);

        if (npcCombo.CurrentSelection.Ids == null || npcCombo.CurrentSelection.Ids.Length == 0)
            NpcIdentifier = ActorIdentifier.Invalid;
        else
        {
            switch (_newKind)
            {
                case ObjectKind.BattleNpc:
                case ObjectKind.EventNpc:
                    NpcIdentifier = _actorManager.CreateNpc(_newKind, npcCombo.CurrentSelection.Ids[0]);
                    break;
                case ObjectKind.MountType:
                case ObjectKind.Companion:
                    var currentPlayer = _actorManager.GetCurrentPlayer();
                    NpcIdentifier = _actorManager.CreateOwned(currentPlayer.PlayerName, currentPlayer.HomeWorld, _newKind, npcCombo.CurrentSelection.Ids[0]);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}