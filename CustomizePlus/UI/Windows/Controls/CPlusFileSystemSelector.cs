using Dalamud.Interface;

namespace CustomizePlus.UI.Windows.Controls;

public abstract class CPlusFileSystemSelector<T, TState> : FileSystemDrawer<CPlusFileSystemSelector<T, TState>.NodeCache>, IDisposable
    where T : class, IFileSystemValue<T>
{
    private readonly SelectorFilter _filter;
    private readonly List<IFileSystemNode> _selectedPaths = [];
    private readonly StringU8 _id;
    private string? _popupToOpen;
    private bool _defaultExpansionApplied;
    private T? _lastSelection;

    protected CPlusFileSystemSelector(MessageService messager, BaseFileSystem fileSystem, string id)
        : this(messager, fileSystem, id, new SelectorFilter())
    { }

    private CPlusFileSystemSelector(MessageService messager, BaseFileSystem fileSystem, string id, SelectorFilter filter)
        : base(messager, fileSystem, filter)
    {
        _id = new StringU8(id);
        _filter = filter;
        _filter.Attach(this);
        FileSystem.Selection.Changed += SyncSelection;
        SyncSelectionState();
    }

    public override ReadOnlySpan<byte> Id
        => _id;

    protected IFileSystemData<T>? SelectedNode { get; private set; }

    protected const TreeNodeFlags LeafFlags = TreeNodeFlags.Leaf | TreeNodeFlags.NoTreePushOnOpen;

    public T? Selected
        => SelectedNode?.Value;

    public IReadOnlyList<IFileSystemNode> SelectedPaths
        => _selectedPaths;

    public delegate void SelectionChangedDelegate(T? oldSelection, T? newSelection, in TState state);

    public event SelectionChangedDelegate? SelectionChanged;

    protected string FilterTooltip { get; set; } = string.Empty;

    protected string FilterValue
        => _filter.Text;

    protected abstract uint ExpandedFolderColorValue { get; }
    protected abstract uint CollapsedFolderColorValue { get; }
    protected abstract bool FoldersDefaultOpen { get; }

    public override Vector4 ExpandedFolderColor
        => ((Rgba32)ExpandedFolderColorValue).ToVector();

    public override Vector4 CollapsedFolderColor
        => ((Rgba32)CollapsedFolderColorValue).ToVector();

    public override Rgba32 FolderLineColor
        => ColorId.FolderLine.Value();

    public virtual void Dispose()
    {
        RefreshRequested = null;
        FileSystem.Selection.Changed -= SyncSelection;
    }

    private event Action? RefreshRequested;

    public override void Draw()
    {
        ApplyDefaultExpansion();
        base.Draw();
    }

    protected void AddButton(FontAwesomeIcon icon, Func<string> tooltip, Func<bool> disabled, Action action, int priority)
        => Footer.Buttons.AddButton(new SelectorActionButton(icon.Icon(), tooltip, disabled, action, priority), 10000 - priority);

    public void DrawSelectorPopups()
    {
        if (_popupToOpen is not null)
        {
            Im.Popup.Open(_popupToOpen);
            _popupToOpen = null;
        }

        DrawPopups();
    }

    protected void OpenSelectorPopup(string popup)
        => _popupToOpen = popup;

    protected void SetFilterDirty()
    {
        _filter.Refresh();
        RefreshRequested?.Invoke();
    }

    protected virtual bool ChangeFilter(string filterValue)
        => true;

    protected virtual void DrawPopups()
    { }

    protected virtual bool CanChangeSelection(IFileSystemData<T>? node)
        => true;

    protected virtual void Select(IFileSystemData<T>? node, bool clear, in TState storage = default!)
    {
        if (!CanChangeSelection(node))
            return;

        if (node is not null)
            FileSystem.Selection.Select(node, true);
        else if (clear)
            FileSystem.Selection.UnselectAll();
    }

    public bool SelectByValue(T value)
    {
        if (value.Node is not IFileSystemData<T> node)
            return false;

        Select(node, true);
        return true;
    }

    public void RemovePathFromMultiSelection(IFileSystemNode path)
        => FileSystem.Selection.RemoveFromSelection(path);

    protected abstract void DrawLeafName(IFileSystemData<T> node, in TState state, bool selected);

    protected abstract bool ApplyFiltersAndState(IFileSystemNode node, out TState state);

    protected static void DrawLeafTreeNode(IFileSystemData<T> node, TreeNodeFlags flags, string? label)
    {
        using var id = Im.Id.Push($"leaf:{node.Value.Identifier}");
        Im.Tree.Leaf(CleanLabel(label, CleanLabel(node.Name.ToString(), "Item")), flags);
    }

    protected void DeleteSelection(DoubleModifier modifier, string singular, Action<T> delete)
    {
        if (Selected is null || !modifier.IsActive())
            return;

        delete(Selected);
    }

    protected string DeleteSelectionTooltip(DoubleModifier modifier, string singular)
        => Selected is null
            ? $"No {singular} selected."
            : $"Hold {modifier} to delete the selected {singular}.";

    protected override FileSystemCache<NodeCache> CreateCache()
        => new SelectorCache(this);

    private bool WouldShow(IFileSystemNode node, out TState state)
        => !ApplyFiltersAndState(node, out state);

    private bool TryChangeFilter(string filterValue)
        => ChangeFilter(filterValue);

    private bool CanSelect(IFileSystemNode node)
        => node is not IFileSystemData<T> data || CanChangeSelection(data);

    private void ApplyDefaultExpansion()
    {
        if (_defaultExpansionApplied || !FoldersDefaultOpen)
            return;

        FileSystem.ExpandAllDescendants(FileSystem.Root, false);
        _defaultExpansionApplied = true;
    }

    private void SyncSelection()
    {
        var oldSelection = _lastSelection;
        SyncSelectionState();

        if (!ReferenceEquals(oldSelection, _lastSelection))
            InvokeSelectionChanged(oldSelection);
    }

    private void SyncSelectionState()
    {
        SelectedNode = FileSystem.Selection.Selection as IFileSystemData<T>;
        _lastSelection = Selected;

        _selectedPaths.Clear();
        _selectedPaths.AddRange(FileSystem.Selection.OrderedNodes);
    }

    private void InvokeSelectionChanged(T? oldSelection)
    {
        var state = default(TState);
        if (SelectedNode is not null)
            ApplyFiltersAndState(SelectedNode, out state);

        SelectionChanged?.Invoke(oldSelection, Selected, state);
    }

    private static string CleanLabel(string? label, string fallback)
        => string.IsNullOrEmpty(label) ? fallback : label;

    public sealed class NodeCache(IFileSystemData<T> node) : BaseFileSystemNodeCache<NodeCache>
    {
        public readonly IFileSystemData<T> Node = node;
        private TState _state;

        public override void Update(FileSystemCache cache, IFileSystemNode node)
        {
            var selector = (CPlusFileSystemSelector<T, TState>)cache.Parent;
            selector.WouldShow(Node, out _state);
        }

        protected override void DrawInternal(FileSystemCache<NodeCache> cache, IFileSystemNode node)
        {
            var selector = (CPlusFileSystemSelector<T, TState>)cache.Parent;
            selector.DrawLeafName(Node, _state, node.Selected);
        }
    }

    private sealed class SelectorCache : FileSystemCache<NodeCache>
    {
        private new CPlusFileSystemSelector<T, TState> Parent
            => (CPlusFileSystemSelector<T, TState>)base.Parent;

        public SelectorCache(CPlusFileSystemSelector<T, TState> parent)
            : base(parent)
        {
            Parent.RefreshRequested += Refresh;
        }

        public void Refresh()
        {
            VisibleDirty = true;
            foreach (var node in AllNodes.Values)
                node.Dirty = true;
        }

        public override void HandleSelection(IFileSystemNode node, bool selectFolders)
        {
            if (!Im.Mouse.IsReleased(MouseButton.Left) || Im.DragDrop.PeekPayload().Valid || !Im.Item.Hovered())
            {
                base.HandleSelection(node, selectFolders);
                return;
            }

            if (!Parent.CanSelect(node))
                return;

            base.HandleSelection(node, selectFolders);
        }

        protected override bool UpdateTreeList()
        {
            foreach (var (node, cache) in AllNodes)
            {
                if (!cache.Dirty)
                    continue;

                cache.Update(this, node);
                cache.Dirty = false;
            }

            return base.UpdateTreeList();
        }

        protected override IFileSystemNodeCache ConvertNode(in IFileSystemNode node)
            => new NodeCache((IFileSystemData<T>)node);

        protected override void Dispose(bool disposing)
        {
            Parent.RefreshRequested -= Refresh;
            base.Dispose(disposing);
        }
    }

    private sealed class SelectorFilter : IFileSystemFilter<NodeCache>
    {
        private CPlusFileSystemSelector<T, TState>? _selector;

        public string Text { get; private set; } = string.Empty;

        public event Action? FilterChanged;

        public bool IsVisible
            => true;

        public bool IsEmpty
            => Text.Length is 0;

        public bool WouldBeVisible(in NodeCache node, int globalIndex)
            => _selector?.WouldShow(node.Node, out _) ?? true;

        public bool WouldBeVisible(in FileSystemFolderCache folder)
            => Text.Length is 0 || folder.FullPath.Contains(Text, StringComparison.OrdinalIgnoreCase);

        public bool WouldBeVisible(in IFileSystemNodeCache node)
        {
            if (node is FileSystemFolderCache folder)
                return WouldBeVisible(folder);

            if (node is not NodeCache dataNode)
                return IsEmpty;

            return _selector?.WouldShow(dataNode.Node, out _) ?? true;
        }

        public bool DrawFilter(ReadOnlySpan<byte> label, Vector2 availableRegion)
        {
            Im.Item.SetNextWidth(availableRegion.X);
            var filter = Text;
            if (!Im.Input.Text("##Filter"u8, ref filter, label))
                return false;

            if (filter == Text || _selector is null || !_selector.TryChangeFilter(filter))
                return false;

            Text = filter;
            Refresh();
            return true;
        }

        public bool Clear()
        {
            if (Text.Length is 0)
                return false;

            Text = string.Empty;
            _selector?.TryChangeFilter(Text);
            Refresh();
            return true;
        }

        public void Attach(CPlusFileSystemSelector<T, TState> selector)
            => _selector = selector;

        public void Refresh()
            => FilterChanged?.Invoke();
    }

    private sealed class SelectorActionButton(AwesomeIcon icon, Func<string> tooltip, Func<bool> disabled, Action action, int id) : BaseIconButton<AwesomeIcon>
    {
        private readonly StringU8 _label = new($"##CPlus{id}");

        public override ReadOnlySpan<byte> Label
            => _label;

        public override AwesomeIcon Icon
            => icon;

        public override bool Enabled
            => !disabled();

        public override bool HasTooltip
            => true;

        public override void DrawTooltip()
            => Im.Text(tooltip());

        public override void OnClick()
            => action();
    }
}
