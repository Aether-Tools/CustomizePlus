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
    private bool DrawBoneTable()
    {
        var showAllColumn = _editingAttribute == BoneAttribute.Scale;
        var tableSize = Im.ContentRegion.Available;
        tableSize.X = MathF.Max(1, tableSize.X);
        tableSize.Y = MathF.Max(Im.Style.FrameHeightWithSpacing * 6, tableSize.Y);

        using var tableSpacing = Im.Style.PushY(ImStyleDouble.ItemSpacing, 0);
        using (var table = Im.Table.Begin(
            "BoneEditorContents"u8,
            showAllColumn ? 6 : 5,
            TableFlags.BordersOuterHorizontal | TableFlags.BordersVertical | TableFlags.ScrollY | TableFlags.SizingStretchSame,
            tableSize))
        {
            if (!table)
                return false;

            var col1Label = _editingAttribute == BoneAttribute.Rotation ? "Roll" : "X";
            var col2Label = _editingAttribute == BoneAttribute.Rotation ? "Pitch" : "Y";
            var col3Label = _editingAttribute == BoneAttribute.Rotation ? "Yaw" : "Z";

            table.SetupColumn("Bones"u8, TableColumnFlags.NoReorder | TableColumnFlags.WidthFixed, 6 * CtrlHelper.IconButtonWidth);

            table.SetupColumn($"{col1Label}", TableColumnFlags.NoReorder | TableColumnFlags.WidthStretch);
            table.SetupColumn($"{col2Label}", TableColumnFlags.NoReorder | TableColumnFlags.WidthStretch);
            table.SetupColumn($"{col3Label}", TableColumnFlags.NoReorder | TableColumnFlags.WidthStretch);
            if (showAllColumn)
                table.SetupColumn("All"u8, TableColumnFlags.NoReorder | TableColumnFlags.WidthStretch);

            table.SetupColumn("Name"u8, TableColumnFlags.NoReorder | TableColumnFlags.WidthStretch);

            DrawBoneEditorHeaderRow(showAllColumn, col1Label, col2Label, col3Label);

            IEnumerable<BoneEditRow> relevantModelBones = null!;
            if (_editorManager.IsEditorActive && _editorManager.EditorProfile != null && _editorManager.EditorProfile.Armatures.Count > 0)
                relevantModelBones = _isShowLiveBones && _editorManager.EditorProfile.Armatures.Count > 0
                    ? _editorManager.EditorProfile.Armatures[0].GetAllBones().DistinctBy(x => x.BoneName).Select(x => new BoneEditRow(x))
                    : _editorManager.EditorProfile.Armatures[0].BoneTemplateBinding.Where(x => x.Value.Bones.ContainsKey(x.Key))
                        .Select(x => new BoneEditRow(x.Key, x.Value.Bones[x.Key])); //todo: this is awful
            else
                relevantModelBones = ((Template)_fileSystem.Selection.Selection!.Value).Bones.Select(x => new BoneEditRow(x.Key, x.Value));

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
                    Im.Table.NextRow(TableRowFlags.Headers);
                else
                    Im.Table.NextRow();

                using var id = Im.Id.Push("FavoritesHeader"u8);
                Im.Table.NextColumn();
                CtrlHelper.ArrowToggle($"##{favoritesHeaderId}", ref expanded);
                Im.Line.Same();
                CtrlHelper.StaticLabel("Favorites");

                if (expanded)
                {
                    Im.Table.NextRow();
                    foreach (var erp in favoriteRows)
                    {
                        var family = BoneData.GetBoneFamily(erp.BoneCodeName);
                        CompleteBoneEditor(family, erp);
                    }
                }

                _groupExpandedState[(BoneData.BoneFamily)(-1)] = expanded;
            }

            if (!string.IsNullOrEmpty(_pendingImportText))
            {
                _logger.Debug("check import text 1: " + _pendingImportText);
                try
                {
                    var importedBones = Base64Helper.ImportEditedBonesFromBase64(_pendingImportText);
                    if (importedBones != null)
                    {
                        _editorManager.ExecuteAtomicEdit(() =>
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
                                        ChildScaling = boneData.ChildScaling,
                                        ChildScalingIndependent = boneData.ChildScalingIndependent,
                                        PropagateTranslation = boneData.PropagateTranslation,
                                        PropagateRotation = boneData.PropagateRotation,
                                        PropagateScale = boneData.PropagateScale
                                    }
                                );
                            }
                        });
                    }
                }
                catch { }
                finally
                {
                    _pendingImportText = null;
                }
            }

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
                    Im.Table.NextRow(TableRowFlags.Headers);
                }
                else
                {
                    Im.Table.NextRow();
                }

                using var id = Im.Id.Push($"{boneGroup.Key}");
                Im.Table.NextColumn();

                CtrlHelper.ArrowToggle($"##{boneGroup.Key}", ref expanded);
                Im.Line.Same();
                CtrlHelper.StaticLabel(boneGroup.Key.ToString());
                if (BoneData.DisplayableFamilies.TryGetValue(boneGroup.Key, out var tip) && tip != null)
                    CtrlHelper.AddHoverText(tip);

                // sigma
                var rowMin = Im.Item.Bounds.Minimum;
                var rowMax = new Vector2(Im.ContentRegion.Available.X + rowMin.X, Im.Item.Bounds.Maximum.Y);

                if (Im.Mouse.IsHoveringRectangle(rowMin, rowMax) && Im.Mouse.IsClicked(MouseButton.Right))
                {
                    Im.Popup.Open($"GroupContext##{boneGroup.Key}");
                }

                using (var popup = Im.Popup.Begin($"GroupContext##{boneGroup.Key}"))
                {
                    if (popup)
                    {
                        using (var disabled = Im.Disabled(!_isUnlocked))
                        {
                            if (Im.Menu.Item("Copy Group"u8))
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
                                    _logger.Error($"Error while copying bone group: {ex}");
                                    _popupSystem.ShowPopup(PopupSystem.Messages.ActionError);
                                }
                            }

                            if (Im.Menu.Item("Import Group"u8))
                            {
                                var clipboardText = Im.Clipboard.GetUtf16();
                                if (!string.IsNullOrEmpty(clipboardText))
                                    _pendingImportText = clipboardText;
                            }
                        }
                    }
                }

                if (expanded)
                {
                    Im.Table.NextRow();
                    foreach (var erp in boneGroup.OrderBy(x => BoneData.GetBoneRanking(x.BoneCodeName)))
                    {
                        CompleteBoneEditor(boneGroup.Key, erp);
                    }
                }

                _groupExpandedState[boneGroup.Key] = expanded;
            }
        }

        return true;
    }

    private void FlushPendingClipboard()
    {
        if (!string.IsNullOrEmpty(_pendingClipboardText))
        {
            try
            {
                Im.Clipboard.Set(_pendingClipboardText);
                _logger.Debug("copied to clipboard: " + _pendingClipboardText);
            }
            catch (Exception ex)
            {
                _logger.Debug($"Could not copy bone editor data to clipboard: {ex}");
            }
            _pendingClipboardText = null;
        }
    }
}
