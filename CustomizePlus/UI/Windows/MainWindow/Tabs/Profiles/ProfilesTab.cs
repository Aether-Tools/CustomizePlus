using CustomizePlus.Configuration.Data;
using CustomizePlus.Templates;

namespace CustomizePlus.UI.Windows.MainWindow.Tabs.Profiles;

public class ProfilesTab : TwoPanelLayout, ITab<MainTabType>
{
    private readonly PluginConfiguration _configuration;
    private readonly TemplateEditorManager _templateEditorManager;

    public ProfilesTab(
        ProfileFileSystemDrawer drawer,
        ProfilePanel panel,
        ProfileHeader header,
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
        => "Profiles"u8;

    public MainTabType Identifier
        => MainTabType.Profiles;

    protected override float MinimumWidth
        => LeftFooter.MinimumWidth;

    protected override float MaximumWidth
        => Im.Window.Width - 500 * Im.Style.GlobalScale;

    protected override void SetWidth(float width, ScalingMode mode)
        => _configuration.LunaUiConfiguration.ProfilesTabScale = new TwoPanelWidth(width, mode);

    public void DrawContent()
        => Draw(_configuration.LunaUiConfiguration.ProfilesTabScale);
}
