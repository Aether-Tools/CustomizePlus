using CustomizePlus.Profiles.Events;
using CustomizePlus.Templates.Data;
using CustomizePlus.Templates.Events;

namespace CustomizePlus.UI.Windows.MainWindow.Tabs.Templates;

public sealed class TemplateFileSystemCache : FileSystemCache<TemplateFileSystemCache.TemplateData>
{
    public TemplateFileSystemCache(TemplateFileSystemDrawer parent)
    : base(parent)
    {
        parent.TemplateChanged.Subscribe(OnTemplateChanged, TemplateChanged.Priority.TemplateFileSystemSelector);
        parent.ProfileChanged.Subscribe(OnProfileChanged, ProfileChanged.Priority.TemplateFileSystemSelector);
    }

    private void OnColorChanged()
    {
        foreach (var node in AllNodes.Values)
            node.Dirty = true;
    }

    private void OnTemplateChanged(in TemplateChanged.Arguments arguments)
    {
        switch (arguments.Type)
        {
            case TemplateChanged.Type.Created:
            case TemplateChanged.Type.Deleted:
            case TemplateChanged.Type.Renamed:
            case TemplateChanged.Type.ReloadedAll:
                VisibleDirty = true;
                break;
        }

        if (arguments.Template?.Node is { } node && AllNodes.TryGetValue(node, out var cache))
            cache.Dirty = true;
    }

    private void OnProfileChanged(in ProfileChanged.Arguments arguments)
    {
        switch (arguments.Type)
        {
            case ProfileChanged.Type.Created:
            case ProfileChanged.Type.Deleted:
            case ProfileChanged.Type.AddedTemplate:
            case ProfileChanged.Type.ChangedTemplate:
            case ProfileChanged.Type.RemovedTemplate:
            case ProfileChanged.Type.ReloadedAll:
                VisibleDirty = true;
                break;
        }

        if (arguments.Profile == null) //not sure this will ever be the case, just a failsafe
        {
            OnColorChanged();
            return;
        }

        if (arguments.Type == ProfileChanged.Type.AddedTemplate || arguments.Type == ProfileChanged.Type.RemovedTemplate)
        {
            var template = (Template)arguments.Data!;

            if (template.Node is { } node && AllNodes.TryGetValue(node, out var cache))
                cache.Dirty = true;
        }

        if (arguments.Type == ProfileChanged.Type.ChangedTemplate)
        {
            var data = ((int idx, Template newTemplate, Template oldTemplate))arguments.Data;

            if (data.newTemplate.Node is { } node && AllNodes.TryGetValue(node, out var cache))
                cache.Dirty = true;

            if (data.oldTemplate.Node is { } node2 && AllNodes.TryGetValue(node2, out var cache2))
                cache2.Dirty = true;
        }
    }

    private new TemplateFileSystemDrawer Parent
        => (TemplateFileSystemDrawer)base.Parent;

    public override void Update()
    {
        if (ColorsDirty)
        {
            CollapsedFolderColor = ColorId.FolderCollapsed.Value().ToVector();
            ExpandedFolderColor = ColorId.FolderExpanded.Value().ToVector();
            LineColor = ColorId.FolderLine.Value().ToVector();
            Dirty &= ~IManagedCache.DirtyFlags.Colors;
            OnColorChanged();
        }
    }

    protected override TemplateData ConvertNode(in IFileSystemNode node)
        => new((IFileSystemData<Template>)node);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Parent.TemplateChanged.Unsubscribe(OnTemplateChanged);
        Parent.ProfileChanged.Unsubscribe(OnProfileChanged);
    }

    public sealed class TemplateData(IFileSystemData<Template> node) : BaseFileSystemNodeCache<TemplateData>
    {
        public readonly IFileSystemData<Template> Node = node;
        public Vector4 Color;
        public StringU8 Name = new(node.Value.Name);
        public StringU8 Incognito = new(node.Value.Incognito);

        public override void Update(FileSystemCache cache, IFileSystemNode node)
        {
            var drawer = (TemplateFileSystemDrawer)cache.Parent;
            Color = drawer.ColorsService.GetTemplateColor(Node.Value).ToVector();
            Name = new StringU8(Node.Value.Name);
        }

        protected override void DrawInternal(FileSystemCache<TemplateData> cache, IFileSystemNode node)
        {
            var c = (TemplateFileSystemCache)cache;
            using var color = ImGuiColor.Text.Push(Color);
            using var id = Im.Id.Push(Node.Value.Index);
            var flags = node.Selected ? TreeNodeFlags.NoTreePushOnOpen | TreeNodeFlags.Selected : TreeNodeFlags.NoTreePushOnOpen;
            Im.Tree.Leaf(c.Parent.Configuration.UISettings.IncognitoMode ? Incognito : Name, flags);
        }
    }
}
