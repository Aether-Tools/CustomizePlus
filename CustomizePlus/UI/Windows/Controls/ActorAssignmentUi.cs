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
using Penumbra.GameData.DataContainers.Bases;
using Penumbra.GameData.Gui;
using Penumbra.GameData.Structs;
using Penumbra.String;

namespace CustomizePlus.UI.Windows.Controls;

public class ActorAssignmentUi
{
    private readonly ActorManager _actorManager;
    private readonly DictBNpcENpc _dictBnpcEnpc;

    private WorldCombo _worldCombo = null!;
    private Penumbra.GameData.Gui.NpcCombo _mountCombo = null!;
    private Penumbra.GameData.Gui.NpcCombo _companionCombo = null!;
    //private BattleEventNpcCombo _npcCombo = null!;
    private Penumbra.GameData.Gui.NpcCombo _npcCombo = null!;

    private bool _ready;

    private string _newCharacterName = string.Empty;
    private ObjectKind _newKind = ObjectKind.BattleNpc;

  /*  public string CharacterName { get => _newCharacterName; }
    public WorldId SelectedWorld { get => _worldCombo.CurrentSelection.Key; }
  */
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

    public ActorAssignmentUi(ActorManager actorManager, DictBNpcENpc dictBnpcEnpc)
    {
        _actorManager = actorManager;
        _dictBnpcEnpc = dictBnpcEnpc;

        _actorManager.Awaiter.ContinueWith(_ => dictBnpcEnpc.Awaiter.ContinueWith(_ => SetupCombos(), TaskScheduler.Default), TaskScheduler.Default);
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

       /* if(_newKind == ObjectKind.BattleNpc || _newKind == ObjectKind.EventNpc)
        {
            if (_npcCombo.Draw(width))
                UpdateIdentifiersInternal();
        }*/

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
            ObjectKind.BattleNpc => _npcCombo,
            ObjectKind.EventNpc => _npcCombo,
            ObjectKind.MountType => _mountCombo,
            ObjectKind.Companion => _companionCombo,
            _ => throw new NotImplementedException(),
        };


    private void SetupCombos()
    {
        _worldCombo = new WorldCombo(_actorManager.Data.Worlds, Plugin.Logger);
        _mountCombo = new Penumbra.GameData.Gui.NpcCombo("##mountCombo", _actorManager.Data.Mounts, Plugin.Logger);
        _companionCombo = new Penumbra.GameData.Gui.NpcCombo("##companionCombo", _actorManager.Data.Companions, Plugin.Logger);
        //_bnpcCombo = new Penumbra.GameData.Gui.NpcCombo("##bnpcCombo", _actorManager.Data.BNpcs, Plugin.Logger);
        //_enpcCombo = new Penumbra.GameData.Gui.NpcCombo("##enpcCombo", _actorManager.Data.ENpcs, Plugin.Logger);
        _npcCombo = new Penumbra.GameData.Gui.NpcCombo("##npcCombo", _dictBnpcEnpc, Plugin.Logger);
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
    }
}

//Todo: Temp
/// <summary> A dictionary that matches BNpcNameId to names. </summary>
public sealed class DictBNpcENpc(IDalamudPluginInterface pluginInterface, Logger log, IDataManager gameData)
    : NameDictionary(pluginInterface, log, gameData, "BNpcsENpcs", 7, () => CreateData(gameData))
{
    /// <summary> Create the data. </summary>
    private static IReadOnlyDictionary<uint, string> CreateData(IDataManager gameData)
    {

        var sheet = gameData.GetExcelSheet<BNpcName>(gameData.Language)!;
        var sheet2 = gameData.GetExcelSheet<ENpcResident>(gameData.Language)!;

        var dict = new Dictionary<uint, string>((int)sheet.RowCount + (int)sheet2.RowCount);

        foreach (var n in sheet.Where(n => n.Singular.RawData.Length > 0))
            dict.TryAdd(n.RowId, DataUtility.ToTitleCaseExtended(n.Singular, n.Article));
        foreach (var n in sheet2.Where(e => e.Singular.RawData.Length > 0))
            dict.TryAdd(n.RowId, DataUtility.ToTitleCaseExtended(n.Singular, n.Article));

        return dict.ToFrozenDictionary();
    }

    /// <inheritdoc cref="NameDictionary.ContainsKey"/>
    public bool ContainsKey(BNpcNameId key)
        => Value.ContainsKey(key.Id);

    /// <inheritdoc cref="NameDictionary.TryGetValue"/>
    public bool TryGetValue(BNpcNameId key, [NotNullWhen(true)] out string? value)
        => Value.TryGetValue(key.Id, out value);

    /// <inheritdoc cref="NameDictionary.this"/>
    public string this[BNpcNameId key]
        => Value[key.Id];
}

