using CustomizePlus.Armatures.Data;
using CustomizePlus.Core.Data;
using CustomizePlus.Core.Helpers;
using Dalamud.Interface;

namespace CustomizePlus.UI.Windows.MainWindow.Tabs.Templates;

public partial class BoneEditorPanel
{
    private void CompleteBoneEditor(BoneData.BoneFamily boneFamily, BoneEditRow bone)
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
        bool atomicEdit = false;
        bool editEnded = false;
        var (xEdited, yEdited, zEdited) = GetEditedAxes(newVector, _editingAttribute);
        var rowEdited = xEdited || yEdited || zEdited || propagationEnabled;

        bool isFavorite = false;

        using var id = Im.Id.Push(codename);
        SetEditedRowBackground(rowEdited);
        Im.Table.NextColumn();
        _parentRowScreenPosY = Im.Cursor.ScreenPosition.Y;
        using (var disabled = Im.Disabled(!_isUnlocked))
        {
            Im.Dummy(new Vector2(CtrlHelper.IconButtonWidth * 0.75f, 0));
            Im.Line.Same();
            ResetBoneButton(bone);
            Im.Line.Same();
            RevertBoneButton(bone);
            Im.Line.Same();

            _propagateButtonXPos = Im.Cursor.X;
            if (PropagateCheckbox(bone, ref propagationEnabled))
            {
                atomicEdit = true;
                valueChanged = true;
            }

            Im.Line.Same();
            isFavorite = FavoriteButton(bone);

            // X
            NextAxisCell(xEdited ? AxisXEditedCellColor : AxisXCellColor);
            float tempX = newVector.X;
            if (TrackedSingleValueSlider($"##{codename}-X", ref tempX, out var xEditEnded))
            {
                newVector.X = tempX;
                valueChanged = true;
            }
            editEnded |= xEditEnded;

            // Y
            NextAxisCell(yEdited ? AxisYEditedCellColor : AxisYCellColor);
            float tempY = newVector.Y;
            if (TrackedSingleValueSlider($"##{codename}-Y", ref tempY, out var yEditEnded))
            {
                newVector.Y = tempY;
                valueChanged = true;
            }
            editEnded |= yEditEnded;

            // Z
            NextAxisCell(zEdited ? AxisZEditedCellColor : AxisZCellColor);
            float tempZ = newVector.Z;
            if (TrackedSingleValueSlider($"##{codename}-Z", ref tempZ, out var zEditEnded))
            {
                newVector.Z = tempZ;
                valueChanged = true;
            }
            editEnded |= zEditEnded;

            if (_editingAttribute == BoneAttribute.Scale)
            {
                Im.Table.NextColumn();
                if (rowEdited)
                    Im.Table.SetBackgroundColor(TableBackgroundTarget.Cell, WithAlpha(ImGuiColor.CheckMark, 0.12f));

                Vector3 tempScale = newVector;
                if (TrackedFullBoneSlider($"##{codename}-All", ref tempScale, out var allEditEnded))
                {
                    newVector = tempScale;
                    valueChanged = true;
                }
                editEnded |= allEditEnded;
            }
        }

        Im.Table.NextColumn();
        if ((BoneData.IsIVCSCompatibleBone(codename) || boneFamily == BoneData.BoneFamily.Unknown) && !codename.StartsWith("j_f_"))
        {
            using (ImGuiColor.Text.Push(Constants.Colors.Warning))
                UiHelpers.DrawIcon(FontAwesomeIcon.Wrench);

            CtrlHelper.AddHoverText("This is a bone from modded skeleton." +
                "\r\nIMPORTANT: The Customize+ team does not provide support for issues related to these bones." +
                "\r\nThese bones need special clothing and body mods designed specifically for them." +
                "\r\nEven if they are intended for these bones, not all clothing mods will support every bone." +
                "\r\nIf you experience issues, try performing the same actions using posing tools.");
            Im.Line.Same();
        }

        CtrlHelper.StaticLabel(!isFavorite ? displayName : $"{displayName} ({boneFamily})", CtrlHelper.TextAlignment.Left,
            BoneData.IsIVCSCompatibleBone(codename) ? $"(IVCS Compatible) {codename}" : codename);

        if (valueChanged)
        {
            void ApplyChange()
            {
                transform.UpdateAttribute(_editingAttribute, newVector, propagationEnabled);
                ApplyBoneTransform(bone, transform);
            }

            if (atomicEdit) //if this isn't atomic edit (ex checkbox), then changes will be persisted by EndEdit later
                _editorManager.ExecuteAtomicEdit(ApplyChange);
            else
                ApplyChange();
        }

        if (editEnded)
            _editorManager.EndEdit();

        Im.Table.NextRow();

        if (_editingAttribute == BoneAttribute.Scale && propagationEnabled)
        {
            RenderChildScalingRow(bone, transform);
        }
    }

    private void ApplyBoneTransform(BoneEditRow bone, BoneTransform transform)
    {
        _editorManager.ModifyBoneTransform(bone.BoneCodeName, transform);

        if (_isMirrorModeEnabled && bone.Basis?.TwinBone != null)
        {
            _editorManager.ModifyBoneTransform(
                bone.Basis.TwinBone.BoneName,
                BoneData.IsIVCSCompatibleBone(bone.BoneCodeName)
                    ? transform.GetSpecialReflection()
                    : transform.GetStandardReflection());
        }
    }

    private void RenderChildScalingRow(BoneEditRow bone, BoneTransform transform)
    {
        var codename = bone.BoneCodeName;
        var displayName = bone.BoneDisplayName;

        bool isChildScaleIndependent = transform.ChildScalingIndependent;
        bool childScaleChanged = false;
        bool atomicEdit = false;
        bool editEnded = false;
        var childScale = isChildScaleIndependent ? transform.ChildScaling : transform.Scaling;
        var (childXEdited, childYEdited, childZEdited) = GetEditedAxes(childScale, BoneAttribute.ChildScaling);
        var xEdited = isChildScaleIndependent && childXEdited;
        var yEdited = isChildScaleIndependent && childYEdited;
        var zEdited = isChildScaleIndependent && childZEdited;
        var rowEdited = xEdited || yEdited || zEdited;

        using var id = Im.Id.Push($"{codename}_childscale");

        SetEditedRowBackground(rowEdited);
        Im.Table.NextColumn();

        Im.Cursor.X = _propagateButtonXPos;

        using (var disabled = Im.Disabled(!_isUnlocked))
        {
            var wasLinked = !isChildScaleIndependent;

            using (ImGuiColor.Text.Push(Constants.Colors.Active, wasLinked))
            {
                if (DrawIconButton($"ChildLink{codename}", FontAwesomeIcon.Link, "Toggle independent child scaling."))
                {
                    atomicEdit = true;
                    isChildScaleIndependent = !isChildScaleIndependent;
                    if (isChildScaleIndependent)
                    {
                        childScale = transform.Scaling;
                    }
                    else
                    {
                        transform.ChildScaling = Vector3.One;
                    }
                    transform.ChildScalingIndependent = isChildScaleIndependent;
                    childScaleChanged = true;
                }
            }

            CtrlHelper.AddHoverText(
                $"Link '{BoneData.GetBoneDisplayName(codename)}' child bone scaling to parent scaling");
        }

        // Draws a bracket between the two rows.
        var drawList = Im.Window.DrawList;
        var bracketColor = ImGuiColor.TextDisabled.Get();
        var lineThickness = 2.0f;

        var rowHeight = Im.Style.FrameHeight;
        var bracketWidth = CtrlHelper.IconButtonWidth * 0.3f;

        var availWidth = Im.ContentRegion.Available.X;
        var cursorScreenPos = Im.Cursor.ScreenPosition;
        var rightEdgeX = cursorScreenPos.X + availWidth - bracketWidth;

        var parentRowCenterY = _parentRowScreenPosY + rowHeight * 0.5f;
        var childRowCenterY = cursorScreenPos.Y + rowHeight * 0.5f;
        var bracketCenterY = (parentRowCenterY + childRowCenterY) * 0.5f;

        var topY = parentRowCenterY;
        var bottomY = bracketCenterY;
        var heightThird = (topY - bottomY) / 3;
        var topRightM = new Vector2(rightEdgeX + bracketWidth - 1, topY);
        var topLeft = new Vector2(rightEdgeX, topY);
        var bottomLeft = new Vector2(rightEdgeX, bottomY);
        var bottomLeftM = new Vector2(rightEdgeX - 1, bottomY); // Just works
        var bottomRight = new Vector2(rightEdgeX + bracketWidth, bottomY);

        drawList.Shape.Line(topRightM, topLeft, bracketColor, lineThickness);   // Top
        if (!isChildScaleIndependent)
        {
            drawList.Shape.Line(topLeft, bottomLeft, bracketColor, lineThickness); // Middle
        }
        else
        {
            var gapStart = new Vector2(rightEdgeX, topY - heightThird);
            var gapEnd = new Vector2(rightEdgeX, topY - 2 * heightThird);
            drawList.Shape.Line(topLeft, gapStart, bracketColor, lineThickness);
            drawList.Shape.Line(gapEnd, bottomLeft, bracketColor, lineThickness);
        }
        drawList.Shape.Line(bottomLeftM, bottomRight, bracketColor, lineThickness); // Bottom

        using (var disabled = Im.Disabled(!_isUnlocked || !isChildScaleIndependent))
        {
            NextAxisCell(xEdited ? AxisXEditedCellColor : AxisXCellColor);
            float tempChildX = childScale.X;
            if (TrackedSingleValueSlider($"##child-{codename}-X", ref tempChildX, out var xEditEnded))
            {
                childScale.X = tempChildX;
                childScaleChanged = true;
            }
            editEnded |= xEditEnded;

            NextAxisCell(yEdited ? AxisYEditedCellColor : AxisYCellColor);
            float tempChildY = childScale.Y;
            if (TrackedSingleValueSlider($"##child-{codename}-Y", ref tempChildY, out var yEditEnded))
            {
                childScale.Y = tempChildY;
                childScaleChanged = true;
            }
            editEnded |= yEditEnded;

            NextAxisCell(zEdited ? AxisZEditedCellColor : AxisZCellColor);
            float tempChildZ = childScale.Z;
            if (TrackedSingleValueSlider($"##child-{codename}-Z", ref tempChildZ, out var zEditEnded))
            {
                childScale.Z = tempChildZ;
                childScaleChanged = true;
            }
            editEnded |= zEditEnded;

            Im.Table.NextColumn();
            if (rowEdited)
                Im.Table.SetBackgroundColor(TableBackgroundTarget.Cell, WithAlpha(ImGuiColor.CheckMark, 0.12f));

            if (TrackedFullBoneSlider($"##child-{codename}-All", ref childScale, out var allEditEnded))
                childScaleChanged = true;
            editEnded |= allEditEnded;
        }

        Im.Table.NextColumn();
        CtrlHelper.StaticLabel($"{displayName} - Child Bones", CtrlHelper.TextAlignment.Left, "Scale applied to child bones");

        if (childScaleChanged)
        {
            void ApplyChange()
            {
                transform.ChildScaling = childScale;
                ApplyBoneTransform(bone, transform);
            }

            if (atomicEdit) //if this isn't atomic edit (ex checkbox), then changes will be persisted by EndEdit later
                _editorManager.ExecuteAtomicEdit(ApplyChange);
            else
                ApplyChange();
        }

        if (editEnded)
            _editorManager.EndEdit();

        Im.Table.NextRow();
    }
}

/// <summary>
/// Simple structure for representing arguments to the editor table.
/// Can be constructed with or without access to a live armature.
/// </summary>
internal readonly record struct BoneEditRow
{
    public string BoneCodeName { get; }
    public string BoneDisplayName => BoneData.GetBoneDisplayName(BoneCodeName);
    public BoneTransform Transform { get; }
    public ModelBone? Basis { get; }

    public BoneEditRow(ModelBone modelBone)
    {
        BoneCodeName = modelBone.BoneName;
        Transform = modelBone.CustomizedTransform ?? new BoneTransform();
        Basis = modelBone;
    }

    public BoneEditRow(string codeName, BoneTransform transform)
    {
        BoneCodeName = codeName;
        Transform = transform;
        Basis = null;
    }
}
