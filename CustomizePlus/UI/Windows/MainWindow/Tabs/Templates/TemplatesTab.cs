using CustomizePlus.Configuration.Data;
using CustomizePlus.Templates;

namespace CustomizePlus.UI.Windows.MainWindow.Tabs.Templates;

public class TemplatesTab : TwoPanelLayout, ITab<MainTabType>
{
    private readonly PluginConfiguration _configuration;
    private readonly TemplateEditorManager _templateEditorManager;

    public TemplatesTab(
        TemplateFileSystemDrawer drawer,
        TemplatePanel panel,
        TemplateHeader header,
        PluginConfiguration configuration,
        TemplateEditorManager templateEditorManager)
    {
        _configuration = configuration;
        _templateEditorManager = templateEditorManager;

        LeftHeader = drawer.Header;
        LeftFooter = drawer.Footer;
        LeftPanel = drawer;

        RightHeader = header;
        RightFooter = EmptyHeaderFooter.Instance;
        RightPanel = panel;
    }

    public override ReadOnlySpan<byte> Label
        => "Templates"u8;

    public MainTabType Identifier
        => MainTabType.Templates;

    protected override void DrawLeftGroup(in TwoPanelWidth width)
    {
        using (var disabled = Im.Disabled(_templateEditorManager.IsEditorActive))
            base.DrawLeftGroup(width);
    }

    protected override float MinimumWidth
        => LeftFooter.MinimumWidth;

    protected override float MaximumWidth
        => Im.Window.Width - 500 * Im.Style.GlobalScale;

    protected override void SetWidth(float width, ScalingMode mode)
        => _configuration.LunaUiConfiguration.TemplatesTabScale = new TwoPanelWidth(width, mode);

    public void DrawContent()
        => Draw(_configuration.LunaUiConfiguration.TemplatesTabScale);
}
