using CustomizePlus.Configuration.Data;
using CustomizePlus.Profiles;
using CustomizePlus.Profiles.Data;

namespace CustomizePlus.UI.Windows.MainWindow.Tabs.Profiles;

public sealed class MultiProfilePanel(
    ProfileFileSystem fileSystem,
    ProfileManager profileManager,
    PluginConfiguration configuration) : IUiService
{
    public void Draw()
    {
        if (fileSystem.Selection.OrderedNodes.Count is 0)
            return;

        var treeNodePos = Im.Cursor.Position;
        DrawDesignList();
        DrawCounts(treeNodePos);
        var offset = DrawMultiLock(out var width);
    }

    private void DrawCounts(Vector2 treeNodePos)
    {
        var startPos = Im.Cursor.Position;
        var numProfiles = fileSystem.Selection.DataNodes.Count;
        var numFolders = fileSystem.Selection.Folders.Count;
        Im.Cursor.Position = treeNodePos;
        ImEx.TextRightAligned((numProfiles, numFolders) switch
        {
            (0, 0) => StringU8.Empty, // should not happen
            ( > 0, 0) => $"{numProfiles} Profiles",
            (0, > 0) => $"{numFolders} Folders",
            _ => $"{numProfiles} Profiles, {numFolders} Folders",
        });
        Im.Cursor.Position = startPos;
    }

    private void ResetCounts()
    {
        _numProfilesLocked = 0;
    }

    private void CountLeaves(IFileSystemNode path)
    {
        if (path is not IFileSystemData<Profile> l)
            return;

        if (l.Value.IsWriteProtected)
            ++_numProfilesLocked;
    }

    private void DrawDesignList()
    {
        ResetCounts();
        using var tree = Im.Tree.Node("Currently Selected Objects"u8, TreeNodeFlags.DefaultOpen | TreeNodeFlags.NoTreePushOnOpen);
        Im.Separator();
        if (!tree)
            return;

        var sizeType = new Vector2(Im.Style.FrameHeight);
        var availableSizePercent = (Im.ContentRegion.Available.X - sizeType.X - 4 * Im.Style.CellPadding.X) / 100;
        var sizeTemplates = availableSizePercent * 35;
        var sizeFolders = availableSizePercent * 65;

        using (var table = Im.Table.Begin("profiles"u8, 3, TableFlags.RowBackground))
        {
            if (!table)
                return;

            table.SetupColumn("type"u8, TableColumnFlags.WidthFixed, sizeType.X);
            table.SetupColumn("profiles"u8, TableColumnFlags.WidthFixed, sizeTemplates);
            table.SetupColumn("path"u8, TableColumnFlags.WidthFixed, sizeFolders);

            foreach (var (index, node) in fileSystem.Selection.OrderedNodes.Index())
            {
                using var id = Im.Id.Push(index);
                var (icon, text) = node is IFileSystemData<Profile> l
                    ? (LunaStyle.RemoveFileIcon, configuration.UISettings.IncognitoMode ? l.Value.Incognito : l.Value.Name)
                    : (LunaStyle.RemoveFolderIcon, string.Empty);
                table.NextColumn();
                if (ImEx.Icon.Button(icon, "Remove from selection."u8, sizeType))
                    fileSystem.Selection.RemoveFromSelection(node);

                table.DrawFrameColumn(text);
                table.DrawFrameColumn(configuration.UISettings.IncognitoMode ? "Incognito mode" : node.FullPath);

                CountLeaves(node);
            }
        }

        Im.Separator();
    }

    private int _numProfilesLocked;

    private float DrawMultiLock(out Vector2 width)
    {
        ImEx.TextFrameAligned("Multi Lock:"u8);
        Im.Line.Same();
        width = new Vector2((Im.ContentRegion.Available.X - Im.Style.ItemInnerSpacing.X) / 2, 0);
        var offset = Im.Item.Size.X + Im.Style.WindowPadding.X;
        Im.Item.SetNextWidth(width.X);
        var diff = fileSystem.Selection.DataNodes.Count - _numProfilesLocked;
        if (ImEx.Button("Turn Write-Protected"u8, width, diff is 0
                ? $"All {fileSystem.Selection.DataNodes.Count} selected profiles are already write protected."
                : $"Write-protect all {fileSystem.Selection.DataNodes.Count} profiles. Changes {diff} templates.", diff is 0))
            foreach (var profile in fileSystem.Selection.DataNodes)
                profileManager.SetWriteProtection(profile.GetValue<Profile>()!, true);

        Im.Line.SameInner();
        if (ImEx.Button("Remove Write-Protection"u8, width, _numProfilesLocked is 0
                    ? $"None of the {fileSystem.Selection.DataNodes.Count} selected profiles are write-protected."
                    : $"Remove the write protection of the {fileSystem.Selection.DataNodes.Count} selected profiles. Changes {_numProfilesLocked} profiles.",
                _numProfilesLocked is 0))
            foreach (var profile in fileSystem.Selection.DataNodes)
                profileManager.SetWriteProtection(profile.GetValue<Profile>()!, false);
        Im.Separator();

        return offset;
    }
}
