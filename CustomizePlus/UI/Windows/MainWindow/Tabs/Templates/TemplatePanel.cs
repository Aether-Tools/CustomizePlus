using Dalamud.Interface;
using ImGuiNET;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Raii;
using System;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using CustomizePlus.Core.Data;
using CustomizePlus.Templates;
using CustomizePlus.Configuration.Data;
using CustomizePlus.Core.Helpers;
using CustomizePlus.Templates.Data;
using OtterGui.Log;
using CustomizePlus.Templates.Events;

namespace CustomizePlus.UI.Windows.MainWindow.Tabs.Templates;

public class TemplatePanel : IDisposable
{
    private readonly TemplateFileSystemSelector _selector;
    private readonly TemplateManager _manager;
    private readonly BoneEditorPanel _boneEditor;
    private readonly PluginConfiguration _configuration;
    private readonly MessageService _messageService;
    private readonly PopupSystem _popupSystem;
    private readonly Logger _logger;

    private readonly TemplateEditorEvent _editorEvent;

    private string? _newName;
    private Template? _changedTemplate;

    /// <summary>
    /// Set to true if we received OnEditorEvent EditorEnableRequested and waiting for selector value to be changed.
    /// </summary>
    private bool _isEditorEnablePending = false;

    private string SelectionName
        => _selector.Selected == null ? "No Selection" : _selector.IncognitoMode ? _selector.Selected.Incognito : _selector.Selected.Name.Text;

    public TemplatePanel(
        TemplateFileSystemSelector selector,
        TemplateManager manager,
        BoneEditorPanel boneEditor,
        PluginConfiguration configuration,
        MessageService messageService,
        PopupSystem popupSystem,
        Logger logger,
        TemplateEditorEvent editorEvent)
    {
        _selector = selector;
        _manager = manager;
        _boneEditor = boneEditor;
        _configuration = configuration;
        _messageService = messageService;
        _popupSystem = popupSystem;
        _logger = logger;

        _editorEvent = editorEvent;

        _editorEvent.Subscribe(OnEditorEvent, TemplateEditorEvent.Priority.TemplatePanel);

        _selector.SelectionChanged += SelectorSelectionChanged;
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

    public void Dispose()
    {
        _editorEvent.Unsubscribe(OnEditorEvent);
    }

    private HeaderDrawer.Button LockButton()
        => _selector.Selected == null
            ? HeaderDrawer.Button.Invisible
            : _selector.Selected.IsWriteProtected
                ? new HeaderDrawer.Button
                {
                    Description = "Make this template editable.",
                    Icon = FontAwesomeIcon.Lock,
                    OnClick = () => _manager.SetWriteProtection(_selector.Selected!, false),
                    Disabled = _boneEditor.IsEditorActive
                }
                : new HeaderDrawer.Button
                {
                    Description = "Write-protect this template.",
                    Icon = FontAwesomeIcon.LockOpen,
                    OnClick = () => _manager.SetWriteProtection(_selector.Selected!, true),
                    Disabled = _boneEditor.IsEditorActive
                };

    private HeaderDrawer.Button ExportToClipboardButton()
        => new()
        {
            Description = "Copy the current template to your clipboard.",
            Icon = FontAwesomeIcon.Copy,
            OnClick = ExportToClipboard,
            Visible = _selector.Selected != null,
            Disabled = _boneEditor.IsEditorActive
        };

    private void DrawHeader()
        => HeaderDrawer.Draw(SelectionName, 0, ImGui.GetColorU32(ImGuiCol.FrameBg),
            1, ExportToClipboardButton(), LockButton(),
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
        ImGui.TextUnformatted("Currently Selected Templates");
        ImGui.Separator();
        using var table = ImRaii.Table("templates", 3, ImGuiTableFlags.RowBg);
        ImGui.TableSetupColumn("btn", ImGuiTableColumnFlags.WidthFixed, sizeType);
        ImGui.TableSetupColumn("name", ImGuiTableColumnFlags.WidthFixed, sizeMods);
        ImGui.TableSetupColumn("path", ImGuiTableColumnFlags.WidthFixed, sizeFolders);

        var i = 0;
        foreach (var (fullName, path) in _selector.SelectedPaths.Select(p => (p.FullName(), p))
                     .OrderBy(p => p.Item1, StringComparer.OrdinalIgnoreCase))
        {
            using var id = ImRaii.PushId(i++);
            ImGui.TableNextColumn();
            var icon = (path is TemplateFileSystem.Leaf ? FontAwesomeIcon.FileCircleMinus : FontAwesomeIcon.FolderMinus).ToIconString();
            if (ImGuiUtil.DrawDisabledButton(icon, new Vector2(sizeType), "Remove from selection.", false, true))
                _selector.RemovePathFromMultiSelection(path);

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(path is TemplateFileSystem.Leaf l ? _selector.IncognitoMode ? l.Value.Incognito : l.Value.Name.Text : string.Empty);

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

        using (var disabled = ImRaii.Disabled(_selector.Selected?.IsWriteProtected ?? true))
        {
            DrawBasicSettings();
            DrawEditorToggle();
        }

        _boneEditor.Draw();
    }

    private void DrawEditorToggle()
    {
        (bool isEditorAllowed, bool isEditorActive) = CanToggleEditor();

        if (ImGuiUtil.DrawDisabledButton($"{(_boneEditor.IsEditorActive ? "Finish" : "Start")} bone editing", Vector2.Zero,
            "Toggle the bone editor for this template", !isEditorAllowed))
        {
            if (!isEditorActive)
                _boneEditor.EnableEditor(_selector.Selected!);
            else
                _boneEditor.DisableEditor();
        }
    }

    private (bool isEditorAllowed, bool isEditorActive) CanToggleEditor()
    {
        return ((!_selector.Selected?.IsWriteProtected ?? false) || _configuration.PluginEnabled, _boneEditor.IsEditorActive);
    }

    private void DrawBasicSettings()
    {
        using (var style = ImRaii.PushStyle(ImGuiStyleVar.ButtonTextAlign, new Vector2(0, 0.5f)))
        {
            using (var table = ImRaii.Table("BasicSettings", 2))
            {
                ImGui.TableSetupColumn("BasicCol1", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("lorem ipsum dolor").X);
                ImGui.TableSetupColumn("BasicCol2", ImGuiTableColumnFlags.WidthStretch);

                ImGuiUtil.DrawFrameColumn("Template Name");
                ImGui.TableNextColumn();
                var width = new Vector2(ImGui.GetContentRegionAvail().X, 0);
                var name = _newName ?? _selector.Selected!.Name;
                ImGui.SetNextItemWidth(width.X);

                if (!_selector.IncognitoMode)
                {
                    if (ImGui.InputText("##Name", ref name, 128))
                    {
                        _newName = name;
                        _changedTemplate = _selector.Selected;
                    }

                    if (ImGui.IsItemDeactivatedAfterEdit() && _changedTemplate != null)
                    {
                        _manager.Rename(_changedTemplate, name);
                        _newName = null;
                        _changedTemplate = null;
                    }
                }
                else
                    ImGui.TextUnformatted(_selector.Selected!.Incognito);
            }
        }
    }

    private void ExportToClipboard()
    {
        try
        {
            Clipboard.SetText(Base64Helper.ExportTemplateToBase64(_selector.Selected!));
            _popupSystem.ShowPopup(PopupSystem.Messages.ClipboardDataNotLongTerm);
        }
        catch (Exception ex)
        {
            _logger.Error($"Could not copy data from template {_selector.Selected!.UniqueId} to clipboard: {ex}");
            _popupSystem.ShowPopup(PopupSystem.Messages.ActionError);
        }
    }


    private void SelectorSelectionChanged(Template? oldSelection, Template? newSelection, in TemplateFileSystemSelector.TemplateState state)
    {
        if (!_isEditorEnablePending)
            return;

        _isEditorEnablePending = false;

        _boneEditor.EnableEditor(_selector.Selected!);
    }

    private void OnEditorEvent(TemplateEditorEvent.Type type, Template? template)
    {
        if (type != TemplateEditorEvent.Type.EditorEnableRequestedStage2)
            return;

        if(template == null)
            return;

        (bool isEditorAllowed, bool isEditorActive) = CanToggleEditor();

        if (!isEditorAllowed || isEditorActive)
            return;

        if(_selector.Selected != template)
        {
            _selector.SelectByValue(template);

            _isEditorEnablePending = true;
        }
        else
            _boneEditor.EnableEditor(_selector.Selected!);
    }
}
