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

public class BoneEditorPanel
{
    private static readonly Vector4 AxisXHeaderColor = new(0.80f, 0.28f, 0.28f, 0.28f);
    private static readonly Vector4 AxisYHeaderColor = new(0.35f, 0.72f, 0.35f, 0.24f);
    private static readonly Vector4 AxisZHeaderColor = new(0.32f, 0.52f, 0.95f, 0.26f);

    private static readonly Vector4 AxisXCellColor = new(0.80f, 0.28f, 0.28f, 0.08f);
    private static readonly Vector4 AxisYCellColor = new(0.35f, 0.72f, 0.35f, 0.07f);
    private static readonly Vector4 AxisZCellColor = new(0.32f, 0.52f, 0.95f, 0.08f);
    private static readonly Vector4 AxisXEditedCellColor = new(0.80f, 0.28f, 0.28f, 0.18f);
    private static readonly Vector4 AxisYEditedCellColor = new(0.35f, 0.72f, 0.35f, 0.16f);
    private static readonly Vector4 AxisZEditedCellColor = new(0.32f, 0.52f, 0.95f, 0.18f);

    //private readonly TemplateFileSystemSelector _templateFileSystemSelector;
    private readonly TemplateFileSystem _fileSystem;
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
    private float _initialChildX, _initialChildY, _initialChildZ;
    private Vector3 _initialChildScale;
    private float _propagateButtonXPos = 0;
    private float _parentRowScreenPosY = 0;

    // favorite bone stuff
    private HashSet<string> _favoriteBones;

    private string? _pendingClipboardText;
    private string? _pendingImportText;
    public bool HasChanges => _editorManager.HasChanges;
    public bool IsEditorActive => _editorManager.IsEditorActive;
    public bool IsEditorPaused => _editorManager.IsEditorPaused;
    public bool IsCharacterFound => _editorManager.IsCharacterFound;

    public BoneEditorPanel(
        // TemplateFileSystemSelector templateFileSystemSelector,
        TemplateFileSystem fileSystem,
        TemplateEditorManager editorManager,
        PluginConfiguration configuration,
        GameObjectService gameObjectService,
        ActorAssignmentUi actorAssignmentUi,
        PopupSystem popupSystem,
        Logger logger)
    {
        //   _templateFileSystemSelector = templateFileSystemSelector;
        _fileSystem = fileSystem;
        _editorManager = editorManager;
        _configuration = configuration;
        _gameObjectService = gameObjectService;
        _actorAssignmentUi = actorAssignmentUi;
        _popupSystem = popupSystem;
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

        Im.Separator();

        using (var style = Im.Style.Push(ImStyleDouble.ButtonTextAlign, new Vector2(0, 0.5f)))
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
                    return;

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
                if (DrawIconButton("UndoBone", FontAwesomeIcon.Undo, "Undo", _undoStack.Count == 0))
                {
                    var state = _undoStack.Pop();
                    _redoStack.Push(_editorManager.EditorProfile.Armatures[0]
                        .GetAllBones()
                        .DistinctBy(b => b.BoneName)
                        .ToDictionary(b => b.BoneName, b => new BoneTransform(b.CustomizedTransform ?? new BoneTransform())));
                    RestoreState(state);
                }

                Im.Line.Same();
                if (DrawIconButton("RedoBone", FontAwesomeIcon.Redo, "Redo", _redoStack.Count == 0))
                {
                    var state = _redoStack.Pop();
                    _undoStack.Push(_editorManager.EditorProfile.Armatures[0]
                        .GetAllBones()
                        .DistinctBy(b => b.BoneName)
                        .ToDictionary(b => b.BoneName, b => new BoneTransform(b.CustomizedTransform ?? new BoneTransform())));
                    RestoreState(state);
                }

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
                    return;

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
                                            ChildScaling = boneData.ChildScaling,
                                            ChildScalingIndependent = boneData.ChildScalingIndependent,
                                            PropagateTranslation = boneData.PropagateTranslation,
                                            PropagateRotation = boneData.PropagateRotation,
                                            PropagateScale = boneData.PropagateScale
                                        }
                                    );
                                }
                            }
                        }
                        catch { }
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
        }

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

    private void DrawEditorConfirmationPopup()
    {
        ReadOnlySpan<byte> popupName = "Unsaved Changes##SavePopup"u8;
        const WindowFlags popupFlags = WindowFlags.NoResize | WindowFlags.NoMove | WindowFlags.NoSavedSettings;

        if (_openSavePopup)
        {
            Im.Popup.Open(popupName);
            _openSavePopup = false;
        }

        var viewportSize = Im.Window.Viewport.Size;
        var scale = ImGuiHelpers.GlobalScale;
        var style = Im.Style;
        var popupWidth = MathF.Min(
            660 * scale,
            viewportSize.X * 0.95f);
        var buttonWidth = MathF.Min(
            150 * scale,
            (popupWidth - (2 * style.WindowPadding.X) - (3 * style.ItemSpacing.X)) / 4);
        var buttonSize = new Vector2(buttonWidth, 0);
        var totalButtonsWidth = (4 * buttonWidth) + (3 * style.ItemSpacing.X);

        Im.Window.SetNextSize(new Vector2(popupWidth, 0), Condition.Always);
        Im.Window.SetNextPosition(viewportSize / 2, Condition.Always, new Vector2(0.5f));
        using var popup = Im.Popup.BeginModal(popupName, popupFlags);
        if (!popup)
            return;

        Im.Cursor.Y = Im.Cursor.Y + style.ItemSpacing.Y;
        Im.TextWrapped("You have unsaved changes in current template, what would you like to do?"u8);
        Im.Line.Spacing();
        Im.Separator();
        Im.Line.Spacing();

        var exitedEditor = false;
        Im.Cursor.X = (Im.Window.Width - totalButtonsWidth) / 2;

        if (Im.Button("Save"u8, buttonSize))
        {
            _editorManager.SaveChangesAndDisableEditor();
            exitedEditor = true;
            Im.Popup.CloseCurrent();
        }

        Im.Line.Same();
        if (Im.Button("Save as a copy"u8, buttonSize))
        {
            _editorManager.SaveChangesAndDisableEditor(true);
            exitedEditor = true;
            Im.Popup.CloseCurrent();
        }

        Im.Line.Same();
        if (Im.Button("Do not save"u8, buttonSize))
        {
            _editorManager.DisableEditor();
            exitedEditor = true;
            Im.Popup.CloseCurrent();
        }

        Im.Line.Same();
        if (Im.Button("Keep editing"u8, buttonSize))
        {
            Im.Popup.CloseCurrent();
        }

        if (exitedEditor)
        {
            _undoStack.Clear();
            _redoStack.Clear();
        }
    }

    #region UI helper functions

    private bool ResetBoneButton(BoneEditRow bone)
    {
        var output = DrawIconButton(
            bone.BoneCodeName,
            FontAwesomeIcon.Recycle,
            $"Reset '{BoneData.GetBoneDisplayName(bone.BoneCodeName)}' to default {_editingAttribute} values");

        if (output)
        {
            _editorManager.ResetBoneAttributeChanges(bone.BoneCodeName, _editingAttribute);
            if (_isMirrorModeEnabled && bone.Basis?.TwinBone != null) //todo: put it inside manager
                _editorManager.ResetBoneAttributeChanges(bone.Basis.TwinBone.BoneName, _editingAttribute);
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
            _editorManager.RevertBoneAttributeChanges(bone.BoneCodeName, _editingAttribute);
            if (_isMirrorModeEnabled && bone.Basis?.TwinBone != null) //todo: put it inside manager
                _editorManager.RevertBoneAttributeChanges(bone.Basis.TwinBone.BoneName, _editingAttribute);
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
                SaveStateForUndo(CaptureCurrentState());
                valueChanged = true;
            }

            Im.Line.Same();
            isFavorite = FavoriteButton(bone);

            // X
            NextAxisCell(xEdited ? AxisXEditedCellColor : AxisXCellColor);
            float tempX = newVector.X;
            if (Im.Item.Activated)
            {
                _initialX = tempX;
                if (_pendingUndoSnapshot == null)
                    _pendingUndoSnapshot = CaptureCurrentState();
            }
            if (SingleValueSlider($"##{codename}-X", ref tempX))
            {
                newVector.X = tempX;
                valueChanged = true;
            }
            if (Im.Item.DeactivatedAfterEdit)
            {
                if (_pendingUndoSnapshot != null && _initialX != newVector.X)
                {
                    SaveStateForUndo(_pendingUndoSnapshot);
                    _pendingUndoSnapshot = null;
                }
            }

            // Y
            NextAxisCell(yEdited ? AxisYEditedCellColor : AxisYCellColor);
            float tempY = newVector.Y;
            if (Im.Item.Activated)
            {
                _initialY = tempY;
                if (_pendingUndoSnapshot == null)
                    _pendingUndoSnapshot = CaptureCurrentState();
            }
            if (SingleValueSlider($"##{codename}-Y", ref tempY))
            {
                newVector.Y = tempY;
                valueChanged = true;
            }
            if (Im.Item.DeactivatedAfterEdit)
            {
                if (_pendingUndoSnapshot != null && _initialY != newVector.Y)
                {
                    SaveStateForUndo(_pendingUndoSnapshot);
                    _pendingUndoSnapshot = null;
                }
            }

            // Z
            NextAxisCell(zEdited ? AxisZEditedCellColor : AxisZCellColor);
            float tempZ = newVector.Z;
            if (Im.Item.Activated)
            {
                _initialZ = tempZ;
                if (_pendingUndoSnapshot == null)
                    _pendingUndoSnapshot = CaptureCurrentState();
            }
            if (SingleValueSlider($"##{codename}-Z", ref tempZ))
            {
                newVector.Z = tempZ;
                valueChanged = true;
            }
            if (Im.Item.DeactivatedAfterEdit)
            {
                if (_pendingUndoSnapshot != null && _initialZ != newVector.Z)
                {
                    SaveStateForUndo(_pendingUndoSnapshot);
                    _pendingUndoSnapshot = null;
                }
            }

            if (_editingAttribute == BoneAttribute.Scale)
            {
                Im.Table.NextColumn();
                if (rowEdited)
                    Im.Table.SetBackgroundColor(TableBackgroundTarget.Cell, WithAlpha(ImGuiColor.CheckMark, 0.12f));

                Vector3 tempScale = newVector;
                if (Im.Item.Activated)
                {
                    _initialScale = tempScale;
                    if (_pendingUndoSnapshot == null)
                        _pendingUndoSnapshot = CaptureCurrentState();
                }
                if (FullBoneSlider($"##{codename}-All", ref tempScale))
                {
                    newVector = tempScale;
                    valueChanged = true;
                }
                if (Im.Item.DeactivatedAfterEdit)
                {
                    if (_pendingUndoSnapshot != null && _initialScale != newVector)
                    {
                        SaveStateForUndo(_pendingUndoSnapshot);
                        _pendingUndoSnapshot = null;
                    }
                }
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

        Im.Table.NextRow();

        if (_editingAttribute == BoneAttribute.Scale && propagationEnabled)
        {
            RenderChildScalingRow(bone, transform);
        }
    }

    private void RenderChildScalingRow(BoneEditRow bone, BoneTransform transform)
    {
        var codename = bone.BoneCodeName;
        var displayName = bone.BoneDisplayName;

        bool isChildScaleIndependent = transform.ChildScalingIndependent;
        bool childScaleChanged = false;
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
                    SaveStateForUndo(CaptureCurrentState());

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
            if (Im.Item.Activated)
            {
                _initialChildX = tempChildX;
                if (_pendingUndoSnapshot == null)
                    _pendingUndoSnapshot = CaptureCurrentState();
            }
            if (SingleValueSlider($"##child-{codename}-X", ref tempChildX))
            {
                childScale.X = tempChildX;
                childScaleChanged = true;
            }
            if (Im.Item.DeactivatedAfterEdit)
            {
                if (_pendingUndoSnapshot != null && _initialChildX != childScale.X)
                {
                    SaveStateForUndo(_pendingUndoSnapshot);
                    _pendingUndoSnapshot = null;
                }
            }

            NextAxisCell(yEdited ? AxisYEditedCellColor : AxisYCellColor);
            float tempChildY = childScale.Y;
            if (Im.Item.Activated)
            {
                _initialChildY = tempChildY;
                if (_pendingUndoSnapshot == null)
                    _pendingUndoSnapshot = CaptureCurrentState();
            }
            if (SingleValueSlider($"##child-{codename}-Y", ref tempChildY))
            {
                childScale.Y = tempChildY;
                childScaleChanged = true;
            }
            if (Im.Item.DeactivatedAfterEdit)
            {
                if (_pendingUndoSnapshot != null && _initialChildY != childScale.Y)
                {
                    SaveStateForUndo(_pendingUndoSnapshot);
                    _pendingUndoSnapshot = null;
                }
            }

            NextAxisCell(zEdited ? AxisZEditedCellColor : AxisZCellColor);
            float tempChildZ = childScale.Z;
            if (Im.Item.Activated)
            {
                _initialChildZ = tempChildZ;
                if (_pendingUndoSnapshot == null)
                    _pendingUndoSnapshot = CaptureCurrentState();
            }
            if (SingleValueSlider($"##child-{codename}-Z", ref tempChildZ))
            {
                childScale.Z = tempChildZ;
                childScaleChanged = true;
            }
            if (Im.Item.DeactivatedAfterEdit)
            {
                if (_pendingUndoSnapshot != null && _initialChildZ != childScale.Z)
                {
                    SaveStateForUndo(_pendingUndoSnapshot);
                    _pendingUndoSnapshot = null;
                }
            }

            Im.Table.NextColumn();
            if (rowEdited)
                Im.Table.SetBackgroundColor(TableBackgroundTarget.Cell, WithAlpha(ImGuiColor.CheckMark, 0.12f));

            if (Im.Item.Activated)
            {
                _initialChildScale = childScale;
                if (_pendingUndoSnapshot == null)
                    _pendingUndoSnapshot = CaptureCurrentState();
            }
            if (FullBoneSlider($"##child-{codename}-All", ref childScale))
                childScaleChanged = true;
            if (Im.Item.DeactivatedAfterEdit)
            {
                if (_pendingUndoSnapshot != null && _initialChildScale != childScale)
                {
                    SaveStateForUndo(_pendingUndoSnapshot);
                    _pendingUndoSnapshot = null;
                }
            }
        }

        Im.Table.NextColumn();
        CtrlHelper.StaticLabel($"{displayName} - Child Bones", CtrlHelper.TextAlignment.Left, "Scale applied to child bones");

        if (childScaleChanged)
        {
            transform.ChildScaling = childScale;
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

        Im.Table.NextRow();
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
