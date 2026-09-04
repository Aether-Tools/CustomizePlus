using CustomizePlus.Armatures.Data;
using CustomizePlus.Configuration.Data;
using CustomizePlus.Core.Data;
using CustomizePlus.Core.Helpers;
using CustomizePlus.Game.Services;
using CustomizePlus.GameData.Extensions;
using CustomizePlus.Templates;
using CustomizePlus.Templates.Data;
using CustomizePlus.UI.Windows.Controls;
using Dalamud.Interface;
using Dalamud.Interface.Utility;

namespace CustomizePlus.UI.Windows.MainWindow.Tabs.Templates;

public partial class BoneEditorPanel
{
    private bool DrawEditorHeader()
    {
        string characterText = null!;

        if (_configuration.UISettings.IncognitoMode)
            characterText = "Previewing on: incognito active";
        else
            characterText = _editorManager.Character.IsValid ? $"Previewing on: {(_editorManager.Character.Type == Penumbra.GameData.Enums.IdentifierType.Owned ?
            _editorManager.Character.ToNameWithoutOwnerName() : _editorManager.Character.ToString())}" : "No valid character selected";

        UiHelpers.DrawIcon(FontAwesomeIcon.User);
        Im.Line.Same();
        Im.Text(characterText);

        Im.Separator();

        var isShouldDraw = Im.Tree.Header("Change preview character"u8);

        if (isShouldDraw)
        {
            var width = new Vector2(Im.ContentRegion.Available.X - Im.Font.CalculateSize("Limit to my creatures"u8).X - 68, 0);

            using (var disabled = Im.Disabled(!IsEditorActive || IsEditorPaused))
            {
                if (!_configuration.UISettings.IncognitoMode)
                {
                    _actorAssignmentUi.DrawWorldCombo(width.X / 2);
                    Im.Line.Same();
                    _actorAssignmentUi.DrawPlayerInput(width.X / 2);

                    var buttonWidth = new Vector2(165 * ImGuiHelpers.GlobalScale - Im.Style.ItemSpacing.X / 2, 0);

                    if (UiHelpers.DrawDisabledButton("Apply to player character", buttonWidth, string.Empty, !_actorAssignmentUi.CanSetPlayer))
                        _editorManager.ChangeEditorCharacter(_actorAssignmentUi.PlayerIdentifier);

                    Im.Line.Same();

                    if (UiHelpers.DrawDisabledButton("Apply to retainer", buttonWidth, string.Empty, !_actorAssignmentUi.CanSetRetainer))
                        _editorManager.ChangeEditorCharacter(_actorAssignmentUi.RetainerIdentifier);

                    Im.Line.Same();

                    if (UiHelpers.DrawDisabledButton("Apply to mannequin", buttonWidth, string.Empty, !_actorAssignmentUi.CanSetMannequin))
                        _editorManager.ChangeEditorCharacter(_actorAssignmentUi.MannequinIdentifier);

                    var currentPlayer = _gameObjectService.GetCurrentPlayerActorIdentifier().CreatePermanent();
                    if (UiHelpers.DrawDisabledButton("Apply to current character", buttonWidth, string.Empty, !currentPlayer.IsValid))
                        _editorManager.ChangeEditorCharacter(currentPlayer);

                    Im.Separator();

                    _actorAssignmentUi.DrawObjectKindCombo(width.X / 2);
                    Im.Line.Same();
                    _actorAssignmentUi.DrawNpcInput(width.X / 2);

                    if (UiHelpers.DrawDisabledButton("Apply to selected NPC", buttonWidth, string.Empty, !_actorAssignmentUi.CanSetNpc))
                        _editorManager.ChangeEditorCharacter(_actorAssignmentUi.NpcIdentifier);
                }
                else
                    Im.Text("Incognito active"u8);
            }
        }

        Im.Separator();

        using (var table = Im.Table.Begin("BoneEditorMenu"u8, 2))
        {
            if (!table)
                return false;

            table.SetupColumn("Attributes"u8, TableColumnFlags.WidthFixed);
            table.SetupColumn("Space"u8, TableColumnFlags.WidthStretch);

            Im.Table.NextRow();
            Im.Table.NextColumn();

            var modeChanged = false;
            if (Im.RadioButton("Position"u8, _editingAttribute == BoneAttribute.Position))
            {
                _editingAttribute = BoneAttribute.Position;
                modeChanged = true;
            }
            CtrlHelper.AddHoverText($"May have unintended effects. Edit at your own risk!");

            Im.Line.Same();
            if (Im.RadioButton("Rotation"u8, _editingAttribute == BoneAttribute.Rotation))
            {
                _editingAttribute = BoneAttribute.Rotation;
                modeChanged = true;
            }
            CtrlHelper.AddHoverText($"May have unintended effects. Edit at your own risk!");

            Im.Line.Same();
            if (Im.RadioButton("Scale"u8, _editingAttribute == BoneAttribute.Scale))
            {
                _editingAttribute = BoneAttribute.Scale;
                modeChanged = true;
            }

            Im.Line.Same();
            Im.Item.SetNextWidth(200 * ImGuiHelpers.GlobalScale);
            Im.Input.Text("##BoneSearch"u8, ref _boneSearch, "Search bones..."u8, maxLength: 64);

            Im.Line.Same();
            if (DrawIconButton("UndoBone", FontAwesomeIcon.Undo, "Undo", !_editorManager.CanUndo))
                _editorManager.Undo();

            Im.Line.Same();
            if (DrawIconButton("RedoBone", FontAwesomeIcon.Redo, "Redo", !_editorManager.CanRedo))
                _editorManager.Redo();

            if (modeChanged)
            {
                _configuration.EditorConfiguration.EditorMode = _editingAttribute;
                _configuration.Save();
            }

            using (var disabled = Im.Disabled(!_isUnlocked))
            {
                Im.Line.Same();
                if (CtrlHelper.Checkbox("Show Live Bones", ref _isShowLiveBones))
                {
                    _configuration.EditorConfiguration.ShowLiveBones = _isShowLiveBones;
                    _configuration.Save();
                }
                CtrlHelper.AddHoverText($"If selected, present for editing all bones found in the game data,\nelse show only bones for which the profile already contains edits.");

                Im.Line.Same();
                using (var disabledMirrorMode = Im.Disabled(!_isShowLiveBones))
                {
                    if (CtrlHelper.Checkbox("Mirror Mode", ref _isMirrorModeEnabled))
                    {
                        _configuration.EditorConfiguration.BoneMirroringEnabled = _isMirrorModeEnabled;
                        _configuration.Save();
                    }
                    CtrlHelper.AddHoverText($"Bone changes will be reflected from left to right and vice versa");
                }
            }

            Im.Table.NextColumn();

            if (Im.Slider("##Precision"u8, ref _precision, $"{_precision} Place{(_precision == 1 ? string.Empty : "s")}", 0, 6))
            {
                _configuration.EditorConfiguration.EditorValuesPrecision = _precision;
                _configuration.Save();
            }
            CtrlHelper.AddHoverText("Level of precision to display while editing values");
        }

        return true;
    }
}
