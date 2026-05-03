using CustomizePlus.Configuration.Data;
using CustomizePlus.Configuration.Services;
using CustomizePlus.Templates;
using Dalamud.Interface.ImGuiNotification;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using static FFXIVClientStructs.FFXIV.Client.Game.Character.ActionEffectHandler;
using static FFXIVClientStructs.FFXIV.Client.UI.AddonItemDetailCompare;

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
        using(var disabled = Im.Disabled(_templateEditorManager.IsEditorActive))
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

    /*    public override ReadOnlySpan<byte> Label
        => "TemplatesTab"u8;

    public void Draw()
        => Draw(new TwoPanelWidth(_configuration.UISettings.CurrentTemplateSelectorWidth, ScalingMode.Absolute));

    protected override void SetWidth(float width, ScalingMode mode)
    {
        var adaptedSize = MathF.Round(width / Im.Style.GlobalScale);
        if (Math.Abs(adaptedSize - _configuration.UISettings.CurrentTemplateSelectorWidth) < 0.1f)
            return;

        _configuration.UISettings.CurrentTemplateSelectorWidth = adaptedSize;
        _configuration.Save();
    }

    protected override void DrawPopups()
        => _selector.DrawSelectorPopups();

    protected override float MinimumWidth
        => Im.ContentRegion.Available.X * _configuration.UISettings.TemplateSelectorMinimumScale;

    protected override float MaximumWidth
        => MathF.Max(MinimumWidth, MathF.Min(
            Im.ContentRegion.Available.X * _configuration.UISettings.TemplateSelectorMaximumScale,
            Im.ContentRegion.Available.X - 470 * Im.Style.GlobalScale));*/
}
