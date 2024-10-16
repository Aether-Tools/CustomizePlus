using Dalamud.Interface.Components;
using Dalamud.Interface;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Utility;
using OtterGui;
using OtterGui.Raii;
using CustomizePlus.Core.Data;
using CustomizePlus.Armatures.Data;
using CustomizePlus.Configuration.Data;
using CustomizePlus.Core.Helpers;
using CustomizePlus.Templates;
using CustomizePlus.Game.Services;
using CustomizePlus.Templates.Data;
using CustomizePlus.UI.Windows.Controls;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using Penumbra.GameData.Actors;
using CustomizePlus.GameData.Extensions;

namespace CustomizePlus.UI.Windows.MainWindow.Tabs.Templates;

public class BoneEditorPanel
{
    private readonly TemplateFileSystemSelector _templateFileSystemSelector;
    private readonly TemplateEditorManager _editorManager;
    private readonly PluginConfiguration _configuration;
    private readonly GameObjectService _gameObjectService;
    private readonly ActorAssignmentUi _actorAssignmentUi;

    private BoneAttribute _editingAttribute;
    private int _precision;

    private bool _isShowLiveBones;
    private bool _isMirrorModeEnabled;

    private Dictionary<BoneData.BoneFamily, bool> _groupExpandedState = new();

    private bool _openSavePopup;

    private bool _isUnlocked = false;

    public bool HasChanges => _editorManager.HasChanges;
    public bool IsEditorActive => _editorManager.IsEditorActive;
    public bool IsEditorPaused => _editorManager.IsEditorPaused;
    public bool IsCharacterFound => _editorManager.IsCharacterFound;

    public BoneEditorPanel(
        TemplateFileSystemSelector templateFileSystemSelector,
        TemplateEditorManager editorManager,
        PluginConfiguration configuration,
        GameObjectService gameObjectService,
        ActorAssignmentUi actorAssignmentUi)
    {
        _templateFileSystemSelector = templateFileSystemSelector;
        _editorManager = editorManager;
        _configuration = configuration;
        _gameObjectService = gameObjectService;
        _actorAssignmentUi = actorAssignmentUi;

        _isShowLiveBones = configuration.EditorConfiguration.ShowLiveBones;
        _isMirrorModeEnabled = configuration.EditorConfiguration.BoneMirroringEnabled;
        _precision = configuration.EditorConfiguration.EditorValuesPrecision;
        _editingAttribute = configuration.EditorConfiguration.EditorMode;
    }

    public bool EnableEditor(Template template)
    {
        if (_editorManager.EnableEditor(template))
        {
            //_editorManager.SetLimitLookupToOwned(_configuration.EditorConfiguration.LimitLookupToOwnedObjects);

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
            var isShouldDraw = ImGui.CollapsingHeader("Preview settings");

            if (isShouldDraw)
            {
                var width = new Vector2(ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize("Limit to my creatures").X - 68, 0);

                using (var disabled = ImRaii.Disabled(!IsEditorActive || IsEditorPaused))
                {
                    if (!_templateFileSystemSelector.IncognitoMode)
                    {
                        ImGui.Text(_editorManager.Character.IsValid ? $"Applies to {(_editorManager.Character.Type == Penumbra.GameData.Enums.IdentifierType.Owned ? 
                            _editorManager.Character.ToNameWithoutOwnerName() : _editorManager.Character.ToString())}" : "No valid character selected");
                        ImGui.Separator();

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

                        var currentPlayer = _gameObjectService.GetCurrentPlayerActorIdentifier();
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

            using (var table = ImRaii.Table("BoneEditorContents", 6, ImGuiTableFlags.BordersOuterH | ImGuiTableFlags.BordersV | ImGuiTableFlags.ScrollY))
            {
                if (!table)
                    return;

                var col1Label = _editingAttribute == BoneAttribute.Rotation ? "Roll" : "X";
                var col2Label = _editingAttribute == BoneAttribute.Rotation ? "Pitch" : "Y";
                var col3Label = _editingAttribute == BoneAttribute.Rotation ? "Yaw" : "Z";
                var col4Label = _editingAttribute == BoneAttribute.Scale ? "All" : "N/A";

                ImGui.TableSetupColumn("Bones", ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.WidthFixed, 3 * CtrlHelper.IconButtonWidth);

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

                var groupedBones = relevantModelBones.GroupBy(x => BoneData.GetBoneFamily(x.BoneCodeName));

                foreach (var boneGroup in groupedBones.OrderBy(x => (int)x.Key))
                {
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

                    if (expanded)
                    {
                        ImGui.TableNextRow();
                        foreach (var erp in boneGroup.OrderBy(x => BoneData.GetBoneRanking(x.BoneCodeName)))
                        {
                            CompleteBoneEditor(erp);
                        }
                    }

                    _groupExpandedState[boneGroup.Key] = expanded;
                }
            }
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
        if (ImGui.Button("Save", buttonWidth))
        {
            _editorManager.SaveChanges();
            _editorManager.DisableEditor();

            ImGui.CloseCurrentPopup();
        }

        ImGui.SameLine();
        if (ImGui.Button("Save as a copy", buttonWidth))
        {
            _editorManager.SaveChanges(true);
            _editorManager.DisableEditor();

            ImGui.CloseCurrentPopup();
        }

        ImGui.SameLine();
        if (ImGui.Button("Do not save", buttonWidth))
        {
            _editorManager.DisableEditor();

            ImGui.CloseCurrentPopup();
        }

        ImGui.SameLine();
        if (ImGui.Button("Keep editing", buttonWidth))
        {
            ImGui.CloseCurrentPopup();
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

    private void CompleteBoneEditor(EditRowParams bone)
    {
        var codename = bone.BoneCodeName;
        var displayName = bone.BoneDisplayName;
        var transform = new BoneTransform(bone.Transform);

        var flagUpdate = false;

        var newVector = _editingAttribute switch
        {
            BoneAttribute.Position => transform.Translation,
            BoneAttribute.Rotation => transform.Rotation,
            _ => transform.Scaling
        };

        using var id = ImRaii.PushId(codename);
        ImGui.TableNextColumn();
        using (var disabled = ImRaii.Disabled(!_isUnlocked))
        {
            //----------------------------------
            ImGui.Dummy(new Vector2(CtrlHelper.IconButtonWidth * 0.75f, 0));
            ImGui.SameLine();
            ResetBoneButton(bone);
            ImGui.SameLine();
            RevertBoneButton(bone);

            //----------------------------------
            ImGui.TableNextColumn();
            flagUpdate |= SingleValueSlider($"##{displayName}-X", ref newVector.X);

            //----------------------------------
            ImGui.TableNextColumn();
            flagUpdate |= SingleValueSlider($"##{displayName}-Y", ref newVector.Y);

            //-----------------------------------
            ImGui.TableNextColumn();
            flagUpdate |= SingleValueSlider($"##{displayName}-Z", ref newVector.Z);

            //----------------------------------
            if (_editingAttribute != BoneAttribute.Scale)
                ImGui.BeginDisabled();

            ImGui.TableNextColumn();
            var tempVec = new Vector3(newVector.X, newVector.Y, newVector.Z);
            flagUpdate |= FullBoneSlider($"##{displayName}-All", ref newVector);

            if (_editingAttribute != BoneAttribute.Scale)
                ImGui.EndDisabled();
        }

        //----------------------------------
        ImGui.TableNextColumn();
        CtrlHelper.StaticLabel(displayName, CtrlHelper.TextAlignment.Left, BoneData.IsIVCSCompatibleBone(codename) ? $"(IVCS Compatible) {codename}" : codename);

        if (flagUpdate)
        {
            transform.UpdateAttribute(_editingAttribute, newVector);

            _editorManager.ModifyBoneTransform(codename, transform);
            if (_isMirrorModeEnabled && bone.Basis?.TwinBone != null) //todo: put it inside manager
                _editorManager.ModifyBoneTransform(bone.Basis.TwinBone.BoneName,
                    BoneData.IsIVCSCompatibleBone(codename) ? transform.GetSpecialReflection() : transform.GetStandardReflection());
        }

        ImGui.TableNextRow();
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