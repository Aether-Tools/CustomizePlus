using CustomizePlus.Configuration.Data;
using CustomizePlus.Templates;
using CustomizePlus.Templates.Data;
using CustomizePlus.Templates.Events;
using Dalamud.Interface.Utility;

namespace CustomizePlus.UI.Windows.MainWindow.Tabs.Templates;

public class TemplatePanel : IPanel, IDisposable
{
    private readonly TemplateFileSystem _fileSystem;
    private readonly TemplateManager _manager;
    private readonly BoneEditorPanel _boneEditor;
    private readonly PluginConfiguration _configuration;
    private readonly MultiTemplatePanel _multiTemplatePanel;
    private readonly FrameworkManager _frameworkManager;

    private readonly TemplateEditorEvent _editorEvent;

    private string? _newName;
    private Template? _changedTemplate;

    /// <summary>
    /// Set to true if we received OnEditorEvent EditorEnableRequested and waiting for selector value to be changed.
    /// </summary>
    private bool _isEditorEnablePending = false;

    public ReadOnlySpan<byte> Id
        => "TemplatePanel"u8;

    public TemplatePanel(
        TemplateFileSystem fileSystem,
        TemplateManager manager,
        BoneEditorPanel boneEditor,
        PluginConfiguration configuration,
        TemplateEditorEvent editorEvent,
        MultiTemplatePanel multiTemplatePanel,
        FrameworkManager frameworkManager)
    {
        _fileSystem = fileSystem;
        _manager = manager;
        _boneEditor = boneEditor;
        _configuration = configuration;
        _multiTemplatePanel = multiTemplatePanel;
        _frameworkManager = frameworkManager;

        _editorEvent = editorEvent;

        _editorEvent.Subscribe(OnEditorEvent, TemplateEditorEvent.Priority.TemplatePanel);

        fileSystem.Selection.Changed += SelectorSelectionChanged;
    }

    private Template Selection
        => (Template)_fileSystem.Selection.Selection!.Value;

    public void Draw()
    {
        if (_fileSystem.Selection.OrderedNodes.Count > 1)
        {
            _multiTemplatePanel.Draw();
            return;
        }

        DrawPanel();
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

    private void SelectorSelectionChanged()
    {
        if (!_isEditorEnablePending)
            return;

        _isEditorEnablePending = false;

        //Ugly hack because selection isn't yet changed at the time this is executed.
        //I don't like it, but I'm dealing with rewriting the entire UI right now, this isn't a priority.
        _frameworkManager.RegisterDelayed("editorenable", () => _boneEditor.EnableEditor(Selection), TimeSpan.FromMilliseconds(500));
    }

    private void OnEditorEvent(in TemplateEditorEvent.Arguments args)
    {
        var (type, template) = args;
        if (type != TemplateEditorEvent.Type.EditorEnableRequestedStage2)
            return;

        if (template == null)
            return;

        (bool isEditorAllowed, bool isEditorActive) = CanToggleEditor();

        if (!isEditorAllowed || isEditorActive)
            return;

        if (_fileSystem.Selection.Selection == null || Selection != template)
        {
            _isEditorEnablePending = true;

            _fileSystem.Selection.Select(template.Node!, true);
        }
        else
            _boneEditor.EnableEditor(Selection);
    }
}
