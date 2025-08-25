﻿using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Bindings.ImGui;
using OtterGui;
using OtterGui.Raii;
using OtterGui.Extensions;
using System;
using System.Linq;
using System.Numerics;
using CustomizePlus.Profiles;
using CustomizePlus.Configuration.Data;
using CustomizePlus.Profiles.Data;
using CustomizePlus.UI.Windows.Controls;
using CustomizePlus.Templates;
using CustomizePlus.Core.Data;
using CustomizePlus.Templates.Events;
using Penumbra.GameData.Actors;
using Penumbra.String;
using static FFXIVClientStructs.FFXIV.Client.LayoutEngine.ILayoutInstance;
using CustomizePlus.GameData.Extensions;
using CustomizePlus.Core.Extensions;
using Dalamud.Interface.Components;
using OtterGui.Extensions;

namespace CustomizePlus.UI.Windows.MainWindow.Tabs.Profiles;

public class ProfilePanel
{
    private readonly ProfileFileSystemSelector _selector;
    private readonly ProfileManager _manager;
    private readonly PluginConfiguration _configuration;
    private readonly TemplateCombo _templateCombo;
    private readonly TemplateEditorManager _templateEditorManager;
    private readonly ActorAssignmentUi _actorAssignmentUi;
    private readonly ActorManager _actorManager;
    private readonly TemplateEditorEvent _templateEditorEvent;

    private string? _newName;
    private int? _newPriority;
    private Profile? _changedProfile;

    private Action? _endAction;

    private int _dragIndex = -1;

    private string SelectionName
        => _selector.Selected == null ? "No Selection" : _selector.IncognitoMode ? _selector.Selected.Incognito : _selector.Selected.Name.Text;

    public ProfilePanel(
        ProfileFileSystemSelector selector,
        ProfileManager manager,
        PluginConfiguration configuration,
        TemplateCombo templateCombo,
        TemplateEditorManager templateEditorManager,
        ActorAssignmentUi actorAssignmentUi,
        ActorManager actorManager,
        TemplateEditorEvent templateEditorEvent)
    {
        _selector = selector;
        _manager = manager;
        _configuration = configuration;
        _templateCombo = templateCombo;
        _templateEditorManager = templateEditorManager;
        _actorAssignmentUi = actorAssignmentUi;
        _actorManager = actorManager;
        _templateEditorEvent = templateEditorEvent;
    }

    public void Draw()
    {
        using var group = ImRaii.Group();
        if (_selector.SelectedPaths.Count > 1)
        {
            DrawMultiSelection();
        }
        else
        {
            DrawHeader();
            DrawPanel();
        }
    }

    private HeaderDrawer.Button LockButton()
        => _selector.Selected == null
            ? HeaderDrawer.Button.Invisible
            : _selector.Selected.IsWriteProtected
                ? new HeaderDrawer.Button
                {
                    Description = "Make this profile editable.",
                    Icon = FontAwesomeIcon.Lock,
                    OnClick = () => _manager.SetWriteProtection(_selector.Selected!, false)
                }
                : new HeaderDrawer.Button
                {
                    Description = "Write-protect this profile.",
                    Icon = FontAwesomeIcon.LockOpen,
                    OnClick = () => _manager.SetWriteProtection(_selector.Selected!, true)
                };

    private void DrawHeader()
        => HeaderDrawer.Draw(SelectionName, 0, ImGui.GetColorU32(ImGuiCol.FrameBg),
            0, LockButton(),
            HeaderDrawer.Button.IncognitoButton(_selector.IncognitoMode, v => _selector.IncognitoMode = v));

    private void DrawMultiSelection()
    {
        if (_selector.SelectedPaths.Count == 0)
            return;

        var sizeType = ImGui.GetFrameHeight();
        var availableSizePercent = (ImGui.GetContentRegionAvail().X - sizeType - 4 * ImGui.GetStyle().CellPadding.X) / 100;
        var sizeMods = availableSizePercent * 35;
        var sizeFolders = availableSizePercent * 65;

        ImGui.NewLine();
        ImGui.TextUnformatted("Currently Selected Profiles");
        ImGui.Separator();
        using var table = ImRaii.Table("profile", 3, ImGuiTableFlags.RowBg);
        ImGui.TableSetupColumn("btn", ImGuiTableColumnFlags.WidthFixed, sizeType);
        ImGui.TableSetupColumn("name", ImGuiTableColumnFlags.WidthFixed, sizeMods);
        ImGui.TableSetupColumn("path", ImGuiTableColumnFlags.WidthFixed, sizeFolders);

        var i = 0;
        foreach (var (fullName, path) in _selector.SelectedPaths.Select(p => (p.FullName(), p))
                     .OrderBy(p => p.Item1, StringComparer.OrdinalIgnoreCase))
        {
            using var id = ImRaii.PushId(i++);
            ImGui.TableNextColumn();
            var icon = (path is ProfileFileSystem.Leaf ? FontAwesomeIcon.FileCircleMinus : FontAwesomeIcon.FolderMinus).ToIconString();
            if (ImGuiUtil.DrawDisabledButton(icon, new Vector2(sizeType), "Remove from selection.", false, true))
                _selector.RemovePathFromMultiSelection(path);

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(path is ProfileFileSystem.Leaf l ? _selector.IncognitoMode ? l.Value.Incognito : l.Value.Name.Text : string.Empty);

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(_selector.IncognitoMode ? "Incognito is active" : fullName);
        }
    }

    private void DrawPanel()
    {
        using var child = ImRaii.Child("##Panel", -Vector2.One, true);
        if (!child || _selector.Selected == null)
            return;

        DrawEnabledSetting();

        ImGui.Separator();

        using (var disabled = ImRaii.Disabled(_selector.Selected?.IsWriteProtected ?? true))
        {
            DrawBasicSettings();

            ImGui.Separator();

            var isShouldDraw = ImGui.CollapsingHeader("Add character");

            if (isShouldDraw)
                DrawAddCharactersArea();

            ImGui.Separator();

            DrawCharacterListArea();

            ImGui.Separator();

            DrawTemplateArea();
        }
    }

    private void DrawEnabledSetting()
    {
        var spacing = ImGui.GetStyle().ItemInnerSpacing with { X = ImGui.GetStyle().ItemSpacing.X, Y = ImGui.GetStyle().ItemSpacing.Y };

        using (var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, spacing))
        {
            var enabled = _selector.Selected?.Enabled ?? false;
            using (ImRaii.Disabled(_templateEditorManager.IsEditorActive || _templateEditorManager.IsEditorPaused))
            {
                if (ImGui.Checkbox("##Enabled", ref enabled))
                    _manager.SetEnabled(_selector.Selected!, enabled);
                ImGuiUtil.LabeledHelpMarker("Enabled",
                    "Whether the templates in this profile should be applied at all.");
            }
        }
    }

    private void DrawBasicSettings()
    {
        using (var style = ImRaii.PushStyle(ImGuiStyleVar.ButtonTextAlign, new Vector2(0, 0.5f)))
        {
            using (var table = ImRaii.Table("BasicSettings", 2))
            {
                ImGui.TableSetupColumn("BasicCol1", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("lorem ipsum dolor").X);
                ImGui.TableSetupColumn("BasicCol2", ImGuiTableColumnFlags.WidthStretch);

                ImGuiUtil.DrawFrameColumn("Profile Name");
                ImGui.TableNextColumn();
                var width = new Vector2(ImGui.GetContentRegionAvail().X, 0);
                var name = _newName ?? _selector.Selected!.Name;
                ImGui.SetNextItemWidth(width.X);

                if (!_selector.IncognitoMode)
                {
                    if (ImGui.InputText("##ProfileName", ref name, 128))
                    {
                        _newName = name;
                        _changedProfile = _selector.Selected;
                    }

                    if (ImGui.IsItemDeactivatedAfterEdit() && _changedProfile != null)
                    {
                        _manager.Rename(_changedProfile, name);
                        _newName = null;
                        _changedProfile = null;
                    }
                }
                else
                    ImGui.TextUnformatted(_selector.Selected!.Incognito);

                ImGui.TableNextRow();

                ImGuiUtil.DrawFrameColumn("Priority");
                ImGui.TableNextColumn();

                var priority = _newPriority ?? _selector.Selected!.Priority;

                ImGui.SetNextItemWidth(50);
                if (ImGui.InputInt("##Priority", ref priority, 0, 0))
                {
                    _newPriority = priority;
                    _changedProfile = _selector.Selected;
                }

                if (ImGui.IsItemDeactivatedAfterEdit() && _changedProfile != null)
                {
                    _manager.SetPriority(_changedProfile, priority);
                    _newPriority = null;
                    _changedProfile = null;
                }

                ImGuiComponents.HelpMarker("Profiles with a higher number here take precedence before profiles with a lower number.\n" +
                    "That means if two or more profiles affect same character, profile with higher priority will be applied to that character.");
            }
        }
    }

    private void DrawAddCharactersArea()
    {
        using (var style = ImRaii.PushStyle(ImGuiStyleVar.ButtonTextAlign, new Vector2(0, 0.5f)))
        {
            var width = new Vector2(ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize("Limit to my creatures").X - 68, 0);

            ImGui.SetNextItemWidth(width.X);

            bool appliesToMultiple = _manager.DefaultProfile == _selector.Selected || _manager.DefaultLocalPlayerProfile == _selector.Selected;
            using (ImRaii.Disabled(appliesToMultiple))
            {
                _actorAssignmentUi.DrawWorldCombo(width.X / 2);
                ImGui.SameLine();
                _actorAssignmentUi.DrawPlayerInput(width.X / 2);

                var buttonWidth = new Vector2(165 * ImGuiHelpers.GlobalScale - ImGui.GetStyle().ItemSpacing.X / 2, 0);

                if (ImGuiUtil.DrawDisabledButton("Apply to player character", buttonWidth, string.Empty, !_actorAssignmentUi.CanSetPlayer))
                    _manager.AddCharacter(_selector.Selected!, _actorAssignmentUi.PlayerIdentifier);

                ImGui.SameLine();

                if (ImGuiUtil.DrawDisabledButton("Apply to retainer", buttonWidth, string.Empty, !_actorAssignmentUi.CanSetRetainer))
                    _manager.AddCharacter(_selector.Selected!, _actorAssignmentUi.RetainerIdentifier);

                ImGui.SameLine();

                if (ImGuiUtil.DrawDisabledButton("Apply to mannequin", buttonWidth, string.Empty, !_actorAssignmentUi.CanSetMannequin))
                    _manager.AddCharacter(_selector.Selected!, _actorAssignmentUi.MannequinIdentifier);

                var currentPlayer = _actorManager.GetCurrentPlayer().CreatePermanent();
                if (ImGuiUtil.DrawDisabledButton("Apply to current character", buttonWidth, string.Empty, !currentPlayer.IsValid))
                    _manager.AddCharacter(_selector.Selected!, currentPlayer);

                ImGui.Separator();

                _actorAssignmentUi.DrawObjectKindCombo(width.X / 2);
                ImGui.SameLine();
                _actorAssignmentUi.DrawNpcInput(width.X / 2);

                if (ImGuiUtil.DrawDisabledButton("Apply to selected NPC", buttonWidth, string.Empty, !_actorAssignmentUi.CanSetNpc))
                    _manager.AddCharacter(_selector.Selected!, _actorAssignmentUi.NpcIdentifier);
            }
        }
    }

    private void DrawCharacterListArea()
    {
        var isDefaultLP = _manager.DefaultLocalPlayerProfile == _selector.Selected;
        var isDefaultLPOrCurrentProfilesEnabled = (_manager.DefaultLocalPlayerProfile?.Enabled ?? false) || (_selector.Selected?.Enabled ?? false);
        using (ImRaii.Disabled(isDefaultLPOrCurrentProfilesEnabled))
        {
            if (ImGui.Checkbox("##DefaultLocalPlayerProfile", ref isDefaultLP))
                _manager.SetDefaultLocalPlayerProfile(isDefaultLP ? _selector.Selected! : null);
            ImGuiUtil.LabeledHelpMarker("Apply to any character you are logged in with",
                "Whether the templates in this profile should be applied to any character you are currently logged in with.\r\nTakes priority over the next option for said character.\r\nThis setting cannot be applied to multiple profiles.");
        }
        if (isDefaultLPOrCurrentProfilesEnabled)
        {
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, Constants.Colors.Warning);
            ImGuiUtil.PrintIcon(FontAwesomeIcon.ExclamationTriangle);
            ImGui.PopStyleColor();
            ImGuiUtil.HoverTooltip("Can only be changed when both currently selected and profile where this checkbox is checked are disabled.");
        }

        ImGui.SameLine();
        using(ImRaii.Disabled(true))
            ImGui.Button("##splitter", new Vector2(1, ImGui.GetFrameHeight()));
        ImGui.SameLine();

        var isDefault = _manager.DefaultProfile == _selector.Selected;
        var isDefaultOrCurrentProfilesEnabled = (_manager.DefaultProfile?.Enabled ?? false) || (_selector.Selected?.Enabled ?? false);
        using (ImRaii.Disabled(isDefaultOrCurrentProfilesEnabled))
        {
            if (ImGui.Checkbox("##DefaultProfile", ref isDefault))
                _manager.SetDefaultProfile(isDefault ? _selector.Selected! : null);
            ImGuiUtil.LabeledHelpMarker("Apply to all players and retainers",
                "Whether the templates in this profile are applied to all players and retainers without a specific profile.\r\nThis setting cannot be applied to multiple profiles.");
        }
        if (isDefaultOrCurrentProfilesEnabled)
        {
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, Constants.Colors.Warning);
            ImGuiUtil.PrintIcon(FontAwesomeIcon.ExclamationTriangle);
            ImGui.PopStyleColor();
            ImGuiUtil.HoverTooltip("Can only be changed when both currently selected and profile where this checkbox is checked are disabled.");
        }
        bool appliesToMultiple = _manager.DefaultProfile == _selector.Selected || _manager.DefaultLocalPlayerProfile == _selector.Selected;

        ImGui.Separator();

        using var dis = ImRaii.Disabled(appliesToMultiple);
        using var table = ImRaii.Table("CharacterTable", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollX | ImGuiTableFlags.ScrollY, new Vector2(ImGui.GetContentRegionAvail().X, 200));
        if (!table)
            return;

        ImGui.TableSetupColumn("##charaDel", ImGuiTableColumnFlags.WidthFixed, ImGui.GetFrameHeight());
        ImGui.TableSetupColumn("Character", ImGuiTableColumnFlags.WidthFixed, 320 * ImGuiHelpers.GlobalScale);
        ImGui.TableHeadersRow();

        if (appliesToMultiple)
        {
            ImGui.TableNextColumn();
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Applies to multiple targets");
            return;
        }

        //warn: .ToList() might be performance critical at some point
        //the copying via ToList is done because manipulations with .Templates list result in "Collection was modified" exception here
        var charas = _selector.Selected!.Characters.WithIndex().ToList();

        if (charas.Count == 0)
        {
            ImGui.TableNextColumn();
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("No characters are associated with this profile");
        }

        foreach (var (character, idx) in charas)
        {
            using var id = ImRaii.PushId(idx);
            ImGui.TableNextColumn();
            var keyValid = _configuration.UISettings.DeleteTemplateModifier.IsActive();
            var tt = keyValid
                ? "Remove this character from the profile."
                : $"Remove this character from the profile.\nHold {_configuration.UISettings.DeleteTemplateModifier} to remove.";

            if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Trash.ToIconString(), new Vector2(ImGui.GetFrameHeight()), tt, !keyValid, true))
                _endAction = () => _manager.DeleteCharacter(_selector.Selected!, character);
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(!_selector.IncognitoMode ? $"{character.ToNameWithoutOwnerName()}{character.TypeToString()}" : "Incognito");

            var profiles = _manager.GetEnabledProfilesByActor(character).ToList();
            if (profiles.Count > 1)
            {
                //todo: make helper
                ImGui.SameLine();
                if (profiles.Any(x => x.IsTemporary))
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, Constants.Colors.Error);
                    ImGuiUtil.PrintIcon(FontAwesomeIcon.Lock);
                }
                else if (profiles[0] != _selector.Selected!)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, Constants.Colors.Warning);
                    ImGuiUtil.PrintIcon(FontAwesomeIcon.ExclamationTriangle);
                }
                else
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, Constants.Colors.Info);
                    ImGuiUtil.PrintIcon(FontAwesomeIcon.Star);
                }

                ImGui.PopStyleColor();

                if (profiles.Any(x => x.IsTemporary))
                    ImGuiUtil.HoverTooltip("This character is being affected by temporary profile set by external plugin. This profile will not be applied!");
                else
                    ImGuiUtil.HoverTooltip(profiles[0] != _selector.Selected! ? "Several profiles are trying to affect this character. This profile will not be applied!" :
                        "Several profiles are trying to affect this character. This profile is being applied.");
            }
        }

        _endAction?.Invoke();
        _endAction = null;
    }

    private void DrawTemplateArea()
    {
        using var table = ImRaii.Table("TemplateTable", 5, ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollX | ImGuiTableFlags.ScrollY);
        if (!table)
            return;

        ImGui.TableSetupColumn("##del", ImGuiTableColumnFlags.WidthFixed, ImGui.GetFrameHeight());
        ImGui.TableSetupColumn("##Index", ImGuiTableColumnFlags.WidthFixed, 30 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("##Enabled", ImGuiTableColumnFlags.WidthFixed, 30 * ImGuiHelpers.GlobalScale);

        ImGui.TableSetupColumn("Template", ImGuiTableColumnFlags.WidthFixed, 220 * ImGuiHelpers.GlobalScale);

        ImGui.TableSetupColumn("##editbtn", ImGuiTableColumnFlags.WidthFixed, 120 * ImGuiHelpers.GlobalScale);

        ImGui.TableHeadersRow();

        //warn: .ToList() might be performance critical at some point
        //the copying via ToList is done because manipulations with .Templates list result in "Collection was modified" exception here
        foreach (var (template, idx) in _selector.Selected!.Templates.WithIndex().ToList())
        {
            using var id = ImRaii.PushId(idx);
            ImGui.TableNextColumn();
            var keyValid = _configuration.UISettings.DeleteTemplateModifier.IsActive();
            var tt = keyValid
                ? "Remove this template from the profile."
                : $"Remove this template from the profile.\nHold {_configuration.UISettings.DeleteTemplateModifier} to remove.";

            if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Trash.ToIconString(), new Vector2(ImGui.GetFrameHeight()), tt, !keyValid, true))
                _endAction = () => _manager.DeleteTemplate(_selector.Selected!, idx);
            ImGui.TableNextColumn();
            ImGui.Selectable($"#{idx + 1:D2}");
            DrawDragDrop(_selector.Selected!, idx);

            ImGui.TableNextColumn();
            var enabled = !_selector.Selected!.DisabledTemplates.Contains(template.UniqueId);
            if (ImGui.Checkbox("##EnableCheckbox", ref enabled))
                _manager.ToggleTemplate(_selector.Selected!, idx);
            ImGuiUtil.HoverTooltip("Whether this template is applied to the profile.");

            ImGui.TableNextColumn();

            _templateCombo.Draw(_selector.Selected!, template, idx);

            DrawDragDrop(_selector.Selected!, idx);

            ImGui.TableNextColumn();

            var disabledCondition = _templateEditorManager.IsEditorActive || template.IsWriteProtected;

            if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Edit.ToIconString(), new Vector2(ImGui.GetFrameHeight()), "Open this template in the template editor.", disabledCondition, true))
                _templateEditorEvent.Invoke(TemplateEditorEvent.Type.EditorEnableRequested, template);

            if (disabledCondition)
            {
                //todo: make helper
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Text, Constants.Colors.Warning);
                ImGuiUtil.PrintIcon(FontAwesomeIcon.ExclamationTriangle);
                ImGui.PopStyleColor();
                ImGuiUtil.HoverTooltip("This template cannot be edited because it is either write protected or you are already editing one of the templates.");
            }
        }

        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("New");
        ImGui.TableNextColumn();
        _templateCombo.Draw(_selector.Selected!, null, -1);
        ImGui.TableNextRow();

        _endAction?.Invoke();
        _endAction = null;
    }

    private void DrawDragDrop(Profile profile, int index)
    {
        const string dragDropLabel = "TemplateDragDrop";
        using (var target = ImRaii.DragDropTarget())
        {
            if (target.Success && ImGuiUtil.IsDropping(dragDropLabel))
            {
                if (_dragIndex >= 0)
                {
                    var idx = _dragIndex;
                    _endAction = () => _manager.MoveTemplate(profile, idx, index);
                }

                _dragIndex = -1;
            }
        }

        using (var source = ImRaii.DragDropSource())
        {
            if (source)
            {
                ImGui.TextUnformatted($"Moving template #{index + 1:D2}...");
                if (ImGui.SetDragDropPayload(dragDropLabel, null, 0))
                {
                    _dragIndex = index;
                }
            }
        }
    }

    private void UpdateIdentifiers()
    {

    }
}
