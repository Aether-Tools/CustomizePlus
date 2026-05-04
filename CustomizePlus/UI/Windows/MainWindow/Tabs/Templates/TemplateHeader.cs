using CustomizePlus.Configuration.Data;
using CustomizePlus.Templates;
using CustomizePlus.Templates.Data;
using CustomizePlus.Templates.Events;
using CustomizePlus.UI.Windows.MainWindow.Tabs.Templates.Controls;
using Penumbra.GameData.Interop;

namespace CustomizePlus.UI.Windows.MainWindow.Tabs.Templates;

public sealed class TemplateHeader : SplitButtonHeader, IDisposable
{
    private readonly TemplateFileSystem _fileSystem;
    private readonly TemplateChanged _templateChanged;
    private readonly PluginConfiguration _config;

    private StringU8 _header = new("No Selection"u8);
    private StringU8 _incognito = new("No Selection"u8);

    public TemplateHeader(
        TemplateFileSystem fileSystem,
        IncognitoButton incognito,
        TemplateChanged templateChanged,
        PluginConfiguration config,
        TemplateManager manager,
        ActorObjectManager objects,
        PopupSystem popupSystem)
    {
        _fileSystem = fileSystem;
        _templateChanged = templateChanged;
        _config = config;

        LeftButtons.AddButton(new ExportToClipboardButton(fileSystem, popupSystem), 100);

        RightButtons.AddButton(incognito, 50);
        RightButtons.AddButton(new LockedButton(fileSystem, manager), 100);
        _fileSystem.Selection.Changed += OnSelectionChanged;
        OnSelectionChanged();
        templateChanged.Subscribe(OnTemplateChanged, TemplateChanged.Priority.DesignHeader);
    }

    private void OnTemplateChanged(in TemplateChanged.Arguments arguments)
    {
        if (arguments.Type is not TemplateChanged.Type.Renamed)
            return;

        if (arguments.Template != _fileSystem.Selection.Selection?.Value)
            return;

        _header = new StringU8(arguments.Template.Name);
    }

    private void OnSelectionChanged()
    {
        if (_fileSystem.Selection.Selection?.GetValue<Template>() is { } selection)
        {
            _header = new StringU8(selection.Name);
            _incognito = new StringU8(selection.Incognito);
        }
        else if (_fileSystem.Selection.OrderedNodes.Count > 0)
        {
            _header = new StringU8($"{_fileSystem.Selection.OrderedNodes.Count} Objects Selected");
            _incognito = _header;
        }
        else
        {
            _header = new StringU8("No Selection"u8);
            _incognito = _header;
        }
    }

    public override void Draw(Vector2 size)
    {
        var color = ColorId.HeaderButtons.Value();
        using var _ = ImGuiColor.Text.Push(color).Push(ImGuiColor.Border, color);
        base.Draw(size with { Y = Im.Style.FrameHeight });
    }

    public override ReadOnlySpan<byte> Text
        => _config.UISettings.IncognitoMode ? _incognito : _header;

    private sealed class LockedButton(TemplateFileSystem fileSystem, TemplateManager manager) : BaseIconButton<AwesomeIcon>
    {
        public override bool IsVisible
            => fileSystem.Selection.Selection is not null;

        public override AwesomeIcon Icon
            => ((Template)fileSystem.Selection.Selection!.Value).IsWriteProtected ? LunaStyle.LockedIcon : LunaStyle.UnlockedIcon;

        public override bool HasTooltip
            => true;

        public override void DrawTooltip()
            => Im.Text(((Template)fileSystem.Selection.Selection!.Value).IsWriteProtected
                ? "Make this template editable."u8
                : "Write-protect this template."u8);

        public override void OnClick()
            => manager.SetWriteProtection((Template)fileSystem.Selection.Selection!.Value,
                !((Template)fileSystem.Selection.Selection!.Value).IsWriteProtected);
    }

    public void Dispose()
    {
        _fileSystem.Selection.Changed -= OnSelectionChanged;
        _templateChanged.Unsubscribe(OnTemplateChanged);
    }
}

