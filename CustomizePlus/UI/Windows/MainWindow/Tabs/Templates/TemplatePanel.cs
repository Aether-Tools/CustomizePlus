using CustomizePlus.Configuration.Data;
using CustomizePlus.Configuration.Services;
using CustomizePlus.Core.Helpers;
using CustomizePlus.Templates;
using CustomizePlus.Templates.Data;
using CustomizePlus.Templates.Events;
using Dalamud.Interface;
using Dalamud.Interface.Utility;

namespace CustomizePlus.UI.Windows.MainWindow.Tabs.Templates;

public class TemplatePanel : IPanel, IDisposable
{
    //private readonly TemplateFileSystemSelector _selector;
    private readonly TemplateFileSystem _fileSystem;
    private readonly TemplateManager _manager;
    private readonly BoneEditorPanel _boneEditor;
    private readonly PluginConfiguration _configuration;
    private readonly MessageService _messageService;
    private readonly PopupSystem _popupSystem;
    private readonly Logger _logger;
    private readonly MultiTemplatePanel _multiTemplatePanel;

    private readonly TemplateEditorEvent _editorEvent;

    private string? _newName;
    private Template? _changedTemplate;

    /// <summary>
    /// Set to true if we received OnEditorEvent EditorEnableRequested and waiting for selector value to be changed.
    /// </summary>
    private bool _isEditorEnablePending = false;

  /*  private string SelectionName
        => _selector.SelectedPaths.Count > 1
            ? "Multiple Templates"
            : _selector.Selected == null
                ? "No Selection"
                : _selector.IncognitoMode
                    ? _selector.Selected.Incognito
                    : _selector.Selected.Name;*/

    public ReadOnlySpan<byte> Id
        => "TemplatePanel"u8;

    public TemplatePanel(
        TemplateFileSystem fileSystem,
        TemplateManager manager,
        BoneEditorPanel boneEditor,
        PluginConfiguration configuration,
        MessageService messageService,
        PopupSystem popupSystem,
        Logger logger,
        TemplateEditorEvent editorEvent,
        MultiTemplatePanel multiTemplatePanel)
    {
        _fileSystem = fileSystem;
        _manager = manager;
        _boneEditor = boneEditor;
        _configuration = configuration;
        _messageService = messageService;
        _popupSystem = popupSystem;
        _logger = logger;
        _multiTemplatePanel = multiTemplatePanel;

        _editorEvent = editorEvent;

        _editorEvent.Subscribe(OnEditorEvent, TemplateEditorEvent.Priority.TemplatePanel);

        //  _selector.SelectionChanged += SelectorSelectionChanged; todo
    }

    private Template Selection
        => (Template)_fileSystem.Selection.Selection!.Value;

    public void Draw()
    {
        if (_fileSystem.Selection.OrderedNodes.Count > 1) //todo
        {
            _multiTemplatePanel.Draw();
            return;
        }

        DrawPanel();

        if (_fileSystem.Selection.Selection is null || Selection.IsWriteProtected)
            return;
    }

    public void Dispose()
    {
        _editorEvent.Unsubscribe(OnEditorEvent);
    }
    private void DrawPanel()
    {
        if (_fileSystem.Selection.Selection is null)
            return;

        using (var disabled = Im.Disabled(Selection.IsWriteProtected))
        {
            DrawBasicSettings();
        }

        _boneEditor.Draw();
    }

    private (bool isEditorAllowed, bool isEditorActive) CanToggleEditor()
    {
        return ((_fileSystem.Selection.Selection is not null ? !Selection.IsWriteProtected : false) || _configuration.PluginEnabled, _boneEditor.IsEditorActive);
    }

    private void DrawBasicSettings()
    {
        using (Im.Group())
        {
            UiHelpers.DrawPropertyLabel("Template Name");
            Im.Line.Same();
            DrawTemplateNameControl();

            UiHelpers.DrawPropertyLabel("Bone Editor");
            Im.Line.Same();
            DrawEditorToggle();
        }
    }

    private void DrawTemplateNameControl()
    {
        var name = _newName ?? Selection.Name;
        Im.Item.SetNextWidthFull();

        if (!_configuration.UISettings.IncognitoMode)
        {
            if (Im.Input.Text("##Name"u8, ref name, maxLength: 128))
            {
                _newName = name;
                _changedTemplate = Selection;
            }

            if (Im.Item.DeactivatedAfterEdit && _changedTemplate != null)
            {
                _manager.Rename(_changedTemplate, name);
                _newName = null;
                _changedTemplate = null;
            }
        }
        else
        {
            Im.Cursor.FrameAlign();
            Im.Text(Selection.Incognito);
        }
    }

    private void DrawEditorToggle()
    {
        (bool isEditorAllowed, bool isEditorActive) = CanToggleEditor();

        var width = MathF.Min(180 * ImGuiHelpers.GlobalScale, Im.ContentRegion.Available.X);
        if (UiHelpers.DrawDisabledButton($"{(_boneEditor.IsEditorActive ? "Finish" : "Start")} bone editing", new Vector2(width, 0),
            "Toggle the bone editor for this template", !isEditorAllowed))
        {
            if (!isEditorActive)
                _boneEditor.EnableEditor(Selection);
            else
                _boneEditor.DisableEditor();
        }
    }


  /*  private void SelectorSelectionChanged(Template? oldSelection, Template? newSelection, in TemplateFileSystemSelector.TemplateState state)
    {
        if (!_isEditorEnablePending)
            return;

        _isEditorEnablePending = false;

        _boneEditor.EnableEditor(Selection);
    }*/

    private void OnEditorEvent(in TemplateEditorEvent.Arguments args)
    {
        var (type, template) = args;
        if (type != TemplateEditorEvent.Type.EditorEnableRequestedStage2)
            return;

        if(template == null)
            return;

        (bool isEditorAllowed, bool isEditorActive) = CanToggleEditor();

        if (!isEditorAllowed || isEditorActive)
            return;

        if(Selection != template)
        {
            //_selector.SelectByValue(template);
            //todo: change selection

            _isEditorEnablePending = true;
        }
        else
            _boneEditor.EnableEditor(Selection);
    }
}
