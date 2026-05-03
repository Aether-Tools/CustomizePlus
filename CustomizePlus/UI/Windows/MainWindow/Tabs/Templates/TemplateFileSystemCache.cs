using CustomizePlus.Profiles.Events;
using CustomizePlus.Templates.Data;
using CustomizePlus.Templates.Events;
using System;
using System.Collections.Generic;
using System.Text;

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

        /*if (arguments.Template?.Node is { } node && AllNodes.TryGetValue(node, out var cache))
            cache.Dirty = true;*/
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
            Color = ColorId.UnusedTemplate.Value().ToVector(); //todo //drawer.DesignColors.GetColor(Node.Value).ToVector();
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
