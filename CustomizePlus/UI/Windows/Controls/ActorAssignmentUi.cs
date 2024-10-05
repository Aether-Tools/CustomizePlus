using Dalamud.Game.ClientState.Objects.Enums;
using ImGuiNET;
using OtterGui.Custom;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Gui;
using Penumbra.GameData.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomizePlus.UI.Windows.Controls;

public class ActorAssignmentUi : IDisposable
{
    private readonly ActorManager _actorManager;

    private WorldCombo _worldCombo = null!;
    private NpcCombo _mountCombo = null!;
    private NpcCombo _companionCombo = null!;
    private NpcCombo _ornamentCombo = null!;
    private NpcCombo _bnpcCombo = null!;
    private NpcCombo _enpcCombo = null!;

    private bool _ready;

    private string _newCharacterName = string.Empty;
    private ObjectKind _newKind = ObjectKind.BattleNpc;

    public ActorAssignmentUi(ActorManager actorManager)
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
        ObjectKind.Ornament,
    };

    private NpcCombo GetNpcCombo(ObjectKind kind)
        => kind switch
        {
            ObjectKind.BattleNpc => _bnpcCombo,
            ObjectKind.EventNpc => _enpcCombo,
            ObjectKind.MountType => _mountCombo,
            ObjectKind.Companion => _companionCombo,
            ObjectKind.Ornament => _ornamentCombo,
            _ => throw new NotImplementedException(),
        };


    private void SetupCombos()
    {
        _worldCombo = new WorldCombo(_actorManager.Data.Worlds, Plugin.Logger);
        _mountCombo = new NpcCombo("##mountCombo", _actorManager.Data.Mounts, Plugin.Logger);
        _companionCombo = new NpcCombo("##companionCombo", _actorManager.Data.Companions, Plugin.Logger);
        _ornamentCombo = new NpcCombo("##ornamentCombo", _actorManager.Data.Ornaments, Plugin.Logger);
        _bnpcCombo = new NpcCombo("##bnpcCombo", _actorManager.Data.BNpcs, Plugin.Logger);
        _enpcCombo = new NpcCombo("##enpcCombo", _actorManager.Data.ENpcs, Plugin.Logger);
        _ready = true;
    }

    private void UpdateIdentifiersInternal()
    {
       /* var combo = GetNpcCombo(_newKind);
        PlayerTooltip = _collectionManager.Active.Individuals.CanAdd(IdentifierType.Player, _newCharacterName,
                _worldCombo.CurrentSelection.Key, ObjectKind.None, [], out _playerIdentifiers) switch
        {
            _ when _newCharacterName.Length == 0 => NewPlayerTooltipEmpty,
            IndividualCollections.AddResult.Invalid => NewPlayerTooltipInvalid,
            IndividualCollections.AddResult.AlreadySet => AlreadyAssigned,
            _ => string.Empty,
        };
        RetainerTooltip =
            _collectionManager.Active.Individuals.CanAdd(IdentifierType.Retainer, _newCharacterName, 0, ObjectKind.None, [],
                    out _retainerIdentifiers) switch
            {
                _ when _newCharacterName.Length == 0 => NewRetainerTooltipEmpty,
                IndividualCollections.AddResult.Invalid => NewRetainerTooltipInvalid,
                IndividualCollections.AddResult.AlreadySet => AlreadyAssigned,
                _ => string.Empty,
            };
        if (combo.CurrentSelection.Ids != null)
        {
            NpcTooltip = _collectionManager.Active.Individuals.CanAdd(IdentifierType.Npc, string.Empty, ushort.MaxValue, _newKind,
                    combo.CurrentSelection.Ids, out _npcIdentifiers) switch
            {
                IndividualCollections.AddResult.AlreadySet => AlreadyAssigned,
                _ => string.Empty,
            };
            OwnedTooltip = _collectionManager.Active.Individuals.CanAdd(IdentifierType.Owned, _newCharacterName,
                    _worldCombo.CurrentSelection.Key, _newKind,
                    combo.CurrentSelection.Ids, out _ownedIdentifiers) switch
            {
                _ when _newCharacterName.Length == 0 => NewPlayerTooltipEmpty,
                IndividualCollections.AddResult.Invalid => NewPlayerTooltipInvalid,
                IndividualCollections.AddResult.AlreadySet => AlreadyAssigned,
                _ => string.Empty,
            };
        }
        else
        {
            NpcTooltip = NewNpcTooltipEmpty;
            OwnedTooltip = NewNpcTooltipEmpty;
            _npcIdentifiers = [];
            _ownedIdentifiers = [];
        }*/
    }

    public void Dispose()
    {
        //throw new NotImplementedException();
    }
}
