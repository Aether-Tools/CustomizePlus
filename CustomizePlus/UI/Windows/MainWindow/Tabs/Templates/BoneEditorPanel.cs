using CustomizePlus.Armatures.Data;
using CustomizePlus.Configuration.Data;
using CustomizePlus.Core.Data;
using CustomizePlus.Core.Helpers;
using CustomizePlus.Game.Services;
using CustomizePlus.GameData.Extensions;
using CustomizePlus.Templates;
using CustomizePlus.Templates.Data;
using CustomizePlus.UI.Windows.Controls;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using OtterGui;
using OtterGui.Log;
using OtterGui.Raii;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using static CustomizePlus.Core.Data.BoneData;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.MinionListModule.Delegates;

namespace CustomizePlus.UI.Windows.MainWindow.Tabs.Templates;

public class BoneEditorPanel
{
    private readonly TemplateFileSystemSelector _templateFileSystemSelector;
    private readonly TemplateEditorManager _editorManager;
    private readonly PluginConfiguration _configuration;
    private readonly GameObjectService _gameObjectService;
    private readonly ActorAssignmentUi _actorAssignmentUi;
    private readonly PopupSystem _popupSystem;
    private readonly Logger _logger;

    private BoneAttribute _editingAttribute;
    private int _precision;

    private bool _isShowLiveBones;
    private bool _isMirrorModeEnabled;

    private Dictionary<BoneData.BoneFamily, bool> _groupExpandedState = new();

    private bool _openSavePopup;

    private bool _isUnlocked = false;

    private string _boneSearch = string.Empty;

    // all the stuff to handle undo/redo
    private readonly Stack<Dictionary<string, BoneTransform>> _undoStack = new();
    private readonly Stack<Dictionary<string, BoneTransform>> _redoStack = new();
    private Dictionary<string, BoneTransform>? _pendingUndoSnapshot = null;
    private float _initialX, _initialY, _initialZ;
    private Vector3 _initialScale;

    // favorite bone stuff
    private HashSet<string> _favoriteBones;

    private string? _pendingClipboardText;
    private string? _pendingImportText;
    public bool HasChanges => _editorManager.HasChanges;
    public bool IsEditorActive => _editorManager.IsEditorActive;
    public bool IsEditorPaused => _editorManager.IsEditorPaused;
    public bool IsCharacterFound => _editorManager.IsCharacterFound;

    public BoneEditorPanel(
        TemplateFileSystemSelector templateFileSystemSelector,
        TemplateEditorManager editorManager,
        PluginConfiguration configuration,
        GameObjectService gameObjectService,
        ActorAssignmentUi actorAssignmentUi,
        Logger logger)
    {
        _templateFileSystemSelector = templateFileSystemSelector;
        _editorManager = editorManager;
        _configuration = configuration;
        _gameObjectService = gameObjectService;
        _actorAssignmentUi = actorAssignmentUi;
        _logger = logger;

        _isShowLiveBones = configuration.EditorConfiguration.ShowLiveBones;
        _isMirrorModeEnabled = configuration.EditorConfiguration.BoneMirroringEnabled;
        _precision = configuration.EditorConfiguration.EditorValuesPrecision;
        _editingAttribute = configuration.EditorConfiguration.EditorMode;
        _favoriteBones = new HashSet<string>(_configuration.EditorConfiguration.FavoriteBones);
    }

    public bool EnableEditor(Template template)
    {
        if (_editorManager.EnableEditor(template))
        {
            //_editorManager.SetLimitLookupToOwned(_configuration.EditorConfiguration.LimitLookupToOwnedObjects);
            _undoStack.Clear();
            _redoStack.Clear();
            return true;
        }

        return false;
    }

    public bool DisableEditor()
    {
        if (!_editorManager.HasChanges)
            return _editorManager.DisableEditor();

        if (_editorManager.HasChanges && !IsEditorActive)
            throw new Exception("Invalid state in BoneEditorPanel: has changes but editor is not active");

        _openSavePopup = true;

        return false;
    }

    public void Draw()
    {
        _isUnlocked = IsCharacterFound && IsEditorActive && !IsEditorPaused;

        DrawEditorConfirmationPopup();

        ImGui.Separator();

        using (var style = ImRaii.PushStyle(ImGuiStyleVar.ButtonTextAlign, new Vector2(0, 0.5f)))
        {
            string characterText = null!;

            if (_templateFileSystemSelector.IncognitoMode)
                characterText = "Previewing on: incognito active";
            else
                characterText = _editorManager.Character.IsValid ? $"Previewing on: {(_editorManager.Character.Type == Penumbra.GameData.Enums.IdentifierType.Owned ?
                _editorManager.Character.ToNameWithoutOwnerName() : _editorManager.Character.ToString())}" : "No valid character selected";

            ImGuiUtil.PrintIcon(FontAwesomeIcon.User);
            ImGui.SameLine();
            ImGui.Text(characterText);

            ImGui.Separator();

            var isShouldDraw = ImGui.CollapsingHeader("Change preview character");

            if (isShouldDraw)
            {
                var width = new Vector2(ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize("Limit to my creatures").X - 68, 0);

                using (var disabled = ImRaii.Disabled(!IsEditorActive || IsEditorPaused))
                {
                    if (!_templateFileSystemSelector.IncognitoMode)
                    {
                        _actorAssignmentUi.DrawWorldCombo(width.X / 2);
                        ImGui.SameLine();
                        _actorAssignmentUi.DrawPlayerInput(width.X / 2);

                        var buttonWidth = new Vector2(165 * ImGuiHelpers.GlobalScale - ImGui.GetStyle().ItemSpacing.X / 2, 0);

                        if (ImGuiUtil.DrawDisabledButton("Apply to player character", buttonWidth, string.Empty, !_actorAssignmentUi.CanSetPlayer))
                            _editorManager.ChangeEditorCharacter(_actorAssignmentUi.PlayerIdentifier);

                        ImGui.SameLine();

                        if (ImGuiUtil.DrawDisabledButton("Apply to retainer", buttonWidth, string.Empty, !_actorAssignmentUi.CanSetRetainer))
                            _editorManager.ChangeEditorCharacter(_actorAssignmentUi.RetainerIdentifier);

                        ImGui.SameLine();

                        if (ImGuiUtil.DrawDisabledButton("Apply to mannequin", buttonWidth, string.Empty, !_actorAssignmentUi.CanSetMannequin))
                            _editorManager.ChangeEditorCharacter(_actorAssignmentUi.MannequinIdentifier);

                        var currentPlayer = _gameObjectService.GetCurrentPlayerActorIdentifier().CreatePermanent();
                        if (ImGuiUtil.DrawDisabledButton("Apply to current character", buttonWidth, string.Empty, !currentPlayer.IsValid))
                            _editorManager.ChangeEditorCharacter(currentPlayer);

                        ImGui.Separator();

                        _actorAssignmentUi.DrawObjectKindCombo(width.X / 2);
                        ImGui.SameLine();
                        _actorAssignmentUi.DrawNpcInput(width.X / 2);

                        if (ImGuiUtil.DrawDisabledButton("Apply to selected NPC", buttonWidth, string.Empty, !_actorAssignmentUi.CanSetNpc))
                            _editorManager.ChangeEditorCharacter(_actorAssignmentUi.NpcIdentifier);
                    }
                    else
                        ImGui.TextUnformatted("Incognito active");
                }
            }

            ImGui.Separator();

            using (var table = ImRaii.Table("BoneEditorMenu", 2))
            {
                ImGui.TableSetupColumn("Attributes", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("Space", ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                var modeChanged = false;
                if (ImGui.RadioButton("Position", _editingAttribute == BoneAttribute.Position))
                {
                    _editingAttribute = BoneAttribute.Position;
                    modeChanged = true;
                }
                CtrlHelper.AddHoverText($"May have unintended effects. Edit at your own risk!");

                ImGui.SameLine();
                if (ImGui.RadioButton("Rotation", _editingAttribute == BoneAttribute.Rotation))
                {
                    _editingAttribute = BoneAttribute.Rotation;
                    modeChanged = true;
                }
                CtrlHelper.AddHoverText($"May have unintended effects. Edit at your own risk!");

                ImGui.SameLine();
                if (ImGui.RadioButton("Scale", _editingAttribute == BoneAttribute.Scale))
                {
                    _editingAttribute = BoneAttribute.Scale;
                    modeChanged = true;
                }

                ImGui.SameLine();
                ImGui.SetNextItemWidth(200 * ImGuiHelpers.GlobalScale);
                ImGui.InputTextWithHint("##BoneSearch", "Search bones...", ref _boneSearch, 64);

                ImGui.SameLine();
                ImGui.BeginDisabled(_undoStack.Count == 0);
                if (ImGuiComponents.IconButton("##UndoBone", FontAwesomeIcon.Undo))
                {
                    var state = _undoStack.Pop();
                    _redoStack.Push(_editorManager.EditorProfile.Armatures[0]
                        .GetAllBones()
                        .DistinctBy(b => b.BoneName)
                        .ToDictionary(b => b.BoneName, b => new BoneTransform(b.CustomizedTransform ?? new BoneTransform())));
                    RestoreState(state);
                }
                ImGui.EndDisabled();
                CtrlHelper.AddHoverText("Undo");

                ImGui.SameLine();
                ImGui.BeginDisabled(_redoStack.Count == 0);
                if (ImGuiComponents.IconButton("##RedoBone", FontAwesomeIcon.Redo))
                {
                    var state = _redoStack.Pop();
                    _undoStack.Push(_editorManager.EditorProfile.Armatures[0]
                        .GetAllBones()
                        .DistinctBy(b => b.BoneName)
                        .ToDictionary(b => b.BoneName, b => new BoneTransform(b.CustomizedTransform ?? new BoneTransform())));
                    RestoreState(state);
                }
                ImGui.EndDisabled();
                CtrlHelper.AddHoverText("Redo");

                if (modeChanged)
                {
                    _configuration.EditorConfiguration.EditorMode = _editingAttribute;
                    _configuration.Save();
                }

                using (var disabled = ImRaii.Disabled(!_isUnlocked))
                {
                    ImGui.SameLine();
                    if (CtrlHelper.Checkbox("Show Live Bones", ref _isShowLiveBones))
                    {
                        _configuration.EditorConfiguration.ShowLiveBones = _isShowLiveBones;
                        _configuration.Save();
                    }
                    CtrlHelper.AddHoverText($"If selected, present for editing all bones found in the game data,\nelse show only bones for which the profile already contains edits.");

                    ImGui.SameLine();
                    ImGui.BeginDisabled(!_isShowLiveBones);
                    if (CtrlHelper.Checkbox("Mirror Mode", ref _isMirrorModeEnabled))
                    {
                        _configuration.EditorConfiguration.BoneMirroringEnabled = _isMirrorModeEnabled;
                        _configuration.Save();
                    }
                    CtrlHelper.AddHoverText($"Bone changes will be reflected from left to right and vice versa");
                    ImGui.EndDisabled();
                }

                ImGui.TableNextColumn();

                if (ImGui.SliderInt("##Precision", ref _precision, 0, 6, $"{_precision} Place{(_precision == 1 ? "" : "s")}"))
                {
                    _configuration.EditorConfiguration.EditorValuesPrecision = _precision;
                    _configuration.Save();
                }
                CtrlHelper.AddHoverText("Level of precision to display while editing values");
            }

            ImGui.Separator();

            using (var table = ImRaii.Table($"BoneEditorContents", 6, ImGuiTableFlags.BordersOuterH | ImGuiTableFlags.BordersV | ImGuiTableFlags.ScrollY))
            {
                if (!table)
                    return;

                var col1Label = _editingAttribute == BoneAttribute.Rotation ? "Roll" : "X";
                var col2Label = _editingAttribute == BoneAttribute.Rotation ? "Pitch" : "Y";
                var col3Label = _editingAttribute == BoneAttribute.Rotation ? "Yaw" : "Z";
                var col4Label = _editingAttribute == BoneAttribute.Scale ? "All" : "N/A";

                ImGui.TableSetupColumn("Bones", ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.WidthFixed, 5 * CtrlHelper.IconButtonWidth);

                ImGui.TableSetupColumn($"{col1Label}", ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn($"{col2Label}", ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn($"{col3Label}", ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn($"{col4Label}", ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetColumnEnabled(4, _editingAttribute == BoneAttribute.Scale);

                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableHeadersRow();

                IEnumerable<EditRowParams> relevantModelBones = null!;
                if (_editorManager.IsEditorActive && _editorManager.EditorProfile != null && _editorManager.EditorProfile.Armatures.Count > 0)
                    relevantModelBones = _isShowLiveBones && _editorManager.EditorProfile.Armatures.Count > 0
                        ? _editorManager.EditorProfile.Armatures[0].GetAllBones().DistinctBy(x => x.BoneName).Select(x => new EditRowParams(x))
                        : _editorManager.EditorProfile.Armatures[0].BoneTemplateBinding.Where(x => x.Value.Bones.ContainsKey(x.Key))
                            .Select(x => new EditRowParams(x.Key, x.Value.Bones[x.Key])); //todo: this is awful
                else
                    relevantModelBones = _templateFileSystemSelector.Selected!.Bones.Select(x => new EditRowParams(x.Key, x.Value));

                if (!string.IsNullOrEmpty(_boneSearch))
                {
                    relevantModelBones = relevantModelBones
                        .Where(x => x.BoneDisplayName.Contains(_boneSearch, StringComparison.OrdinalIgnoreCase)
                                 || x.BoneCodeName.Contains(_boneSearch, StringComparison.OrdinalIgnoreCase));
                }

                var favoriteRows = relevantModelBones
                    .Where(b => _favoriteBones.Contains(b.BoneCodeName))
                    .OrderBy(b => BoneData.GetBoneRanking(b.BoneCodeName))
                    .ToList();

                var nonFavoriteRows = relevantModelBones
                    .Where(b => !_favoriteBones.Contains(b.BoneCodeName))
                    .ToList();

                var groupedBones = nonFavoriteRows
                    .GroupBy(x => BoneData.GetBoneFamily(x.BoneCodeName));

                if (favoriteRows.Count > 0)
                {
                    const string favoritesHeaderId = "FavoritesHeader";

                    if (!_groupExpandedState.TryGetValue((BoneData.BoneFamily)(-1), out var expanded))
                        _groupExpandedState[(BoneData.BoneFamily)(-1)] = expanded = true;

                    if (expanded)
                        ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
                    else
                        ImGui.TableNextRow();

                    using var id = ImRaii.PushId(favoritesHeaderId);
                    ImGui.TableNextColumn();
                    CtrlHelper.ArrowToggle($"##{favoritesHeaderId}", ref expanded);
                    ImGui.SameLine();
                    CtrlHelper.StaticLabel("Favorites");

                    if (expanded)
                    {
                        ImGui.TableNextRow();
                        foreach (var erp in favoriteRows)
                        {
                            var family = BoneData.GetBoneFamily(erp.BoneCodeName);
                            CompleteBoneEditor(family, erp);
                        }
                    }

                    _groupExpandedState[(BoneData.BoneFamily)(-1)] = expanded;
                }

                foreach (var boneGroup in groupedBones.OrderBy(x => (int)x.Key))
                {
                    if (!string.IsNullOrEmpty(_pendingImportText))
                    {
                        _logger.Debug("check import text 1: " + (_pendingImportText));
                        try
                        {
                            var importedBones = Base64Helper.ImportEditedBonesFromBase64(_pendingImportText);
                            if (importedBones != null)
                            {
                                foreach (var boneData in importedBones)
                                {
                                    _editorManager.ModifyBoneTransform(
                                        boneData.BoneCodeName,
                                        new BoneTransform
                                        {
                                            Translation = boneData.Translation,
                                            Rotation = boneData.Rotation,
                                            Scaling = boneData.Scaling,
                                            PropagateTranslation = boneData.PropagateTranslation,
                                            PropagateRotation = boneData.PropagateRotation,
                                            PropagateScale = boneData.PropagateScale
                                        }
                                    );
                                }
                            }
                        }
                        catch {  }
                        finally
                        {
                            _pendingImportText = null;
                        }
                    }

                    //Hide root bone if it's not enabled in settings or if we are in rotation mode
                    if (boneGroup.Key == BoneData.BoneFamily.Root &&
                        (!_configuration.EditorConfiguration.RootPositionEditingEnabled ||
                            _editingAttribute == BoneAttribute.Rotation))
                        continue;

                    //create a dropdown entry for the family if one doesn't already exist
                    //mind that it'll only be rendered if bones exist to fill it
                    if (!_groupExpandedState.TryGetValue(boneGroup.Key, out var expanded))
                    {
                        _groupExpandedState[boneGroup.Key] = false;
                        expanded = false;
                    }

                    if (expanded)
                    {
                        //paint the row in header colors if it's expanded
                        ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
                    }
                    else
                    {
                        ImGui.TableNextRow();
                    }

                    using var id = ImRaii.PushId(boneGroup.Key.ToString());
                    ImGui.TableNextColumn();

                    CtrlHelper.ArrowToggle($"##{boneGroup.Key}", ref expanded);
                    ImGui.SameLine();
                    CtrlHelper.StaticLabel(boneGroup.Key.ToString());
                    if (BoneData.DisplayableFamilies.TryGetValue(boneGroup.Key, out var tip) && tip != null)
                        CtrlHelper.AddHoverText(tip);

                    // sigma
                    var rowMin = ImGui.GetItemRectMin();
                    var rowMax = new Vector2(ImGui.GetContentRegionAvail().X + rowMin.X, ImGui.GetItemRectMax().Y);

                    if (ImGui.IsMouseHoveringRect(rowMin, rowMax) && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
                    {
                        ImGui.OpenPopup($"GroupContext##{boneGroup.Key}");
                    }

                    if (ImGui.BeginPopup($"GroupContext##{boneGroup.Key}"))
                    {
                        using (var disabled = ImRaii.Disabled(!_isUnlocked))
                        {
                            if (ImGui.MenuItem("Copy Group"))
                            {
                                try
                                {
                                    var editedBones = boneGroup
                                        .Where(b => b.Transform != null && b.Transform.IsEdited())
                                        .Select(b => (b.BoneCodeName, b.Transform))
                                        .ToList();

                                    if (editedBones.Count > 0)
                                    {
                                        _pendingClipboardText = Base64Helper.ExportEditedBonesToBase64(editedBones);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _popupSystem.ShowPopup(PopupSystem.Messages.ActionError);
                                }
                            }

                            if (ImGui.MenuItem("Import Group"))
                            {
                                var clipboardText = Clipboard.GetText();
                                if (!string.IsNullOrEmpty(clipboardText))
                                    _pendingImportText = clipboardText;
                            }
                        }

                        ImGui.EndPopup();
                    }

                    if (expanded)
                    {
                        ImGui.TableNextRow();
                        foreach (var erp in boneGroup.OrderBy(x => BoneData.GetBoneRanking(x.BoneCodeName)))
                        {
                            CompleteBoneEditor(boneGroup.Key, erp);
                        }
                    }

                    _groupExpandedState[boneGroup.Key] = expanded;
                }
            }
        }

        if (!string.IsNullOrEmpty(_pendingClipboardText))
        {
            try
            {
                Clipboard.SetText(_pendingClipboardText);
                _logger.Debug("copied to clipboard: " + _pendingClipboardText);
            }
            catch (Exception ex)
            {
                _logger.Debug("clipboard blew up :(");
            }
            _pendingClipboardText = null;
        }

    }

    private void DrawEditorConfirmationPopup()
    {
        if (_openSavePopup)
        {
            ImGui.OpenPopup("SavePopup");
            _openSavePopup = false;
        }

        var viewportSize = ImGui.GetWindowViewport().Size;
        ImGui.SetNextWindowSize(new Vector2(viewportSize.X / 4, viewportSize.Y / 12));
        ImGui.SetNextWindowPos(viewportSize / 2, ImGuiCond.Always, new Vector2(0.5f));
        using var popup = ImRaii.Popup("SavePopup", ImGuiWindowFlags.Modal);
        if (!popup)
            return;

        ImGui.SetCursorPos(new Vector2(ImGui.GetWindowWidth() / 4 - 40, ImGui.GetWindowHeight() / 4));
        ImGuiUtil.TextWrapped("You have unsaved changes in current template, what would you like to do?");

        var buttonWidth = new Vector2(150 * ImGuiHelpers.GlobalScale, 0);
        var yPos = ImGui.GetWindowHeight() - 2 * ImGui.GetFrameHeight();
        var xPos = (ImGui.GetWindowWidth() - ImGui.GetStyle().ItemSpacing.X) / 4 - buttonWidth.X;
        ImGui.SetCursorPos(new Vector2(xPos, yPos));

        var ExitedEditor = false;

        if (ImGui.Button("Save", buttonWidth))
        {
            _editorManager.SaveChangesAndDisableEditor();
            ExitedEditor = true;
            ImGui.CloseCurrentPopup();
        }

        ImGui.SameLine();
        if (ImGui.Button("Save as a copy", buttonWidth))
        {
            _editorManager.SaveChangesAndDisableEditor(true);
            ExitedEditor = true;
            ImGui.CloseCurrentPopup();
        }

        ImGui.SameLine();
        if (ImGui.Button("Do not save", buttonWidth))
        {
            _editorManager.DisableEditor();
            ExitedEditor = true;
            ImGui.CloseCurrentPopup();
        }

        ImGui.SameLine();
        if (ImGui.Button("Keep editing", buttonWidth))
        {
            ImGui.CloseCurrentPopup();
        }

        if (ExitedEditor)
        {
            _undoStack.Clear();
            _redoStack.Clear();
        }
    }

    #region ImGui helper functions

    private bool ResetBoneButton(EditRowParams bone)
    {
        var output = ImGuiComponents.IconButton(bone.BoneCodeName, FontAwesomeIcon.Recycle);
        CtrlHelper.AddHoverText(
            $"Reset '{BoneData.GetBoneDisplayName(bone.BoneCodeName)}' to default {_editingAttribute} values");

        if (output)
        {
            _editorManager.ResetBoneAttributeChanges(bone.BoneCodeName, _editingAttribute);
            if (_isMirrorModeEnabled && bone.Basis?.TwinBone != null) //todo: put it inside manager
                _editorManager.ResetBoneAttributeChanges(bone.Basis.TwinBone.BoneName, _editingAttribute);
        }

        return output;
    }

    private bool RevertBoneButton(EditRowParams bone)
    {
        var output = ImGuiComponents.IconButton(bone.BoneCodeName, FontAwesomeIcon.ArrowCircleLeft);
        CtrlHelper.AddHoverText(
            $"Revert '{BoneData.GetBoneDisplayName(bone.BoneCodeName)}' to last saved {_editingAttribute} values");

        if (output)
        {
            _editorManager.RevertBoneAttributeChanges(bone.BoneCodeName, _editingAttribute);
            if (_isMirrorModeEnabled && bone.Basis?.TwinBone != null) //todo: put it inside manager
                _editorManager.RevertBoneAttributeChanges(bone.Basis.TwinBone.BoneName, _editingAttribute);
        }

        return output;
    }

    private bool PropagateCheckbox(EditRowParams bone, ref bool enabled)
    {
        const FontAwesomeIcon icon = FontAwesomeIcon.Link;
        var id = $"##Propagate{bone.BoneCodeName}";

        if (enabled)
            ImGui.PushStyleColor(ImGuiCol.Text, Constants.Colors.Active);

        var output = ImGuiComponents.IconButton(id, icon);
        CtrlHelper.AddHoverText(
            $"Apply '{BoneData.GetBoneDisplayName(bone.BoneCodeName)}' transformations to its child bones");

        if (enabled)
            ImGui.PopStyleColor();

        if (output)
            enabled = !enabled;

        return output;
    }

    private bool FavoriteButton(EditRowParams bone)
    {
        var isFavorite = _favoriteBones.Contains(bone.BoneCodeName);

        const FontAwesomeIcon icon = FontAwesomeIcon.Star;
        var id = $"##Favorite{bone.BoneCodeName}";

        if (isFavorite)
            ImGui.PushStyleColor(ImGuiCol.Text, Constants.Colors.Favorite);

        var output = ImGuiComponents.IconButton(id, icon);

        if (isFavorite)
            ImGui.PopStyleColor();

        CtrlHelper.AddHoverText(
            $"Toggle favorite on '{BoneData.GetBoneDisplayName(bone.BoneCodeName)}' bone");

        if (output)
        {
            if (isFavorite)
                _favoriteBones.Remove(bone.BoneCodeName);
            else
                _favoriteBones.Add(bone.BoneCodeName);

            _configuration.EditorConfiguration.FavoriteBones = _favoriteBones.ToHashSet();
            _configuration.Save();
        }

        return isFavorite;
    }

    private bool FullBoneSlider(string label, ref Vector3 value)
    {
        var velocity = _editingAttribute == BoneAttribute.Rotation ? 0.1f : 0.001f;
        var minValue = _editingAttribute == BoneAttribute.Rotation ? -360.0f : -10.0f;
        var maxValue = _editingAttribute == BoneAttribute.Rotation ? 360.0f : 10.0f;

        var temp = _editingAttribute switch
        {
            BoneAttribute.Position => 0.0f,
            BoneAttribute.Rotation => 0.0f,
            _ => value.X == value.Y && value.Y == value.Z ? value.X : 1.0f
        };


        ImGui.PushItemWidth(ImGui.GetColumnWidth());
        if (ImGui.DragFloat(label, ref temp, velocity, minValue, maxValue, $"%.{_precision}f"))
        {
            value = new Vector3(temp, temp, temp);
            return true;

        }

        return false;
    }

    private bool SingleValueSlider(string label, ref float value)
    {
        var velocity = _editingAttribute == BoneAttribute.Rotation ? 0.1f : 0.001f;
        var minValue = _editingAttribute == BoneAttribute.Rotation ? -360.0f : -10.0f;
        var maxValue = _editingAttribute == BoneAttribute.Rotation ? 360.0f : 10.0f;

        ImGui.PushItemWidth(ImGui.GetColumnWidth());
        var temp = value;
        if (ImGui.DragFloat(label, ref temp, velocity, minValue, maxValue, $"%.{_precision}f"))
        {
            value = temp;
            return true;
        }

        return false;
    }

    private void CompleteBoneEditor(BoneData.BoneFamily boneFamily, EditRowParams bone)
    {
        var codename = bone.BoneCodeName;
        var displayName = bone.BoneDisplayName;
        var transform = new BoneTransform(bone.Transform);

        var newVector = _editingAttribute switch
        {
            BoneAttribute.Position => transform.Translation,
            BoneAttribute.Rotation => transform.Rotation,
            _ => transform.Scaling
        };

        var propagationEnabled = _editingAttribute switch
        {
            BoneAttribute.Position => transform.PropagateTranslation,
            BoneAttribute.Rotation => transform.PropagateRotation,
            _ => transform.PropagateScale
        };

        bool valueChanged = false;

        bool isFavorite = false;

        using var id = ImRaii.PushId(codename);
        ImGui.TableNextColumn();
        using (var disabled = ImRaii.Disabled(!_isUnlocked))
        {
            ImGui.Dummy(new Vector2(CtrlHelper.IconButtonWidth * 0.75f, 0));
            ImGui.SameLine();
            ResetBoneButton(bone);
            ImGui.SameLine();
            RevertBoneButton(bone);
            ImGui.SameLine();

            if (PropagateCheckbox(bone, ref propagationEnabled))
                valueChanged = true;

            ImGui.SameLine();
            isFavorite = FavoriteButton(bone);

            // adjusted logic, should only snapshot if there is a change in the value.
            // change da X
            ImGui.TableNextColumn();
            float tempX = newVector.X;
            if (ImGui.IsItemActivated())
            {
                _initialX = tempX;
                if (_pendingUndoSnapshot == null)
                    _pendingUndoSnapshot = CaptureCurrentState();
            }
            if (SingleValueSlider($"##{displayName}-X", ref tempX))
            {
                newVector.X = tempX;
                valueChanged = true;
            }
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                if (_pendingUndoSnapshot != null && _initialX != newVector.X)
                {
                    SaveStateForUndo(_pendingUndoSnapshot);
                    _pendingUndoSnapshot = null;
                }
            }

            // change da Y
            ImGui.TableNextColumn();
            float tempY = newVector.Y;
            if (ImGui.IsItemActivated())
            {
                _initialY = tempY;
                if (_pendingUndoSnapshot == null)
                    _pendingUndoSnapshot = CaptureCurrentState();
            }
            if (SingleValueSlider($"##{displayName}-Y", ref tempY))
            {
                newVector.Y = tempY;
                valueChanged = true;
            }
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                if (_pendingUndoSnapshot != null && _initialY != newVector.Y)
                {
                    SaveStateForUndo(_pendingUndoSnapshot);
                    _pendingUndoSnapshot = null;
                }
            }

            // change da Z
            ImGui.TableNextColumn();
            float tempZ = newVector.Z;
            if (ImGui.IsItemActivated())
            {
                _initialZ = tempZ;
                if (_pendingUndoSnapshot == null)
                    _pendingUndoSnapshot = CaptureCurrentState();
            }
            if (SingleValueSlider($"##{displayName}-Z", ref tempZ))
            {
                newVector.Z = tempZ;
                valueChanged = true;
            }
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                if (_pendingUndoSnapshot != null && _initialZ != newVector.Z)
                {
                    SaveStateForUndo(_pendingUndoSnapshot);
                    _pendingUndoSnapshot = null;
                }
            }

            // scale
            if (_editingAttribute != BoneAttribute.Scale)
                ImGui.BeginDisabled();

            ImGui.TableNextColumn();
            Vector3 tempScale = newVector;
            if (ImGui.IsItemActivated())
            {
                _initialScale = tempScale;
                if (_pendingUndoSnapshot == null)
                    _pendingUndoSnapshot = CaptureCurrentState();
            }
            if (FullBoneSlider($"##{displayName}-All", ref tempScale))
            {
                newVector = tempScale;
                valueChanged = true;
            }
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                if (_pendingUndoSnapshot != null && _initialScale != newVector)
                {
                    SaveStateForUndo(_pendingUndoSnapshot);
                    _pendingUndoSnapshot = null;
                }
            }

            if (_editingAttribute != BoneAttribute.Scale)
                ImGui.EndDisabled();
        }

        ImGui.TableNextColumn();
        if ((BoneData.IsIVCSCompatibleBone(codename) || boneFamily == BoneData.BoneFamily.Unknown) && !codename.StartsWith("j_f_"))
        {
            ImGui.PushStyleColor(ImGuiCol.Text, Constants.Colors.Warning);
            ImGuiUtil.PrintIcon(FontAwesomeIcon.Wrench);
            ImGui.PopStyleColor();
            CtrlHelper.AddHoverText("This is a bone from modded skeleton." +
                "\r\nIMPORTANT: The Customize+ team does not provide support for issues related to these bones." +
                "\r\nThese bones need special clothing and body mods designed specifically for them." +
                "\r\nEven if they are intended for these bones, not all clothing mods will support every bone." +
                "\r\nIf you experience issues, try performing the same actions using posing tools.");
            ImGui.SameLine();
        }

        CtrlHelper.StaticLabel(!isFavorite ? displayName : $"{displayName} ({boneFamily})", CtrlHelper.TextAlignment.Left,
            BoneData.IsIVCSCompatibleBone(codename) ? $"(IVCS Compatible) {codename}" : codename);

        if (valueChanged)
        {
            transform.UpdateAttribute(_editingAttribute, newVector, propagationEnabled);
            _editorManager.ModifyBoneTransform(codename, transform);

            if (_isMirrorModeEnabled && bone.Basis?.TwinBone != null)
            {
                _editorManager.ModifyBoneTransform(
                    bone.Basis.TwinBone.BoneName,
                    BoneData.IsIVCSCompatibleBone(codename)
                        ? transform.GetSpecialReflection()
                        : transform.GetStandardReflection()
                );
            }
        }

        ImGui.TableNextRow();
    }

    private Dictionary<string, BoneTransform> CaptureCurrentState()
    {
        return _editorManager.EditorProfile?.Armatures.Count > 0
            ? _editorManager.EditorProfile.Armatures[0]
                .GetAllBones()
                .DistinctBy(b => b.BoneName)
                .ToDictionary(
                    b => b.BoneName,
                    b => new BoneTransform(b.CustomizedTransform ?? new BoneTransform())
                )
            : new Dictionary<string, BoneTransform>();
    }

    private void SaveStateForUndo(Dictionary<string, BoneTransform> snapshot)
    {
        if (_undoStack.Count == 0 || !_undoStack.Peek().SequenceEqual(snapshot))
        {
            _undoStack.Push(snapshot);
            _redoStack.Clear();
        }
    }

    private void RestoreState(Dictionary<string, BoneTransform> state)
    {
        foreach (var kvp in state.DistinctBy(x => x.Key))
        {
            _editorManager.ModifyBoneTransform(kvp.Key, kvp.Value);
        }
    }

    #endregion
}

/// <summary>
/// Simple structure for representing arguments to the editor table.
/// Can be constructed with or without access to a live armature.
/// </summary>
internal struct EditRowParams
{
    public string BoneCodeName;
    public string BoneDisplayName => BoneData.GetBoneDisplayName(BoneCodeName);
    public BoneTransform Transform;
    public ModelBone? Basis = null;

    public EditRowParams(ModelBone mb)
    {
        BoneCodeName = mb.BoneName;
        Transform = mb.CustomizedTransform ?? new BoneTransform();
        Basis = mb;
    }

    public EditRowParams(string codename, BoneTransform tr)
    {
        BoneCodeName = codename;
        Transform = tr;
        Basis = null;
    }
}