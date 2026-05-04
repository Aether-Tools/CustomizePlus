using Dalamud.Game.ClientState.Objects.Enums;
using Penumbra.GameData.Actors;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Gui;
using Penumbra.String;
using System.Threading.Tasks;

namespace CustomizePlus.UI.Windows.Controls;

public class ActorAssignmentUi
{
    private readonly ActorManager _actorManager;

    private WorldCombo _worldCombo = null!;
    private NpcCombo _mountCombo = null!;
    private NpcCombo _companionCombo = null!;
    private NpcCombo _bnpcCombo = null!;
    private NpcCombo _enpcCombo = null!;
    private NpcCombo _ornamentCombo = null!;

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

        Im.Item.SetNextWidth(width);
        if (Im.Input.Text("##NewCharacter"u8, ref _newCharacterName, "Character Name..."u8, maxLength: 32))
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
            ObjectKind.Mount,
        ObjectKind.Ornament,
    };

    private Penumbra.GameData.Gui.NpcCombo GetNpcCombo(ObjectKind kind)
        => kind switch
        {
            ObjectKind.BattleNpc => _bnpcCombo,
            ObjectKind.EventNpc => _enpcCombo,
            ObjectKind.Mount => _mountCombo,
            ObjectKind.Companion => _companionCombo,
            ObjectKind.Ornament => _ornamentCombo,
            _ => throw new NotImplementedException(),
        };


    private void SetupCombos()
    {
        _worldCombo = new WorldCombo(_actorManager.Data.Worlds);
        _mountCombo = new Penumbra.GameData.Gui.NpcCombo(new StringU8("##mountCombo"u8), _actorManager.Data.Mounts);
        _companionCombo = new Penumbra.GameData.Gui.NpcCombo(new StringU8("##companionCombo"u8), _actorManager.Data.Companions);
        _bnpcCombo = new Penumbra.GameData.Gui.NpcCombo(new StringU8("##bnpcCombo"u8), _actorManager.Data.BNpcs);
        _enpcCombo = new Penumbra.GameData.Gui.NpcCombo(new StringU8("##enpcCombo"u8), _actorManager.Data.ENpcs);
        _ornamentCombo = new Penumbra.GameData.Gui.NpcCombo(new StringU8("##ornamentCombo"u8), _actorManager.Data.Ornaments);
        _ready = true;
    }

    private void UpdateIdentifiersInternal()
    {
        if (ByteString.FromString(_newCharacterName, out var byteName))
        {
            PlayerIdentifier = _actorManager.CreatePlayer(byteName, _worldCombo.Selected.Key);
            RetainerIdentifier = _actorManager.CreateRetainer(byteName, ActorIdentifier.RetainerType.Bell);
            MannequinIdentifier = _actorManager.CreateRetainer(byteName, ActorIdentifier.RetainerType.Mannequin);
        }

        var npcCombo = GetNpcCombo(_newKind);

        if (npcCombo.Selected.Ids == null || npcCombo.Selected.Ids.Length == 0)
            NpcIdentifier = ActorIdentifier.Invalid;
        else
        {
            switch (_newKind)
            {
                case ObjectKind.BattleNpc:
                case ObjectKind.EventNpc:
                    NpcIdentifier = _actorManager.CreateNpc(_newKind, npcCombo.Selected.Ids[0]);
                    break;
                case ObjectKind.Mount:
                case ObjectKind.Companion:
                case ObjectKind.Ornament:
                    var currentPlayer = _actorManager.GetCurrentPlayer();
                    NpcIdentifier = _actorManager.CreateOwned(currentPlayer.PlayerName, currentPlayer.HomeWorld, _newKind, npcCombo.Selected.Ids[0]);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}