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
    private bool ResetBoneButton(BoneEditRow bone)
    {
        var output = DrawIconButton(
            bone.BoneCodeName,
            FontAwesomeIcon.Recycle,
            $"Reset '{BoneData.GetBoneDisplayName(bone.BoneCodeName)}' to default {_editingAttribute} values");

        if (output)
        {
            _editorManager.ExecuteAtomicEdit(() =>
            {
                _editorManager.ResetBoneAttributeChanges(bone.BoneCodeName, _editingAttribute);
                if (_isMirrorModeEnabled && bone.Basis?.TwinBone != null) //todo: put it inside manager
                    _editorManager.ResetBoneAttributeChanges(bone.Basis.TwinBone.BoneName, _editingAttribute);
            });
        }

        return output;
    }

    private bool RevertBoneButton(BoneEditRow bone)
    {
        var output = DrawIconButton(
            bone.BoneCodeName,
            FontAwesomeIcon.ArrowCircleLeft,
            $"Revert '{BoneData.GetBoneDisplayName(bone.BoneCodeName)}' to last saved {_editingAttribute} values");

        if (output)
        {
            _editorManager.ExecuteAtomicEdit(() =>
            {
                _editorManager.RevertBoneAttributeChanges(bone.BoneCodeName, _editingAttribute);
                if (_isMirrorModeEnabled && bone.Basis?.TwinBone != null) //todo: put it inside manager
                    _editorManager.RevertBoneAttributeChanges(bone.Basis.TwinBone.BoneName, _editingAttribute);
            });
        }

        return output;
    }

    private bool PropagateCheckbox(BoneEditRow bone, ref bool enabled)
    {
        const FontAwesomeIcon icon = FontAwesomeIcon.Link;
        var id = $"##Propagate{bone.BoneCodeName}";

        using var color = ImGuiColor.Text.Push(Constants.Colors.Active, enabled);
        var output = DrawIconButton(
            id,
            icon,
            $"Apply '{BoneData.GetBoneDisplayName(bone.BoneCodeName)}' transformations to its child bones");

        if (output)
            enabled = !enabled;

        return output;
    }

    private bool FavoriteButton(BoneEditRow bone)
    {
        var isFavorite = _favoriteBones.Contains(bone.BoneCodeName);

        const FontAwesomeIcon icon = FontAwesomeIcon.Star;
        var id = $"##Favorite{bone.BoneCodeName}";

        using var color = ImGuiColor.Text.Push(Constants.Colors.Favorite, isFavorite);
        var output = DrawIconButton(
            id,
            icon,
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

    private static bool DrawIconButton(string id, FontAwesomeIcon icon, string tooltip, bool disabled = false)
    {
        using var pushId = Im.Id.Push(id);
        return ImEx.Icon.Button(icon.Icon(), tooltip, disabled);
    }

    private static void DrawBoneEditorHeaderRow(bool showAllColumn, string col1Label, string col2Label, string col3Label)
    {
        Im.Table.NextRow(TableRowFlags.Headers);

        DrawHeaderCell("Bones");
        DrawHeaderCell(col1Label, AxisXHeaderColor);
        DrawHeaderCell(col2Label, AxisYHeaderColor);
        DrawHeaderCell(col3Label, AxisZHeaderColor);

        if (showAllColumn)
            DrawHeaderCell("All");

        DrawHeaderCell("Name");
    }

    private static void DrawHeaderCell(string label, Vector4? color = null)
    {
        Im.Table.NextColumn();
        if (color.HasValue)
            Im.Table.SetBackgroundColor(TableBackgroundTarget.Cell, color.Value);

        Im.Table.Header(label);
    }

    private static void NextAxisCell(Vector4 color)
    {
        Im.Table.NextColumn();
        Im.Table.SetBackgroundColor(TableBackgroundTarget.Cell, color);
    }

    private static void SetEditedRowBackground(bool edited)
    {
        if (edited)
            Im.Table.SetBackgroundColor(TableBackgroundTarget.Row1, WithAlpha(ImGuiColor.CheckMark, 0.08f));
    }

    private static (bool X, bool Y, bool Z) GetEditedAxes(Vector3 value, BoneAttribute attribute)
        => (
            IsValueEdited(value.X, attribute),
            IsValueEdited(value.Y, attribute),
            IsValueEdited(value.Z, attribute)
        );

    private static bool IsValueEdited(float value, BoneAttribute attribute)
    {
        return attribute switch
        {
            BoneAttribute.Rotation => Math.Abs(value) > 0.1f,
            BoneAttribute.Scale or BoneAttribute.ChildScaling => Math.Abs(value - 1f) > 0.00001f,
            _ => Math.Abs(value) > 0.00001f
        };
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

        if (DragBoneValue(label, ref temp, minValue, maxValue, velocity))
        {
            value = new Vector3(temp, temp, temp);
            return true;

        }

        return false;
    }

    private bool TrackedFullBoneSlider(string label, ref Vector3 value, out bool editEnded)
    {
        var changed = FullBoneSlider(label, ref value);
        if (Im.Item.Activated)
            _editorManager.BeginEdit();

        editEnded = Im.Item.Deactivated;
        return changed;
    }

    private bool SingleValueSlider(string label, ref float value)
    {
        var velocity = _editingAttribute == BoneAttribute.Rotation ? 0.1f : 0.001f;
        var minValue = _editingAttribute == BoneAttribute.Rotation ? -360.0f : -10.0f;
        var maxValue = _editingAttribute == BoneAttribute.Rotation ? 360.0f : 10.0f;

        var temp = value;
        if (DragBoneValue(label, ref temp, minValue, maxValue, velocity))
        {
            value = temp;
            return true;
        }

        return false;
    }

    private bool TrackedSingleValueSlider(string label, ref float value, out bool editEnded)
    {
        var changed = SingleValueSlider(label, ref value);
        if (Im.Item.Activated)
            _editorManager.BeginEdit();

        editEnded = Im.Item.Deactivated;
        return changed;
    }

    private bool DragBoneValue(string label, ref float value, float minValue, float maxValue, float velocity)
    {
        using var colors = Im.Color.Push(ImGuiColor.FrameBackground, DarkenWithMinimumAlpha(ImGuiColor.FrameBackground, 0.52f, 0.78f))
            .Push(ImGuiColor.FrameBackgroundHovered, DarkenWithMinimumAlpha(ImGuiColor.FrameBackgroundHovered, 0.64f, 0.86f))
            .Push(ImGuiColor.FrameBackgroundActive, DarkenWithMinimumAlpha(ImGuiColor.FrameBackgroundActive, 0.76f, 0.94f))
            .Push(ImGuiColor.Border, WithMinimumAlpha(ImGuiColor.Border, 0.30f));
        using var style = Im.Style.Push(ImStyleSingle.FrameBorderThickness, ImGuiHelpers.GlobalScale);

        Im.Item.SetNextWidthFull();
        return Im.Drag(label, ref value, $"%.{_precision}f", minValue, maxValue, velocity);
    }

    private static Vector4 DarkenWithMinimumAlpha(ImGuiColor color, float brightness, float alpha)
    {
        var value = Im.Color.Get(color).ToVector();
        value.X *= brightness;
        value.Y *= brightness;
        value.Z *= brightness;
        value.W = MathF.Max(value.W, alpha);
        return value;
    }

    private static Vector4 WithMinimumAlpha(ImGuiColor color, float alpha)
    {
        var value = Im.Color.Get(color).ToVector();
        value.W = MathF.Max(value.W, alpha);
        return value;
    }

    private static Vector4 WithAlpha(ImGuiColor color, float alpha)
    {
        var value = Im.Color.Get(color).ToVector();
        value.W = alpha;
        return value;
    }
}
