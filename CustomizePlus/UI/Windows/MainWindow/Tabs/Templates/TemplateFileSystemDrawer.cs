using CustomizePlus.Anamnesis;
using CustomizePlus.Configuration.Data;
using CustomizePlus.Core.Helpers;
using CustomizePlus.Core.Services;
using CustomizePlus.Profiles.Events;
using CustomizePlus.Templates;
using CustomizePlus.Templates.Events;
using CustomizePlus.UI.Windows.MainWindow.Tabs.Templates.Controls;
using Dalamud.Interface.ImGuiFileDialog;

namespace CustomizePlus.UI.Windows.MainWindow.Tabs.Templates;

public sealed class TemplateFileSystemDrawer : FileSystemDrawer<TemplateFileSystemCache.TemplateData>, IDisposable
{
    internal readonly TemplateChanged TemplateChanged;
    internal readonly ProfileChanged ProfileChanged;
    internal readonly TemplateManager TemplateManager;
    internal readonly PluginConfiguration Configuration;
    internal readonly ColorsService ColorsService;

    private readonly FileDialogManager fileDialogManager = new();

    public TemplateFileSystemDrawer(MessageService messager,
        TemplateFileSystem fileSystem,
        TemplateChanged templateChanged,
        ProfileChanged profileChanged,
        TemplateManager templateManager,
        TemplateEditorManager editorManager,
        PopupSystem popupSystem,
        PoseFileBoneLoader poseFileBoneLoader,
        PluginConfiguration configuration,
        ColorsService colorsService)
        : base(messager, fileSystem, new TemplateFilter(configuration))
    {
        TemplateChanged = templateChanged;
        ProfileChanged = profileChanged;
        TemplateManager = templateManager;
        Configuration = configuration;
        ColorsService = colorsService;

        Footer.Buttons.AddButton(new NewTemplateButton(templateManager, editorManager, popupSystem), 1000);
        Footer.Buttons.AddButton(new AnamnesisImportButton(templateManager, editorManager, popupSystem, messager, poseFileBoneLoader, fileDialogManager), 800);
        Footer.Buttons.AddButton(new ImportTemplateButton(templateManager, editorManager, popupSystem), 800);
        Footer.Buttons.AddButton(new DuplicateTemplateButton(fileSystem, templateManager, editorManager, popupSystem), 700);
        Footer.Buttons.AddButton(new DeleteTemplateButton(fileSystem, templateManager, editorManager, popupSystem, configuration), -100);

        DataContext.AddButton(new RenameTemplateInput(this), -1001);
        DataContext.AddButton(new MoveTemplateInput(this), -1000);

        SortMode = configuration.SortMode;
    }

    public void Dispose()
    {

    }

    public override void Draw()
    {
        base.Draw();

        fileDialogManager.Draw();
    }

    public override Vector4 ExpandedFolderColor
        => ColorId.FolderExpanded.Value().ToVector();

    public override Vector4 CollapsedFolderColor
        => ColorId.FolderCollapsed.Value().ToVector();

    public override Vector4 FolderLineColor
        => ColorId.FolderLine.Value().ToVector();

    public override IEnumerable<ISortMode> ValidSortModes
        => ISortMode.Valid.Values;

    public override ReadOnlySpan<byte> Id
        => "Templates"u8;

    protected override FileSystemCache<TemplateFileSystemCache.TemplateData> CreateCache()
        => new TemplateFileSystemCache(this);
}
