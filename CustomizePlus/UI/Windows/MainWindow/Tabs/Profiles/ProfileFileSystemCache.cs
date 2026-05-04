using CustomizePlus.Profiles.Data;
using CustomizePlus.Profiles.Events;

namespace CustomizePlus.UI.Windows.MainWindow.Tabs.Profiles;


public sealed class ProfileFileSystemCache : FileSystemCache<ProfileFileSystemCache.ProfileData>
{
    public ProfileFileSystemCache(ProfileFileSystemDrawer parent)
    : base(parent)
    {
        parent.ProfileChanged.Subscribe(OnProfileChanged, ProfileChanged.Priority.ProfileFileSystemSelector);

        parent.ClientState.Login += OnLogin;
        parent.ClientState.Logout += OnLogout;
    }

    private void OnColorChanged()
    {
        foreach (var node in AllNodes.Values)
            node.Dirty = true;
    }

    private void OnProfileChanged(in ProfileChanged.Arguments arguments)
    {
        switch (arguments.Type)
        {
            case ProfileChanged.Type.Created:
            case ProfileChanged.Type.Deleted:
            case ProfileChanged.Type.Renamed:
            case ProfileChanged.Type.Toggled:
            case ProfileChanged.Type.AddedCharacter:
            case ProfileChanged.Type.RemovedCharacter:
            case ProfileChanged.Type.ReloadedAll:
                VisibleDirty = true;
                break;
        }

        if (arguments.Profile?.Node is { } node && AllNodes.TryGetValue(node, out var cache))
            cache.Dirty = true;
    }

    private void OnLogin()
    {
        VisibleDirty = true;
        OnColorChanged();
    }

    private void OnLogout(int type, int code)
    {
        VisibleDirty = true;
        OnColorChanged();
    }

    private new ProfileFileSystemDrawer Parent
        => (ProfileFileSystemDrawer)base.Parent;

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

    protected override ProfileData ConvertNode(in IFileSystemNode node)
        => new((IFileSystemData<Profile>)node);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Parent.ProfileChanged.Unsubscribe(OnProfileChanged);
    }

    public sealed class ProfileData(IFileSystemData<Profile> node) : BaseFileSystemNodeCache<ProfileData>
    {
        public readonly IFileSystemData<Profile> Node = node;
        public Vector4 Color;
        public StringU8 Name = new(node.Value.Name);
        public StringU8 Incognito = new(node.Value.Incognito);

        public override void Update(FileSystemCache cache, IFileSystemNode node)
        {
            var drawer = (ProfileFileSystemDrawer)cache.Parent;
            Color = drawer.ColorsService.GetProfileColor(Node.Value).ToVector();
            Name = new StringU8(Node.Value.Name);
        }

        protected override void DrawInternal(FileSystemCache<ProfileData> cache, IFileSystemNode node)
        {
            var c = (ProfileFileSystemCache)cache;
            using var color = ImGuiColor.Text.Push(Color);
            using var id = Im.Id.Push(Node.Value.Index);
            var flags = node.Selected ? TreeNodeFlags.NoTreePushOnOpen | TreeNodeFlags.Selected : TreeNodeFlags.NoTreePushOnOpen;
            Im.Tree.Leaf(c.Parent.Configuration.UISettings.IncognitoMode ? Incognito : Name, flags);
        }
    }
}
